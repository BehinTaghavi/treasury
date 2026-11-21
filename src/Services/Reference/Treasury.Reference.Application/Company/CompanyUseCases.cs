using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Application.Company;

/// <summary>The form of «مشخصات شرکت»: the name and the city are required; the official details (owner, 16 Nov 2026) are each optional.</summary>
/// <param name="CityId">City of the legal address.</param>
/// <param name="PostalCode">Postal code of the legal address.</param>
public sealed record CompanyProfileForm(
    string? Name, string? Address, string? LegalAddress, Guid? CityId, string? PostalCode, string? NationalId = null, string? EconomicCode = null,
    string? RegistrationNumber = null, string? Phone = null, string? Website = null, string? Email = null)
{
    public CompanyDetails Details => new(NationalId, EconomicCode, RegistrationNumber, Phone, Website, Email);
}

/// <param name="IsSet">False until the profile is entered; the other fields are then empty and the version 0.</param>
/// <param name="CityName">The city's name as it is in the list of cities now.</param>
/// <param name="Version">0 until the profile is entered; sent back with every change.</param>
public sealed record CompanyProfileView(
    bool IsSet, string? Name, string? Address, string? LegalAddress, Guid? CityId, string? CityName, string? PostalCode, long Version,
    bool HasLogo = false, string? NationalId = null, string? EconomicCode = null, string? RegistrationNumber = null, string? Phone = null,
    string? Website = null, string? Email = null)
{
    public static CompanyProfileView NotSet { get; } = new(false, null, null, null, null, null, null, 0);
}

/// <summary>
/// «مشخصات شرکت» of Basic Information. Owner, 16 Nov 2026: the company's name and logo, shown at the top of every screen, and its
/// official details for printed forms. Owner, 18 Nov 2026: address, and the legal address with its city and postal code; the receipt
/// screen reads the city to tick «خارج از مرکز» on a received cheque whose bank is in another city. Each write is one command in one
/// transaction on the one profile of the database.
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
            var d = profile.Details;
            return new CompanyProfileView(
                true, profile.Name, profile.Address, profile.LegalAddress, profile.CityId, city?.Name, profile.PostalCode, profile.Version,
                profile.HasLogo, d.NationalId, d.EconomicCode, d.RegistrationNumber, d.Phone, d.Website, d.Email);
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

        return await RunAsync(key, async ct =>
        {
            CompanyCity? city = null;
            if (form.CityId is { } id && await catalogue.GetAsync(id, ct).ConfigureAwait(false) is { Kind: CatalogueKind.City } item)
            {
                city = new CompanyCity(item.Id, item.Name, item.IsActive);
            }

            var profile = await company.GetAsync(ct).ConfigureAwait(false);
            Check(profile, expectedVersion);
            if (profile is null)
            {
                await company.AddAsync(CompanyProfile.Register(form.Name, form.Address, form.LegalAddress, city, form.PostalCode, form.Details), ct).ConfigureAwait(false);
            }
            else
            {
                profile.Change(form.Name, form.Address, form.LegalAddress, city, form.PostalCode, form.Details);
                await company.SaveAsync(profile, ct).ConfigureAwait(false);
            }

            return Result<Guid>.Ok(CompanyProfile.TheId);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stores the company's logo (PNG or JPEG, at most 1 MB), replacing an earlier one. The profile comes first.</summary>
    public Task<Result<Guid>> AttachLogoAsync(string key, byte[] file, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(file);
            if (await company.GetAsync(ct).ConfigureAwait(false) is not { } profile)
            {
                return Result<Guid>.Fail("Company.NameRequired", "Enter the company's particulars before its logo.");
            }

            Check(profile, expectedVersion);
            profile.AttachLogo(file);
            await company.SaveAsync(profile, ct, file).ConfigureAwait(false);
            return Result<Guid>.Ok(CompanyProfile.TheId);
        }, cancellationToken);

    public Task<Result<Guid>> RemoveLogoAsync(string key, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            if (await company.GetAsync(ct).ConfigureAwait(false) is not { } profile)
            {
                return Result<Guid>.Fail("Company.NoLogo", "The company has no logo.");
            }

            Check(profile, expectedVersion);
            profile.RemoveLogo();
            await company.SaveAsync(profile, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(CompanyProfile.TheId);
        }, cancellationToken);

    public Task<(string ContentType, byte[] Data)?> GetLogoAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => company.GetLogoAsync(ct), cancellationToken);

    private static void Check(CompanyProfile? profile, long expectedVersion)
    {
        if ((profile?.Version ?? 0) != expectedVersion)
        {
            throw new ConcurrencyConflictException("The company's particulars were changed by someone else.");
        }
    }

    private Task<Result<Guid>> RunAsync(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
