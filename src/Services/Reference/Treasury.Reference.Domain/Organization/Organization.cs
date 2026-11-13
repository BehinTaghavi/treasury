using System.Numerics;
using System.Text.RegularExpressions;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Ledger;

namespace Treasury.Reference.Domain.Organization;

/// <summary>What the copy of the accounting system's تفصیل list says about one code; null in place of a fact: no such تفصیل.</summary>
/// <param name="IsCostCenter">Marked as a cost centre or of a cost-centre type (owner, 13 Nov 2026).</param>
/// <param name="Role">Staff, cost centre or organisational unit, from the type; null for other تفصیل.</param>
public sealed record DetailFact(string Code, string Name, bool IsCostCenter, bool IsActive, DetailRole? Role = null);

internal static class Rules
{
    public static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }

    public static string Name(string? text, int max, string required, string tooLong)
    {
        var cleaned = PersianText.Clean(text);
        Require(cleaned.Length > 0, required, "A name is required.");
        Require(cleaned.Length <= max, tooLong, $"At most {max} characters.");
        return cleaned;
    }

    public static string? Optional(string? text) => PersianText.Clean(text) is { Length: > 0 } c ? c : null;

    /// <summary>A تفصیل that exists; a newly chosen one must be active, one kept from before may have been switched off since.</summary>
    public static DetailFact Detail(string code, DetailFact? fact, string? kept, string notFound, string inactive)
    {
        Require(fact is not null, notFound, $"تفصیل {code} is not in the list.");
        Require(fact!.IsActive || code == kept, inactive, $"تفصیل {code} is inactive.");
        return fact;
    }
}

/// <summary>
/// A department (بخش / واحد سازمانی) of the company that requests money, new-version manual item 12: a number, a title and the
/// cost centres (مراکز هزینه, تفصیل marked as cost centres in the accounting system) it consists of.
/// </summary>
public sealed class Department : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    private Department(Guid id, int number, string name, IReadOnlyList<string> costCenters)
        : base(id)
    {
        Number = number;
        Name = name;
        CostCenterCodes = costCenters;
        IsActive = true;
    }

    /// <summary>Manual: "جهت هر بخش یک شماره شناسائی اختصاص داده میشود" – typed by the user, unique.</summary>
    public int Number { get; private set; }

    public string Name { get; private set; }

    public string NameKey => PersianText.Key(Name);

    /// <summary>Codes of the cost-centre تفصیل of the department (manual: "هر بخش ... میتواند از چند مرکز هزینه تشکیل شده باشد").</summary>
    public IReadOnlyList<string> CostCenterCodes { get; private set; }

    public bool IsActive { get; private set; }

    /// <param name="facts">The تفصیل named on the form as found in the list, by code.</param>
    public static Department Register(Guid id, int number, string name, IEnumerable<string> costCenters, IReadOnlyDictionary<string, DetailFact> facts)
    {
        var d = new Department(id, ValidNumber(number), Rules.Name(name, MaxNameLength, "Department.NameRequired", "Department.NameTooLong"), ValidCenters(costCenters, facts, []));
        d.Raise(DepartmentChanged.From(d));
        return d;
    }

    public void Edit(int number, string name, IEnumerable<string> costCenters, IReadOnlyDictionary<string, DetailFact> facts)
    {
        var n = ValidNumber(number);
        var title = Rules.Name(name, MaxNameLength, "Department.NameRequired", "Department.NameTooLong");
        var centers = ValidCenters(costCenters, facts, CostCenterCodes);
        if (n == Number && title == Name && centers.SequenceEqual(CostCenterCodes))
        {
            return;
        }

        (Number, Name, CostCenterCodes) = (n, title, centers);
        Raise(DepartmentChanged.From(this));
    }

    public void Activate() => SetActive(true);

    public void Deactivate() => SetActive(false);

    /// <summary>Only a department without requesters is deleted (owner, 11 Nov 2026: delete only what nothing refers to).</summary>
    public void Delete(bool hasRequesters)
    {
        Rules.Require(!hasRequesters, "Department.HasRequesters", "Requesters belong to this department; move or delete them first.");
        Raise(new DepartmentDeleted(Id));
    }

    public static Department Restore(Guid id, int number, string name, IReadOnlyList<string> costCenters, bool isActive, long version)
    {
        var d = new Department(id, number, name, costCenters) { IsActive = isActive };
        d.SetLoadedVersion(version);
        return d;
    }

    private void SetActive(bool active)
    {
        if (IsActive != active)
        {
            IsActive = active;
            Raise(DepartmentChanged.From(this));
        }
    }

    private static int ValidNumber(int number)
    {
        Rules.Require(number > 0, "Department.NumberInvalid", "The department number is a positive number.");
        return number;
    }

    private static List<string> ValidCenters(IEnumerable<string> codes, IReadOnlyDictionary<string, DetailFact> facts, IReadOnlyList<string> kept)
    {
        ArgumentNullException.ThrowIfNull(codes);
        var list = codes.Select(Rules.Optional).OfType<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        foreach (var code in list)
        {
            var fact = Rules.Detail(code, facts.GetValueOrDefault(code), kept.Contains(code) ? code : null, "Department.CostCenterNotFound", "Department.CostCenterInactive");
            Rules.Require(fact.IsCostCenter, "Department.NotCostCenter", $"تفصیل {code} is not a cost centre.");
        }

        return list;
    }
}

