using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.Instruments;

/// <summary>
/// A guarantor (ضامن / متعهد) of a received promissory note. New-version manual, form «دریافت چک و اسناد دریافتنی»: "بجای مشخصات
/// بانکی چک … مشخصات متعهدین سفته شامل نام و کد ملی متعهدین (ضامنین) اول و دوم وارد میشود".
/// </summary>
/// <param name="NationalCode">10 digits for a person (کد ملی) or 11 for a company (شناسه ملی); optional.</param>
/// <param name="DetailCode">The guarantor's تفصیل, when it has one (owner's note receipt form, 16 Nov 2026: «شماره حساب تفصیل … ضامن سفته»).</param>
/// <param name="Phone">شماره تماس (owner's note receipt form).</param>
public sealed record NoteGuarantor(string Name, string? NationalCode, string? DetailCode = null, string? Phone = null);

/// <summary>
/// What the treasurer checked on a received note (owner's note receipt form, «کنترل مشخصات روی هر سفته»): the items article 308 of the
/// Commercial Code asks of a promissory note. Owner, 16 Nov 2026 (answer to Q40): ticked for each note, and a note with an item not
/// ticked is not received on a note receipt.
/// </summary>
public sealed record NoteChecks(bool Signature = false, bool Date = false, bool AmountInWords = false, bool Payee = false, bool PaymentDate = false)
{
    /// <summary>
    /// Every item is ticked. A note payable on demand («عندالمطالبه») has no payment date written on it (owner, 16 Nov 2026: «فاقد
    /// سررسید است»), so that item is not asked of it.
    /// </summary>
    public bool IsComplete(bool onDemand) => Signature && Date && AmountInWords && Payee && (onDemand || PaymentDate);
}

