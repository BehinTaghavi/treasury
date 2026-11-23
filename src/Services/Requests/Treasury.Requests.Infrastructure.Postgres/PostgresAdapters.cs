using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Requests.Application.Abstractions;
using Treasury.Requests.Domain.PaymentRequests;
using Treasury.Requests.Infrastructure.InMemory;

namespace Treasury.Requests.Infrastructure.Postgres;

// Written against Npgsql 10 and db/postgres/026_requests.sql.
// Integration tests against PostgreSQL 17: tests/Treasury.Requests.Infrastructure.Postgres.Tests.

public static class PostgresRegistration
{
    /// <summary>Replaces the in-memory adapters. The connection string must log in as the requests_svc role.</summary>
    public static IServiceCollection AddRequestsPostgres(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IUnitOfWork, PostgresUnitOfWork>();
        services.AddSingleton<IPaymentRequestRepository, PostgresPaymentRequestRepository>();
        services.AddSingleton<IIdempotencyStore, PostgresIdempotencyStore>();
        return services;
    }
}

internal sealed class TransactionContext(NpgsqlConnection connection, NpgsqlTransaction transaction)
{
    public NpgsqlCommand Command(string sql) => new(sql, connection, transaction);
}

public sealed class PostgresUnitOfWork(NpgsqlDataSource dataSource) : IUnitOfWork
{
    private static readonly AsyncLocal<TransactionContext?> Ambient = new();

