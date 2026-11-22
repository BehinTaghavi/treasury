using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Application.Abstractions;

/// <summary>The company's own particulars («مشخصات شرکت»): at most one per client database, with its logo.</summary>
public interface ICompanyProfileRepository
{
    /// <summary>The company's profile; null until it is entered for the first time.</summary>
    Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Stores the first profile. Throws <see cref="ConcurrencyConflictException"/> if someone else entered it first.</summary>
    Task AddAsync(CompanyProfile profile, CancellationToken cancellationToken);

    /// <summary>
    /// Saves changes and pending events; with <paramref name="logo"/> also replaces the logo, and a profile without a logo type loses
    /// its stored logo, all in the same transaction. Throws <see cref="ConcurrencyConflictException"/> if someone else saved first.
    /// </summary>
    Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken, byte[]? logo = null);

    Task<(string ContentType, byte[] Data)?> GetLogoAsync(CancellationToken cancellationToken);
}
