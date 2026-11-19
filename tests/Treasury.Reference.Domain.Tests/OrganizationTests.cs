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
        ["111078"] = new("111078", "شرکت نمونه بازرگانی", false, true, DetailRole.PersonOrCompany),
        ["600046"] = new("600046", "علی رضایی", false, true, DetailRole.Staff),
        ["300010"] = new("300010", "فروشگاه آفتاب", false, true, DetailRole.Customer),
        ["101001"] = new("101001", "بانک ملت جاری ۱۲۳", false, true), // a bank account's تفصیل: no person
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
    [InlineData("مشتريان", DetailRole.Customer)]
    [InlineData("مشتری عمده", DetailRole.Customer)]
    [InlineData("اشخاص وشركتها", DetailRole.PersonOrCompany)]
    [InlineData("اشخاص و شرکت‌ها", DetailRole.PersonOrCompany)]
    public void The_role_of_a_detail_account_comes_from_its_type_owner_13_nov(string type, DetailRole role) =>
        Assert.Equal(role, DetailRoles.Of(type));

    [Fact]
    public void Other_types_have_no_role_unless_marked_as_cost_centre()
    {
        Assert.Null(DetailRoles.Of("بانكها"));
        Assert.Null(DetailRoles.Of("سفارشات"));
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

    // ---- owner, 19 Nov 2026: «ذینفعان / متعهدین / ضامنین» ----

    private static Beneficiary Party(string code = "111078", PartyRoles roles = PartyRoles.Beneficiary, BeneficiaryContact? contact = null, string? phone = null, string? sheba = null) =>
        Beneficiary.Register(Guid.NewGuid(), code, Facts.GetValueOrDefault(code), null, new(null, null, null, sheba), null, phone, Person, roles, contact);

    [Fact]
    public void A_sheba_of_the_right_length_with_wrong_check_digits_gets_its_own_message_owner_19_nov()
    {
        // The owner typed a made-up number of exactly IR and 24 digits and was told to enter "IR and 24 digits".
        Assert.Equal("Beneficiary.ShebaCheckDigits", Code(() => Party(sheba: "IR123456789012345678901234")));
        Assert.Equal("Beneficiary.ShebaCheckDigits", Code(() => Party(sheba: "123456789012345678901234"))); // typed after the fixed «IR»
        Assert.Equal("Beneficiary.ShebaInvalid", Code(() => Party(sheba: "IR4601200000000031234567"))); // 22 digits
        Assert.Equal("IR460120000000003123456789", Party(sheba: "460120000000003123456789").Bank.Sheba);
    }

    [Fact]
    public void The_detail_account_of_a_beneficiary_is_a_person_not_a_bank_owner_19_nov()
    {
        // «شماره تفصیل ذینفعان اساسا از نوع اشخاص است شامل کارکنان، مشتریان و شرکتها»
        Assert.Equal("Beneficiary.DetailNotPerson", Code(() => Party("101001")));
        Assert.Equal("Beneficiary.DetailNotPerson", Code(() => Party("770100"))); // a cost centre
        foreach (var code in new[] { "111078", "600046", "300010" })
        {
            Assert.Equal(code, Party(code).DetailCode);
        }

        // One saved before the rule keeps its تفصیل when it is corrected; a newly chosen one is checked.
        var old = Beneficiary.Restore(Guid.NewGuid(), "101001", "قدیمی", new(null, null, null, null), null, true, false, 1, Person);
        old.Edit("101001", Facts["101001"], "قدیمی ۲", old.Bank, null, null, Person);
        Assert.Equal("قدیمی ۲", old.LegalName);
        Assert.Equal("Beneficiary.DetailNotPerson", Code(() => old.Edit("770100", Facts["770100"], null, old.Bank, null, null, Person)));
    }

    [Fact]
    public void One_form_keeps_beneficiaries_obligors_and_guarantors_told_apart_by_their_group_owner_19_nov()
    {
        Assert.Equal(PartyRoles.Beneficiary, Party().Roles); // as before the groups existed
        var both = Party(roles: PartyRoles.Obligor | PartyRoles.Guarantor);
        Assert.True(both.Is(PartyRoles.Obligor) && both.Is(PartyRoles.Guarantor) && !both.Is(PartyRoles.Beneficiary));
        Assert.Equal("Beneficiary.RoleRequired", Code(() => Party(roles: PartyRoles.None)));

        // Correcting other fields keeps the groups; a group can be added.
        both.Edit("111078", Facts["111078"], "نام تازه", both.Bank, null, null, Person);
        Assert.Equal(PartyRoles.Obligor | PartyRoles.Guarantor, both.Roles);
        both.Edit("111078", Facts["111078"], "نام تازه", both.Bank, null, null, Person, PartyRoles.Beneficiary | PartyRoles.Guarantor);
        Assert.Equal(PartyRoles.Beneficiary | PartyRoles.Guarantor, both.Roles);

        // Named by a payment request, it stays a beneficiary (it may gain the other groups).
        both.MarkInUse();
        Assert.Equal("Beneficiary.RoleInUse", Code(() => both.Edit("111078", Facts["111078"], "نام تازه", both.Bank, null, null, Person, PartyRoles.Guarantor)));
        both.Edit("111078", Facts["111078"], "نام تازه", both.Bank, null, null, Person, PartyRoles.Beneficiary | PartyRoles.Obligor);
        Assert.True(both.Is(PartyRoles.Obligor));
    }

    [Theory]
    [InlineData("09121234567")]
    [InlineData("۰۹۱۲۱۲۳۴۵۶۷")]
    [InlineData("0912-123-4567")]
    [InlineData("+989121234567")]
    [InlineData("00989121234567")]
    [InlineData("9121234567")]
    public void A_mobile_number_is_kept_as_09_and_nine_digits_owner_19_nov(string typed) =>
        Assert.Equal("09121234567", Party(contact: new(null, null, typed)).Mobile);

    [Fact]
    public void The_area_code_has_its_own_box_of_at_most_four_digits_owner_19_nov()
    {
        var p = Party(contact: new("۰۲۱", null, null), phone: "88776655");
        Assert.Equal(("021", "88776655"), (p.PhoneAreaCode, p.Phone));
        Assert.Equal("Beneficiary.AreaCodeInvalid", Code(() => Party(contact: new("21", null, null), phone: "88776655")));   // starts with 0
        Assert.Equal("Beneficiary.AreaCodeInvalid", Code(() => Party(contact: new("02155", null, null), phone: "88776655"))); // at most 4 digits
        Assert.Equal("Beneficiary.MobileInvalid", Code(() => Party(contact: new(null, null, "0812123456"))));
        Assert.Null(Party(contact: new("021", null, null)).PhoneAreaCode); // no phone number: no area code

        // A correction that does not speak of the contact keeps it; one that does replaces it.
        p.Edit("111078", Facts["111078"], null, p.Bank, null, "88776655", Person);
        Assert.Equal("021", p.PhoneAreaCode);
        p.Edit("111078", Facts["111078"], null, p.Bank, null, "88776655", Person, null, new("031", null, "09121234567"));
        Assert.Equal(("031", "09121234567"), (p.PhoneAreaCode, p.Mobile));
    }
}
