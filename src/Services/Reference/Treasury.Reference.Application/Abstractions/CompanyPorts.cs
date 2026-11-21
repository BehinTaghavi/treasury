using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Application.Abstractions;

/// <summary>The company's own particulars («مشخصات شرکت»): at most one per client database.</summary>
public interface ICompanyProfileRepository
{
    Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken);

    Task AddAsync(CompanyProfile profile, CancellationToken cancellationToken);

    /// <summary>Saves changes and pending events. Throws ConcurrencyConflictException if someone else saved first.</summary>
    Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken);
}
