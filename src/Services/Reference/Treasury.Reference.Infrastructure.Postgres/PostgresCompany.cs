using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Infrastructure.Postgres;

// Written against db/postgres/037_reference_company_profile.sql.

public sealed class PostgresCompanyProfileRepository : ICompanyProfileRepository
{
    public async Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT name, address, legal_address, city_id, postal_code, version FROM reference.company_profile");
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await r.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? CompanyProfile.Restore(
                r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.GetGuid(3),
                r.IsDBNull(4) ? null : r.GetString(4), r.GetInt64(5))
            : null;
    }

    public async Task AddAsync(CompanyProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "INSERT INTO reference.company_profile (name, address, legal_address, city_id, postal_code) " +
            "VALUES (@name, @address, @legal, @city, @postal) ON CONFLICT (only_one) DO NOTHING"))
        {
            Bind(cmd, profile);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException("The company's particulars were entered by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, profile.PendingEvents, cancellationToken).ConfigureAwait(false);
        profile.MarkSaved(1);
    }

    public async Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE reference.company_profile SET name = @name, address = @address, legal_address = @legal, city_id = @city, postal_code = @postal, " +
            "version = version + 1, updated_at = now() " +
            "WHERE version = @expected"))
        {
            Bind(cmd, profile);
            cmd.Parameters.AddWithValue("expected", profile.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException("The company's particulars were changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, profile.PendingEvents, cancellationToken).ConfigureAwait(false);
        profile.MarkSaved(profile.Version + 1);
    }

    private static void Bind(NpgsqlCommand cmd, CompanyProfile p)
    {
        cmd.Parameters.AddWithValue("name", p.Name);
        cmd.Parameters.Add(new NpgsqlParameter("address", NpgsqlDbType.Text) { Value = (object?)p.Address ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("legal", NpgsqlDbType.Text) { Value = (object?)p.LegalAddress ?? DBNull.Value });
        cmd.Parameters.AddWithValue("city", p.CityId);
        cmd.Parameters.Add(new NpgsqlParameter("postal", NpgsqlDbType.Text) { Value = (object?)p.PostalCode ?? DBNull.Value });
    }
}