/// <summary>
/// What is written on a received cheque or note besides number, amount and dates. New-version manual, form «دریافت چک و اسناد
/// دریافتنی»: account number, bank, branch, branch code, city, account holder (when the payer hands over someone else's cheque),
/// «خارج از مرکز»; MVC Receipt_Detail (BankID, Bank_BranchDesc, Bank_BranchCode, Bank_AccountNo). The 16-digit Sayad ID (شناسه صیاد)
/// is printed on every cheque since 1397.
/// A promissory note received on a note receipt (owner's form «فرم دریافت سفته», 16 Nov 2026) also carries the date written on it
/// («تاریخ صدور»), whether it is payable on demand («عندالمطالبه», no due date) and what was checked on it.
/// </summary>
/// <param name="WrittenOn">Note: the issue date written on it (the instrument's own IssueDate is the day it was received).</param>
/// <param name="OnDemand">Note: payable on demand («عندالمطالبه»); it has no due date.</param>
public sealed record ReceivedInstrumentDetails(
    Guid? BankId = null,
    string? BranchName = null,
    string? BranchCode = null,
    string? AccountNumber = null,
    string? AccountHolder = null,
    string? City = null,
    bool OutOfTown = false,
    string? SayadId = null,
    IReadOnlyList<NoteGuarantor>? Guarantors = null,
    DateOnly? WrittenOn = null,
    bool OnDemand = false,
    NoteChecks? Checks = null)
{
    public const int SayadIdLength = 16;

    /// <summary>Owner, 16 Nov 2026 (answer to Q39): up to three; the new-version manual named a first and a second guarantor.</summary>
    public const int MaxGuarantors = 3;

    public const int MaxPhoneLength = 30;

    public static ReceivedInstrumentDetails None { get; } = new();

    public IReadOnlyList<NoteGuarantor> GuarantorList => Guarantors ?? [];

    /// <summary>
    /// «خارج از مرکز» (owner, 18 Nov 2026): ticked by the system when the city of the cheque's bank is not the city of the company's
    /// address in Basic Information — a Tehran company with a cheque on a Shiraz bank, or a Shiraz company with a cheque on an Isfahan
    /// bank. Names are compared as PersianText.Key does (Arabic and Persian letters alike). When either city is not known, what the
    /// user ticked stays. Manual, form «دریافت چک و اسناد دریافتنی»: «خارج از مرکز».
    /// </summary>
    public ReceivedInstrumentDetails WithOutOfTownFor(string? companyCity) =>
        string.IsNullOrWhiteSpace(City) || string.IsNullOrWhiteSpace(companyCity)
            ? this
            : this with { OutOfTown = !string.Equals(PersianText.Key(City), PersianText.Key(companyCity), StringComparison.Ordinal) };

    /// <summary>Checks the details against the kind of instrument and returns them with empty texts removed.</summary>
    internal ReceivedInstrumentDetails Validated(InstrumentKind kind)
    {
        var cheque = kind.IsCheque();
        var sayad = Clean(SayadId);
        if (sayad is not null)
        {
            Require(cheque, "Instrument.SayadIdOnlyForCheques", "Only a cheque has a Sayad ID.");
            Require(sayad.Length == SayadIdLength && sayad.All(char.IsAsciiDigit), "Instrument.SayadIdInvalid", "The Sayad ID has 16 digits.");
        }

        var guarantors = GuarantorList;
        if (guarantors.Count > 0)
        {
            Require(kind.IsNote(), "Instrument.GuarantorsOnlyForNotes", "Only a promissory note has guarantors.");
            Require(guarantors.Count <= MaxGuarantors, "Instrument.TooManyGuarantors", "A note has at most three guarantors.");
        }

        var cleanGuarantors = guarantors.Select(g =>
        {
            Require(!string.IsNullOrWhiteSpace(g?.Name), "Instrument.GuarantorNameRequired", "Enter the guarantor's name.");
            var code = Clean(g!.NationalCode);
            Require(code is null || IranianIds.IsValidPersonOrCompany(code), "Instrument.GuarantorNationalCodeInvalid",
                "The guarantor's national code (10 digits) or national ID (11 digits) is not valid.");
            var phone = Clean(g.Phone);
            Require(phone is null || phone.Length <= MaxPhoneLength, "Instrument.GuarantorPhoneTooLong", "The guarantor's phone number is too long.");
            return new NoteGuarantor(g.Name.Trim(), code, Clean(g.DetailCode), phone);
        }).ToList();

        var note = kind.IsNote();
        return new ReceivedInstrumentDetails(
            cheque ? BankId : null, cheque ? Clean(BranchName) : null, cheque ? Clean(BranchCode) : null, cheque ? Clean(AccountNumber) : null,
            Clean(AccountHolder), Clean(City), cheque && OutOfTown, sayad, cleanGuarantors,
            note ? WrittenOn : null, note && OnDemand, note ? Checks : null);
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

/// <summary>
/// Check digits of Iranian identity numbers. The same rules as reference-svc's beneficiary identity (owner's report no. 2,
/// item 7); repeated here because services share no domain code (ADR 0001).
/// </summary>
public static class IranianIds
{
    /// <summary>کد ملی of a person (10 digits, mod 11) or شناسه ملی of a company (11 digits).</summary>
    public static bool IsValidPersonOrCompany(string code) => code.Length switch
    {
        10 => IsValidNationalCode(code),
        11 => IsValidLegalId(code),
        _ => false,
    };

    public static bool IsValidNationalCode(string code)
    {
        if (code.Length != 10 || !code.All(char.IsAsciiDigit) || code.Distinct().Count() == 1)
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += (code[i] - '0') * (10 - i);
        }

        var r = sum % 11;
        var check = code[9] - '0';
        return r < 2 ? check == r : check == 11 - r;
    }

    public static bool IsValidLegalId(string code)
    {
        if (code.Length != 11 || !code.All(char.IsAsciiDigit) || code.Distinct().Count() == 1)
        {
            return false;
        }

        int[] weights = [29, 27, 23, 19, 17, 29, 27, 23, 19, 17];
        var tenth = code[9] - '0' + 2;
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            sum += (code[i] - '0' + tenth) * weights[i];
        }

        var r = sum % 11;
        return (r == 10 ? 0 : r) == code[10] - '0';
    }
}
