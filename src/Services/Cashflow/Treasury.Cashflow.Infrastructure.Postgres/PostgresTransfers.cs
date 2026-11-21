using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Transfers;

namespace Treasury.Cashflow.Infrastructure.Postgres;

// Written against db/postgres/046_cashflow_bank_transfers.sql.

public sealed class PostgresBankTransferRepository : IBankTransferRepository
{
    private const string Columns =
        "fiscal_year, id, number, transfer_date, method, channel, source_account_id, source_account_number, source_bank_id, source_bank_name, " +
        "destination_account_id, destination_account_number, destination_bank_id, destination_bank_name, amount, currency, rate_to_rial, amount_rial, " +
        "tracking_number, description, cheque, state, recorded_by, deleted_by, deleted_at, version, fee_rial, payment_request_id, payment_request_number, payment_key, " +
        "fee_detail_code";

    private static readonly JsonSerializerOptions ChequeJson = new(JsonSerializerDefaults.Web);

    public async Task<BankTransfer?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM cashflow.bank_transfer WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        var list = await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
        return list.Count == 0 ? null : list[0];
    }

    public async Task<int> NextNumberAsync(int fiscalYear, CancellationToken cancellationToken)
    {
        // The row stays locked until the transaction ends: a second transfer of the year waits and gets the next number.
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "INSERT INTO cashflow.bank_transfer_number (fiscal_year, last_number) VALUES (@year, 1) " +
            "ON CONFLICT (fiscal_year) DO UPDATE SET last_number = cashflow.bank_transfer_number.last_number + 1 RETURNING last_number");
        cmd.Parameters.AddWithValue("year", fiscalYear);
        return (int)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(BankTransfer transfer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            $"INSERT INTO cashflow.bank_transfer ({Columns}) VALUES (@year, @id, @number, @date, @method, @channel, @source, @source_number, @source_bank, @source_bank_name, " +
            "@destination, @destination_number, @destination_bank, @destination_bank_name, @amount, @currency, @rate, @rial, @tracking, @description, @cheque, @state, " +
            "@by, @deleted_by, @deleted_at, 1, @fee, @request, @request_number, @payment_key, @fee_detail)"))
        {
            Bind(cmd, BankTransferSnapshot.From(transfer, 1));
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, transfer.PendingEvents, cancellationToken).ConfigureAwait(false);
        transfer.MarkSaved(1);
    }

    public async Task SaveAsync(BankTransfer transfer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        var tx = PostgresUnitOfWork.Current;

        // A correction changes everything but the number and the fiscal year; a deletion or a step of the cheque the state and the cheque.
        await using (var cmd = tx.Command(
            "UPDATE cashflow.bank_transfer SET transfer_date = @date, method = @method, channel = @channel, source_account_id = @source, " +
            "source_account_number = @source_number, source_bank_id = @source_bank, source_bank_name = @source_bank_name, destination_account_id = @destination, " +
            "destination_account_number = @destination_number, destination_bank_id = @destination_bank, destination_bank_name = @destination_bank_name, " +
            "amount = @amount, currency = @currency, rate_to_rial = @rate, amount_rial = @rial, tracking_number = @tracking, description = @description, " +
            "cheque = @cheque, state = @state, deleted_by = @deleted_by, deleted_at = @deleted_at, fee_rial = @fee, payment_request_id = @request, " +
            "payment_request_number = @request_number, payment_key = @payment_key, fee_detail_code = @fee_detail, version = version + 1, updated_at = now() " +
            "WHERE fiscal_year = @year AND id = @id AND number = @number AND version = @expected"))
        {
            Bind(cmd, BankTransferSnapshot.From(transfer, transfer.Version + 1));
            cmd.Parameters.AddWithValue("expected", transfer.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Transfer {transfer.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, transfer.PendingEvents, cancellationToken).ConfigureAwait(false);
        transfer.MarkSaved(transfer.Version + 1);
    }

    public async Task<IReadOnlyList<BankTransfer>> ListAsync(int fiscalYear, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT {Columns} FROM cashflow.bank_transfer WHERE fiscal_year = @year ORDER BY transfer_date DESC, number DESC");
        cmd.Parameters.AddWithValue("year", fiscalYear);
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TransferDayUsage> DayUsageAsync(Guid sourceAccountId, FiscalDate day, TransferMethod method, TransferChannel channel, Guid? exceptId,
        CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT coalesce(sum(amount_rial) FILTER (WHERE channel = @channel), 0), count(*) FROM cashflow.bank_transfer " +
            "WHERE source_account_id = @source AND transfer_date = @date AND method = @method AND state <> 'Deleted' AND (@except::uuid IS NULL OR id <> @except)");
        cmd.Parameters.AddWithValue("source", sourceAccountId);
        cmd.Parameters.AddWithValue("date", day.Value);
        cmd.Parameters.AddWithValue("method", method.ToString());
        cmd.Parameters.AddWithValue("channel", channel.ToString());
        cmd.Parameters.Add(new NpgsqlParameter("except", NpgsqlDbType.Uuid) { Value = (object?)exceptId ?? DBNull.Value });
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await r.ReadAsync(cancellationToken).ConfigureAwait(false);
        return new TransferDayUsage(r.GetDecimal(0), (int)r.GetInt64(1));
    }

    private static void Bind(NpgsqlCommand cmd, BankTransferSnapshot s)
    {
        object Db(object? value) => value ?? DBNull.Value;
        cmd.Parameters.AddWithValue("year", s.FiscalYear);
        cmd.Parameters.AddWithValue("id", s.Id);
        cmd.Parameters.AddWithValue("number", s.Number);
        cmd.Parameters.AddWithValue("date", s.Date);
        cmd.Parameters.AddWithValue("method", s.Method.ToString());
        cmd.Parameters.Add(new NpgsqlParameter("channel", NpgsqlDbType.Text) { Value = Db(s.Channel?.ToString()) });
        cmd.Parameters.AddWithValue("source", s.SourceAccountId);
        cmd.Parameters.AddWithValue("source_number", s.SourceAccountNumber);
        cmd.Parameters.AddWithValue("source_bank", s.SourceBankId);
        cmd.Parameters.AddWithValue("source_bank_name", s.SourceBankName);
        cmd.Parameters.AddWithValue("destination", s.DestinationAccountId);
        cmd.Parameters.AddWithValue("destination_number", s.DestinationAccountNumber);
        cmd.Parameters.AddWithValue("destination_bank", s.DestinationBankId);
        cmd.Parameters.AddWithValue("destination_bank_name", s.DestinationBankName);
        cmd.Parameters.AddWithValue("amount", s.Amount);
        cmd.Parameters.AddWithValue("currency", s.Currency);
        cmd.Parameters.Add(new NpgsqlParameter("rate", NpgsqlDbType.Numeric) { Value = Db(s.RateToRial) });
        cmd.Parameters.AddWithValue("rial", s.AmountRial);
        cmd.Parameters.Add(new NpgsqlParameter("tracking", NpgsqlDbType.Text) { Value = Db(s.TrackingNumber) });
        cmd.Parameters.Add(new NpgsqlParameter("description", NpgsqlDbType.Text) { Value = Db(s.Description) });
        cmd.Parameters.Add(new NpgsqlParameter("cheque", NpgsqlDbType.Jsonb) { Value = s.Cheque is null ? DBNull.Value : JsonSerializer.Serialize(s.Cheque, ChequeJson) });
        cmd.Parameters.AddWithValue("state", s.State.ToString());
        cmd.Parameters.AddWithValue("by", s.RecordedBy);
        cmd.Parameters.Add(new NpgsqlParameter("deleted_by", NpgsqlDbType.Text) { Value = Db(s.DeletedBy) });
        cmd.Parameters.Add(new NpgsqlParameter("deleted_at", NpgsqlDbType.TimestampTz) { Value = Db(s.DeletedAt?.ToUniversalTime()) });
        cmd.Parameters.Add(new NpgsqlParameter("fee", NpgsqlDbType.Numeric) { Value = Db(s.FeeRial) });
        cmd.Parameters.Add(new NpgsqlParameter("request", NpgsqlDbType.Uuid) { Value = Db(s.RequestId) });
        cmd.Parameters.Add(new NpgsqlParameter("request_number", NpgsqlDbType.Integer) { Value = Db(s.RequestNumber) });
        cmd.Parameters.Add(new NpgsqlParameter("payment_key", NpgsqlDbType.Text) { Value = Db(s.RequestPaymentKey) });
        cmd.Parameters.Add(new NpgsqlParameter("fee_detail", NpgsqlDbType.Text) { Value = Db(s.FeeDetailCode) });
    }

    private static async Task<IReadOnlyList<BankTransfer>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<BankTransfer>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            string? Text(int i) => r.IsDBNull(i) ? null : r.GetString(i);
            list.Add(new BankTransferSnapshot(
                Id: r.GetGuid(1),
                FiscalYear: r.GetInt32(0),
                Number: r.GetInt32(2),
                Date: r.GetFieldValue<DateOnly>(3),
                Method: Enum.Parse<TransferMethod>(r.GetString(4)),
                Channel: Text(5) is { } c ? Enum.Parse<TransferChannel>(c) : null,
                SourceAccountId: r.GetGuid(6),
                SourceAccountNumber: r.GetString(7),
                SourceBankId: r.GetGuid(8),
                SourceBankName: r.GetString(9),
                DestinationAccountId: r.GetGuid(10),
                DestinationAccountNumber: r.GetString(11),
                DestinationBankId: r.GetGuid(12),
                DestinationBankName: r.GetString(13),
                Amount: r.GetDecimal(14),
                Currency: r.GetString(15).Trim(),
                RateToRial: r.IsDBNull(16) ? null : r.GetDecimal(16),
                AmountRial: r.GetDecimal(17),
                TrackingNumber: Text(18),
                Description: Text(19),
                Cheque: r.IsDBNull(20) ? null : JsonSerializer.Deserialize<TransferChequeSnapshot>(r.GetString(20), ChequeJson),
                State: Enum.Parse<TransferState>(r.GetString(21)),
                RecordedBy: r.GetString(22),
                DeletedBy: Text(23),
                DeletedAt: r.IsDBNull(24) ? null : new DateTimeOffset(r.GetFieldValue<DateTime>(24), TimeSpan.Zero),
                Version: r.GetInt64(25),
                FeeRial: r.IsDBNull(26) ? null : r.GetDecimal(26),
                RequestId: r.IsDBNull(27) ? null : r.GetGuid(27),
                RequestNumber: r.IsDBNull(28) ? null : r.GetInt32(28),
                RequestPaymentKey: Text(29),
                FeeDetailCode: Text(30)).ToDomain());
        }

        return list;
    }
}

