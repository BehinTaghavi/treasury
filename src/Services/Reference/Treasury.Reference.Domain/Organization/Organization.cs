using System.Numerics;
using System.Text.RegularExpressions;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Domain.Organization;

/// <summary>What the copy of the accounting system's تفصیل list says about one code; null in place of a fact: no such تفصیل.</summary>
public sealed record DetailFact(string Code, string Name, bool IsCostCenter, bool IsActive);

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
/// A person who requests money (درخواست کننده وجوه), new-version manual item 13: a number (may differ from their تفصیل), full name,
/// department and cost centre. When the department lists its cost centres, the requester's cost centre is one of them.
/// </summary>
public sealed class Requester : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    private Requester(Guid id, int number, string fullName, Guid departmentId, string costCenterCode)
        : base(id)
    {
        Number = number;
        FullName = fullName;
        DepartmentId = departmentId;
        CostCenterCode = costCenterCode;
        IsActive = true;
    }

    public int Number { get; private set; }

    public string FullName { get; private set; }

    public Guid DepartmentId { get; private set; }

    public string CostCenterCode { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>A payment request names this requester (recorded by requests-svc); then it is never deleted.</summary>
    public bool IsInUse { get; private set; }

    public static Requester Register(Guid id, int number, string fullName, Department? department, string costCenterCode, DetailFact? costCenter)
    {
        var (n, name, dep, cc) = Validate(number, fullName, department, costCenterCode, costCenter, null);
        var r = new Requester(id, n, name, dep, cc);
        r.Raise(RequesterChanged.From(r));
        return r;
    }

    public void Edit(int number, string fullName, Department? department, string costCenterCode, DetailFact? costCenter)
    {
        var v = Validate(number, fullName, department, costCenterCode, costCenter, this);
        if (v == (Number, FullName, DepartmentId, CostCenterCode))
        {
            return;
        }

        (Number, FullName, DepartmentId, CostCenterCode) = v;
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

    public static Requester Restore(Guid id, int number, string fullName, Guid departmentId, string costCenterCode, bool isActive, bool isInUse, long version)
    {
        var r = new Requester(id, number, fullName, departmentId, costCenterCode) { IsActive = isActive, IsInUse = isInUse };
        r.SetLoadedVersion(version);
        return r;
    }

    private static (int, string, Guid, string) Validate(int number, string fullName, Department? department, string costCenterCode, DetailFact? fact, Requester? before)
    {
        Rules.Require(number > 0, "Requester.NumberInvalid", "The requester number is a positive number.");
        var name = Rules.Name(fullName, MaxNameLength, "Requester.NameRequired", "Requester.NameTooLong");
        Rules.Require(department is not null, "Requester.DepartmentRequired", "The department of the requester is required.");
        Rules.Require(department!.IsActive || department.Id == before?.DepartmentId, "Requester.DepartmentInactive", "The department is switched off.");
        var code = Rules.Optional(costCenterCode) ?? throw new DomainException("Requester.CostCenterRequired", "The cost centre of the requester is required.");
        var cc = Rules.Detail(code, fact, before?.CostCenterCode, "Requester.CostCenterNotFound", "Requester.CostCenterInactive");
        Rules.Require(cc.IsCostCenter, "Requester.NotCostCenter", $"تفصیل {code} is not a cost centre.");
        Rules.Require(department.CostCenterCodes.Count == 0 || department.CostCenterCodes.Contains(code), "Requester.CostCenterNotInDepartment",
            $"Cost centre {code} is not one of the department's cost centres.");
        return (number, name, department.Id, code);
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

public sealed record RequesterChanged(Guid RequesterId, int Number, string FullName, Guid DepartmentId, string CostCenterCode, bool IsActive) : DomainEvent
{
    public static RequesterChanged From(Requester r) => new(r.Id, r.Number, r.FullName, r.DepartmentId, r.CostCenterCode, r.IsActive);
}

public sealed record RequesterDeleted(Guid RequesterId) : DomainEvent;

/// <summary>The bank details of a beneficiary as typed on the form.</summary>
public sealed record BeneficiaryBank(Guid? BankId, string? BranchName, string? AccountNumber, string? Sheba);

/// <summary>
/// A beneficiary (ذینفع), new-version manual item 14: the تفصیل (the beneficiary must exist in the accounting system first), the
/// registered legal title printed on cheques (by default the تفصیل title), bank, branch, account number, Sheba (شبا: needed when the
/// paying bank differs from the beneficiary's) and a phone number. MVC: Beneficiaries, BeneficiariesDetailInfo, Beneficiaries_TELS.
/// </summary>
public sealed partial class Beneficiary : AggregateRoot<Guid>
{
    public const int MaxNameLength = 150;

    private Beneficiary(Guid id, string detailCode, string legalName, BeneficiaryBank bank, string? phone)
        : base(id)
    {
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

    public bool IsActive { get; private set; }

    /// <summary>A payment request or issued cheque names this beneficiary; then it is never deleted.</summary>
    public bool IsInUse { get; private set; }

    /// <param name="bank">The beneficiary's bank as loaded by the use case (a bank-name entry); null when none was chosen.</param>
    public static Beneficiary Register(Guid id, string detailCode, DetailFact? detail, string? legalName, BeneficiaryBank form, CatalogueItem? bank, string? phone)
    {
        var v = Validate(detailCode, detail, legalName, form, bank, phone, null);
        var b = new Beneficiary(id, v.Code, v.Name, v.Bank, v.Phone);
        b.Raise(BeneficiaryChanged.From(b));
        return b;
    }

    public void Edit(string detailCode, DetailFact? detail, string? legalName, BeneficiaryBank form, CatalogueItem? bank, string? phone)
    {
        var v = Validate(detailCode, detail, legalName, form, bank, phone, this);
        if (v == (DetailCode, LegalName, Bank, Phone))
        {
            return;
        }

        (DetailCode, LegalName, Bank, Phone) = v;
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

    public static Beneficiary Restore(Guid id, string detailCode, string legalName, BeneficiaryBank bank, string? phone, bool isActive, bool isInUse, long version)
    {
        var b = new Beneficiary(id, detailCode, legalName, bank, phone) { IsActive = isActive, IsInUse = isInUse };
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
