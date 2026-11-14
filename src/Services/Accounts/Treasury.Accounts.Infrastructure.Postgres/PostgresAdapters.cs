using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Infrastructure.InMemory;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Infrastructure.Postgres;

// Written against Npgsql 10 and db/postgres/006_accounts.sql, 008_accounts_collection_priority.sql.
// Integration tests against PostgreSQL 17: tests/Treasury.Accounts.Infrastructure.Postgres.Tests.

public static class PostgresRegistration
{
    /// <summary>Replaces the in-memory adapters. The connection string must log in as the accounts_svc role.</summary>
    public static IServiceCollection AddAccountsPostgres(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IUnitOfWork, PostgresUnitOfWork>();
        services.AddSingleton<IBankAccountRepository, PostgresBankAccountRepository>();
        services.AddSingleton<ICashDeskRepository, PostgresCashDeskRepository>();
        services.AddSingleton<IPettyCashRepository, PostgresPettyCashRepository>();
        services.AddSingleton<IOpeningBalanceRepository, PostgresOpeningBalanceRepository>();
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
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.TableName == "petty_cash")
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw new DomainException("PettyCash.HolderTaken", "This holder already has a petty cash.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.TableName == "cash_desk")
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw new DomainException("CashDesk.NameTaken", "A cash desk with this name already exists.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.TableName == "bank_account")
        {
            // Two users saved at the same moment: the indexes decide, the second gets the normal message.
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw ex.ConstraintName == "bank_account_one_backup"
                ? new DomainException("BankAccount.MainAccountHasBackup", "This main account already has a backup account.")
                : new DomainException("BankAccount.NumberTaken", "This account number is already defined.");
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

public sealed class PostgresBankAccountRepository : IBankAccountRepository
{
    private const string Columns =
        "id, account_number, bank_id, branch_name, branch_code, city_id, opening_date, currency, main_account_id, ledger_account, " +
        "detail1, detail2, collection_account, bounced_account, collection_priority, is_facility, is_active, version, in_use";

    public async Task<BankAccount?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.bank_account WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return (await ReadAsync(cmd, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }

    public async Task<bool> NumberTakenAsync(string numberKey, Guid? exceptId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT EXISTS (SELECT 1 FROM accounts.bank_account WHERE number_key = @key AND id IS DISTINCT FROM @except)");
        cmd.Parameters.AddWithValue("key", numberKey);
        cmd.Parameters.Add(new NpgsqlParameter("except", NpgsqlDbType.Uuid) { Value = (object?)exceptId ?? DBNull.Value });
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<bool> HasBackupAsync(Guid mainAccountId, Guid? exceptId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT EXISTS (SELECT 1 FROM accounts.bank_account WHERE main_account_id = @main AND id IS DISTINCT FROM @except)");
        cmd.Parameters.AddWithValue("main", mainAccountId);
        cmd.Parameters.Add(new NpgsqlParameter("except", NpgsqlDbType.Uuid) { Value = (object?)exceptId ?? DBNull.Value });
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(BankAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "INSERT INTO accounts.bank_account (id, account_number, number_key, bank_id, branch_name, branch_code, city_id, opening_date, currency, " +
            "main_account_id, ledger_account, detail1, detail2, collection_account, bounced_account, collection_priority, is_facility, is_active, version) " +
            "VALUES (@id, @number, @key, @bank, @branch, @branchCode, @city, @opened, @currency, @main, @ledger, @d1, @d2, @collection, @bounced, " +
            "@priority, @facility, @active, 1)"))
        {
            Bind(cmd, account);
            cmd.Parameters.Add(Nullable("main", NpgsqlDbType.Uuid, account.MainAccountId));
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, account.PendingEvents, cancellationToken).ConfigureAwait(false);
        account.MarkSaved(1);
    }

    public async Task SaveAsync(BankAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE accounts.bank_account SET account_number = @number, number_key = @key, bank_id = @bank, branch_name = @branch, " +
            "branch_code = @branchCode, city_id = @city, opening_date = @opened, currency = @currency, ledger_account = @ledger, detail1 = @d1, " +
            "detail2 = @d2, collection_account = @collection, bounced_account = @bounced, collection_priority = @priority, is_facility = @facility, " +
            "is_active = @active, in_use = @in_use, version = version + 1, updated_at = now() WHERE id = @id AND version = @expected"))
        {
            Bind(cmd, account);
            cmd.Parameters.AddWithValue("expected", account.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Bank account {account.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, account.PendingEvents, cancellationToken).ConfigureAwait(false);
        account.MarkSaved(account.Version + 1);
    }

    public async Task DeleteAsync(BankAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        var tx = PostgresUnitOfWork.Current;
        // "NOT in_use" and "no backup": a cheque book or a backup saved since the account was loaded refuses the delete.
        await using (var cmd = tx.Command(
            "DELETE FROM accounts.bank_account b WHERE id = @id AND version = @expected AND NOT in_use " +
            "AND NOT EXISTS (SELECT 1 FROM accounts.bank_account x WHERE x.main_account_id = b.id) " +
            "AND NOT EXISTS (SELECT 1 FROM accounts.opening_balance o WHERE o.account_kind = 'BankAccount' AND o.account_id = b.id)"))
        {
            cmd.Parameters.AddWithValue("id", account.Id);
            cmd.Parameters.AddWithValue("expected", account.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Bank account {account.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, account.PendingEvents, cancellationToken).ConfigureAwait(false);
        account.MarkSaved(account.Version);
    }

    public async Task<IReadOnlyList<BankAccount>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.bank_account WHERE @all OR is_active");
        cmd.Parameters.AddWithValue("all", includeInactive);
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    private static void Bind(NpgsqlCommand cmd, BankAccount a)
    {
        var d = a.Data;
        cmd.Parameters.AddWithValue("id", a.Id);
        cmd.Parameters.AddWithValue("number", d.AccountNumber);
        cmd.Parameters.AddWithValue("key", a.NumberKey);
        cmd.Parameters.AddWithValue("bank", d.BankId);
        cmd.Parameters.Add(Nullable("branch", NpgsqlDbType.Text, d.BranchName));
        cmd.Parameters.Add(Nullable("branchCode", NpgsqlDbType.Text, d.BranchCode));
        cmd.Parameters.Add(Nullable("city", NpgsqlDbType.Uuid, d.CityId));
        cmd.Parameters.Add(Nullable("opened", NpgsqlDbType.Date, d.OpeningDate?.Value));
        cmd.Parameters.Add(new NpgsqlParameter("currency", NpgsqlDbType.Char) { Value = d.CurrencyCode });
        cmd.Parameters.AddWithValue("ledger", d.Ledger.AccountCode);
        cmd.Parameters.Add(Nullable("d1", NpgsqlDbType.Text, d.Ledger.Detail1Code));
        cmd.Parameters.Add(Nullable("d2", NpgsqlDbType.Text, d.Ledger.Detail2Code));
        cmd.Parameters.Add(Nullable("collection", NpgsqlDbType.Text, d.Ledger.CollectionAccountCode));
        cmd.Parameters.Add(Nullable("bounced", NpgsqlDbType.Text, d.Ledger.BouncedAccountCode));
        cmd.Parameters.Add(new NpgsqlParameter("priority", NpgsqlTypes.NpgsqlDbType.Smallint) { Value = d.CollectionPriority is { } p ? (object)(short)p : DBNull.Value });
        cmd.Parameters.AddWithValue("in_use", a.IsInUse);
        cmd.Parameters.AddWithValue("facility", d.IsFacility);
        cmd.Parameters.AddWithValue("active", a.IsActive);
    }

    private static NpgsqlParameter Nullable(string name, NpgsqlDbType type, object? value) => new(name, type) { Value = value ?? DBNull.Value };

    private static async Task<List<BankAccount>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<BankAccount>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            string? S(int i) => r.IsDBNull(i) ? null : r.GetString(i);
            var data = new BankAccountData(
                r.GetString(1), r.GetGuid(2), S(3), S(4), r.IsDBNull(5) ? null : r.GetGuid(5),
                r.IsDBNull(6) ? null : new FiscalDate(r.GetFieldValue<DateOnly>(6)), r.GetString(7).Trim(),
                new BankLedger(r.GetString(9), S(10), S(11), S(12), S(13)), r.IsDBNull(14) ? null : r.GetInt16(14), r.GetBoolean(15));
            list.Add(BankAccount.Restore(r.GetGuid(0), data, r.IsDBNull(8) ? null : r.GetGuid(8), r.GetBoolean(16), r.GetInt64(17), r.GetBoolean(18)));
        }

        return list;
    }
}

public sealed class PostgresIdempotencyStore(NpgsqlDataSource dataSource) : IIdempotencyStore
{
    public async Task<string?> GetResultAsync(string key, CancellationToken cancellationToken)
    {
        await using var cmd = dataSource.CreateCommand("SELECT result FROM accounts.idempotency WHERE key = @key");
        cmd.Parameters.AddWithValue("key", key);
        return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
    }

    public async Task SaveResultAsync(string key, string result, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("INSERT INTO accounts.idempotency (key, result) VALUES (@key, @result)");
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
            await using var cmd = tx.Command("INSERT INTO accounts.outbox (id, type, payload, occurred_at) VALUES (@id, @type, @payload, @at)");
            cmd.Parameters.AddWithValue("id", message.Id);
            cmd.Parameters.AddWithValue("type", message.Type);
            cmd.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = message.Payload });
            cmd.Parameters.AddWithValue("at", message.OccurredAt);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }
}
