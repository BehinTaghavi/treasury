using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Infrastructure.InMemory;

public sealed record CompanySnapshot(Guid Id, string Name, string? LogoType, byte[]? Logo, long Version, CompanyDetails Details, Guid? CityId = null);

/// <summary>In-memory company profile (local runs, tests): one row, same version checks and outbox as the database.</summary>
public sealed class InMemoryCompanyRepository(InMemoryReferenceDatabase db) : ICompanyRepository
{
    private const string Table = "company";

    public Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Stored() is { } s ? CompanyProfile.Restore(s.Id, s.Name, s.LogoType, s.Version, s.Details, s.CityId) : null);

    public Task AddAsync(CompanyProfile company, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(company);
        if (Stored() is not null)
        {
            throw new ConcurrencyConflictException("The company profile was entered by someone else.");
        }

        return Write(company, 1, null);
    }

    public Task SaveAsync(CompanyProfile company, CancellationToken cancellationToken, byte[]? logo = null)
    {
        ArgumentNullException.ThrowIfNull(company);
        if (Stored() is not { } stored || stored.Id != company.Id || stored.Version != company.Version)
        {
            throw new ConcurrencyConflictException("The company profile was changed by someone else.");
        }

        return Write(company, company.Version + 1, company.HasLogo ? logo ?? stored.Logo : null);
    }

    public Task<(string ContentType, byte[] Data)?> GetLogoAsync(CancellationToken cancellationToken) =>
        Task.FromResult<(string, byte[])?>(Stored() is { LogoType: { } type, Logo: { } data } ? (type, data) : null);

    /// <summary>The committed row overlaid with this transaction's own change.</summary>
    private CompanySnapshot? Stored()
    {
        if (InMemoryUnitOfWork.Pending.Tables.TryGetValue(Table, out var mine) && mine.Count > 0)
        {
            return mine.Values.OfType<CompanySnapshot>().SingleOrDefault();
        }

        lock (db.Gate)
        {
            return db.Tables.TryGetValue(Table, out var rows) ? rows.Values.OfType<CompanySnapshot>().SingleOrDefault() : null;
        }
    }

    private static Task Write(CompanyProfile company, long newVersion, byte[]? logo)
    {
        var tables = InMemoryUnitOfWork.Pending.Tables;
        if (!tables.TryGetValue(Table, out var rows))
        {
            rows = [];
            tables[Table] = rows;
        }

        rows[company.Id] = new CompanySnapshot(company.Id, company.Name, company.LogoType, logo, newVersion, company.Details, company.CityId);
        InMemoryUnitOfWork.Pending.Outbox.AddRange(company.PendingEvents.Select(OutboxMessage.From));
        company.MarkSaved(newVersion);
        return Task.CompletedTask;
    }
}
