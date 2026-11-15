using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Vouchers;

namespace Treasury.Cashflow.Infrastructure.Postgres;

// Written against db/postgres/030_cashflow_vouchers.sql.

public sealed class PostgresVoucherTemplateRepository : IVoucherTemplateRepository
{
    public async Task<VoucherTemplate> GetAsync(CancellationToken cancellationToken)
    {
        var accounts = new Dictionary<FixedAccount, string>();
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT account_key, ledger_account_code FROM cashflow.voucher_account");
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            accounts[Enum.Parse<FixedAccount>(r.GetString(0))] = r.GetString(1);
        }

        return new VoucherTemplate(accounts);
    }

    public async Task SaveAsync(VoucherTemplate voucherTemplate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(voucherTemplate);
        var tx = PostgresUnitOfWork.Current;
        await using (var del = tx.Command("DELETE FROM cashflow.voucher_account WHERE NOT (account_key = ANY(@keys))"))
        {
            del.Parameters.AddWithValue("keys", voucherTemplate.Accounts.Keys.Select(k => k.ToString()).ToArray());
            await del.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var (key, code) in voucherTemplate.Accounts)
        {
            await using var up = tx.Command(
                "INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES (@key, @code) " +
                "ON CONFLICT (account_key) DO UPDATE SET ledger_account_code = excluded.ledger_account_code, updated_at = now()");
            up.Parameters.AddWithValue("key", key.ToString());
            up.Parameters.AddWithValue("code", code);
            await up.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class PostgresConfirmedVoucherRepository : IConfirmedVoucherRepository
{
    private const string Columns = "id, source, source_id, step, operation, voucher_date, lines, confirmed_by, confirmed_at, version";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<ConfirmedVoucher?> FindAsync(VoucherSource source, Guid sourceId, int stepNumber, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM cashflow.voucher WHERE source = @source AND source_id = @id AND step = @step");
        cmd.Parameters.AddWithValue("source", source.ToString());
        cmd.Parameters.AddWithValue("id", sourceId);
        cmd.Parameters.AddWithValue("step", stepNumber);
        var found = await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
        return found.Count > 0 ? found[0] : null;
    }

    public async Task<IReadOnlyList<ConfirmedVoucher>> ListBySourceAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM cashflow.voucher WHERE source_id = @id ORDER BY step");
        cmd.Parameters.AddWithValue("id", sourceId);
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(ConfirmedVoucher voucher, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            $"INSERT INTO cashflow.voucher (fiscal_year, {Columns}, total_rial) VALUES " +
            "(@year, @id, @source, @source_id, @step, @operation, @date, @lines, @by, @at, 1, @total)"))
        {
            cmd.Parameters.AddWithValue("year", voucher.FiscalYear);
            cmd.Parameters.AddWithValue("id", voucher.Id);
            cmd.Parameters.AddWithValue("source", voucher.Source.ToString());
            cmd.Parameters.AddWithValue("source_id", voucher.SourceId);
            cmd.Parameters.AddWithValue("step", voucher.Step);
            cmd.Parameters.AddWithValue("operation", voucher.Voucher.Operation);
            cmd.Parameters.AddWithValue("date", voucher.Voucher.Date.Value);
            cmd.Parameters.Add(new NpgsqlParameter("lines", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(voucher.Voucher.Lines, Json) });
            cmd.Parameters.AddWithValue("by", voucher.ConfirmedBy);
            cmd.Parameters.AddWithValue("at", voucher.ConfirmedAt);
            cmd.Parameters.AddWithValue("total", voucher.Voucher.TotalDebit);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, voucher.PendingEvents, cancellationToken).ConfigureAwait(false);
        voucher.MarkSaved(1);
    }

    private static async Task<IReadOnlyList<ConfirmedVoucher>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<ConfirmedVoucher>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            var lines = JsonSerializer.Deserialize<List<VoucherLine>>(r.GetString(6), Json) ?? [];
            var voucher = new Voucher(r.GetString(4), new FiscalDate(r.GetFieldValue<DateOnly>(5)), lines);
            list.Add(ConfirmedVoucher.Restore(r.GetGuid(0), Enum.Parse<VoucherSource>(r.GetString(1)), r.GetGuid(2), r.GetInt32(3), voucher,
                r.GetString(7), r.GetFieldValue<DateTimeOffset>(8), r.GetInt64(9)));
        }

        return list;
    }
}
