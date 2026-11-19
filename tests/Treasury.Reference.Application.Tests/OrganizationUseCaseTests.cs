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
            new(5, "600046", "علی رضایی", "كاركنان كارخانه", true, false),
            new(6, "770300", "مرکز هزینه تبلیغات", "مراکز هزینه تبلیغات", true, false), // a cost-centre type, not marked
            new(7, "101001", "بانک ملت جاری ۱۲۳", "بانكها", true, false),
            new(8, "300010", "فروشگاه آفتاب", "مشتريان", true, false),
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
    public async Task Cost_centres_and_staff_are_found_by_the_detail_search()
    {
        // Marked cost centres (770100, 770200) and a cost-centre type (770300, owner 13 Nov 2026); staff by type.
        Assert.Equal(["770100", "770200", "770300"], (await Ledger().SearchDetailsAsync(null, null, false, 50, CancellationToken.None, costCentersOnly: true)).Select(d => d.Code));
        Assert.Equal(["600046"], (await Ledger().SearchDetailsAsync(null, null, false, 50, CancellationToken.None, roles: [DetailRole.Staff])).Select(d => d.Code));
        Assert.Equal(4, await Ledger().CountDetailsAsync(null, null, false, false, CancellationToken.None, [DetailRole.Staff, DetailRole.CostCentre]));
    }

    private LedgerUseCases Ledger() =>
        new(new InMemoryUnitOfWork(_db), new InMemoryLedgerRepository(_db), new InMemoryLedgerRepository(_db), new InMemoryIdempotencyStore(_db));

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
        var r = await _useCases.AddRequesterAsync(Key(), new(1, "600046", d, "770200"), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Equal("علی رضایی", (await _useCases.ListRequestersAsync(null, true, CancellationToken.None)).Single().FullName);
        Assert.Equal("Requester.NumberTaken", (await _useCases.AddRequesterAsync(Key(), new(1, "600046", d, "770100"), CancellationToken.None)).ErrorCode);
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

    [Fact]
    public async Task Beneficiaries_obligors_and_guarantors_share_one_list_and_each_list_box_takes_its_group_owner_19_nov()
    {
        // Three ticks at the top of the form; mobile and area code; the تفصیل is a person, not a bank account.
        var supplier = await _useCases.AddBeneficiaryAsync(Key(), new("111078", null, null, null, null, "460120000000003123456789", "88888888",
            BeneficiaryKind.Legal, "10101010103", Mobile: "۰۹۱۲۱۲۳۴۵۶۷", PhoneAreaCode: "021"), CancellationToken.None);
        Assert.True(supplier.IsSuccess, supplier.ErrorCode);
        var guarantor = await _useCases.AddBeneficiaryAsync(Key(), new("600046", null, null, null, null, null, null,
            BeneficiaryKind.Natural, "0084575948", IsObligor: true, IsGuarantor: true), CancellationToken.None);
        Assert.True(guarantor.IsSuccess, guarantor.ErrorCode);
        Assert.Equal("Beneficiary.DetailNotPerson", (await _useCases.AddBeneficiaryAsync(Key(), new("101001", null, null, null, null, null, null,
            BeneficiaryKind.Natural, "0084575948"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Beneficiary.RoleRequired", (await _useCases.AddBeneficiaryAsync(Key(), new("300010", null, null, null, null, null, null,
            BeneficiaryKind.Natural, "0084575948", IsBeneficiary: false), CancellationToken.None)).ErrorCode);

        var all = await _useCases.ListBeneficiariesAsync(true, CancellationToken.None);
        Assert.Equal(2, all.Count);
        var s = all.Single(x => x.Id == supplier.Value);
        Assert.Equal((true, false, false, "09121234567", "021", "88888888", "IR460120000000003123456789"),
            (s.IsBeneficiary, s.IsObligor, s.IsGuarantor, s.Mobile, s.PhoneAreaCode, s.Phone, s.Sheba));
        Assert.Equal([supplier.Value], (await _useCases.ListBeneficiariesAsync(true, CancellationToken.None, PartyRoles.Beneficiary)).Select(x => x.Id));
        Assert.Equal([guarantor.Value], (await _useCases.ListBeneficiariesAsync(true, CancellationToken.None, PartyRoles.Obligor)).Select(x => x.Id));
        Assert.Equal([guarantor.Value], (await _useCases.ListBeneficiariesAsync(true, CancellationToken.None, PartyRoles.Guarantor)).Select(x => x.Id));

        // The guarantor also becomes a beneficiary: one person, three groups.
        var edit = await _useCases.EditBeneficiaryAsync(Key(), guarantor.Value, new("600046", null, null, null, null, null, null,
            BeneficiaryKind.Natural, "0084575948", IsBeneficiary: true, IsObligor: true, IsGuarantor: true), 1, CancellationToken.None);
        Assert.True(edit.IsSuccess, edit.ErrorCode);
        Assert.Equal(2, (await _useCases.ListBeneficiariesAsync(true, CancellationToken.None, PartyRoles.Beneficiary)).Count);
    }

    [Fact]
    public async Task Cost_centres_and_staff_come_from_the_detail_types_and_a_requester_needs_no_department_owner_13_nov()
    {
        // 770300 is only of a cost-centre type (not marked "مرکز هزینه است"): still a cost centre.
        Assert.True((await Department(12, "تبلیغات", "770300")) != Guid.Empty);
        var r = await _useCases.AddRequesterAsync(Key(), new(2, "600046", null, "770300"), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Equal("Requester.PersonNotStaff", (await _useCases.AddRequesterAsync(Key(), new(3, "111079", null, "770300"), CancellationToken.None)).ErrorCode);
    }
}

