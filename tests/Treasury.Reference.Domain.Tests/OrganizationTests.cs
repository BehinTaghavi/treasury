using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Organization;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>Departments, requesters and beneficiaries: new-version manual items 12–14; MVC Beneficiaries.</summary>
public sealed class OrganizationTests
{
    private static readonly Dictionary<string, DetailFact> Facts = new()
    {
        ["770100"] = new("770100", "تولید", true, true),
        ["770200"] = new("770200", "اداری", true, true),
        ["770300"] = new("770300", "قدیمی", true, false),
        ["111078"] = new("111078", "شرکت نمونه بازرگانی", false, true),
    };

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    private static Department Production() => Department.Register(Guid.NewGuid(), 10, "تولید", ["770100"], Facts);

    [Fact]
    public void A_department_has_a_number_a_title_and_cost_centres_that_are_cost_centre_detail_accounts()
    {
        var d = Department.Register(Guid.NewGuid(), 10, "  واحد   تولید ", ["770200", "770100", "770100"], Facts);
        Assert.Equal((10, "واحد تولید"), (d.Number, d.Name));
        Assert.Equal(["770100", "770200"], d.CostCenterCodes);
        Assert.Equal("Department.NumberInvalid", Code(() => Department.Register(Guid.NewGuid(), 0, "x", [], Facts)));
        Assert.Equal("Department.NameRequired", Code(() => Department.Register(Guid.NewGuid(), 1, " ", [], Facts)));
        Assert.Equal("Department.CostCenterNotFound", Code(() => Department.Register(Guid.NewGuid(), 1, "x", ["999999"], Facts)));
        Assert.Equal("Department.NotCostCenter", Code(() => Department.Register(Guid.NewGuid(), 1, "x", ["111078"], Facts)));
        Assert.Equal("Department.CostCenterInactive", Code(() => Department.Register(Guid.NewGuid(), 1, "x", ["770300"], Facts)));
        Assert.Equal("Department.HasRequesters", Code(() => d.Delete(hasRequesters: true)));
    }

    [Fact]
    public void A_requester_has_a_department_and_one_of_its_cost_centres()
    {
        var d = Production();
        var r = Requester.Register(Guid.NewGuid(), 5, "علی رضایی", d, "770100", Facts["770100"]);
        Assert.Equal((5, d.Id, "770100"), (r.Number, r.DepartmentId, r.CostCenterCode));
        Assert.Equal("Requester.CostCenterNotInDepartment", Code(() => Requester.Register(Guid.NewGuid(), 6, "x", d, "770200", Facts["770200"])));
        Assert.Equal("Requester.NotCostCenter", Code(() => Requester.Register(Guid.NewGuid(), 6, "x", d, "111078", Facts["111078"])));
        Assert.Equal("Requester.DepartmentRequired", Code(() => Requester.Register(Guid.NewGuid(), 6, "x", null, "770100", Facts["770100"])));
        Assert.Equal("Requester.CostCenterRequired", Code(() => Requester.Register(Guid.NewGuid(), 6, "x", d, " ", null)));
        var open = Department.Register(Guid.NewGuid(), 11, "بدون مرکز هزینه", [], Facts);
        Requester.Register(Guid.NewGuid(), 7, "y", open, "770200", Facts["770200"]); // a department without listed centres takes any
        r.MarkInUse();
        Assert.Equal("Requester.InUse", Code(r.Delete));
    }

    [Theory]
    [InlineData("IR460120000000003123456789")]
    [InlineData("ir46 0120 0000 0000 3123 4567 89")]
    [InlineData("۴۶۰۱۲۰۰۰۰۰۰۰۰۰۳۱۲۳۴۵۶۷۸۹")]
    public void A_valid_sheba_is_accepted_written_in_any_common_way(string typed) =>
        Assert.Equal("IR460120000000003123456789", Beneficiary.NormaliseSheba(typed));

    [Theory]
    [InlineData("IR470120000000003123456789")] // wrong check digits
    [InlineData("IR46012000000000312345678")]  // 23 digits
    [InlineData("DE460120000000003123456789")]
    public void An_invalid_sheba_is_refused(string typed) => Assert.Null(Beneficiary.NormaliseSheba(typed));

    [Fact]
    public void A_beneficiary_takes_the_detail_title_as_legal_name_unless_another_is_typed()
    {
        var bank = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "بانک ملت");
        var b = Beneficiary.Register(Guid.NewGuid(), "111078", Facts["111078"], null, new(bank.Id, "شعبه بازار", "0101-55", "ir46 0120 0000 0000 3123 4567 89"), bank, "۰۲۱-۸۸۸۸۸۸۸۸");
        Assert.Equal(("شرکت نمونه بازرگانی", "IR460120000000003123456789"), (b.LegalName, b.Bank.Sheba));
        b.Edit("111078", Facts["111078"], "شرکت نمونه بازرگانی (سهامی خاص)", b.Bank with { Sheba = b.Bank.Sheba }, bank, null);
        Assert.Equal("شرکت نمونه بازرگانی (سهامی خاص)", b.LegalName);
    }

    [Fact]
    public void Beneficiary_bank_details_are_checked()
    {
        var bank = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "بانک ملت");
        var detail = Facts["111078"];
        Assert.Equal("Beneficiary.DetailNotFound", Code(() => Beneficiary.Register(Guid.NewGuid(), "555555", null, null, new(null, null, null, null), null, null)));
        Assert.Equal("Beneficiary.ShebaInvalid", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(null, null, null, "IR47"), null, null)));
        Assert.Equal("Beneficiary.BankRequired", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(null, null, "0101-55", null), null, null)));
        Assert.Equal("Beneficiary.AccountNumberInvalid", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(bank.Id, null, "abc", null), bank, null)));
        Assert.Equal("Beneficiary.PhoneInvalid", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(null, null, null, null), null, "12")));
        Assert.Equal("Beneficiary.BankNotFound", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(Guid.NewGuid(), null, null, null), null, null)));
    }
}
