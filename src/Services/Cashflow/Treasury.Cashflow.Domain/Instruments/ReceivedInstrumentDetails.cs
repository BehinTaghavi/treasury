using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.Instruments;

/// <summary>
/// A guarantor (ضامن / متعهد) of a received promissory note. New-version manual, form «دریافت چک و اسناد دریافتنی»: "بجای مشخصات
/// بانکی چک … مشخصات متعهدین سفته شامل نام و کد ملی متعهدین (ضامنین) اول و دوم وارد میشود".
/// </summary>
/// <param name="NationalCode">10 digits for a person (کد ملی) or 11 for a company (شناسه ملی); optional.</param>
public sealed record NoteGuarantor(string Name, string? NationalCode);

/// <summary>
/// What is written on a received cheque or note besides number, amount and dates. New-version manual, form «دریافت چک و اسناد
/// دریافتنی»: account number, bank, branch, branch code, city, account holder (when the payer hands over someone else's cheque),
/// «خارج از مرکز»; MVC Receipt_Detail (BankID, Bank_BranchDesc, Bank_BranchCode, Bank_AccountNo). The 16-digit Sayad ID (شناسه صیاد)
/// is printed on every cheque since 1397.
/// </summary>
public sealed record ReceivedInstrumentDetails(
    Guid? BankId = null,
    string? BranchName = null,
    string? BranchCode = null,
    string? AccountNumber = null,
    string? AccountHolder = null,
    string? City = null,
    bool OutOfTown = false,
    string? SayadId = null,
    IReadOnlyList<NoteGuarantor>? Guarantors = null)
{
    public const int SayadIdLength = 16;

    public const int MaxGuarantors = 2;

    public static ReceivedInstrumentDetails None { get; } = new();

    public IReadOnlyList<NoteGuarantor> GuarantorList => Guarantors ?? [];

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
            Require(guarantors.Count <= MaxGuarantors, "Instrument.TooManyGuarantors", "A note has at most two guarantors.");
        }

        var cleanGuarantors = guarantors.Select(g =>
        {
            Require(!string.IsNullOrWhiteSpace(g?.Name), "Instrument.GuarantorNameRequired", "Enter the guarantor's name.");
            var code = Clean(g!.NationalCode);
            Require(code is null || IranianIds.IsValidPersonOrCompany(code), "Instrument.GuarantorNationalCodeInvalid",
                "The guarantor's national code (10 digits) or national ID (11 digits) is not valid.");
            return new NoteGuarantor(g.Name.Trim(), code);
        }).ToList();

        return new ReceivedInstrumentDetails(
            cheque ? BankId : null, cheque ? Clean(BranchName) : null, cheque ? Clean(BranchCode) : null, cheque ? Clean(AccountNumber) : null,
            Clean(AccountHolder), Clean(City), cheque && OutOfTown, sayad, cleanGuarantors);
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