public sealed record DepartmentChanged(Guid DepartmentId, int Number, string Name, IReadOnlyList<string> CostCenterCodes, bool IsActive) : DomainEvent
{
    public static DepartmentChanged From(Department d) => new(d.Id, d.Number, d.Name, d.CostCenterCodes, d.IsActive);
}

public sealed record DepartmentDeleted(Guid DepartmentId) : DomainEvent;

/// <summary>
/// A person who requests money (درخواست کننده وجوه), new-version manual item 13: a number (may differ from their تفصیل), the person
/// – a staff تفصیل (owner's report no. 2, item 6; the name comes from it) –, department and cost centre. The department is optional
/// (owner, 13 Nov 2026: Iran Nazho Co. has no departments); when given and it lists its cost centres, the cost centre is one of them.
/// </summary>
public sealed class Requester : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    private Requester(Guid id, int number, string fullName, Guid? departmentId, string costCenterCode, string? personDetailCode)
        : base(id)
    {
        PersonDetailCode = personDetailCode;
        Number = number;
        FullName = fullName;
        DepartmentId = departmentId;
        CostCenterCode = costCenterCode;
        IsActive = true;
    }

    public int Number { get; private set; }

    public string FullName { get; private set; }

    public Guid? DepartmentId { get; private set; }

    /// <summary>The staff تفصیل of the requester; empty only on requesters saved before report no. 2.</summary>
    public string? PersonDetailCode { get; private set; }

    public string CostCenterCode { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>A payment request names this requester (recorded by requests-svc); then it is never deleted.</summary>
    public bool IsInUse { get; private set; }

    /// <param name="personCode">The requester's staff تفصیل; <paramref name="person"/> is what the list says about it.</param>
    public static Requester Register(
        Guid id, int number, Department? department, string costCenterCode, DetailFact? costCenter, string personCode, DetailFact? person)
    {
        var (n, name, dep, cc, p) = Validate(number, department, costCenterCode, costCenter, personCode, person, null);
        var r = new Requester(id, n, name, dep, cc, p);
        r.Raise(RequesterChanged.From(r));
        return r;
    }

    public void Edit(int number, Department? department, string costCenterCode, DetailFact? costCenter, string personCode, DetailFact? person)
    {
        var v = Validate(number, department, costCenterCode, costCenter, personCode, person, this);
        if (v == (Number, FullName, DepartmentId, CostCenterCode, PersonDetailCode))
        {
            return;
        }

        (Number, FullName, DepartmentId, CostCenterCode, PersonDetailCode) = v;
        Raise(RequesterChanged.From(this));
    }

    public void Activate() => SetActive(true);

    public void Deactivate() => SetActive(false);

    public bool MarkInUse()
    {
        if (IsInUse)
        {
            return false;
        }

        IsInUse = true;
        return true;
    }

    public void Delete()
    {
        Rules.Require(!IsInUse, "Requester.InUse", "Payment requests name this requester; switch them off instead.");
        Raise(new RequesterDeleted(Id));
    }

    public static Requester Restore(
        Guid id, int number, string fullName, Guid? departmentId, string costCenterCode, bool isActive, bool isInUse, long version, string? personDetailCode = null)
    {
        var r = new Requester(id, number, fullName, departmentId, costCenterCode, personDetailCode) { IsActive = isActive, IsInUse = isInUse };
        r.SetLoadedVersion(version);
        return r;
    }

    private static (int, string, Guid?, string, string?) Validate(
        int number, Department? department, string costCenterCode, DetailFact? fact, string personCode, DetailFact? person, Requester? before)
    {
        Rules.Require(number > 0, "Requester.NumberInvalid", "The requester number is a positive number.");
        var pc = Rules.Optional(personCode) ?? throw new DomainException("Requester.PersonRequired", "Choose the requester from the staff تفصیل.");
        var p = Rules.Detail(pc, person, before?.PersonDetailCode, "Requester.PersonNotFound", "Requester.PersonInactive");
        Rules.Require(p.Role == DetailRole.Staff || pc == before?.PersonDetailCode, "Requester.PersonNotStaff", $"تفصیل {pc} is not of a staff type (کارکنان).");
        var name = Rules.Name(p.Name, MaxNameLength, "Requester.NameRequired", "Requester.NameTooLong");
        Rules.Require(department is null || department.IsActive || department.Id == before?.DepartmentId, "Requester.DepartmentInactive", "The department is switched off.");
        var code = Rules.Optional(costCenterCode) ?? throw new DomainException("Requester.CostCenterRequired", "The cost centre of the requester is required.");
        var cc = Rules.Detail(code, fact, before?.CostCenterCode, "Requester.CostCenterNotFound", "Requester.CostCenterInactive");
        Rules.Require(cc.IsCostCenter, "Requester.NotCostCenter", $"تفصیل {code} is not a cost centre.");
        Rules.Require(department is null || department.CostCenterCodes.Count == 0 || department.CostCenterCodes.Contains(code), "Requester.CostCenterNotInDepartment",
            $"Cost centre {code} is not one of the department's cost centres.");
        return (number, name, department?.Id, code, pc);
    }

    private void SetActive(bool active)
    {
        if (IsActive != active)
        {
            IsActive = active;
            Raise(RequesterChanged.From(this));
        }
    }
}

