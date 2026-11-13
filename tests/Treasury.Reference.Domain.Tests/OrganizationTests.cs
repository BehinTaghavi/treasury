using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Ledger;
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
        ["600046"] = new("600046", "علی رضایی", false, true, DetailRole.Staff),
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
    public void A_requester_is_a_staff_detail_account_with_a_cost_centre_of_its_department_owner_report_2()
    {
        var d = Production();
        Requester Make(Department? dep, string cc, string person = "600046") =>
            Requester.Register(Guid.NewGuid(), 6, dep, cc, Facts.GetValueOrDefault(cc), person, Facts.GetValueOrDefault(person));
        var r = Requester.Register(Guid.NewGuid(), 5, d, "770100", Facts["770100"], "600046", Facts["600046"]);
        Assert.Equal((5, d.Id, "770100", "علی رضایی", "600046"), (r.Number, r.DepartmentId, r.CostCenterCode, r.FullName, r.PersonDetailCode));
        Assert.Equal("Requester.CostCenterNotInDepartment", Code(() => Make(d, "770200")));
        Assert.Equal("Requester.NotCostCenter", Code(() => Make(d, "111078")));
        Assert.Equal("Requester.CostCenterRequired", Code(() => Make(d, " ")));
        Assert.Equal("Requester.PersonRequired", Code(() => Make(d, "770100", " ")));
        Assert.Equal("Requester.PersonNotStaff", Code(() => Make(d, "770100", "111078")));
        Assert.Equal("Requester.PersonNotFound", Code(() => Make(d, "770100", "999999")));
        Assert.Null(Make(null, "770200").DepartmentId); // owner, 13 Nov 2026: Iran Nazho Co. has no departments
        var open = Department.Register(Guid.NewGuid(), 11, "بدون مرکز هزینه", [], Facts);
        Make(open, "770200"); // a department without listed centres takes any
        r.MarkInUse();
        Assert.Equal("Requester.InUse", Code(r.Delete));
    }

    [Theory]
    [InlineData("کارکنان کارخانه", DetailRole.Staff)]
    [InlineData("كاركنان دفتر", DetailRole.Staff)]
    [InlineData("مراکز هزینه تولیدی", DetailRole.CostCentre)]
    [InlineData("مراكز هزينه سربار كارخانه", DetailRole.CostCentre)]
    [InlineData("مراکز هزینه اداری دفتر", DetailRole.CostCentre)]
    [InlineData("مرکز هزینه تبلیغات", DetailRole.CostCentre)]
    [InlineData("واحدهای سازمانی", DetailRole.OrgUnit)]
    public void The_role_of_a_detail_account_comes_from_its_type_owner_13_nov(string type, DetailRole role) =>
        Assert.Equal(role, DetailRoles.Of(type));

    [Fact]
    public void Other_types_have_no_role_unless_marked_as_cost_centre()
    {
        Assert.Null(DetailRoles.Of("اشخاص وشركتها"));
        Assert.Null(DetailRoles.Of(null));
        Assert.Equal(DetailRole.CostCentre, DetailRoles.Of("بانكها", markedCostCentre: true));
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
        var b = Beneficiary.Register(Guid.NewGuid(), "111078", Facts["111078"], null, new(bank.Id, "شعبه بازار", "0101-55", "ir46 0120 0000 0000 3123 4567 89"), bank, "۰۲۱-۸۸۸۸۸۸۸۸", Company);
        Assert.Equal(("شرکت نمونه بازرگانی", "IR460120000000003123456789"), (b.LegalName, b.Bank.Sheba));
        b.Edit("111078", Facts["111078"], "شرکت نمونه بازرگانی (سهامی خاص)", b.Bank with { Sheba = b.Bank.Sheba }, bank, null, Company);
        Assert.Equal("شرکت نمونه بازرگانی (سهامی خاص)", b.LegalName);
    }

    private static readonly BeneficiaryIdentity Person = new(BeneficiaryKind.Natural, "0084575948", null, null);
    private static readonly BeneficiaryIdentity Company = new(BeneficiaryKind.Legal, "10101010103", "تهران، خیابان آزادی، پلاک ۱", "1234567890");

    [Theory]
    [InlineData("0084575948", true)]
    [InlineData("۰۰۸۴۵۷۵۹۴۸", true)]
    [InlineData("1234567891", true)]
    [InlineData("0084575949", false)]
    [InlineData("1111111111", false)]
    [InlineData("008457594", false)]
    public void The_national_code_of_a_real_person_has_ten_digits_and_a_check_digit_owner_report_2(string typed, bool valid) =>
        Assert.Equal(valid, Beneficiary.IsNationalCode(Treasury.BuildingBlocks.Domain.PersianText.Key(typed)));

    [Theory]
    [InlineData("10101010103", true)]
    [InlineData("14003628900", true)]
    [InlineData("10101010104", false)]
    [InlineData("1010101010", false)]
    public void The_national_id_of_a_legal_person_has_eleven_digits_and_a_check_digit_owner_report_2(string typed, bool valid) =>
        Assert.Equal(valid, Beneficiary.IsLegalNationalId(typed));

    [Fact]
    public void A_beneficiary_needs_its_national_code_or_id_and_a_ten_digit_postal_code_owner_report_2()
    {
        var detail = Facts["111078"];
        Beneficiary Make(BeneficiaryIdentity? id) => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(null, null, null, null), null, null, id);
        Assert.Equal("Beneficiary.NationalCodeRequired", Code(() => Make(null)));
        Assert.Equal("Beneficiary.NationalCodeInvalid", Code(() => Make(Person with { NationalId = "0084575949" })));
        Assert.Equal("Beneficiary.LegalIdRequired", Code(() => Make(Company with { NationalId = " " })));
        Assert.Equal("Beneficiary.LegalIdInvalid", Code(() => Make(Company with { NationalId = "0084575948" })));
        Assert.Equal("Beneficiary.PostalCodeInvalid", Code(() => Make(Person with { PostalCode = "12345" })));
        var b = Make(Company with { PostalCode = "۱۲۳۴۵-۶۷۸۹۰" });
        Assert.Equal(new BeneficiaryIdentity(BeneficiaryKind.Legal, "10101010103", "تهران، خیابان آزادی، پلاک ۱", "1234567890"), b.Identity);
    }

    [Fact]
    public void Beneficiary_bank_details_are_checked()
    {
        var bank = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "بانک ملت");
        var detail = Facts["111078"];
        Assert.Equal("Beneficiary.DetailNotFound", Code(() => Beneficiary.Register(Guid.NewGuid(), "555555", null, null, new(null, null, null, null), null, null, Person)));
        Assert.Equal("Beneficiary.ShebaInvalid", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(null, null, null, "IR47"), null, null, Person)));
        Assert.Equal("Beneficiary.BankRequired", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(null, null, "0101-55", null), null, null, Person)));
        Assert.Equal("Beneficiary.AccountNumberInvalid", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(bank.Id, null, "abc", null), bank, null, Person)));
        Assert.Equal("Beneficiary.PhoneInvalid", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(null, null, null, null), null, "12", Person)));
        Assert.Equal("Beneficiary.BankNotFound", Code(() => Beneficiary.Register(Guid.NewGuid(), "111078", detail, null, new(Guid.NewGuid(), null, null, null), null, null, Person)));
    }
}
