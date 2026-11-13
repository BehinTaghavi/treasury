using System.Globalization;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Organization;

namespace Treasury.Reference.Application.Organization;

public sealed record DepartmentForm(int Number, string Name, IReadOnlyList<string>? CostCenterCodes);

public sealed record RequesterForm(int Number, string FullName, Guid DepartmentId, string CostCenterCode);

/// <param name="LegalName">Empty: the title of the تفصیل.</param>
/// <param name="Kind">Real or legal person; with <paramref name="NationalId"/> (کد ملی or شناسه ملی), address and postal code (report no. 2, item 7).</param>
public sealed record BeneficiaryForm(
    string DetailCode, string? LegalName, Guid? BankId, string? BranchName, string? AccountNumber, string? Sheba, string? Phone,
    BeneficiaryKind Kind = BeneficiaryKind.Natural, string? NationalId = null, string? PostalAddress = null, string? PostalCode = null)
{
    public BeneficiaryIdentity Identity => new(Kind, NationalId, PostalAddress, PostalCode);
}

/// <summary>Switch on/off or delete; <paramref name="Change"/> is "activate", "deactivate" or "delete".</summary>
public sealed record OrganizationStateCommand(string IdempotencyKey, Guid Id, string Change, long ExpectedVersion);

public sealed record DepartmentView(Guid Id, int Number, string Name, IReadOnlyList<string> CostCenterCodes, bool IsActive, long Version)
{
    public static DepartmentView From(Department d) => new(d.Id, d.Number, d.Name, d.CostCenterCodes, d.IsActive, d.Version);
}

public sealed record RequesterView(Guid Id, int Number, string FullName, Guid DepartmentId, string CostCenterCode, bool IsActive, bool IsInUse, long Version)
{
    public static RequesterView From(Requester r) => new(r.Id, r.Number, r.FullName, r.DepartmentId, r.CostCenterCode, r.IsActive, r.IsInUse, r.Version);
}

/// <param name="DetailName">Title of the تفصیل in the accounting system's list (may differ from the legal name).</param>
public sealed record BeneficiaryView(
    Guid Id, string DetailCode, string? DetailName, string LegalName, Guid? BankId, string? BranchName, string? AccountNumber, string? Sheba, string? Phone,
    bool IsActive, bool IsInUse, long Version, BeneficiaryKind? Kind = null, string? NationalId = null, string? PostalAddress = null, string? PostalCode = null);

