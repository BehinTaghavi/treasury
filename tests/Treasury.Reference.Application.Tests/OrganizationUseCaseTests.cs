using Treasury.Reference.Domain.Organization;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Application.Ledger;
using Treasury.Reference.Application.Organization;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Ledger;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

public sealed class OrganizationUseCaseTests : IDisposable
{
    private readonly InMemoryReferenceDatabase _db = new();
    private readonly CatalogueUseCases _catalogue;
    private readonly OrganizationUseCases _useCases;

    public OrganizationUseCaseTests()
    {
        var uow = new InMemoryUnitOfWork(_db);
        var items = new InMemoryCatalogueRepository(_db);
        var idempotency = new InMemoryIdempotencyStore(_db);
        var ledger = new InMemoryLedgerRepository(_db);
        _catalogue = new CatalogueUseCases(uow, items, idempotency);
        _useCases = new OrganizationUseCases(uow, new InMemoryOrganizationRepository(_db), items, ledger, idempotency, _catalogue);
        var ledgerUseCases = new LedgerUseCases(uow, ledger, ledger, idempotency);
        LedgerFixture.Load(ledgerUseCases);
        DetailRow[] details =
        [
            new(1, "770100", "مرکز هزینه تولید", "مراكز هزينه", true, true),
            new(2, "770200", "مرکز هزینه اداری", "مراكز هزينه", true, true),
            new(3, "111078", "شركت نمونه بازرگاني", "اشخاص وشركتها", true, false),
            new(4, "111079", "شخص نمونه", "اشخاص وشركتها", true, false),
        ];
        Assert.True(ledgerUseCases.ImportDetailsAsync("fixture-details", details, CancellationToken.None).GetAwaiter().GetResult().IsSuccess);
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private async Task<Guid> Department(int number, string name, params string[] centers)
    {
        var r = await _useCases.AddDepartmentAsync(Key(), new(number, name, centers), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    [Fact]
    public async Task Cost_centres_are_found_by_the_detail_search() =>
        Assert.Equal(2, (await new LedgerUseCases(new InMemoryUnitOfWork(_db), new InMemoryLedgerRepository(_db), new InMemoryLedgerRepository(_db), new InMemoryIdempotencyStore(_db))
            .SearchDetailsAsync(null, null, false, 50, CancellationToken.None, costCentersOnly: true)).Count);

    [Fact]
    public async Task Department_numbers_names_and_cost_centres_are_each_used_once()
    {
        await Department(10, "تولید", "770100");
        Assert.Equal("Department.NumberTaken", (await _useCases.AddDepartmentAsync(Key(), new(10, "اداری", []), CancellationToken.None)).ErrorCode);
        Assert.Equal("Department.NameTaken", (await _useCases.AddDepartmentAsync(Key(), new(11, "توليد", []), CancellationToken.None)).ErrorCode);
        Assert.Equal("Department.CostCenterTaken", (await _useCases.AddDepartmentAsync(Key(), new(11, "اداری", ["770100"]), CancellationToken.None)).ErrorCode);
        await Department(11, "اداری", "770200");
    }

    [Fact]
    public async Task A_department_keeps_cost_centres_its_requesters_use_and_is_deleted_only_without_requesters()
    {
        var d = await Department(10, "تولید", "770100", "770200");
        var r = await _useCases.AddRequesterAsync(Key(), new(1, "علی رضایی", d, "770200"), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Equal("Requester.NumberTaken", (await _useCases.AddRequesterAsync(Key(), new(1, "دیگری", d, "770100"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Department.CostCenterUsedByRequester", (await _useCases.EditDepartmentAsync(Key(), d, new(10, "تولید", ["770100"]), 1, CancellationToken.None)).ErrorCode);
        Assert.Equal("Department.HasRequesters", (await _useCases.ChangeDepartmentAsync(new(Key(), d, "delete", 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangeRequesterAsync(new(Key(), r.Value, "delete", 1), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeDepartmentAsync(new(Key(), d, "delete", 1), CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task A_beneficiary_per_detail_account_with_its_bank_marked_in_use()
    {
        var mellat = (await _catalogue.AddAsync(new(Key(), CatalogueKind.Bank, "بانک ملت"), CancellationToken.None)).Value;
        var form = new BeneficiaryForm("111078", null, mellat, "شعبه بازار", "0101-55", "IR460120000000003123456789", "021-88888888",
            BeneficiaryKind.Legal, "10101010103", "تهران", "1234567890");
        var b = await _useCases.AddBeneficiaryAsync(Key(), form, CancellationToken.None);
        Assert.True(b.IsSuccess, b.ErrorCode);
        var view = Assert.Single(await _useCases.ListBeneficiariesAsync(true, CancellationToken.None));
        Assert.Equal(("شركت نمونه بازرگاني", "شركت نمونه بازرگاني"), (view.LegalName, view.DetailName));
        Assert.Equal(((BeneficiaryKind?)BeneficiaryKind.Legal, "10101010103", "1234567890"), (view.Kind, view.NationalId, view.PostalCode));
        Assert.True((await _catalogue.ListAsync(CatalogueKind.Bank, null, true, CancellationToken.None)).Single().IsInUse);
        Assert.Equal("Beneficiary.DetailTaken", (await _useCases.AddBeneficiaryAsync(Key(), form with { LegalName = "x" }, CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.MarkInUseAsync(Key(), b.Value, CancellationToken.None)).IsSuccess);
        Assert.Equal("Beneficiary.InUse", (await _useCases.ChangeBeneficiaryAsync(new(Key(), b.Value, "delete", 2), CancellationToken.None)).ErrorCode);
    }
}
