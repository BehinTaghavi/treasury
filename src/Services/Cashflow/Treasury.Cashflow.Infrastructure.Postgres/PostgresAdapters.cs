using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Infrastructure.Postgres;

// Written against Npgsql 10 and db/postgres/002_cashflow.sql.
// Integration tests against PostgreSQL 17: tests/Treasury.Cashflow.Infrastructure.Postgres.Tests.

public static class PostgresRegistration
{
    /// <summary>Replaces the in-memory adapters. The connection string must log in as the cashflow_svc role.</summary>
    public static IServiceCollection AddCashflowPostgres(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<PostgresUnitOfWork>();
        services.AddSingleton<IUnitOfWork>(sp => sp.GetRequiredService<PostgresUnitOfWork>());
        services.AddSingleton<IInstrumentRepository, PostgresInstrumentRepository>();
        services.AddSingleton<IChequeBookRepository, PostgresChequeBookRepository>();
        services.AddSingleton<IChequeFormatRepository, PostgresChequeFormatRepository>();
        services.AddSingleton<IIdempotencyStore, PostgresIdempotencyStore>();
        return services;
    }
}

/// <summary>Connection and transaction of the running use case.</summary>
internal sealed class TransactionContext(NpgsqlConnection connection, NpgsqlTransaction transaction)
{
    public NpgsqlConnection Connection { get; } = connection;

    public NpgsqlTransaction Transaction { get; } = transaction;

    /// <summary>Sheets of each cheque book as they were when loaded, to insert only the new ones.</summary>
    public Dictionary<Guid, HashSet<long>> LoadedSheets { get; } = [];

    /// <summary>Number of history steps of each instrument as loaded, to insert only the new steps.</summary>
    public Dictionary<Guid, int> LoadedSteps { get; } = [];

