using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Infrastructure.InMemory;

public sealed record CompanyProfileSnapshot(
    string Name, string? Address, string? LegalAddress, Guid CityId, string? PostalCode, long Version, string? LogoType, byte[]? Logo,
    CompanyDetails Details);

/// <summary>In-memory company profile (local runs, tests): one row, with the same version check and outbox as the database.</summary>
public sealed class InMemoryCompanyProfileRepository(InMemoryReferenceDatabase db) : ICompanyProfileRepository
{
    private const string Table = "company";

    public Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Row() is { } s
            ? CompanyProfile.Restore(s.Name, s.Address, s.LegalAddress, s.CityId, s.PostalCode, s.Version, s.LogoType, s.Details)
            : null);

    public Task AddAsync(CompanyProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (Row() is not null)
        {
            throw new ConcurrencyConflictException("The company's particulars were entered by someone else.");
        }

        return Write(profile, 1, null);
    }

    public Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken, byte[]? logo = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (Row() is not { } stored || stored.Version != profile.Version)
        {
            throw new ConcurrencyConflictException("The company's particulars were changed by someone else.");
        }

        return Write(profile, profile.Version + 1, profile.HasLogo ? logo ?? stored.Logo : null);
    }

    public Task<(string ContentType, byte[] Data)?> GetLogoAsync(CancellationToken cancellationToken) =>
        Task.FromResult<(string, byte[])?>(Row() is { LogoType: { } type, Logo: { } data } ? (type, data) : null);

    /// <summary>The committed row overlaid with this transaction's own change.</summary>
    private CompanyProfileSnapshot? Row()
    {
        var pending = InMemoryUnitOfWork.Pending;
        if (pending.Tables.TryGetValue(Table, out var mine) && mine.TryGetValue(CompanyProfile.TheId, out var changed))
        {
            return (CompanyProfileSnapshot?)changed;
        }

        lock (db.Gate)
        {
            return db.Tables.TryGetValue(Table, out var rows) && rows.TryGetValue(CompanyProfile.TheId, out var row) ? (CompanyProfileSnapshot)row : null;
        }
    }

    private static Task Write(CompanyProfile profile, long newVersion, byte[]? logo)
    {
        var pending = InMemoryUnitOfWork.Pending;
        if (!pending.Tables.TryGetValue(Table, out var rows))
        {
            rows = [];
            pending.Tables[Table] = rows;
        }

        rows[CompanyProfile.TheId] = new CompanyProfileSnapshot(
            profile.Name, profile.Address, profile.LegalAddress, profile.CityId, profile.PostalCode, newVersion, profile.LogoType, logo, profile.Details);
        pending.Outbox.AddRange(profile.PendingEvents.Select(OutboxMessage.From));
        profile.MarkSaved(newVersion);
        return Task.CompletedTask;
    }
}
