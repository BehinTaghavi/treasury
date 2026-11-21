using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Application.Company;

/// <param name="CityId">City of the legal address.</param>
/// <param name="PostalCode">Postal code of the legal address.</param>
public sealed record CompanyProfileForm(string? Name, string? Address, string? LegalAddress, Guid? CityId, string? PostalCode);

/// <param name="IsSet">False until the profile is entered; the other fields are then empty and the version 0.</param>
/// <param name="CityName">The city's name as it is in the list of cities now.</param>
public sealed record CompanyProfileView(
    bool IsSet, string? Name, string? Address, string? LegalAddress, Guid? CityId, string? CityName, string? PostalCode, long Version)
{
    public static CompanyProfileView NotSet { get; } = new(false, null, null, null, null, null, null, 0);
}

/// <summary>
/// «مشخصات شرکت» of Basic Information (owner, 18 Nov 2026): name, address, and the legal address with its city and postal code. cashflow-svc reads the city to
/// tick «خارج از مرکز» on a received cheque whose bank is in another city.
/// </summary>
public sealed class CompanyUseCases(
    IUnitOfWork unitOfWork, ICompanyProfileRepository company, ICatalogueRepository catalogue, IIdempotencyStore idempotency, CatalogueUseCases catalogueUseCases)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<CompanyProfileView> GetAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var profile = await company.GetAsync(ct).ConfigureAwait(false);
            if (profile is null)
            {
                return CompanyProfileView.NotSet;
            }

            var city = await catalogue.GetAsync(profile.CityId, ct).ConfigureAwait(false);
            return new CompanyProfileView(
                true, profile.Name, profile.Address, profile.LegalAddress, profile.CityId, city?.Name, profile.PostalCode, profile.Version);
        }, cancellationToken);

    /// <summary>Enters the profile (expected version 0) or changes it. The city becomes "in use" in the list of cities.</summary>
    public async Task<Result<Guid>> SetAsync(string key, CompanyProfileForm form, long expectedVersion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);

        // Own command, before the profile's transaction (as a beneficiary's bank is marked): a city named by the profile is not deleted.
        if (form.CityId is { } cityId && cityId != Guid.Empty)
        {
            var entry = await catalogueUseCases.GetAsync(cityId, cancellationToken).ConfigureAwait(false);
            if (entry is null || entry.Kind != nameof(CatalogueKind.City))
            {
                return Result<Guid>.Fail("Company.CityNotFound", "The city is not in the list of cities.");
            }

            var marked = await catalogueUseCases.MarkInUseAsync(new MarkCatalogueItemInUseCommand($"in-use:{cityId}", cityId), cancellationToken).ConfigureAwait(false);
            if (!marked.IsSuccess)
            {
                return marked;
            }
        }

        return await _runner.RunAsync(key, async ct =>
        {
            CompanyCity? city = null;
            if (form.CityId is { } id && await catalogue.GetAsync(id, ct).ConfigureAwait(false) is { Kind: CatalogueKind.City } item)
            {
                city = new CompanyCity(item.Id, item.Name, item.IsActive);
            }

            var profile = await company.GetAsync(ct).ConfigureAwait(false);
            if ((profile?.Version ?? 0) != expectedVersion)
            {
                throw new ConcurrencyConflictException("The company's particulars were changed by someone else.");
            }

            if (profile is null)
            {
                await company.AddAsync(CompanyProfile.Register(form.Name, form.Address, form.LegalAddress, city, form.PostalCode), ct).ConfigureAwait(false);
            }
            else
            {
                profile.Change(form.Name, form.Address, form.LegalAddress, city, form.PostalCode);
                await company.SaveAsync(profile, ct).ConfigureAwait(false);
            }

            return Result<Guid>.Ok(CompanyProfile.TheId);
        }, id => id.ToString(), Guid.Parse, cancellationToken).ConfigureAwait(false);
    }
}