/// <summary>The company's limits of the electronic ways of transfer; bank_key '' is the general row of a way.</summary>
public sealed class PostgresTransferLimitRepository : ITransferLimitRepository
{
    public async Task<TransferLimits> GetAsync(CancellationToken cancellationToken)
    {
        var rows = new List<TransferLimit>();
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT method, bank_key, min_per_order, max_per_order_in_person, max_per_order_remote, max_per_day_in_person, max_per_day_remote, max_count_per_day " +
            "FROM cashflow.transfer_limit");
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            decimal? Amount(int i) => r.IsDBNull(i) ? null : r.GetDecimal(i);
            var bank = r.GetString(1);
            rows.Add(new TransferLimit(Enum.Parse<TransferMethod>(r.GetString(0)), bank.Length == 0 ? null : Guid.Parse(bank), Amount(2), Amount(3), Amount(4), Amount(5),
                Amount(6), r.IsDBNull(7) ? null : r.GetInt32(7)));
        }

        return new TransferLimits(rows);
    }

    public async Task SaveAsync(TransferLimits limits, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(limits);
        var tx = PostgresUnitOfWork.Current;

        // Only a bank's own row is ever removed; the general rows stay (an emptied limit is a row of NULLs, see 046).
        await using (var del = tx.Command("DELETE FROM cashflow.transfer_limit WHERE bank_key <> '' AND NOT (method || '|' || bank_key = ANY(@keys))"))
        {
            del.Parameters.AddWithValue("keys", limits.Rows.Where(l => l.BankId is not null).Select(l => $"{l.Method}|{Key(l)}").ToArray());
            await del.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var l in limits.Rows)
        {
            await using var up = tx.Command(
                "INSERT INTO cashflow.transfer_limit (method, bank_key, min_per_order, max_per_order_in_person, max_per_order_remote, max_per_day_in_person, " +
                "max_per_day_remote, max_count_per_day) VALUES (@method, @bank, @min, @order_in, @order_remote, @day_in, @day_remote, @count) " +
                "ON CONFLICT (method, bank_key) DO UPDATE SET min_per_order = excluded.min_per_order, max_per_order_in_person = excluded.max_per_order_in_person, " +
                "max_per_order_remote = excluded.max_per_order_remote, max_per_day_in_person = excluded.max_per_day_in_person, " +
                "max_per_day_remote = excluded.max_per_day_remote, max_count_per_day = excluded.max_count_per_day, updated_at = now()");
            up.Parameters.AddWithValue("method", l.Method.ToString());
            up.Parameters.AddWithValue("bank", Key(l));
            foreach (var (name, value) in new (string, decimal?)[]
            {
                ("min", l.MinPerOrder), ("order_in", l.MaxPerOrderInPerson), ("order_remote", l.MaxPerOrderRemote), ("day_in", l.MaxPerDayInPerson),
                ("day_remote", l.MaxPerDayRemote),
            })
            {
                up.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Numeric) { Value = (object?)value ?? DBNull.Value });
            }

            up.Parameters.Add(new NpgsqlParameter("count", NpgsqlDbType.Integer) { Value = (object?)l.MaxCountPerDay ?? DBNull.Value });
            await up.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static string Key(TransferLimit l) => l.BankId?.ToString("D") ?? string.Empty;
}