public sealed record RequesterChanged(Guid RequesterId, int Number, string FullName, Guid? DepartmentId, string CostCenterCode, bool IsActive) : DomainEvent
{
    public static RequesterChanged From(Requester r) => new(r.Id, r.Number, r.FullName, r.DepartmentId, r.CostCenterCode, r.IsActive);
}

public sealed record RequesterDeleted(Guid RequesterId) : DomainEvent;

/// <summary>The bank details of a beneficiary as typed on the form.</summary>
public sealed record BeneficiaryBank(Guid? BankId, string? BranchName, string? AccountNumber, string? Sheba);

/// <summary>Real person (شخص حقیقی) or legal person (شخص حقوقی, a company or organisation).</summary>
public enum BeneficiaryKind
{
    Natural,
    Legal,
}

/// <summary>
/// Who the beneficiary is (owner's report no. 2, item 7): a real person's national code (کد ملی, 10 digits) or a legal person's
/// national ID (شناسه ملی, 11 digits) – required, checked with their check digit – and the postal address and 10-digit postal code.
/// </summary>
public sealed record BeneficiaryIdentity(BeneficiaryKind Kind, string? NationalId, string? PostalAddress, string? PostalCode);

/// <summary>
/// A beneficiary (ذینفع), new-version manual item 14: the تفصیل (the beneficiary must exist in the accounting system first), the
/// registered legal title printed on cheques (by default the تفصیل title), bank, branch, account number, Sheba (شبا: needed when the
/// paying bank differs from the beneficiary's) and a phone number. MVC: Beneficiaries, BeneficiariesDetailInfo, Beneficiaries_TELS.
/// </summary>
public sealed partial class Beneficiary : AggregateRoot<Guid>
{
    public const int MaxNameLength = 150;

    public const int MaxAddressLength = 300;

    private Beneficiary(Guid id, string detailCode, string legalName, BeneficiaryBank bank, string? phone, BeneficiaryIdentity? identity)
        : base(id)
    {
        Identity = identity;
        DetailCode = detailCode;
        LegalName = legalName;
        Bank = bank;
        Phone = phone;
        IsActive = true;
    }

    public string DetailCode { get; private set; }

    /// <summary>Manual: "عنوان حقوقی ثبت شده" – printed on the cheque; defaults to the تفصیل title.</summary>
    public string LegalName { get; private set; }

    public BeneficiaryBank Bank { get; private set; }

    public string? Phone { get; private set; }

