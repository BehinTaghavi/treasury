using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Receipts;

namespace Treasury.Cashflow.Infrastructure.Postgres;

// Written against db/postgres/025_cashflow_receipts.sql, 036_cashflow_deposit_kinds.sql and 039_cashflow_note_receipts.sql.

public sealed class PostgresReceiptRepository : IReceiptRepository
{
    private const string Columns =
        "fiscal_year, id, kind, number, receipt_date, payer_id, payer_name, payer_detail_code, purpose_group_id, purpose_group_name, purpose, " +
        "reference1, reference2, department_id, sales_proceeds, note, recorded_by, account_kind, account_id, deposit_method, tracking_number, " +
        "amount, currency, rate_to_rial, instrument_ids, total_rial, state, cancelled_on, cancel_reason, version, replaced_instrument_id, payer_sheba, note_terms";

    /// <summary>Enums as their names, so the stored JSON reads the same as the other columns.</summary>
    private static readonly System.Text.Json.JsonSerializerOptions TermsJson = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public async Task<Receipt?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM cashflow.receipt WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        var list = await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
        return list.Count == 0 ? null : list[0];
    }

    public async Task<int> NextNumberAsync(int fiscalYear, ReceiptKind kind, CancellationToken cancellationToken)
    {
        // The row stays locked until the transaction ends: a second receipt of the same kind waits and gets the next number.
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "INSERT INTO cashflow.receipt_number (fiscal_year, kind, last_number) VALUES (@year, @kind, 1) " +
            "ON CONFLICT (fiscal_year, kind) DO UPDATE SET last_number = cashflow.receipt_number.last_number + 1 RETURNING last_number");
        cmd.Parameters.AddWithValue("year", fiscalYear);
        cmd.Parameters.AddWithValue("kind", kind.ToString());
        return (int)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var tx = PostgresUnitOfWork.Current;
        var s = ReceiptSnapshot.From(receipt, 1);
        await using (var cmd = tx.Command(
            $"INSERT INTO cashflow.receipt ({Columns}) VALUES (@year, @id, @kind, @number, @date, @payer, @payer_name, @payer_code, @group, @group_name, @purpose, " +
            "@ref1, @ref2, @department, @sales, @note, @by, @account_kind, @account, @method, @tracking, @amount, @currency, @rate, @instruments, @total, @state, " +
            "@cancelled_on, @cancel_reason, 1, @replaced, @sheba, @note_terms)"))
        {
            Bind(cmd, s);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, receipt.PendingEvents, cancellationToken).ConfigureAwait(false);
        receipt.MarkSaved(1);
    }

    public async Task SaveAsync(Receipt receipt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var tx = PostgresUnitOfWork.Current;
        var s = ReceiptSnapshot.From(receipt, receipt.Version + 1);
        await using (var cmd = tx.Command(
            "UPDATE cashflow.receipt SET state = @state, cancelled_on = @cancelled_on, cancel_reason = @cancel_reason, version = version + 1, updated_at = now() " +
            "WHERE fiscal_year = @year AND id = @id AND version = @expected"))
        {
            cmd.Parameters.AddWithValue("state", s.State.ToString());
            cmd.Parameters.Add(new NpgsqlParameter("cancelled_on", NpgsqlDbType.Date) { Value = (object?)s.CancelledOn ?? DBNull.Value });
            cmd.Parameters.AddWithValue("cancel_reason", (object?)s.CancelReason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("year", s.FiscalYear);
            cmd.Parameters.AddWithValue("id", s.Id);
            cmd.Parameters.AddWithValue("expected", receipt.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Receipt {s.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, receipt.PendingEvents, cancellationToken).ConfigureAwait(false);
        receipt.MarkSaved(receipt.Version + 1);
    }

    public async Task<IReadOnlyList<Receipt>> ListAsync(int fiscalYear, ReceiptKind? kind, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT {Columns} FROM cashflow.receipt WHERE fiscal_year = @year AND (@kind::text IS NULL OR kind = @kind) ORDER BY receipt_date DESC, number DESC");
        cmd.Parameters.AddWithValue("year", fiscalYear);
        cmd.Parameters.Add(new NpgsqlParameter("kind", NpgsqlDbType.Text) { Value = (object?)kind?.ToString() ?? DBNull.Value });
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    private static void Bind(NpgsqlCommand cmd, ReceiptSnapshot s)
    {
        object Db(object? value) => value ?? DBNull.Value;
        cmd.Parameters.AddWithValue("year", s.FiscalYear);
        cmd.Parameters.AddWithValue("id", s.Id);
        cmd.Parameters.AddWithValue("kind", s.Kind.ToString());
        cmd.Parameters.AddWithValue("number", s.Number);
        cmd.Parameters.AddWithValue("date", s.Date);
        cmd.Parameters.AddWithValue("payer", s.PayerId);
        cmd.Parameters.AddWithValue("payer_name", s.PayerName);
        cmd.Parameters.AddWithValue("payer_code", s.PayerDetailCode ?? string.Empty);
        cmd.Parameters.AddWithValue("group", s.PurposeGroupId);
        cmd.Parameters.AddWithValue("group_name", s.PurposeGroupName);
        cmd.Parameters.AddWithValue("purpose", s.Purpose);
        cmd.Parameters.AddWithValue("ref1", Db(s.Reference1));
        cmd.Parameters.AddWithValue("ref2", Db(s.Reference2));
        cmd.Parameters.Add(new NpgsqlParameter("department", NpgsqlDbType.Uuid) { Value = Db(s.DepartmentId) });
        cmd.Parameters.AddWithValue("sales", s.SalesProceeds);
        cmd.Parameters.AddWithValue("note", Db(s.Note));
        cmd.Parameters.AddWithValue("by", s.RecordedBy);
        cmd.Parameters.AddWithValue("account_kind", Db(s.AccountKind?.ToString()));
        cmd.Parameters.Add(new NpgsqlParameter("account", NpgsqlDbType.Uuid) { Value = Db(s.AccountId) });
        cmd.Parameters.AddWithValue("method", Db(s.Method?.ToString()));
        cmd.Parameters.AddWithValue("tracking", Db(s.TrackingNumber));
        cmd.Parameters.Add(new NpgsqlParameter("amount", NpgsqlDbType.Numeric) { Value = Db(s.Amount) });
        cmd.Parameters.AddWithValue("currency", Db(s.Currency));
        cmd.Parameters.Add(new NpgsqlParameter("rate", NpgsqlDbType.Numeric) { Value = Db(s.RateToRial) });
        cmd.Parameters.Add(new NpgsqlParameter("instruments", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = s.InstrumentIds.ToArray() });
        cmd.Parameters.AddWithValue("total", s.TotalRial);
        cmd.Parameters.AddWithValue("state", s.State.ToString());
        cmd.Parameters.Add(new NpgsqlParameter("cancelled_on", NpgsqlDbType.Date) { Value = Db(s.CancelledOn) });
        cmd.Parameters.AddWithValue("cancel_reason", Db(s.CancelReason));
        cmd.Parameters.Add(new NpgsqlParameter("replaced", NpgsqlDbType.Uuid) { Value = Db(s.ReplacedInstrumentId) });
        cmd.Parameters.Add(new NpgsqlParameter("sheba", NpgsqlDbType.Text) { Value = Db(s.PayerSheba) });
        cmd.Parameters.Add(new NpgsqlParameter("note_terms", NpgsqlDbType.Jsonb)
        {
            Value = s.NoteTerms is null ? DBNull.Value : System.Text.Json.JsonSerializer.Serialize(s.NoteTerms, TermsJson),
        });
    }

    private static async Task<IReadOnlyList<Receipt>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<Receipt>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            string? Text(int i) => r.IsDBNull(i) ? null : r.GetString(i);
            list.Add(new ReceiptSnapshot(
                Id: r.GetGuid(1),
                Kind: Enum.Parse<ReceiptKind>(r.GetString(2)),
                FiscalYear: r.GetInt32(0),
                Number: r.GetInt32(3),
                Date: r.GetFieldValue<DateOnly>(4),
                PayerId: r.GetGuid(5),
                PayerName: r.GetString(6),
                PayerDetailCode: r.GetString(7),
                PurposeGroupId: r.GetGuid(8),
                PurposeGroupName: r.GetString(9),
                Purpose: r.GetString(10),
                Reference1: Text(11),
                Reference2: Text(12),
                DepartmentId: r.IsDBNull(13) ? null : r.GetGuid(13),
                SalesProceeds: r.GetBoolean(14),
                Note: Text(15),
                RecordedBy: r.GetString(16),
                AccountKind: Text(17) is { } k ? Enum.Parse<ReceiptAccountKind>(k) : null,
                AccountId: r.IsDBNull(18) ? null : r.GetGuid(18),
                Method: Text(19) is { } m ? Enum.Parse<DepositMethod>(m) : null,
                TrackingNumber: Text(20),
                Amount: r.IsDBNull(21) ? null : r.GetDecimal(21),
                Currency: Text(22)?.Trim(),
                RateToRial: r.IsDBNull(23) ? null : r.GetDecimal(23),
                InstrumentIds: r.GetFieldValue<Guid[]>(24),
                TotalRial: r.GetDecimal(25),
                State: Enum.Parse<ReceiptState>(r.GetString(26)),
                CancelledOn: r.IsDBNull(27) ? null : r.GetFieldValue<DateOnly>(27),
                CancelReason: Text(28),
                Version: r.GetInt64(29),
                ReplacedInstrumentId: r.IsDBNull(30) ? null : r.GetGuid(30),
                PayerSheba: Text(31),
                NoteTerms: r.IsDBNull(32) ? null : System.Text.Json.JsonSerializer.Deserialize<NoteTermsSnapshot>(r.GetString(32), TermsJson)).ToDomain());
        }

        return list;
    }
}
