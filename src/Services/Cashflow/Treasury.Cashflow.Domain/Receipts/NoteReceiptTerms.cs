using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Domain.Receipts;

/// <summary>Why the notes were received (owner's form «فرم دریافت سفته», 16 Nov 2026: «دلیل دریافت سفته»).</summary>
public enum NoteReason
{
    /// <summary>«بابت بدهی»: the notes pay a debt; they are receivables (اسناد دریافتنی).</summary>
    Debt,

    /// <summary>«تضمین»: held as security for a commitment; kept off the balance sheet (انتظامی).</summary>
    Guarantee,

    /// <summary>«امانی»: held in trust; kept off the balance sheet like a guarantee (open question Q37).</summary>
    Trust,

    /// <summary>«سایر».</summary>
    Other,
}

/// <summary>«نوع سند»: a paper note or an electronic one (سفتهٔ الکترونیکی).</summary>
public enum NoteForm
{
    Paper,
    Electronic,
}

/// <summary>«نوع سررسید»: a note with a due date («معین») or payable on demand («عندالمطالبه»).</summary>
public enum NoteMaturity
{
    Fixed,
    OnDemand,
}

/// <summary>«پیوست‌ها»: what was handed over with the notes. Ticks only; the files themselves come with the documents store.</summary>
public sealed record NoteAttachments(bool NoteImage = false, bool Contract = false, bool Representation = false, bool Other = false);

/// <summary>
/// What a receipt of promissory notes says besides the header and the notes themselves (owner's form «فرم دریافت سفته», 16 Nov 2026).
/// The issuer and obligor («صادرکننده و متعهد») is the receipt's payer; the receiver is the treasury and the beneficiary is the company.
/// </summary>
/// <param name="IssuerNationalCode">کد ملی / شناسه ملی of the issuer, as typed on the form.</param>
/// <param name="CommitmentSubject">«موضوع تعهد»: what a guarantee note secures; kept only for a guarantee.</param>
/// <param name="ReturnTerms">«شرایط و مرجع تأیید استرداد سفته به متعهد»; not kept for a note that pays a debt.</param>
/// <param name="CustodyPlace">«محل نگهداری اصل سند / مرجع الکترونیکی».</param>
/// <param name="OriginalReceived">«وضعیت دریافت اصل سند»; when received, <paramref name="OriginalReceivedOn"/> is its date.</param>
/// <param name="Discrepancy">«شرح نقص یا مغایرت احتمالی».</param>
/// <param name="DeliveredBy">«تحویل‌دهنده»: who handed the notes over.</param>
/// <param name="ApprovedBy">«تأییدکنندهٔ مجاز»: named on the printed receipt, where the three sign (open question Q41).</param>
public sealed record NoteReceiptTerms(
    NoteReason Reason,
    NoteForm Form,
    NoteMaturity Maturity,
    string? IssuerNationalCode = null,
    string? IssuerPhone = null,
    string? CommitmentSubject = null,
    string? ReturnTerms = null,
    string? CustodyPlace = null,
    bool OriginalReceived = false,
    FiscalDate? OriginalReceivedOn = null,
    string? Discrepancy = null,
    NoteAttachments? Attachments = null,
    string? DeliveredBy = null,
    string? ApprovedBy = null)
{
    public const int MaxTextLength = 500;

    /// <summary>Guarantee and trust notes are held, not owed: they go to the off-balance accounts (انتظامی).</summary>
    public bool HeldOffBalance => Reason is NoteReason.Guarantee or NoteReason.Trust;

    /// <summary>The kind of instrument every note on the receipt becomes.</summary>
    public InstrumentKind InstrumentKind => HeldOffBalance ? InstrumentKind.GuaranteeNote : InstrumentKind.PromissoryNote;

    /// <summary>Checks the terms and returns them with empty texts removed and the fields that do not apply dropped.</summary>
    internal NoteReceiptTerms Validated()
    {
        Require(Enum.IsDefined(Reason) && Enum.IsDefined(Form) && Enum.IsDefined(Maturity), "Receipt.NoteTermsRequired",
            "Choose why the notes are received, the kind of document and the kind of due date.");
        var code = Clean(IssuerNationalCode);
        Require(code is null || IranianIds.IsValidPersonOrCompany(code), "Receipt.IssuerNationalCodeInvalid",
            "The issuer's national code (10 digits) or national ID (11 digits) is not valid.");
        Require(!OriginalReceived || OriginalReceivedOn is not null, "Receipt.OriginalDateRequired", "Enter the date the original note was received.");
        string?[] texts = [IssuerPhone, CommitmentSubject, ReturnTerms, CustodyPlace, Discrepancy, DeliveredBy, ApprovedBy];
        Require(texts.All(t => (t?.Trim().Length ?? 0) <= MaxTextLength), "Receipt.NoteTextTooLong", "One of the texts is too long.");
        return new NoteReceiptTerms(
            Reason, Form, Maturity, code, Clean(IssuerPhone),
            Reason == NoteReason.Guarantee ? Clean(CommitmentSubject) : null,
            Reason == NoteReason.Debt ? null : Clean(ReturnTerms),
            Clean(CustodyPlace), OriginalReceived, OriginalReceived ? OriginalReceivedOn : null, Clean(Discrepancy),
            Attachments ?? new NoteAttachments(), Clean(DeliveredBy), Clean(ApprovedBy));
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