    /// <summary>National code or ID, address and postal code; empty only on beneficiaries saved before report no. 2.</summary>
    public BeneficiaryIdentity? Identity { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>A payment request or issued cheque names this beneficiary; then it is never deleted.</summary>
    public bool IsInUse { get; private set; }

    /// <param name="bank">The beneficiary's bank as loaded by the use case (a bank-name entry); null when none was chosen.</param>
    public static Beneficiary Register(
        Guid id, string detailCode, DetailFact? detail, string? legalName, BeneficiaryBank form, CatalogueItem? bank, string? phone, BeneficiaryIdentity? identity)
    {
        var v = Validate(detailCode, detail, legalName, form, bank, phone, null);
        var b = new Beneficiary(id, v.Code, v.Name, v.Bank, v.Phone, CheckIdentity(identity));
        b.Raise(BeneficiaryChanged.From(b));
        return b;
    }

    public void Edit(string detailCode, DetailFact? detail, string? legalName, BeneficiaryBank form, CatalogueItem? bank, string? phone, BeneficiaryIdentity? identity)
    {
        var v = Validate(detailCode, detail, legalName, form, bank, phone, this);
        var id = CheckIdentity(identity);
        if (v == (DetailCode, LegalName, Bank, Phone) && id == Identity)
        {
            return;
        }

        (DetailCode, LegalName, Bank, Phone) = v;
        Identity = id;
        Raise(BeneficiaryChanged.From(this));
    }

    public void Activate() => SetActive(true);

    public void Deactivate() => SetActive(false);

    public bool MarkInUse()
    {
        if (IsInUse)
        {
            return false;
        }

        IsInUse = true;
        return true;
    }

    public void Delete()
    {
        Rules.Require(!IsInUse, "Beneficiary.InUse", "Payments name this beneficiary; switch them off instead.");
        Raise(new BeneficiaryDeleted(Id));
    }

    public static Beneficiary Restore(
        Guid id, string detailCode, string legalName, BeneficiaryBank bank, string? phone, bool isActive, bool isInUse, long version, BeneficiaryIdentity? identity = null)
    {
        var b = new Beneficiary(id, detailCode, legalName, bank, phone, identity) { IsActive = isActive, IsInUse = isInUse };
        b.SetLoadedVersion(version);
        return b;
    }

    /// <summary>
    /// Iranian IBAN (شبا): "IR" and 24 digits, checked with the ISO 13616 mod-97 rule. Spaces, dashes and Persian digits are
    /// accepted and the "IR" may be left out. Returns the normalised form or null when the text is not a valid Sheba.
    /// </summary>
    public static string? NormaliseSheba(string? text)
    {
        var digits = PersianText.Key(text).ToUpperInvariant().Replace("-", string.Empty, StringComparison.Ordinal);
        if (ShebaDigits().IsMatch(digits))
        {
            digits = "IR" + digits;
        }

        if (!ShebaPattern().IsMatch(digits))
        {
            return null;
        }

        // Move "IR" and the check digits to the end, letters as numbers (I=18, R=27), remainder mod 97 must be 1.
        var rearranged = digits[4..] + "1827" + digits[2..4];
        return BigInteger.Parse(rearranged, System.Globalization.CultureInfo.InvariantCulture) % 97 == 1 ? digits : null;
    }

    /// <summary>
    /// Iranian national code (کد ملی): 10 digits; the last is a check digit. s = Σ digit(i)·(10−i) for the first nine; with
    /// r = s mod 11 the check digit is r when r &lt; 2, otherwise 11 − r. Ten equal digits are refused.
    /// </summary>
    public static bool IsNationalCode(string digits)
    {
        if (digits.Length != 10 || !digits.All(char.IsAsciiDigit) || digits.Distinct().Count() == 1)
        {
            return false;
        }

        var s = 0;
        for (var i = 0; i < 9; i++)
        {
            s += (digits[i] - '0') * (10 - i);
        }

        var r = s % 11;
        return digits[9] - '0' == (r < 2 ? r : 11 - r);
    }

    /// <summary>
    /// National ID of a legal person (شناسه ملی): 11 digits; the last is a check digit. With d = 10th digit + 2 and weights
    /// 29, 27, 23, 19, 17 (twice), s = Σ (digit(i) + d)·weight(i) for the first ten; the check digit is s mod 11 (10 counts as 0).
    /// </summary>
    public static bool IsLegalNationalId(string digits)
    {
        if (digits.Length != 11 || !digits.All(char.IsAsciiDigit) || digits.Distinct().Count() == 1)
        {
            return false;
        }

        int[] weights = [29, 27, 23, 19, 17, 29, 27, 23, 19, 17];
        var d = digits[9] - '0' + 2;
        var s = 0;
        for (var i = 0; i < 10; i++)
        {
            s += (digits[i] - '0' + d) * weights[i];
        }

        var r = s % 11;
        return digits[10] - '0' == (r == 10 ? 0 : r);
    }

    private static BeneficiaryIdentity CheckIdentity(BeneficiaryIdentity? identity)
    {
        var kind = identity?.Kind ?? BeneficiaryKind.Natural;
        var id = PersianText.Key(identity?.NationalId).Replace("-", string.Empty, StringComparison.Ordinal);
        if (kind == BeneficiaryKind.Natural)
        {
            Rules.Require(id.Length > 0, "Beneficiary.NationalCodeRequired", "The national code (کد ملی) of a real person is required.");
            Rules.Require(IsNationalCode(id), "Beneficiary.NationalCodeInvalid", "The national code has 10 digits and a valid check digit.");
        }
        else
        {
            Rules.Require(id.Length > 0, "Beneficiary.LegalIdRequired", "The national ID (شناسه ملی) of a legal person is required.");
            Rules.Require(IsLegalNationalId(id), "Beneficiary.LegalIdInvalid", "The national ID has 11 digits and a valid check digit.");
        }

        var address = Rules.Optional(identity?.PostalAddress);
        Rules.Require(address is null || address.Length <= MaxAddressLength, "Beneficiary.AddressTooLong", $"An address has at most {MaxAddressLength} characters.");
        var postal = Rules.Optional(identity?.PostalCode) is { } p ? PersianText.Key(p).Replace("-", string.Empty, StringComparison.Ordinal) : null;
        Rules.Require(postal is null || (postal.Length == 10 && postal.All(char.IsAsciiDigit)), "Beneficiary.PostalCodeInvalid", "A postal code has 10 digits.");
        return new BeneficiaryIdentity(kind, id, address, postal);
    }

    private static (string Code, string Name, BeneficiaryBank Bank, string? Phone) Validate(
        string detailCode, DetailFact? detail, string? legalName, BeneficiaryBank form, CatalogueItem? bank, string? phone, Beneficiary? before)
    {
        ArgumentNullException.ThrowIfNull(form);
        var code = Rules.Optional(detailCode) ?? throw new DomainException("Beneficiary.DetailRequired", "The تفصیل of the beneficiary is required.");
        var fact = Rules.Detail(code, detail, before?.DetailCode, "Beneficiary.DetailNotFound", "Beneficiary.DetailInactive");
        var name = Rules.Name(Rules.Optional(legalName) ?? fact.Name, MaxNameLength, "Beneficiary.NameRequired", "Beneficiary.NameTooLong");

        var account = Rules.Optional(form.AccountNumber);
        Rules.Require(account is null || AccountPattern().IsMatch(PersianText.Key(account)), "Beneficiary.AccountNumberInvalid", "An account number has digits, dashes and slashes only.");
        string? sheba = null;
        if (Rules.Optional(form.Sheba) is { } typed)
        {
            sheba = NormaliseSheba(typed) ?? throw new DomainException("Beneficiary.ShebaInvalid", "The Sheba number is not valid (IR and 24 digits).");
        }

        if (form.BankId is { } bankId && bankId != Guid.Empty)
        {
            Rules.Require(bank is not null && bank.Kind == CatalogueKind.Bank, "Beneficiary.BankNotFound", "The bank is not in the bank list.");
            Rules.Require(bank!.IsActive || bankId == before?.Bank.BankId, "Beneficiary.BankInactive", "The bank is switched off in the bank list.");
        }

        var bankIdOrNull = form.BankId is { } b && b != Guid.Empty ? b : (Guid?)null;
        Rules.Require(account is null || bankIdOrNull is not null, "Beneficiary.BankRequired", "Choose the bank of the account number.");

        var phoneText = Rules.Optional(phone);
        Rules.Require(phoneText is null || PhonePattern().IsMatch(PersianText.Key(phoneText)), "Beneficiary.PhoneInvalid", "A phone number has 6 to 15 digits.");
        return (code, name, new BeneficiaryBank(bankIdOrNull, Rules.Optional(form.BranchName), account, sheba), phoneText);
    }

    private void SetActive(bool active)
    {
        if (IsActive != active)
        {
            IsActive = active;
            Raise(BeneficiaryChanged.From(this));
        }
    }

    [GeneratedRegex("^[0-9]{24}$")]
    private static partial Regex ShebaDigits();

    [GeneratedRegex("^IR[0-9]{24}$")]
    private static partial Regex ShebaPattern();

    [GeneratedRegex("^[0-9][0-9\\-/.]{0,39}$")]
    private static partial Regex AccountPattern();

    [GeneratedRegex("^\\+?[0-9\\-]{6,20}$")]
    private static partial Regex PhonePattern();
}

public sealed record BeneficiaryChanged(Guid BeneficiaryId, string DetailCode, string LegalName, Guid? BankId, string? AccountNumber, string? Sheba, bool IsActive) : DomainEvent
{
    public static BeneficiaryChanged From(Beneficiary b) => new(b.Id, b.DetailCode, b.LegalName, b.Bank.BankId, b.Bank.AccountNumber, b.Bank.Sheba, b.IsActive);
}

public sealed record BeneficiaryDeleted(Guid BeneficiaryId) : DomainEvent;