/// <summary>
/// Departments (manual item 12), requesters (13) and beneficiaries (14). Each write is one command in one transaction on one
/// aggregate; the bank named by a beneficiary is first marked "in use" by its own command (as for purpose groups).
/// </summary>
public sealed class OrganizationUseCases(
    IUnitOfWork unitOfWork, IOrganizationRepository organization, ICatalogueRepository catalogue, ILedgerQueries ledger, IIdempotencyStore idempotency,
    CatalogueUseCases catalogueUseCases)
{
    private static readonly StringComparer PersianOrder = StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true);
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> AddDepartmentAsync(string key, DepartmentForm form, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var department = Department.Register(Guid.NewGuid(), form.Number, form.Name, form.CostCenterCodes ?? [], await DetailsAsync(form.CostCenterCodes ?? [], ct).ConfigureAwait(false));
            if (await CheckDepartmentAsync(department, ct).ConfigureAwait(false) is { } refused)
            {
                return refused;
            }

            await organization.AddDepartmentAsync(department, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(department.Id);
        }, cancellationToken);

    public Task<Result<Guid>> EditDepartmentAsync(string key, Guid id, DepartmentForm form, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var department = Check(await organization.GetDepartmentAsync(id, ct).ConfigureAwait(false), expectedVersion, "Department.NotFound", id);
            var removed = department.CostCenterCodes.Except(form.CostCenterCodes ?? [], StringComparer.Ordinal).ToList();
            department.Edit(form.Number, form.Name, form.CostCenterCodes ?? [], await DetailsAsync(form.CostCenterCodes ?? [], ct).ConfigureAwait(false));
            if (await CheckDepartmentAsync(department, ct).ConfigureAwait(false) is { } refused)
            {
                return refused;
            }

            // A cost centre still given to one of the department's requesters stays in the department.
            var requesters = await organization.ListRequestersAsync(department.Id, includeInactive: true, ct).ConfigureAwait(false);
            if (department.CostCenterCodes.Count > 0 && requesters.FirstOrDefault(r => removed.Contains(r.CostCenterCode)) is { } r)
            {
                return Result<Guid>.Fail("Department.CostCenterUsedByRequester", $"Requester {r.FullName} has cost centre {r.CostCenterCode}.");
            }

            await organization.SaveDepartmentAsync(department, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(department.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeDepartmentAsync(OrganizationStateCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var department = Check(await organization.GetDepartmentAsync(command.Id, ct).ConfigureAwait(false), command.ExpectedVersion, "Department.NotFound", command.Id);
            switch (command.Change)
            {
                case "activate":
                    department.Activate();
                    break;
                case "deactivate":
                    department.Deactivate();
                    break;
                case "delete":
                    department.Delete((await organization.ListRequestersAsync(department.Id, true, ct).ConfigureAwait(false)).Count > 0);
                    await organization.DeleteDepartmentAsync(department, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(department.Id);
                default:
                    return Unknown(command.Change);
            }

            await organization.SaveDepartmentAsync(department, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(department.Id);
        }, cancellationToken);

    public Task<Result<Guid>> AddRequesterAsync(string key, RequesterForm form, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var requester = Requester.Register(Guid.NewGuid(), form.Number, form.FullName,
                await organization.GetDepartmentAsync(form.DepartmentId, ct).ConfigureAwait(false), form.CostCenterCode, await DetailAsync(form.CostCenterCode, ct).ConfigureAwait(false));
            if (await RequesterNumberTakenAsync(requester, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Requester.NumberTaken", $"Requester number {requester.Number} is already used.");
            }

            await organization.AddRequesterAsync(requester, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(requester.Id);
        }, cancellationToken);

    public Task<Result<Guid>> EditRequesterAsync(string key, Guid id, RequesterForm form, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var requester = Check(await organization.GetRequesterAsync(id, ct).ConfigureAwait(false), expectedVersion, "Requester.NotFound", id);
            requester.Edit(form.Number, form.FullName, await organization.GetDepartmentAsync(form.DepartmentId, ct).ConfigureAwait(false),
                form.CostCenterCode, await DetailAsync(form.CostCenterCode, ct).ConfigureAwait(false));
            if (await RequesterNumberTakenAsync(requester, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Requester.NumberTaken", $"Requester number {requester.Number} is already used.");
            }

            await organization.SaveRequesterAsync(requester, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(requester.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeRequesterAsync(OrganizationStateCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var requester = Check(await organization.GetRequesterAsync(command.Id, ct).ConfigureAwait(false), command.ExpectedVersion, "Requester.NotFound", command.Id);
            switch (command.Change)
            {
                case "activate":
                    requester.Activate();
                    break;
                case "deactivate":
                    requester.Deactivate();
                    break;
                case "delete":
                    requester.Delete();
                    await organization.DeleteRequesterAsync(requester, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(requester.Id);
                default:
                    return Unknown(command.Change);
            }

            await organization.SaveRequesterAsync(requester, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(requester.Id);
        }, cancellationToken);

    public async Task<Result<Guid>> AddBeneficiaryAsync(string key, BeneficiaryForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (await MarkBankAsync(form.BankId, cancellationToken).ConfigureAwait(false) is { } refused)
        {
            return refused;
        }

        return await RunAsync(key, async ct =>
        {
            var (detail, bank, bankForm) = await BeneficiaryFactsAsync(form, ct).ConfigureAwait(false);
            var beneficiary = Beneficiary.Register(Guid.NewGuid(), form.DetailCode, detail, form.LegalName, bankForm, bank, form.Phone, form.Identity);
            if (await BeneficiaryTakenAsync(beneficiary, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Beneficiary.DetailTaken", $"تفصیل {beneficiary.DetailCode} is already a beneficiary.");
            }

            await organization.AddBeneficiaryAsync(beneficiary, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(beneficiary.Id);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<Guid>> EditBeneficiaryAsync(string key, Guid id, BeneficiaryForm form, long expectedVersion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (await MarkBankAsync(form.BankId, cancellationToken).ConfigureAwait(false) is { } refused)
        {
            return refused;
        }

        return await RunAsync(key, async ct =>
        {
            var beneficiary = Check(await organization.GetBeneficiaryAsync(id, ct).ConfigureAwait(false), expectedVersion, "Beneficiary.NotFound", id);
            var (detail, bank, bankForm) = await BeneficiaryFactsAsync(form, ct).ConfigureAwait(false);
            beneficiary.Edit(form.DetailCode, detail, form.LegalName, bankForm, bank, form.Phone, form.Identity);
            if (await BeneficiaryTakenAsync(beneficiary, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Beneficiary.DetailTaken", $"تفصیل {beneficiary.DetailCode} is already a beneficiary.");
            }

            await organization.SaveBeneficiaryAsync(beneficiary, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(beneficiary.Id);
        }, cancellationToken).ConfigureAwait(false);
    }

    public Task<Result<Guid>> ChangeBeneficiaryAsync(OrganizationStateCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var beneficiary = Check(await organization.GetBeneficiaryAsync(command.Id, ct).ConfigureAwait(false), command.ExpectedVersion, "Beneficiary.NotFound", command.Id);
            switch (command.Change)
            {
                case "activate":
                    beneficiary.Activate();
                    break;
                case "deactivate":
                    beneficiary.Deactivate();
                    break;
                case "delete":
                    beneficiary.Delete();
                    await organization.DeleteBeneficiaryAsync(beneficiary, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(beneficiary.Id);
                default:
                    return Unknown(command.Change);
            }

            await organization.SaveBeneficiaryAsync(beneficiary, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(beneficiary.Id);
        }, cancellationToken);

    /// <summary>Called by requests-svc and cashflow-svc before they save data naming the requester or beneficiary.</summary>
    public Task<Result<Guid>> MarkInUseAsync(string key, Guid id, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            if (await organization.GetRequesterAsync(id, ct).ConfigureAwait(false) is { } r)
            {
                if (r.MarkInUse())
                {
                    await organization.SaveRequesterAsync(r, ct).ConfigureAwait(false);
                }

                return Result<Guid>.Ok(id);
            }

            if (await organization.GetBeneficiaryAsync(id, ct).ConfigureAwait(false) is { } b)
            {
                if (b.MarkInUse())
                {
                    await organization.SaveBeneficiaryAsync(b, ct).ConfigureAwait(false);
                }

                return Result<Guid>.Ok(id);
            }

            return Result<Guid>.Fail("Organization.NotFound", $"Entry {id} was not found.");
        }, cancellationToken);

    public Task<IReadOnlyList<DepartmentView>> ListDepartmentsAsync(bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<DepartmentView>>(async ct =>
            (await organization.ListDepartmentsAsync(includeInactive, ct).ConfigureAwait(false)).OrderBy(d => d.Number).Select(DepartmentView.From).ToList(), cancellationToken);

    public Task<IReadOnlyList<RequesterView>> ListRequestersAsync(Guid? departmentId, bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<RequesterView>>(async ct =>
            (await organization.ListRequestersAsync(departmentId, includeInactive, ct).ConfigureAwait(false)).OrderBy(r => r.Number).Select(RequesterView.From).ToList(), cancellationToken);

    /// <summary>Beneficiaries by legal name, each with the title of its تفصیل.</summary>
    public Task<IReadOnlyList<BeneficiaryView>> ListBeneficiariesAsync(bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<BeneficiaryView>>(async ct =>
        {
            var list = new List<BeneficiaryView>();
            foreach (var b in (await organization.ListBeneficiariesAsync(includeInactive, ct).ConfigureAwait(false)).OrderBy(b => b.LegalName, PersianOrder))
            {
                var detail = await ledger.DetailAsync(b.DetailCode, ct).ConfigureAwait(false);
                list.Add(new(b.Id, b.DetailCode, detail?.Name, b.LegalName, b.Bank.BankId, b.Bank.BranchName, b.Bank.AccountNumber, b.Bank.Sheba, b.Phone, b.IsActive, b.IsInUse, b.Version,
                    b.Identity?.Kind, b.Identity?.NationalId, b.Identity?.PostalAddress, b.Identity?.PostalCode));
            }

            return list;
        }, cancellationToken);

    /// <summary>Same number or name as another department, or a cost centre already in another department.</summary>
    private async Task<Result<Guid>?> CheckDepartmentAsync(Department department, CancellationToken ct)
    {
        var others = (await organization.ListDepartmentsAsync(includeInactive: true, ct).ConfigureAwait(false)).Where(d => d.Id != department.Id).ToList();
        if (others.Any(d => d.Number == department.Number))
        {
            return Result<Guid>.Fail("Department.NumberTaken", $"Department number {department.Number} is already used.");
        }

        if (others.Any(d => d.NameKey == department.NameKey))
        {
            return Result<Guid>.Fail("Department.NameTaken", $"'{department.Name}' is already a department.");
        }

        return others.FirstOrDefault(d => d.CostCenterCodes.Intersect(department.CostCenterCodes, StringComparer.Ordinal).Any()) is { } owner
            ? Result<Guid>.Fail("Department.CostCenterTaken", $"A cost centre is already in department '{owner.Name}'.")
            : null;
    }

    private async Task<bool> RequesterNumberTakenAsync(Requester requester, CancellationToken ct) =>
        (await organization.ListRequestersAsync(null, includeInactive: true, ct).ConfigureAwait(false)).Any(r => r.Id != requester.Id && r.Number == requester.Number);

    private async Task<bool> BeneficiaryTakenAsync(Beneficiary beneficiary, CancellationToken ct) =>
        (await organization.ListBeneficiariesAsync(includeInactive: true, ct).ConfigureAwait(false)).Any(b => b.Id != beneficiary.Id && b.DetailCode == beneficiary.DetailCode);

    private async Task<DetailFact?> DetailAsync(string? code, CancellationToken ct) =>
        PersianText.Clean(code) is { Length: > 0 } c && await ledger.DetailAsync(c, ct).ConfigureAwait(false) is { } d
            ? new DetailFact(d.Code, d.Name, d.IsCostCenter, d.IsActive)
            : null;

    private async Task<Dictionary<string, DetailFact>> DetailsAsync(IEnumerable<string> codes, CancellationToken ct)
    {
        var facts = new Dictionary<string, DetailFact>(StringComparer.Ordinal);
        foreach (var code in codes.Select(PersianText.Clean).Where(c => c.Length > 0).Distinct())
        {
            if (await DetailAsync(code, ct).ConfigureAwait(false) is { } fact)
            {
                facts[code] = fact;
            }
        }

        return facts;
    }

    private async Task<(DetailFact? Detail, CatalogueItem? Bank, BeneficiaryBank Form)> BeneficiaryFactsAsync(BeneficiaryForm form, CancellationToken ct)
    {
        var bank = form.BankId is { } id && id != Guid.Empty ? await catalogue.GetAsync(id, ct).ConfigureAwait(false) : null;
        return (await DetailAsync(form.DetailCode, ct).ConfigureAwait(false), bank, new BeneficiaryBank(form.BankId, form.BranchName, form.AccountNumber, form.Sheba));
    }

    /// <summary>The beneficiary's bank becomes "in use" in the bank list (own command, before the beneficiary's transaction).</summary>
    private async Task<Result<Guid>?> MarkBankAsync(Guid? bankId, CancellationToken ct)
    {
        if (bankId is not { } id || id == Guid.Empty)
        {
            return null;
        }

        var entry = await catalogueUseCases.GetAsync(id, ct).ConfigureAwait(false);
        if (entry is null || entry.Kind != nameof(CatalogueKind.Bank))
        {
            return Result<Guid>.Fail("Beneficiary.BankNotFound", "The bank is not in the bank list.");
        }

        var marked = await catalogueUseCases.MarkInUseAsync(new MarkCatalogueItemInUseCommand($"in-use:{id}", id), ct).ConfigureAwait(false);
        return marked.IsSuccess ? null : marked;
    }

    private static T Check<T>(T? aggregate, long expectedVersion, string notFound, Guid id)
        where T : AggregateRoot<Guid> =>
        aggregate is null ? throw new DomainException(notFound, $"Entry {id} was not found.")
        : aggregate.Version == expectedVersion ? aggregate
        : throw new ConcurrencyConflictException($"Entry {id} was changed by someone else.");

    private static Result<Guid> Unknown(string change) => Result<Guid>.Fail("Request.UnknownChange", $"Unknown change '{change}'.");

    private Task<Result<Guid>> RunAsync(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
