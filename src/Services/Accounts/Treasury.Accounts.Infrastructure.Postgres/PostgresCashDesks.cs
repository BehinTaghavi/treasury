using Npgsql;
using NpgsqlTypes;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.CashDesks;
using Treasury.BuildingBlocks.Application;

namespace Treasury.Accounts.Infrastructure.Postgres;

// Written against db/postgres/010_accounts_cash_desks.sql.

public sealed class PostgresCashDeskRepository : ICashDeskRepository
{
    private const string Columns = "id, name, ledger_account, holder_detail, detail2, currency, is_active, version";

    public async Task<CashDesk?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.cash_desk WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return (await ReadAsync(cmd, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }

    public async Task<bool> NameTakenAsync(string nameKey, Guid? exceptId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT EXISTS (SELECT 1 FROM accounts.cash_desk WHERE name_key = @key AND id IS DISTINCT FROM @except)");
        cmd.Parameters.AddWithValue("key", nameKey);
        cmd.Parameters.Add(new NpgsqlParameter("except", NpgsqlDbType.Uuid) { Value = (object?)exceptId ?? DBNull.Value });
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(CashDesk desk, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(desk);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "INSERT INTO accounts.cash_desk (id, name, name_key, ledger_account, holder_detail, detail2, currency, is_active, version) " +
            "VALUES (@id, @name, @key, @ledger, @holder, @d2, @currency, @active, 1)"))
        {
            Parameters(cmd, desk);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, desk.PendingEvents, cancellationToken).ConfigureAwait(false);
        desk.MarkSaved(1);
    }

    public async Task SaveAsync(CashDesk desk, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(desk);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE accounts.cash_desk SET name = @name, name_key = @key, ledger_account = @ledger, holder_detail = @holder, detail2 = @d2, " +
            "currency = @currency, is_active = @active, version = version + 1, updated_at = now() WHERE id = @id AND version = @expected"))
        {
            Parameters(cmd, desk);
            cmd.Parameters.AddWithValue("expected", desk.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Cash desk {desk.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, desk.PendingEvents, cancellationToken).ConfigureAwait(false);
        desk.MarkSaved(desk.Version + 1);
    }

    public async Task<IReadOnlyList<CashDesk>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.cash_desk WHERE @all OR is_active");
        cmd.Parameters.AddWithValue("all", includeInactive);
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    private static void Parameters(NpgsqlCommand cmd, CashDesk d)
    {
        cmd.Parameters.AddWithValue("id", d.Id);
        cmd.Parameters.AddWithValue("name", d.Data.Name);
        cmd.Parameters.AddWithValue("key", d.NameKey);
        cmd.Parameters.AddWithValue("ledger", d.Data.LedgerAccountCode);
        cmd.Parameters.AddWithValue("holder", d.Data.HolderDetailCode);
        cmd.Parameters.Add(new NpgsqlParameter("d2", NpgsqlDbType.Text) { Value = (object?)d.Data.Detail2Code ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("currency", NpgsqlDbType.Char) { Value = d.Data.CurrencyCode });
        cmd.Parameters.AddWithValue("active", d.IsActive);
    }

    private static async Task<List<CashDesk>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<CashDesk>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            var data = new CashDeskData(r.GetString(1), r.GetString(2), r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4), r.GetString(5).Trim());
            list.Add(CashDesk.Restore(r.GetGuid(0), data, r.GetBoolean(6), r.GetInt64(7)));
        }

        return list;
    }
}
