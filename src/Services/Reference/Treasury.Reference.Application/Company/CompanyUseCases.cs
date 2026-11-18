using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Application.Company;

/// <summary>
/// The form of «مشخصات شرکت»: the name and the city of the legal address (required), and the official details of Q38, the address
/// and the legal address (each optional).
/// </summary>
/// <param name="PostalCode">Postal code of the legal address.</param>
/// <param name="CityId">City of the legal address (owner, 18 Nov 2026); it decides «خارج از مرکز» on a received cheque.</param>
public sealed record CompanyForm(
    string? Name, string? NationalId = null, string? EconomicCode = null, string? RegistrationNumber = null, string? Address = null,
    string? PostalCode = null, string? Phone = null, string? Website = null, string? Email = null, string? LegalAddress = null, Guid? CityId = null)
{
    public CompanyDetails Details => new(NationalId, EconomicCode, RegistrationNumber, Address, PostalCode, Phone, Website, Email, LegalAddress);
}

/// <param name="Name">Empty until the company name is entered.</param>
/// <param name="Version">0 until the company name is entered; sent back with every change.</param>
/// <param name="CityName">The city's name as it is in the list of cities now; cashflow-svc reads it for «خارج از مرکز».</param>
public sealed record CompanyView(
    string Name, bool HasLogo, long Version, string? NationalId = null, string? EconomicCode = null, string? RegistrationNumber = null,
    string? Address = null, string? PostalCode = null, string? Phone = null, string? Website = null, string? Email = null,
    string? LegalAddress = null, Guid? CityId = null, string? CityName = null)
{
    public static readonly CompanyView NotEntered = new(string.Empty, false, 0);

    /// <summary>False until the company is entered.</summary>
    public bool IsSet => Version > 0;

    public static CompanyView From(CompanyProfile c, string? cityName) =>
        new(c.Name, c.HasLogo, c.Version, c.Details.NationalId, c.Details.EconomicCode, c.Details.RegistrationNumber, c.Details.Address,
            c.Details.PostalCode, c.Details.Phone, c.Details.Website, c.Details.Email, c.Details.LegalAddress, c.CityId, cityName);
}

/// <summary>
/// «اطلاعات شرکت» (owner, 16 Nov 2026): the company's name and logo, entered in Basic Information and shown at the top of every
/// screen, and its official details for printed forms (Q37, Q38). Each write is one command in one transaction on the one profile
/// of the database. «مشخصات شرکت» (owner, 18 Nov 2026: address, legal address with its city and postal code) is the same page since
/// the two were joined (owner, 18 Nov 2026).
/// </summary>
public sealed class CompanyUseCases(
    IUnitOfWork unitOfWork, ICompanyRepository companies, ICatalogueRepository catalogue, IIdempotencyStore idempotency, CatalogueUseCases catalogueUseCases)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<CompanyView> GetAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (await companies.GetAsync(ct).ConfigureAwait(false) is not { } c)
            {
                return CompanyView.NotEntered;
            }

            var city = c.CityId is { } id ? await catalogue.GetAsync(id, ct).ConfigureAwait(false) : null;
            return CompanyView.From(c, city?.Name);
        }, cancellationToken);

    /// <summary>
    /// Enters the company's name and details for the first time (<paramref name="expectedVersion"/> 0) or corrects them. The city
    /// becomes "in use" in the list of cities, so it is not deleted.
    /// </summary>
    public async Task<Result<Guid>> SaveAsync(string key, CompanyForm form, long expectedVersion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);

        // Own command, before the profile's transaction (as a beneficiary's bank is marked).
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

            var company = await companies.GetAsync(ct).ConfigureAwait(false);
            if (company is null)
            {
                company = CompanyProfile.Register(Guid.NewGuid(), form.Name, city, form.Details);
                if (expectedVersion != 0)
                {
                    throw new ConcurrencyConflictException("The company profile was changed by someone else.");
                }

                await companies.AddAsync(company, ct).ConfigureAwait(false);
                return Result<Guid>.Ok(company.Id);
            }

            Check(company, expectedVersion);
            company.Edit(form.Name, city, form.Details);
            await companies.SaveAsync(company, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(company.Id);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stores the company's logo (PNG or JPEG, at most 1 MB), replacing an earlier one. The name comes first.</summary>
    public Task<Result<Guid>> AttachLogoAsync(string key, byte[] file, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(file);
            if (await companies.GetAsync(ct).ConfigureAwait(false) is not { } company)
            {
                return Result<Guid>.Fail("Company.NameRequired", "Enter the company name before its logo.");
            }

            Check(company, expectedVersion);
            company.AttachLogo(file);
            await companies.SaveAsync(company, ct, file).ConfigureAwait(false);
            return Result<Guid>.Ok(company.Id);
        }, cancellationToken);

    public Task<Result<Guid>> RemoveLogoAsync(string key, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            if (await companies.GetAsync(ct).ConfigureAwait(false) is not { } company)
            {
                return Result<Guid>.Fail("Company.NoLogo", "The company has no logo.");
            }

            Check(company, expectedVersion);
            company.RemoveLogo();
            await companies.SaveAsync(company, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(company.Id);
        }, cancellationToken);

    public Task<(string ContentType, byte[] Data)?> GetLogoAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => companies.GetLogoAsync(ct), cancellationToken);

    private static void Check(CompanyProfile company, long expectedVersion)
    {
        if (company.Version != expectedVersion)
        {
            throw new ConcurrencyConflictException("The company profile was changed by someone else.");
        }
    }

    private Task<Result<Guid>> RunAsync(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
