using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Infrastructure.Postgres;

// Written against db/postgres/033_reference_company.sql and 034_reference_company_details.sql.

public sealed class PostgresCompanyRepository : ICompanyRepository
{
    private const string Details = "national_id, economic_code, registration_number, address, postal_code, phone, website, email";

    public async Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT id, name, logo_type, version, {Details} FROM reference.company");
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        string? S(int i) => r.IsDBNull(i) ? null : r.GetString(i);
        return CompanyProfile.Restore(r.GetGuid(0), r.GetString(1), S(2), r.GetInt64(3), new CompanyDetails(S(4), S(5), S(6), S(7), S(8), S(9), S(10), S(11)));
    }

    public async Task AddAsync(CompanyProfile company, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(company);
        var tx = PostgresUnitOfWork.Current;
        // The table holds one row (company_single): a second first entry at the same moment changes nothing and is a conflict.
        await using (var cmd = tx.Command(
            $"INSERT INTO reference.company (id, name, logo_type, {Details}) " +
            "VALUES (@id, @name, @logo, @national_id, @economic_code, @registration_number, @address, @postal_code, @phone, @website, @email) ON CONFLICT DO NOTHING"))
        {
            Parameters(cmd, company);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException("The company profile was entered by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, company.PendingEvents, cancellationToken).ConfigureAwait(false);
        company.MarkSaved(1);
    }

    public async Task SaveAsync(CompanyProfile company, CancellationToken cancellationToken, byte[]? logo = null)
    {
        ArgumentNullException.ThrowIfNull(company);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE reference.company SET name = @name, logo_type = @logo, national_id = @national_id, economic_code = @economic_code, " +
            "registration_number = @registration_number, address = @address, postal_code = @postal_code, phone = @phone, website = @website, email = @email, " +
            "version = version + 1, updated_at = now() WHERE id = @id AND version = @expected"))
        {
            Parameters(cmd, company);
            cmd.Parameters.AddWithValue("expected", company.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException("The company profile was changed by someone else.");
            }
        }

        if (!company.HasLogo)
        {
            await using var del = tx.Command("DELETE FROM reference.company_logo WHERE company_id = @id");
            del.Parameters.AddWithValue("id", company.Id);
            await del.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (logo is not null)
        {
            await using var cmd = tx.Command(
                "INSERT INTO reference.company_logo (company_id, data) VALUES (@id, @data) ON CONFLICT (company_id) DO UPDATE SET data = excluded.data");
            cmd.Parameters.AddWithValue("id", company.Id);
            cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlDbType.Bytea) { Value = logo });
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, company.PendingEvents, cancellationToken).ConfigureAwait(false);
        company.MarkSaved(company.Version + 1);
    }

    public async Task<(string ContentType, byte[] Data)?> GetLogoAsync(CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT c.logo_type, l.data FROM reference.company c JOIN reference.company_logo l ON l.company_id = c.id");
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await r.ReadAsync(cancellationToken).ConfigureAwait(false) && !r.IsDBNull(0) ? (r.GetString(0), r.GetFieldValue<byte[]>(1)) : null;
    }

    private static void Parameters(NpgsqlCommand cmd, CompanyProfile c)
    {
        cmd.Parameters.AddWithValue("id", c.Id);
        cmd.Parameters.AddWithValue("name", c.Name);
        Text("logo", c.LogoType);
        Text("national_id", c.Details.NationalId);
        Text("economic_code", c.Details.EconomicCode);
        Text("registration_number", c.Details.RegistrationNumber);
        Text("address", c.Details.Address);
        Text("postal_code", c.Details.PostalCode);
        Text("phone", c.Details.Phone);
        Text("website", c.Details.Website);
        Text("email", c.Details.Email);

        void Text(string name, string? value) => cmd.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Text) { Value = (object?)value ?? DBNull.Value });
    }
}