    public NpgsqlCommand Command(string sql) => new(sql, Connection, Transaction);
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
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.TableName == "cheque_format")
        {
            // Two users added a type at the same moment: the indexes decide.
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw new DomainException("ChequeFormat.Taken", "This bank account already has this cheque type (or the print number was taken; save again).");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation && ex.ConstraintName == "cheque_book_format")
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw new DomainException("ChequeFormat.InUse", "Cheque books are printed with this type.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.TableName == "cheque_sheet")
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw new DomainException("ChequeBook.SheetAlreadyUsed", "This cheque sheet has just been used by another user.");
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

public sealed class PostgresInstrumentRepository : IInstrumentRepository
{
    private const string Columns =
        "origin_fiscal_year, id, direction, kind, number, bank_name, bank_account_id, cheque_book_id, payment_id, amount, currency, rate_to_rial, " +
        "issue_date, due_date, counterparty_id, counterparty_name, purpose, destination, state, version";

    public async Task<Instrument?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var tx = PostgresUnitOfWork.Current;
        InstrumentSnapshot? head = null;
        await using (var cmd = tx.Command($"SELECT {Columns} FROM cashflow.instrument WHERE id = @id"))
        {
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                head = new InstrumentSnapshot(
                    Id: r.GetGuid(1),
                    Direction: Enum.Parse<InstrumentDirection>(r.GetString(2)),
                    Kind: Enum.Parse<InstrumentKind>(r.GetString(3)),
                    Number: r.IsDBNull(4) ? null : r.GetString(4),
                    BankName: r.IsDBNull(5) ? null : r.GetString(5),
                    BankAccountId: r.IsDBNull(6) ? null : r.GetGuid(6),
                    ChequeBookId: r.IsDBNull(7) ? null : r.GetGuid(7),
                    PaymentId: r.IsDBNull(8) ? null : r.GetGuid(8),
                    Amount: r.GetDecimal(9),
                    Currency: r.GetString(10).Trim(),
                    RateToRial: r.IsDBNull(11) ? null : r.GetDecimal(11),
                    IssueDate: r.GetFieldValue<DateOnly>(12),
                    DueDate: r.IsDBNull(13) ? null : r.GetFieldValue<DateOnly>(13),
                    CounterpartyId: r.GetGuid(14),
                    CounterpartyName: r.GetString(15),
                    Purpose: r.GetString(16),
                    Destination: r.IsDBNull(17) ? null : r.GetString(17),
                    State: Enum.Parse<InstrumentState>(r.GetString(18)),
                    OriginFiscalYear: r.GetInt32(0),
                    History: [],
                    Version: r.GetInt64(19));
            }
        }

        if (head is null)
        {
            return null;
        }

        var history = new List<HistorySnapshot>();
        await using (var cmd = tx.Command(
            "SELECT action, from_state, to_state, event_date, user_id, reference, note FROM cashflow.instrument_event WHERE instrument_id = @id ORDER BY seq"))
        {
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                history.Add(new HistorySnapshot(
                    Enum.Parse<InstrumentAction>(r.GetString(0)),
                    r.IsDBNull(1) ? null : Enum.Parse<InstrumentState>(r.GetString(1)),
                    Enum.Parse<InstrumentState>(r.GetString(2)),
                    r.GetFieldValue<DateOnly>(3),
                    r.GetString(4),
                    r.IsDBNull(5) ? null : r.GetString(5),
                    r.IsDBNull(6) ? null : r.GetString(6)));
            }
        }

        tx.LoadedSteps[id] = history.Count;
        return (head with { History = history }).ToDomain();
    }

    public async Task<IReadOnlyList<Guid>> FindReceivedAsync(string bankName, string number, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT id FROM cashflow.instrument WHERE direction = 'Received' AND bank_name = @bank AND number = @number");
        cmd.Parameters.AddWithValue("bank", bankName);
        cmd.Parameters.AddWithValue("number", number);
        var ids = new List<Guid>();
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            ids.Add(r.GetGuid(0));
        }

        return ids;
    }

    public async Task AddAsync(Instrument instrument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        var tx = PostgresUnitOfWork.Current;
        var s = InstrumentSnapshot.From(instrument, 1);
        await using (var cmd = tx.Command(
            $"INSERT INTO cashflow.instrument ({Columns}, amount_rial) VALUES " +
            "(@year, @id, @direction, @kind, @number, @bank, @account, @book, @payment, @amount, @currency, @rate, @issue, @due, @cp, @cpname, @purpose, @destination, @state, 1, @rial)"))
        {
            BindInstrument(cmd, s, instrument.Amount.ToRial());
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await InsertStepsAsync(tx, s, 0, cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, instrument.PendingEvents, cancellationToken).ConfigureAwait(false);
        instrument.MarkSaved(1);
    }

    public async Task SaveAsync(Instrument instrument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        var tx = PostgresUnitOfWork.Current;
        var s = InstrumentSnapshot.From(instrument, instrument.Version + 1);
        await using (var cmd = tx.Command(
            "UPDATE cashflow.instrument SET number = @number, cheque_book_id = @book, destination = @destination, state = @state, " +
            "version = version + 1, updated_at = now() WHERE origin_fiscal_year = @year AND id = @id AND version = @expected"))
        {
            cmd.Parameters.AddWithValue("number", (object?)s.Number ?? DBNull.Value);
            cmd.Parameters.AddWithValue("book", (object?)s.ChequeBookId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("destination", (object?)s.Destination ?? DBNull.Value);
            cmd.Parameters.AddWithValue("state", s.State.ToString());
            cmd.Parameters.AddWithValue("year", s.OriginFiscalYear);
            cmd.Parameters.AddWithValue("id", s.Id);
            cmd.Parameters.AddWithValue("expected", instrument.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Instrument {s.Id} was changed by someone else.");
            }
        }

        var alreadyStored = tx.LoadedSteps.TryGetValue(s.Id, out var n) ? n : 0;
        await InsertStepsAsync(tx, s, alreadyStored, cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, instrument.PendingEvents, cancellationToken).ConfigureAwait(false);
        tx.LoadedSteps[s.Id] = s.History.Count;
        instrument.MarkSaved(instrument.Version + 1);
    }

    private static async Task InsertStepsAsync(TransactionContext tx, InstrumentSnapshot s, int alreadyStored, CancellationToken ct)
    {
        for (var i = alreadyStored; i < s.History.Count; i++)
        {
            var h = s.History[i];
            // The step belongs to the fiscal year in which it happens (doc 7.4).
            await using var cmd = tx.Command(
                "INSERT INTO cashflow.instrument_event (fiscal_year, instrument_year, instrument_id, seq, action, from_state, to_state, event_date, user_id, reference, note) " +
                "VALUES (cashflow.fiscal_year_of(@date), @iyear, @iid, @seq, @action, @from, @to, @date, @user, @ref, @note)");
            cmd.Parameters.AddWithValue("date", h.Date);
            cmd.Parameters.AddWithValue("iyear", s.OriginFiscalYear);
            cmd.Parameters.AddWithValue("iid", s.Id);
            cmd.Parameters.AddWithValue("seq", i + 1);
            cmd.Parameters.AddWithValue("action", h.Action.ToString());
            cmd.Parameters.AddWithValue("from", (object?)h.From?.ToString() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("to", h.To.ToString());
            cmd.Parameters.AddWithValue("user", h.UserId);
            cmd.Parameters.AddWithValue("ref", (object?)h.Reference ?? DBNull.Value);
            cmd.Parameters.AddWithValue("note", (object?)h.Note ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

    private static void BindInstrument(NpgsqlCommand cmd, InstrumentSnapshot s, decimal amountRial)
    {
        cmd.Parameters.AddWithValue("year", s.OriginFiscalYear);
        cmd.Parameters.AddWithValue("id", s.Id);
        cmd.Parameters.AddWithValue("direction", s.Direction.ToString());
        cmd.Parameters.AddWithValue("kind", s.Kind.ToString());
        cmd.Parameters.AddWithValue("number", (object?)s.Number ?? DBNull.Value);
        cmd.Parameters.AddWithValue("bank", (object?)s.BankName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("account", (object?)s.BankAccountId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("book", (object?)s.ChequeBookId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("payment", (object?)s.PaymentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("amount", s.Amount);
        cmd.Parameters.AddWithValue("currency", s.Currency);
        cmd.Parameters.AddWithValue("rate", (object?)s.RateToRial ?? DBNull.Value);
        cmd.Parameters.AddWithValue("issue", s.IssueDate);
        cmd.Parameters.AddWithValue("due", (object?)s.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("cp", s.CounterpartyId);
        cmd.Parameters.AddWithValue("cpname", s.CounterpartyName);
        cmd.Parameters.AddWithValue("purpose", s.Purpose);
        cmd.Parameters.AddWithValue("destination", (object?)s.Destination ?? DBNull.Value);
        cmd.Parameters.AddWithValue("state", s.State.ToString());
        cmd.Parameters.AddWithValue("rial", amountRial);
    }
}

public sealed class PostgresChequeBookRepository : IChequeBookRepository
{
    public async Task<ChequeBook?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var tx = PostgresUnitOfWork.Current;
        ChequeBookSnapshot? head = null;
        // Row lock: a second user issuing from the same book waits here until the first transaction ends.
        await using (var cmd = tx.Command(
            "SELECT bank_account_id, serial, first_sheet, last_sheet, is_active, version, format_id, sayad_middle, sayad_first, partly_used FROM cashflow.cheque_book WHERE id = @id FOR UPDATE"))
        {
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                head = new ChequeBookSnapshot(id, r.GetGuid(0), r.GetString(1), r.GetInt64(2), r.GetInt64(3), r.GetBoolean(4), new Dictionary<long, SheetUse>(), r.GetInt64(5),
                    r.IsDBNull(6) ? null : r.GetGuid(6), r.IsDBNull(7) ? null : r.GetString(7), r.IsDBNull(8) ? null : r.GetInt16(8), r.GetBoolean(9));
            }
        }

        if (head is null)
        {
            return null;
        }

        var sheets = new Dictionary<long, SheetUse>();
        await using (var cmd = tx.Command("SELECT sheet, sheet_use FROM cashflow.cheque_sheet WHERE cheque_book_id = @id"))
        {
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                sheets[r.GetInt64(0)] = Enum.Parse<SheetUse>(r.GetString(1));
            }
        }

        tx.LoadedSheets[id] = [.. sheets.Keys];
        return (head with { UsedSheets = sheets }).ToDomain();
    }

    public async Task AddAsync(ChequeBook book, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(book);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "INSERT INTO cashflow.cheque_book (id, bank_account_id, serial, first_sheet, last_sheet, is_active, version, format_id, sayad_middle, sayad_first, partly_used) " +
            "VALUES (@id, @account, @serial, @first, @last, @active, 1, @format, @sayad_middle, @sayad_first, @partly_used)"))
        {
            SayadParameters(cmd, book);
            cmd.Parameters.Add(new NpgsqlParameter("format", NpgsqlDbType.Uuid) { Value = (object?)book.FormatId ?? DBNull.Value });
            cmd.Parameters.AddWithValue("id", book.Id);
            cmd.Parameters.AddWithValue("account", book.BankAccountId);
            cmd.Parameters.AddWithValue("serial", book.Serial);
            cmd.Parameters.AddWithValue("first", book.FirstSheet);
            cmd.Parameters.AddWithValue("last", book.LastSheet);
            cmd.Parameters.AddWithValue("active", book.IsActive);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        tx.LoadedSheets[book.Id] = [];
        await InsertNewSheetsAsync(tx, book, cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, book.PendingEvents, cancellationToken).ConfigureAwait(false);
        book.MarkSaved(1);
    }

    public async Task SaveAsync(ChequeBook book, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(book);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE cashflow.cheque_book SET serial = @serial, first_sheet = @first, last_sheet = @last, is_active = @active, format_id = @format, " +
            "sayad_middle = @sayad_middle, sayad_first = @sayad_first, partly_used = @partly_used, version = version + 1 WHERE id = @id AND version = @expected"))
        {
            SayadParameters(cmd, book);
            cmd.Parameters.Add(new NpgsqlParameter("format", NpgsqlDbType.Uuid) { Value = (object?)book.FormatId ?? DBNull.Value });
            cmd.Parameters.AddWithValue("serial", book.Serial);
            cmd.Parameters.AddWithValue("first", book.FirstSheet);
            cmd.Parameters.AddWithValue("last", book.LastSheet);
            cmd.Parameters.AddWithValue("active", book.IsActive);
            cmd.Parameters.AddWithValue("id", book.Id);
            cmd.Parameters.AddWithValue("expected", book.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Cheque book {book.Id} was changed by someone else.");
            }
        }

        await InsertNewSheetsAsync(tx, book, cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, book.PendingEvents, cancellationToken).ConfigureAwait(false);
        book.MarkSaved(book.Version + 1);
    }

    public async Task DeleteAsync(ChequeBook book, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(book);
        var tx = PostgresUnitOfWork.Current;
        // "NOT EXISTS": a sheet issued since the book was loaded refuses the delete (the book row lock normally prevents that).
        await using (var cmd = tx.Command(
            "DELETE FROM cashflow.cheque_book b WHERE id = @id AND version = @expected AND NOT EXISTS (SELECT 1 FROM cashflow.cheque_sheet s WHERE s.cheque_book_id = b.id)"))
        {
            cmd.Parameters.AddWithValue("id", book.Id);
            cmd.Parameters.AddWithValue("expected", book.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Cheque book {book.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, book.PendingEvents, cancellationToken).ConfigureAwait(false);
        book.MarkSaved(book.Version);
    }

    public async Task<IReadOnlyList<ChequeBook>> ListAsync(Guid? bankAccountId, bool includeInactive, CancellationToken cancellationToken)
    {
        var tx = PostgresUnitOfWork.Current;
        var heads = new List<ChequeBookSnapshot>();
        await using (var cmd = tx.Command(
            "SELECT id, bank_account_id, serial, first_sheet, last_sheet, is_active, version, format_id, sayad_middle, sayad_first, partly_used FROM cashflow.cheque_book " +
            "WHERE (@account::uuid IS NULL OR bank_account_id = @account) AND (@all OR is_active)"))
        {
            cmd.Parameters.Add(new NpgsqlParameter("account", NpgsqlDbType.Uuid) { Value = (object?)bankAccountId ?? DBNull.Value });
            cmd.Parameters.AddWithValue("all", includeInactive);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                heads.Add(new ChequeBookSnapshot(r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetInt64(3), r.GetInt64(4), r.GetBoolean(5), new Dictionary<long, SheetUse>(), r.GetInt64(6),
                    r.IsDBNull(7) ? null : r.GetGuid(7), r.IsDBNull(8) ? null : r.GetString(8), r.IsDBNull(9) ? null : r.GetInt16(9), r.GetBoolean(10)));
            }
        }

        var sheets = heads.ToDictionary(h => h.Id, _ => new Dictionary<long, SheetUse>());
        if (heads.Count > 0)
        {
            await using var cmd = tx.Command("SELECT cheque_book_id, sheet, sheet_use FROM cashflow.cheque_sheet WHERE cheque_book_id = ANY(@ids)");
            cmd.Parameters.AddWithValue("ids", heads.Select(h => h.Id).ToArray());
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                sheets[r.GetGuid(0)][r.GetInt64(1)] = Enum.Parse<SheetUse>(r.GetString(2));
            }
        }

        return heads.Select(h => (h with { UsedSheets = sheets[h.Id] }).ToDomain()).ToList();
    }

    private static void SayadParameters(NpgsqlCommand cmd, ChequeBook book)
    {
        cmd.Parameters.Add(new NpgsqlParameter("sayad_middle", NpgsqlDbType.Char) { Value = (object?)book.Sayad?.Middle ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("sayad_first", NpgsqlDbType.Smallint) { Value = book.Sayad is { } s ? (object)(short)s.FirstSerial : DBNull.Value });
        cmd.Parameters.AddWithValue("partly_used", book.PartlyUsed);
    }

    private static async Task InsertNewSheetsAsync(TransactionContext tx, ChequeBook book, CancellationToken ct)
    {
        var loaded = tx.LoadedSheets.TryGetValue(book.Id, out var set) ? set : [];
        foreach (var (sheet, use) in book.UsedSheets.Where(kv => !loaded.Contains(kv.Key)))
        {
            await using var cmd = tx.Command("INSERT INTO cashflow.cheque_sheet (cheque_book_id, sheet, sheet_use) VALUES (@book, @sheet, @use)");
            cmd.Parameters.AddWithValue("book", book.Id);
            cmd.Parameters.AddWithValue("sheet", sheet);
            cmd.Parameters.AddWithValue("use", use.ToString());
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            loaded.Add(sheet);
        }

        tx.LoadedSheets[book.Id] = loaded;
    }
}

public sealed class PostgresIdempotencyStore(NpgsqlDataSource dataSource) : IIdempotencyStore
{
    public async Task<string?> GetResultAsync(string key, CancellationToken cancellationToken)
    {
        await using var cmd = dataSource.CreateCommand("SELECT result FROM cashflow.idempotency WHERE key = @key");
        cmd.Parameters.AddWithValue("key", key);
        return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
    }

    public async Task SaveResultAsync(string key, string result, CancellationToken cancellationToken)
    {
        // Same transaction as the command: if the command is rolled back, so is its idempotency record.
        await using var cmd = PostgresUnitOfWork.Current.Command("INSERT INTO cashflow.idempotency (key, result) VALUES (@key, @result)");
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
            await using var cmd = tx.Command("INSERT INTO cashflow.outbox (id, type, payload, occurred_at) VALUES (@id, @type, @payload, @at)");
            cmd.Parameters.AddWithValue("id", message.Id);
            cmd.Parameters.AddWithValue("type", message.Type);
            cmd.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = message.Payload });
            cmd.Parameters.AddWithValue("at", message.OccurredAt);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }
}
