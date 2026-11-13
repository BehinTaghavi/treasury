using Treasury.BuildingBlocks.Domain;
using Npgsql;
using NpgsqlTypes;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.PettyCash;
using Treasury.BuildingBlocks.Application;

namespace Treasury.Accounts.Infrastructure.Postgres;

// Written against db/postgres/013_accounts_petty_cash.sql.

public sealed class PostgresPettyCashRepository : IPettyCashRepository
{
    private const string Columns = "id, holder_detail, ledger_account, amount, settle_limit, pay_ceiling, purpose_groups, recovery, is_active, version, created_on, keeping::text";

    public async Task<PettyCashFund?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.petty_cash WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        var list = await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
        return list.Count == 0 ? null : list[0];
    }

    public async Task<bool> HolderTakenAsync(string holderDetailCode, Guid? exceptId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT EXISTS (SELECT 1 FROM accounts.petty_cash WHERE holder_detail = @holder AND id IS DISTINCT FROM @except)");
        cmd.Parameters.AddWithValue("holder", holderDetailCode);
        cmd.Parameters.Add(new NpgsqlParameter("except", NpgsqlDbType.Uuid) { Value = (object?)exceptId ?? DBNull.Value });
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(PettyCashFund fund, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fund);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "INSERT INTO accounts.petty_cash (id, holder_detail, ledger_account, amount, settle_limit, pay_ceiling, purpose_groups, recovery, is_active, created_on, keeping) " +
            "VALUES (@id, @holder, @ledger, @amount, @limit, @ceiling, @groups, @recovery, @active, @created_on, @keeping::jsonb)"))
        {
            Parameters(cmd, fund);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, fund.PendingEvents, cancellationToken).ConfigureAwait(false);
        fund.MarkSaved(1);
    }

    public async Task SaveAsync(PettyCashFund fund, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fund);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE accounts.petty_cash SET holder_detail = @holder, ledger_account = @ledger, amount = @amount, settle_limit = @limit, pay_ceiling = @ceiling, " +
            "purpose_groups = @groups, recovery = @recovery, is_active = @active, created_on = @created_on, keeping = @keeping::jsonb, version = version + 1, updated_at = now() WHERE id = @id AND version = @expected"))
        {
            Parameters(cmd, fund);
            cmd.Parameters.AddWithValue("expected", fund.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Petty cash {fund.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, fund.PendingEvents, cancellationToken).ConfigureAwait(false);
        fund.MarkSaved(fund.Version + 1);
    }

    public async Task<IReadOnlyList<PettyCashFund>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.petty_cash WHERE @all OR is_active");
        cmd.Parameters.AddWithValue("all", includeInactive);
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    private static void Parameters(NpgsqlCommand cmd, PettyCashFund f)
    {
        cmd.Parameters.AddWithValue("id", f.Id);
        cmd.Parameters.AddWithValue("holder", f.Data.HolderDetailCode);
        cmd.Parameters.AddWithValue("ledger", f.Data.LedgerAccountCode);
        cmd.Parameters.AddWithValue("amount", f.Data.Amount);
        cmd.Parameters.AddWithValue("limit", f.Data.SettleLimit);
        cmd.Parameters.AddWithValue("ceiling", f.Data.PayCeiling);
        cmd.Parameters.Add(new NpgsqlParameter("groups", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = f.Data.AllowedPurposeGroupIds.ToArray() });
        cmd.Parameters.AddWithValue("recovery", f.Data.Recovery.ToString());
        cmd.Parameters.Add(new NpgsqlParameter("created_on", NpgsqlDbType.Date) { Value = f.Data.CreatedOn is { } d ? d.Value : DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("keeping", NpgsqlDbType.Text) { Value = f.Data.Keeping is { } k ? System.Text.Json.JsonSerializer.Serialize(k) : DBNull.Value });
        cmd.Parameters.AddWithValue("active", f.IsActive);
    }

    private static async Task<List<PettyCashFund>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<PettyCashFund>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            var data = new PettyCashData(r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetDecimal(4), r.GetDecimal(5),
                r.GetFieldValue<Guid[]>(6), Enum.Parse<PettyCashRecovery>(r.GetString(7)),
                r.IsDBNull(10) ? null : new FiscalDate(r.GetFieldValue<DateOnly>(10)),
                r.IsDBNull(11) ? null : System.Text.Json.JsonSerializer.Deserialize<PettyCashKeeping>(r.GetString(11)));
            list.Add(PettyCashFund.Restore(r.GetGuid(0), data, r.GetBoolean(8), r.GetInt64(9)));
        }

        return list;
    }
}
