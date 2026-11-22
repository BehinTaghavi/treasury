using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Infrastructure.Postgres;

// Written against db/postgres/037_reference_company_profile.sql and 038_reference_company_details_and_logo.sql.

public sealed class PostgresCompanyProfileRepository : ICompanyProfileRepository
{
    private const string Columns = "name, address, legal_address, city_id, postal_code, logo_type, national_id, economic_code, registration_number, phone, website, email";

    public async Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns}, version FROM reference.company_profile");
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        string? S(int i) => r.IsDBNull(i) ? null : r.GetString(i);
        return CompanyProfile.Restore(r.GetString(0), S(1), S(2), r.GetGuid(3), S(4), r.GetInt64(12), S(5), new CompanyDetails(S(6), S(7), S(8), S(9), S(10), S(11)));
    }

    public async Task AddAsync(CompanyProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var tx = PostgresUnitOfWork.Current;
        // The table holds one row (only_one): a second first entry at the same moment changes nothing and is a conflict.
        await using (var cmd = tx.Command(
            $"INSERT INTO reference.company_profile ({Columns}) " +
            "VALUES (@name, @address, @legal, @city, @postal, @logo, @national_id, @economic_code, @registration_number, @phone, @website, @email) " +
            "ON CONFLICT (only_one) DO NOTHING"))
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

    public async Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken, byte[]? logo = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE reference.company_profile SET name = @name, address = @address, legal_address = @legal, city_id = @city, postal_code = @postal, " +
            "logo_type = @logo, national_id = @national_id, economic_code = @economic_code, registration_number = @registration_number, " +
            "phone = @phone, website = @website, email = @email, version = version + 1, updated_at = now() " +
            "WHERE version = @expected"))
        {
            Bind(cmd, profile);
            cmd.Parameters.AddWithValue("expected", profile.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException("The company's particulars were changed by someone else.");
            }
        }

        if (!profile.HasLogo)
        {
            await using var del = tx.Command("DELETE FROM reference.company_profile_logo");
            await del.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (logo is not null)
        {
            await using var cmd = tx.Command(
                "INSERT INTO reference.company_profile_logo (data) VALUES (@data) ON CONFLICT (only_one) DO UPDATE SET data = excluded.data");
            cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlDbType.Bytea) { Value = logo });
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, profile.PendingEvents, cancellationToken).ConfigureAwait(false);
        profile.MarkSaved(profile.Version + 1);
    }

    public async Task<(string ContentType, byte[] Data)?> GetLogoAsync(CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT p.logo_type, l.data FROM reference.company_profile p JOIN reference.company_profile_logo l ON l.only_one = p.only_one");
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await r.ReadAsync(cancellationToken).ConfigureAwait(false) && !r.IsDBNull(0) ? (r.GetString(0), r.GetFieldValue<byte[]>(1)) : null;
    }

    private static void Bind(NpgsqlCommand cmd, CompanyProfile p)
    {
        cmd.Parameters.AddWithValue("name", p.Name);
        cmd.Parameters.AddWithValue("city", p.CityId);
        Text("address", p.Address);
        Text("legal", p.LegalAddress);
        Text("postal", p.PostalCode);
        Text("logo", p.LogoType);
        Text("national_id", p.Details.NationalId);
        Text("economic_code", p.Details.EconomicCode);
        Text("registration_number", p.Details.RegistrationNumber);
        Text("phone", p.Details.Phone);
        Text("website", p.Details.Website);
        Text("email", p.Details.Email);

        void Text(string name, string? value) => cmd.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Text) { Value = (object?)value ?? DBNull.Value });
    }
}