    internal static TransactionContext Current =>
        Ambient.Value ?? throw new InvalidOperationException("Database access must run inside IUnitOfWork.ExecuteAsync.");

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
        Ambient.Value = new TransactionContext(connection, transaction);
        try
        {
            var result = await work(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == "P0001" && ex.MessageText.Contains("is closed", StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw new DomainException("FiscalPeriod.Closed", "The fiscal year of this request is closed.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        finally
        {
            Ambient.Value = null;
        }
    }
}

public sealed class PostgresPaymentRequestRepository : IPaymentRequestRepository
{
    private const string Columns =
        "fiscal_year, id, number, recorded_by, request_date, requester_id, requester_name, requester_department_id, cost_center_code, " +
        "beneficiary_id, beneficiary_detail_code, beneficiary_name, purpose_group_id, purpose_group_name, purpose, reference1, reference2, " +
        "amount, currency, method, priority, transfer_account, card_number, pay_from_kind, pay_from_id, due_date, department_id, " +
        "has_main_beneficiaries, attachments, note, state, approved_by, approved_at, cancel_reason, version, payments";

    public async Task<PaymentRequest?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM requests.payment_request WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        var list = await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
        return list.Count == 0 ? null : (await WithFilesAsync(list, cancellationToken).ConfigureAwait(false))[0];
    }

    public async Task<int> NextNumberAsync(int fiscalYear, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "INSERT INTO requests.request_number (fiscal_year, last_number) VALUES (@year, 1) " +
            "ON CONFLICT (fiscal_year) DO UPDATE SET last_number = requests.request_number.last_number + 1 RETURNING last_number");
        cmd.Parameters.AddWithValue("year", fiscalYear);
        return (int)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            $"INSERT INTO requests.payment_request ({Columns}) VALUES (@year, @id, @number, @by, @date, @requester, @requester_name, @requester_dept, @cost_center, " +
            "@beneficiary, @beneficiary_code, @beneficiary_name, @group, @group_name, @purpose, @ref1, @ref2, @amount, @currency, @method, @priority, " +
            "@transfer, @card, @from_kind, @from_id, @due, @department, @main, @attachments, @note, @state, @approved_by, @approved_at, @cancel_reason, 1, @payments)"))
        {
            cmd.Parameters.AddWithValue("number", request.Number);
            cmd.Parameters.AddWithValue("by", request.RecordedBy);
            Bind(cmd, request);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, request.PendingEvents, cancellationToken).ConfigureAwait(false);
        request.MarkSaved(1);
    }

    public async Task SaveAsync(PaymentRequest request, CancellationToken cancellationToken, (Guid FileId, byte[] Content)? newFile = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE requests.payment_request SET request_date = @date, requester_id = @requester, requester_name = @requester_name, " +
            "requester_department_id = @requester_dept, cost_center_code = @cost_center, beneficiary_id = @beneficiary, beneficiary_detail_code = @beneficiary_code, " +
            "beneficiary_name = @beneficiary_name, purpose_group_id = @group, purpose_group_name = @group_name, purpose = @purpose, reference1 = @ref1, " +
            "reference2 = @ref2, amount = @amount, currency = @currency, method = @method, priority = @priority, transfer_account = @transfer, " +
            "card_number = @card, pay_from_kind = @from_kind, pay_from_id = @from_id, due_date = @due, department_id = @department, " +
            "has_main_beneficiaries = @main, attachments = @attachments, note = @note, state = @state, approved_by = @approved_by, approved_at = @approved_at, " +
            "cancel_reason = @cancel_reason, payments = @payments, version = version + 1, updated_at = now() WHERE fiscal_year = @year AND id = @id AND version = @expected"))
        {
            Bind(cmd, request);
            cmd.Parameters.AddWithValue("expected", request.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Payment request {request.Id} was changed by someone else.");
            }
        }

        // Scans removed from the request go; a scan just attached is stored with its content.
        await using (var del = tx.Command("DELETE FROM requests.request_file WHERE fiscal_year = @year AND request_id = @id AND NOT (file_id = ANY(@keep))"))
        {
            del.Parameters.AddWithValue("year", request.FiscalYear);
            del.Parameters.AddWithValue("id", request.Id);
            del.Parameters.AddWithValue("keep", request.Files.Select(f => f.Id).ToArray());
            await del.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (newFile is { } nf && request.Files.FirstOrDefault(f => f.Id == nf.FileId) is { } meta)
        {
            await using var ins = tx.Command(
                "INSERT INTO requests.request_file (fiscal_year, file_id, request_id, file_name, content_type, size, uploaded_by, uploaded_at, pages, content) " +
                "VALUES (@year, @file, @id, @name, @type, @size, @by, @at, @pages, @content)");
            ins.Parameters.AddWithValue("year", request.FiscalYear);
            ins.Parameters.AddWithValue("file", meta.Id);
            ins.Parameters.AddWithValue("id", request.Id);
            ins.Parameters.AddWithValue("name", meta.FileName);
            ins.Parameters.AddWithValue("type", meta.ContentType);
            ins.Parameters.AddWithValue("size", meta.Size);
            ins.Parameters.AddWithValue("by", meta.UploadedBy);
            ins.Parameters.AddWithValue("at", meta.UploadedAt);
            ins.Parameters.AddWithValue("pages", meta.Pages);
            ins.Parameters.Add(new NpgsqlParameter("content", NpgsqlDbType.Bytea) { Value = nf.Content });
            await ins.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, request.PendingEvents, cancellationToken).ConfigureAwait(false);
        request.MarkSaved(request.Version + 1);
    }

    public async Task<(RequestAttachment File, byte[] Content)?> GetFileAsync(Guid requestId, Guid fileId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT file_name, content_type, size, uploaded_by, uploaded_at, content, pages FROM requests.request_file WHERE request_id = @id AND file_id = @file");
        cmd.Parameters.AddWithValue("id", requestId);
        cmd.Parameters.AddWithValue("file", fileId);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return (new RequestAttachment(fileId, r.GetString(0), r.GetString(1), r.GetInt32(2), r.GetString(3), r.GetFieldValue<DateTimeOffset>(4), r.GetInt32(6)),
            r.GetFieldValue<byte[]>(5));
    }

    /// <summary>Adds the scans' names and sizes (not their content) to requests read from the table.</summary>
    private static async Task<IReadOnlyList<PaymentRequest>> WithFilesAsync(IReadOnlyList<PaymentRequest> list, CancellationToken ct)
    {
        if (list.Count == 0)
        {
            return list;
        }

        var files = new Dictionary<Guid, List<RequestAttachment>>();
        await using (var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT request_id, file_id, file_name, content_type, size, uploaded_by, uploaded_at, pages FROM requests.request_file WHERE request_id = ANY(@ids) ORDER BY uploaded_at"))
        {
            cmd.Parameters.AddWithValue("ids", list.Select(r => r.Id).ToArray());
            await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await r.ReadAsync(ct).ConfigureAwait(false))
            {
                var id = r.GetGuid(0);
                if (!files.TryGetValue(id, out var l))
                {
                    files[id] = l = [];
                }

                l.Add(new RequestAttachment(r.GetGuid(1), r.GetString(2), r.GetString(3), r.GetInt32(4), r.GetString(5), r.GetFieldValue<DateTimeOffset>(6), r.GetInt32(7)));
            }
        }

        return list.Select(x => files.TryGetValue(x.Id, out var l)
            ? PaymentRequest.Restore(x.Id, x.Number, x.FiscalYear, x.RecordedBy, x.Data(), x.Version, l)
            : x).ToList();
    }

    public async Task<IReadOnlyList<PaymentRequest>> ListAsync(int fiscalYear, PaymentRequestState? state, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT {Columns} FROM requests.payment_request WHERE fiscal_year = @year AND (@state::text IS NULL OR state = @state)");
        cmd.Parameters.AddWithValue("year", fiscalYear);
        cmd.Parameters.Add(new NpgsqlParameter("state", NpgsqlDbType.Text) { Value = (object?)state?.ToString() ?? DBNull.Value });
        return await WithFilesAsync(await ReadAsync(cmd, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    private static void Bind(NpgsqlCommand cmd, PaymentRequest r)
    {
        static object Db(object? value) => value ?? DBNull.Value;
        var d = r.Data();
        cmd.Parameters.AddWithValue("year", r.FiscalYear);
        cmd.Parameters.AddWithValue("id", r.Id);
        cmd.Parameters.AddWithValue("date", d.Date.Value);
        cmd.Parameters.AddWithValue("requester", d.RequesterId);
        cmd.Parameters.AddWithValue("requester_name", d.RequesterName);
        cmd.Parameters.Add(new NpgsqlParameter("requester_dept", NpgsqlDbType.Uuid) { Value = Db(d.RequesterDepartmentId) });
        cmd.Parameters.AddWithValue("cost_center", Db(d.CostCenterCode));
        cmd.Parameters.AddWithValue("beneficiary", d.BeneficiaryId);
        cmd.Parameters.AddWithValue("beneficiary_code", d.BeneficiaryDetailCode);
        cmd.Parameters.AddWithValue("beneficiary_name", d.BeneficiaryName);
        cmd.Parameters.AddWithValue("group", d.PurposeGroupId);
        cmd.Parameters.AddWithValue("group_name", d.PurposeGroupName);
        cmd.Parameters.AddWithValue("purpose", d.Purpose);
        cmd.Parameters.AddWithValue("ref1", Db(d.Reference1));
        cmd.Parameters.AddWithValue("ref2", Db(d.Reference2));
        cmd.Parameters.AddWithValue("amount", d.Amount);
        cmd.Parameters.AddWithValue("currency", d.Currency);
        cmd.Parameters.AddWithValue("method", d.Method.ToString());
        cmd.Parameters.AddWithValue("priority", (short)d.Priority);
        cmd.Parameters.AddWithValue("transfer", Db(d.TransferAccount));
        cmd.Parameters.AddWithValue("card", Db(d.CardNumber));
        cmd.Parameters.AddWithValue("from_kind", Db(d.PayFromKind?.ToString()));
        cmd.Parameters.Add(new NpgsqlParameter("from_id", NpgsqlDbType.Uuid) { Value = Db(d.PayFromId) });
        cmd.Parameters.Add(new NpgsqlParameter("due", NpgsqlDbType.Date) { Value = Db(d.DueDate?.Value) });
        cmd.Parameters.Add(new NpgsqlParameter("department", NpgsqlDbType.Uuid) { Value = Db(d.DepartmentId) });
        cmd.Parameters.AddWithValue("main", d.HasMainBeneficiaries);
        cmd.Parameters.AddWithValue("attachments", d.Attachments);
        cmd.Parameters.AddWithValue("note", Db(d.Note));
        cmd.Parameters.AddWithValue("state", d.State.ToString());
        cmd.Parameters.AddWithValue("approved_by", Db(d.ApprovedBy));
        cmd.Parameters.Add(new NpgsqlParameter("approved_at", NpgsqlDbType.TimestampTz) { Value = Db(d.ApprovedAt) });
        cmd.Parameters.AddWithValue("cancel_reason", Db(d.CancelReason));
        cmd.Parameters.Add(new NpgsqlParameter("payments", NpgsqlDbType.Jsonb)
        {
            Value = System.Text.Json.JsonSerializer.Serialize(
                (d.Payments ?? []).Select(p => new PaymentRow(p.Key, p.Kind, p.InstrumentId, p.Reference, p.Amount, p.Date.Value, p.UserId)), PaymentJson),
        });
    }

    /// <summary>Stored form of a payment in requests.payment_request.payments (db/postgres/032).</summary>
    private sealed record PaymentRow(string Key, string Kind, Guid? InstrumentId, string? Reference, decimal Amount, DateOnly Date, string UserId);

    private static readonly System.Text.Json.JsonSerializerOptions PaymentJson = new(System.Text.Json.JsonSerializerDefaults.Web);

    private static async Task<IReadOnlyList<PaymentRequest>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<PaymentRequest>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            string? Text(int i) => r.IsDBNull(i) ? null : r.GetString(i);
            Guid? Id(int i) => r.IsDBNull(i) ? null : r.GetGuid(i);
            var data = new PaymentRequestSnapshotData(
                Date: new FiscalDate(r.GetFieldValue<DateOnly>(4)),
                RequesterId: r.GetGuid(5),
                RequesterName: r.GetString(6),
                RequesterDepartmentId: Id(7),
                CostCenterCode: Text(8),
                BeneficiaryId: r.GetGuid(9),
                BeneficiaryDetailCode: r.GetString(10),
                BeneficiaryName: r.GetString(11),
                PurposeGroupId: r.GetGuid(12),
                PurposeGroupName: r.GetString(13),
                Purpose: r.GetString(14),
                Reference1: Text(15),
                Reference2: Text(16),
                Amount: r.GetDecimal(17),
                Currency: r.GetString(18).Trim(),
                Method: Enum.Parse<PaymentMethod>(r.GetString(19)),
                Priority: r.GetInt16(20),
                TransferAccount: Text(21),
                CardNumber: Text(22),
                PayFromKind: Text(23) is { } k ? Enum.Parse<PayFromKind>(k) : null,
                PayFromId: Id(24),
                DueDate: r.IsDBNull(25) ? null : new FiscalDate(r.GetFieldValue<DateOnly>(25)),
                DepartmentId: Id(26),
                HasMainBeneficiaries: r.GetBoolean(27),
                Attachments: r.GetInt32(28),
                Note: Text(29),
                State: Enum.Parse<PaymentRequestState>(r.GetString(30)),
                ApprovedBy: Text(31),
                ApprovedAt: r.IsDBNull(32) ? null : r.GetFieldValue<DateTimeOffset>(32),
                CancelReason: Text(33),
                Payments: r.IsDBNull(35) ? null
                    : (System.Text.Json.JsonSerializer.Deserialize<List<PaymentRow>>(r.GetString(35), PaymentJson) ?? [])
                        .Select(p => new RequestPayment(p.Key, p.Kind, p.InstrumentId, p.Reference, p.Amount, new FiscalDate(p.Date), p.UserId)).ToList());
            list.Add(PaymentRequest.Restore(r.GetGuid(1), r.GetInt32(2), r.GetInt32(0), r.GetString(3), data, r.GetInt64(34)));
        }

        return list;
    }
}

public sealed class PostgresIdempotencyStore(NpgsqlDataSource dataSource) : IIdempotencyStore
{
    public async Task<string?> GetResultAsync(string key, CancellationToken cancellationToken)
    {
        await using var cmd = dataSource.CreateCommand("SELECT result FROM requests.idempotency WHERE key = @key");
        cmd.Parameters.AddWithValue("key", key);
        return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
    }

    public async Task SaveResultAsync(string key, string result, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("INSERT INTO requests.idempotency (key, result) VALUES (@key, @result)");
        cmd.Parameters.AddWithValue("key", key);
        cmd.Parameters.AddWithValue("result", result);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal static class PostgresOutbox
{
    public static async Task WriteAsync(TransactionContext tx, IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var message in events.Select(OutboxMessage.From))
        {
            await using var cmd = tx.Command("INSERT INTO requests.outbox (id, type, payload, occurred_at) VALUES (@id, @type, @payload, @at)");
            cmd.Parameters.AddWithValue("id", message.Id);
            cmd.Parameters.AddWithValue("type", message.Type);
            cmd.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = message.Payload });
            cmd.Parameters.AddWithValue("at", message.OccurredAt);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }
}
