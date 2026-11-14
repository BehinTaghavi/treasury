using Npgsql;
using NpgsqlTypes;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.OpeningBalances;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Infrastructure.Postgres;

// Written against db/postgres/024_accounts_opening_balances.sql.

public sealed class PostgresOpeningBalanceRepository : IOpeningBalanceRepository
{
    private const string Columns = "id, account_kind, account_id, fiscal_year, balance_date, amount, rial_equivalent, version";

    public async Task<OpeningBalance?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.opening_balance WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return (await ReadAsync(cmd, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }

    public async Task<OpeningBalance?> FindAsync(BalanceAccountKind kind, Guid accountId, int fiscalYear, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT {Columns} FROM accounts.opening_balance WHERE account_kind = @kind AND account_id = @account AND fiscal_year = @year FOR UPDATE");
        cmd.Parameters.AddWithValue("kind", kind.ToString());
        cmd.Parameters.AddWithValue("account", accountId);
        cmd.Parameters.AddWithValue("year", (short)fiscalYear);
        return (await ReadAsync(cmd, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<OpeningBalance>> ListUpToAsync(int fiscalYear, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.opening_balance WHERE fiscal_year <= @year");
        cmd.Parameters.AddWithValue("year", (short)fiscalYear);
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AnyForAccountAsync(BalanceAccountKind kind, Guid accountId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT EXISTS (SELECT 1 FROM accounts.opening_balance WHERE account_kind = @kind AND account_id = @account)");
        cmd.Parameters.AddWithValue("kind", kind.ToString());
        cmd.Parameters.AddWithValue("account", accountId);
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(OpeningBalance balance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(balance);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "INSERT INTO accounts.opening_balance (id, account_kind, account_id, fiscal_year, balance_date, amount, rial_equivalent, version) " +
            "VALUES (@id, @kind, @account, @year, @date, @amount, @rial, 1) ON CONFLICT (account_kind, account_id, fiscal_year) DO NOTHING"))
        {
            Bind(cmd, balance);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException("An opening balance for this account and year was saved by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, balance.PendingEvents, cancellationToken).ConfigureAwait(false);
        balance.MarkSaved(1);
    }

    public async Task SaveAsync(OpeningBalance balance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(balance);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE accounts.opening_balance SET balance_date = @date, amount = @amount, rial_equivalent = @rial, version = version + 1, updated_at = now() " +
            "WHERE id = @id AND version = @expected"))
        {
            Bind(cmd, balance);
            cmd.Parameters.AddWithValue("expected", balance.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Opening balance {balance.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, balance.PendingEvents, cancellationToken).ConfigureAwait(false);
        balance.MarkSaved(balance.Version + 1);
    }

    public async Task DeleteAsync(OpeningBalance balance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(balance);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command("DELETE FROM accounts.opening_balance WHERE id = @id AND version = @expected"))
        {
            cmd.Parameters.AddWithValue("id", balance.Id);
            cmd.Parameters.AddWithValue("expected", balance.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Opening balance {balance.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, balance.PendingEvents, cancellationToken).ConfigureAwait(false);
        balance.MarkSaved(balance.Version);
    }

    private static void Bind(NpgsqlCommand cmd, OpeningBalance b)
    {
        cmd.Parameters.AddWithValue("id", b.Id);
        cmd.Parameters.AddWithValue("kind", b.AccountKind.ToString());
        cmd.Parameters.AddWithValue("account", b.AccountId);
        cmd.Parameters.AddWithValue("year", (short)b.FiscalYear);
        cmd.Parameters.Add(new NpgsqlParameter("date", NpgsqlDbType.Date) { Value = b.Date.Value });
        cmd.Parameters.AddWithValue("amount", b.Amount);
        cmd.Parameters.Add(new NpgsqlParameter("rial", NpgsqlDbType.Numeric) { Value = (object?)b.RialEquivalent ?? DBNull.Value });
    }

    private static async Task<List<OpeningBalance>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<OpeningBalance>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(OpeningBalance.Restore(r.GetGuid(0), Enum.Parse<BalanceAccountKind>(r.GetString(1)), r.GetGuid(2), r.GetInt16(3),
                new FiscalDate(r.GetFieldValue<DateOnly>(4)), r.GetDecimal(5), r.IsDBNull(6) ? null : r.GetDecimal(6), r.GetInt64(7)));
        }

        return list;
    }
}
