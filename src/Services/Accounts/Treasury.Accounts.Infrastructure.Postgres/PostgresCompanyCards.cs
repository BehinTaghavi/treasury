using Npgsql;
using NpgsqlTypes;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.CompanyCards;
using Treasury.BuildingBlocks.Application;

namespace Treasury.Accounts.Infrastructure.Postgres;

// Written against db/postgres/044_accounts_company_cards.sql.

public sealed class PostgresCompanyCardRepository : ICompanyCardRepository
{
    private const string Columns = "id, card_number, holder_name, bank_id, ledger_account, detail1, is_active, version";

    public async Task<CompanyCard?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.company_card WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return (await ReadAsync(cmd, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }

    public async Task<bool> NumberTakenAsync(string cardNumber, Guid? exceptId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT EXISTS (SELECT 1 FROM accounts.company_card WHERE card_number = @number AND id IS DISTINCT FROM @except)");
        cmd.Parameters.AddWithValue("number", cardNumber);
        cmd.Parameters.Add(new NpgsqlParameter("except", NpgsqlDbType.Uuid) { Value = (object?)exceptId ?? DBNull.Value });
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(CompanyCard card, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(card);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "INSERT INTO accounts.company_card (id, card_number, holder_name, bank_id, ledger_account, detail1, is_active, version) " +
            "VALUES (@id, @number, @holder, @bank, @ledger, @detail, @active, 1)"))
        {
            Parameters(cmd, card);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, card.PendingEvents, cancellationToken).ConfigureAwait(false);
        card.MarkSaved(1);
    }

    public async Task SaveAsync(CompanyCard card, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(card);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE accounts.company_card SET card_number = @number, holder_name = @holder, bank_id = @bank, ledger_account = @ledger, detail1 = @detail, " +
            "is_active = @active, version = version + 1, updated_at = now() WHERE id = @id AND version = @expected"))
        {
            Parameters(cmd, card);
            cmd.Parameters.AddWithValue("expected", card.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Company card {card.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, card.PendingEvents, cancellationToken).ConfigureAwait(false);
        card.MarkSaved(card.Version + 1);
    }

    public async Task<IReadOnlyList<CompanyCard>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM accounts.company_card WHERE @all OR is_active");
        cmd.Parameters.AddWithValue("all", includeInactive);
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    private static void Parameters(NpgsqlCommand cmd, CompanyCard c)
    {
        cmd.Parameters.AddWithValue("id", c.Id);
        cmd.Parameters.AddWithValue("number", c.Data.CardNumber);
        cmd.Parameters.AddWithValue("holder", c.Data.HolderName);
        cmd.Parameters.AddWithValue("bank", c.Data.BankId);
        cmd.Parameters.AddWithValue("ledger", c.Data.LedgerAccountCode);
        cmd.Parameters.Add(new NpgsqlParameter("detail", NpgsqlDbType.Text) { Value = (object?)c.Data.DetailCode ?? DBNull.Value });
        cmd.Parameters.AddWithValue("active", c.IsActive);
    }

    private static async Task<List<CompanyCard>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<CompanyCard>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            var data = new CompanyCardData(r.GetString(1), r.GetString(2), r.GetGuid(3), r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5));
            list.Add(CompanyCard.Restore(r.GetGuid(0), data, r.GetBoolean(6), r.GetInt64(7)));
        }

        return list;
    }
}
