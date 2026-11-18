using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Application.Abstractions;

/// <summary>The profile of the company that owns this database («اطلاعات شرکت»): at most one, with its logo.</summary>
public interface ICompanyRepository
{
    /// <summary>The company's profile; null until its name is entered for the first time.</summary>
    Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Stores the first profile. Throws <see cref="ConcurrencyConflictException"/> if someone else entered it first.</summary>
    Task AddAsync(CompanyProfile company, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the profile; with <paramref name="logo"/> also replaces the logo, and a profile without a logo type loses its stored
    /// logo, all in the same transaction. Throws <see cref="ConcurrencyConflictException"/> if someone else saved first.
    /// </summary>
    Task SaveAsync(CompanyProfile company, CancellationToken cancellationToken, byte[]? logo = null);

    Task<(string ContentType, byte[] Data)?> GetLogoAsync(CancellationToken cancellationToken);
}
