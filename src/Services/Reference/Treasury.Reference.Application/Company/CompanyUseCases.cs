using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Application.Company;

/// <param name="Name">Empty until the company name is entered.</param>
/// <param name="Version">0 until the company name is entered; sent back with every change.</param>
public sealed record CompanyView(string Name, bool HasLogo, long Version)
{
    public static readonly CompanyView NotEntered = new(string.Empty, false, 0);

    public static CompanyView From(CompanyProfile c) => new(c.Name, c.HasLogo, c.Version);
}

/// <summary>
/// «اطلاعات شرکت» (owner, 16 Nov 2026): the company's name and logo, entered in Basic Information and shown at the top of every
/// screen. Each write is one command in one transaction on the one profile of the database.
/// </summary>
public sealed class CompanyUseCases(IUnitOfWork unitOfWork, ICompanyRepository companies, IIdempotencyStore idempotency)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<CompanyView> GetAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct => await companies.GetAsync(ct).ConfigureAwait(false) is { } c ? CompanyView.From(c) : CompanyView.NotEntered, cancellationToken);

    /// <summary>Enters the company name for the first time (<paramref name="expectedVersion"/> 0) or corrects it.</summary>
    public Task<Result<Guid>> SaveNameAsync(string key, string? name, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            var company = await companies.GetAsync(ct).ConfigureAwait(false);
            if (company is null)
            {
                company = CompanyProfile.Register(Guid.NewGuid(), name);
                if (expectedVersion != 0)
                {
                    throw new ConcurrencyConflictException("The company profile was changed by someone else.");
                }

                await companies.AddAsync(company, ct).ConfigureAwait(false);
                return Result<Guid>.Ok(company.Id);
            }

            Check(company, expectedVersion);
            company.Rename(name);
            await companies.SaveAsync(company, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(company.Id);
        }, cancellationToken);

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
