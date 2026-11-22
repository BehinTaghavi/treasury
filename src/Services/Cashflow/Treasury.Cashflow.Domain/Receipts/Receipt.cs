using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Domain.Receipts;

/// <summary>
/// The kinds of receipt (new-version manual, «دریافت ها شامل»): cash into a cash desk, money paid into one of our bank accounts
/// (deposit slip, transfer, POS, card-to-card), and cheques and promissory notes, current or post-dated, guarantee or not. Each
/// kind has its own serial number per fiscal year, as in the old program (Subs.Scode 17 «دريافت نقدي», 16 «واريز به حساب», D_Check).
/// Promissory notes have a receipt of their own (owner's report no. 4, item 13, and his form «فرم دریافت سفته» of 16 Nov 2026).
/// </summary>
public enum ReceiptKind
{
    Cash,
    BankDeposit,
    Instruments,
    Notes,

    /// <summary>
    /// «واریز به کارت بانک» (owner's report no. 6, item 12; his answers to Q68–Q70, 20 Nov 2026): money a payer pays onto the bank card
    /// of one of the company's petty-cash holders, or onto a company bank card that has no bank account. Numbered in its own series.
    /// </summary>
    CardDeposit,
}

/// <summary>
/// How money reached the bank account (old D_variz «نوع واريز»; manual: «ثبت دریافتهای از محل عابر بانک – POS – (کارت به کارت)»).
/// Owner, 18 Nov 2026: the kind of a direct deposit is one of حواله پایا با شماره شبا، حواله ساتنا با شماره شبا، انتقال داخلی حساب به حساب،
/// واریز نقدی در شعبه، کارت به کارت (when the account has a bank card)، واریز به درگاه پرداخت متصل به حساب شرکت.
/// </summary>
public enum DepositMethod
{
    /// <summary>«واریز نقدی در شعبه»: cash paid in at a branch, with its slip (فیش واریزی).</summary>
    DepositSlip,

    /// <summary>«حواله / انتقال» of receipts recorded before 18 Nov 2026, when Paya, Satna and internal transfers were one kind. Not for new receipts.</summary>
    Transfer,

    /// <summary>«کارتخوان (POS)».</summary>
    Pos,

    /// <summary>«کارت به کارت»: only into an account that has a bank card.</summary>
    CardToCard,

    /// <summary>«حواله پایا با شماره شبا».</summary>
    Paya,

    /// <summary>«حواله ساتنا با شماره شبا».</summary>
    Satna,

    /// <summary>«انتقال داخلی حساب به حساب»: from another account of the same bank.</summary>
    InternalTransfer,

    /// <summary>«واریز به درگاه پرداخت متصل به حساب شرکت».</summary>
    PaymentGateway,
}

public static class DepositMethodRules
{
    /// <summary>Paya and Satna transfers are recorded with the Sheba they came from.</summary>
    public static bool NeedsSheba(this DepositMethod method) => method is DepositMethod.Paya or DepositMethod.Satna;

    /// <summary>
    /// A deposit onto a bank card is card to card, or — owner's answer to Q70, 20 Nov 2026: «ممکن است فیش واریزی داشته باشد و یا بصورت
    /// استفاده از درگاه شرکت باشد» — paid in with a deposit slip or through the company's payment gateway.
    /// </summary>
    public static bool IsForCard(this DepositMethod method) =>
        method is DepositMethod.CardToCard or DepositMethod.DepositSlip or DepositMethod.PaymentGateway;

    /// <summary>A foreign-currency account receives cash deposits and bank transfers only (owner's report no. 4, item 7).</summary>
    public static bool IsForForeignCurrency(this DepositMethod method) =>
        method is DepositMethod.DepositSlip or DepositMethod.Paya or DepositMethod.Satna or DepositMethod.InternalTransfer;

    /// <summary>
    /// An Iranian Sheba (IBAN): IR and 24 digits, checked with the IBAN check digits. The same rule as reference-svc's beneficiary Sheba;
    /// repeated here because services share no domain code (ADR 0001). Returns it without spaces and in capitals, or null when invalid.
    /// </summary>
    public static string? NormaliseSheba(string? text) => ShebaShape(text) is { } sheba && ShebaCheckDigitsFit(sheba) ? sheba : null;

    /// <summary>
    /// The text as a Sheba of the right shape — IR and 24 digits, without spaces, in capitals; the "IR" may be left out, Persian digits are
    /// read — or null. The check digits are a second question (<see cref="ShebaCheckDigitsFit"/>), so the user is told which of the two
    /// is wrong (owner's report no. 6, item 17: 24 digits were typed and the refusal spoke of the length).
    /// </summary>
    public static string? ShebaShape(string? text)
    {
        var s = new string(PersianText.Key(text).Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();
        if (s.Length == 24 && s.All(char.IsAsciiDigit))
        {
            s = "IR" + s;
        }

        return s.Length == 26 && s.StartsWith("IR", StringComparison.Ordinal) && s[2..].All(char.IsAsciiDigit) ? s : null;
    }

    /// <summary>IBAN check: move "IRkk" to the end, I = 18 and R = 27, the number mod 97 is 1.</summary>
    public static bool ShebaCheckDigitsFit(string sheba)
    {
        var remainder = 0;
        foreach (var c in sheba[4..] + "1827" + sheba[2..4])
        {
            remainder = ((remainder * 10) + (c - '0')) % 97;
        }

        return remainder == 1;
    }
}

public enum ReceiptState
{
    Recorded,
    Cancelled,

    /// <summary>
    /// «حذف شده»: recorded by mistake and deleted before its voucher was confirmed (owner's answer to Q59). The receipt stays in the
    /// list, marked, so its number is seen to be taken (owner, 20 Nov 2026, answer to Q67); its cheques and notes are gone.
    /// </summary>
    Deleted,
}

public enum ReceiptAccountKind
{
    CashDesk,
    BankAccount,

    /// <summary>The bank card of a petty-cash holder (accounts-svc: the petty cash's «نحوهٔ نگهداری وجوه»); «واریز به کارت بانک».</summary>
    PettyCashCard,

    /// <summary>A bank card of the company that has no bank account (accounts-svc: company cards); «واریز به کارت بانک».</summary>
    CompanyCard,
}

public static class ReceiptAccountKinds
{
    /// <summary>The bank cards money is paid onto on «واریز به کارت بانک».</summary>
    public static bool IsCard(this ReceiptAccountKind kind) => kind is ReceiptAccountKind.PettyCashCard or ReceiptAccountKind.CompanyCard;
}

/// <summary>A cash desk or one of our bank accounts as accounts-svc describes it when a receipt is recorded.</summary>
/// <param name="LedgerAccountCode">Its معین, for the voucher (owner's report no. 4, item 8).</param>
/// <param name="DetailCode">Its تفصیل: the cash desk holder's, or the bank account's تفصیل 1.</param>
/// <param name="HasCards">A bank account with at least one bank card (card-to-card deposits need one; owner, 18 Nov 2026).</param>
public sealed record ReceiptAccountFact(ReceiptAccountKind Kind, Guid Id, string Name, string CurrencyCode, bool IsActive, string? LedgerAccountCode = null, string? DetailCode = null,
    bool HasCards = false);

/// <summary>A purpose group (گروه بابت) as reference-svc describes it; its معین is the other side of the voucher.</summary>
public sealed record PurposeGroupFact(Guid Id, string Name, bool IsForReceipts, bool IsActive, string? LedgerAccountCode = null);

/// <summary>A payer's تفصیل as reference-svc describes it (old: Person + Tafno, "پرداخت كننده نامشخص است").</summary>
/// <param name="MayPay">Its type is a customer or a person/company (owner, 14 Nov 2026, answer to Q22).</param>
public sealed record PayerFact(string DetailCode, string Name, bool IsActive, bool MayPay = true);

/// <summary>What every receipt says, whatever its kind (new-version manual, form «دریافت چک و اسناد دریافتنی»).</summary>
/// <param name="Reference1">Value of the purpose group's first reference (عطف), e.g. an invoice or contract number.</param>
/// <param name="DepartmentId">واحد سازمانی دریافت کننده: the department that received it (optional).</param>
/// <param name="SalesProceeds">«وجوه حاصل فروش»: links the receipt to the sales system.</param>
public sealed record ReceiptHeader(
    FiscalDate Date,
    PayerFact Payer,
    PurposeGroupFact PurposeGroup,
    string Purpose,
    string? Reference1 = null,
    string? Reference2 = null,
    Guid? DepartmentId = null,
    bool SalesProceeds = false,
    string? Note = null);

/// <summary>
/// A receipt (رسید دریافت): one payer, one date, one purpose; either an amount of money into a cash desk or bank account, or one
/// or more cheques and notes, which become instruments in the cash desk (state InCashDesk) and follow their own life from there.
/// Manual: "چنانچه دردریافت چک از یک ذینفع تعدد چک وجود داشته باشد … یک سند دریافت واحد با یک شماره سریال واحد برای آنان صادر میگردد".
/// </summary>
public sealed class Receipt : AggregateRoot<Guid>
{
    public const int MaxNoteLength = 500;

    private readonly List<Guid> _instrumentIds = [];

    private Receipt(Guid id, ReceiptKind kind, int number, ReceiptHeader header, string userId)
        : base(id)
    {
        Kind = kind;
        Number = number;
        FiscalYear = header.Date.JalaliYear;
        RecordedBy = userId;
        Payer = CounterpartyRef.ForDetail(header.Payer.DetailCode, header.Payer.Name);
        PurposeGroupName = header.PurposeGroup.Name;
        Purpose = header.Purpose.Trim();
        ApplyHeader(header);
    }

    /// <summary>What the form's header says; the payer keeps its identity while its تفصیل stays the same.</summary>
    private void ApplyHeader(ReceiptHeader header)
    {
        Date = header.Date;
        if (Payer.DetailCode != header.Payer.DetailCode || Payer.DisplayName != header.Payer.Name)
        {
            Payer = CounterpartyRef.ForDetail(header.Payer.DetailCode, header.Payer.Name);
        }

        PurposeGroupId = header.PurposeGroup.Id;
        PurposeGroupName = header.PurposeGroup.Name;
        Purpose = header.Purpose.Trim();
        Reference1 = Clean(header.Reference1);
        Reference2 = Clean(header.Reference2);
        DepartmentId = header.DepartmentId;
        SalesProceeds = header.SalesProceeds;
        Note = Clean(header.Note);
    }

    public ReceiptKind Kind { get; }

    /// <summary>Serial number within the fiscal year and kind (old: Sno = DMax(Sno) + 1 per Scode and Sal).</summary>
    public int Number { get; }

    /// <summary>Jalali year of <see cref="Date"/> (open question Q5: the fiscal year is the Jalali year).</summary>
    public int FiscalYear { get; }

    public FiscalDate Date { get; private set; }

    public CounterpartyRef Payer { get; private set; }

    public Guid PurposeGroupId { get; private set; }

    public string PurposeGroupName { get; private set; }

    /// <summary>بابت, kept as typed even if the purpose list changes later (decision of 13 Nov 2026).</summary>
    public string Purpose { get; private set; }

    public string? Reference1 { get; private set; }

    public string? Reference2 { get; private set; }

    public Guid? DepartmentId { get; private set; }

    public bool SalesProceeds { get; private set; }

    public string? Note { get; private set; }

    public string RecordedBy { get; }

    /// <summary>Cash: the cash desk; bank deposit: our bank account. Null for cheques and notes (they wait in the cash desk as instruments).</summary>
    public ReceiptAccountKind? AccountKind { get; private set; }

    public Guid? AccountId { get; private set; }

    public DepositMethod? Method { get; private set; }

    /// <summary>Deposit slip, transfer tracking or POS reference number.</summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>Paya and Satna transfers: the Sheba the money came from (owner, 18 Nov 2026: «حواله … با شماره شبا»).</summary>
    public string? PayerSheba { get; private set; }

    /// <summary>Cash and bank deposits: the amount, in the currency of the cash desk or bank account.</summary>
    public Money? Amount { get; private set; }

    /// <summary>Cheques and notes received on this receipt.</summary>
    public IReadOnlyList<Guid> InstrumentIds => _instrumentIds;

    /// <summary>A receipt of promissory notes: why and how they were received (owner's form «فرم دریافت سفته»); null for the other kinds.</summary>
    public NoteReceiptTerms? NoteTerms { get; private set; }

    /// <summary>Rial total: the amount, or the sum of the cheques and notes (foreign currency at its rate).</summary>
    public decimal TotalRial { get; private set; }

    public ReceiptState State { get; private set; }

    /// <summary>A cheque receipt that replaces a cheque of the same payer (تعویض, owner's item 14 (d)): that cheque's id.</summary>
    public Guid? ReplacedInstrumentId { get; private init; }

    public FiscalDate? CancelledOn { get; private set; }

    public string? CancelReason { get; private set; }

    /// <summary>Who deleted the receipt («حذف شده»), and the computer's date and time then; null while it is not deleted.</summary>
    public string? DeletedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Cash received into a cash desk (old Access form D_Naghd, Scode 17).</summary>
    public static Receipt RecordCash(Guid id, int number, ReceiptHeader header, ReceiptAccountFact cashDesk, Money amount, string userId)
    {
        ArgumentNullException.ThrowIfNull(cashDesk);
        Require(cashDesk.Kind == ReceiptAccountKind.CashDesk, "Receipt.AccountKindWrong", "Cash is received into a cash desk.");
        return RecordMoney(id, ReceiptKind.Cash, number, header, cashDesk, null, null, amount, userId);
    }

    /// <summary>Money paid into one of our bank accounts (old Access form D_variz, Scode 16; POS and card-to-card per the manual).</summary>
    /// <param name="payerSheba">Paya and Satna: the Sheba the transfer came from.</param>
    public static Receipt RecordBankDeposit(
        Guid id, int number, ReceiptHeader header, ReceiptAccountFact bankAccount, DepositMethod method, string? trackingNumber, Money amount, string userId,
        string? payerSheba = null)
    {
        var sheba = CheckDeposit(bankAccount, method, payerSheba);
        return RecordMoney(id, ReceiptKind.BankDeposit, number, header, bankAccount, method, trackingNumber, amount, userId, sheba);
    }

    /// <summary>
    /// «واریز به کارت بانک» (owner's report no. 6, item 12): money a payer pays onto the bank card of a petty-cash holder of the company,
    /// or onto a company bank card that has no bank account. Owner's answers of 20 Nov 2026: the voucher debits the petty cash's account
    /// with the holder's تفصیل — the money is added to that petty cash — or the card's own معین and تفصیل (Q68, Q69), and credits the
    /// purpose group with the payer's تفصیل; the money comes card to card, with a deposit slip or through the company's gateway (Q70).
    /// </summary>
    public static Receipt RecordCardDeposit(
        Guid id, int number, ReceiptHeader header, ReceiptAccountFact card, DepositMethod method, string? trackingNumber, Money amount, string userId)
    {
        CheckCardDeposit(card, method);
        return RecordMoney(id, ReceiptKind.CardDeposit, number, header, card, method, trackingNumber, amount, userId);
    }

    private static void CheckCardDeposit(ReceiptAccountFact card, DepositMethod method)
    {
        ArgumentNullException.ThrowIfNull(card);
        Require(card.Kind.IsCard(), "Receipt.AccountKindWrong", "A deposit onto a bank card names a petty-cash holder's card or a company card.");
        Require(Enum.IsDefined(method), "Receipt.DepositMethodRequired", "Choose how the money was paid in.");
        Require(method.IsForCard(), "Receipt.CardDepositMethod", "A deposit onto a bank card is card to card, a deposit slip or the company's payment gateway.");
    }

    /// <summary>The rules of a deposit's kind; returns the Sheba as it is kept (Paya and Satna only).</summary>
    private static string? CheckDeposit(ReceiptAccountFact bankAccount, DepositMethod method, string? payerSheba)
    {
        ArgumentNullException.ThrowIfNull(bankAccount);
        Require(bankAccount.Kind == ReceiptAccountKind.BankAccount, "Receipt.AccountKindWrong", "A deposit is received into a bank account.");

        // Owner, 18 Nov 2026: the kind of deposit is one of the kinds he listed; the old combined «حواله / انتقال» is no longer chosen.
        Require(Enum.IsDefined(method) && method != DepositMethod.Transfer, "Receipt.DepositMethodRequired",
            "Choose how the money was paid in (old: \"نوع واريز نامشخص است\").");
        // Owner's report no. 4, item 7: a foreign-currency account receives cash deposits and bank transfers (فیش واریزی، حواله).
        Require(string.Equals(bankAccount.CurrencyCode.Trim(), "IRR", StringComparison.OrdinalIgnoreCase) || method.IsForForeignCurrency(),
            "Receipt.MethodNotForForeignCurrency", "A foreign-currency account receives cash deposits and bank transfers only.");
        Require(method != DepositMethod.CardToCard || bankAccount.HasCards, "Receipt.AccountHasNoCard", "Card-to-card is for a bank account that has a bank card.");

        string? sheba = null;
        if (method.NeedsSheba())
        {
            Require(!string.IsNullOrWhiteSpace(payerSheba), "Receipt.ShebaRequired", "Enter the Sheba number of the Paya or Satna transfer.");
            sheba = DepositMethodRules.ShebaShape(payerSheba) ?? throw new DomainException("Receipt.ShebaInvalid", "A Sheba number is IR and 24 digits.");
            Require(DepositMethodRules.ShebaCheckDigitsFit(sheba), "Receipt.ShebaCheckDigits", "The check digits of the Sheba number do not fit: a digit is wrong.");
        }

        return sheba;
    }

    /// <summary>
    /// Cheques and notes from one payer on one receipt (old Access form D_Check; manual: «ثبت چک بعدی»). The instruments are created
    /// in the same command and name this receipt; they must have been received from the same payer on the same day.
    /// </summary>
    public static Receipt RecordInstruments(Guid id, int number, ReceiptHeader header, IReadOnlyList<Instrument> instruments, string userId,
        Instrument? replaces = null)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        Validate(header, number, userId);
        if (replaces is not null)
        {
            // Owner's item 14 (d), answer to Q31: the new cheques may differ in amount and number; they come from the same payer.
            Require(replaces.Direction == InstrumentDirection.Received && replaces.Counterparty.DetailCode == header.Payer.DetailCode,
                "Receipt.ReplacementPayerDiffers", "The new cheques must come from the payer of the cheque they replace.");
        }

        CheckCheques(instruments, replacement: replaces is not null);
        var receipt = new Receipt(id, ReceiptKind.Instruments, number, header, userId) { ReplacedInstrumentId = replaces?.Id };
        receipt.SetInstruments(instruments);
        receipt.Raise(receipt.Recorded());
        return receipt;
    }

    private static void CheckCheques(IReadOnlyList<Instrument> instruments, bool replacement)
    {
        Require(!replacement || instruments.All(i => i.Kind.IsCollectable()), "Receipt.ReplacementKind", "A cheque is replaced by ordinary or bank cheques.");
        Require(instruments.Count > 0, "Receipt.NoInstruments", "Enter at least one cheque or note.");
        foreach (var i in instruments.Where(i => i.Kind.IsCheque() && i.Details.SayadId is null))
        {
            // Owner, 14 Nov 2026 (answer to Q21): every received cheque carries its 16-digit Sayad ID.
            throw new DomainException("Receipt.SayadIdRequired", $"Enter the Sayad ID of cheque {i.Number}.");
        }
    }

    /// <summary>The cheques or notes of the receipt, each received from its payer on its date.</summary>
    private void SetInstruments(IReadOnlyList<Instrument> instruments)
    {
        foreach (var i in instruments)
        {
            Require(i.Direction == InstrumentDirection.Received && i.ReceiptId == Id && i.IssueDate == Date && i.Counterparty.Id == Payer.Id,
                "Receipt.InstrumentMismatch", "Every cheque and note must be received from this payer on this receipt's date.");
        }

        _instrumentIds.Clear();
        _instrumentIds.AddRange(instruments.Select(i => i.Id));
        TotalRial = instruments.Sum(i => i.Amount.ToRial());
    }

    /// <summary>
    /// Promissory notes of one issuer on one receipt (owner's form «فرم دریافت سفته», 16 Nov 2026; report no. 4, item 13). Every note
    /// becomes an instrument in the cash desk: a guarantee note when the notes are held as security or in trust, otherwise an ordinary
    /// note. With «معین» every note has its due date; with «عندالمطالبه» none has (it can be collected whenever its holder wishes).
    /// Every note must have passed the checks of article 308 (owner's answer to Q40). The receipt records the handing over of the
    /// notes only; it is not a receipt of money. Who signs it as «تأییدکننده» is chosen among the holders of a permission in the
    /// operation «دریافت سفته» (Basic Information → «دارندگان مجوزها», owner's report no. 8; before it, the signature rule of 16 Nov 2026).
    /// </summary>
    /// <param name="signers">The holders of a permission in the receipt of notes; null when there is none (nobody to choose).</param>
    /// <param name="signerIds">The persons chosen on the form.</param>
    public static Receipt RecordNotes(Guid id, int number, ReceiptHeader header, NoteReceiptTerms terms, IReadOnlyList<Instrument> notes, string userId,
        FormSigners? signers = null, IReadOnlyList<Guid>? signerIds = null)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(notes);
        Validate(header, number, userId);
        var checkedTerms = CheckNotes(terms, notes, signers, signerIds);
        var receipt = new Receipt(id, ReceiptKind.Notes, number, header, userId) { NoteTerms = checkedTerms };
        receipt.SetInstruments(notes);
        receipt.Raise(receipt.Recorded());
        return receipt;
    }

    private static NoteReceiptTerms CheckNotes(NoteReceiptTerms terms, IReadOnlyList<Instrument> notes, FormSigners? signers, IReadOnlyList<Guid>? signerIds)
    {
        var checkedTerms = terms.Validated() with { Signers = (signers ?? FormSigners.None).Choose(signerIds) };
        Require(notes.Count > 0, "Receipt.NoInstruments", "Enter at least one cheque or note.");
        var onDemand = checkedTerms.Maturity == NoteMaturity.OnDemand;
        foreach (var n in notes)
        {
            Require(n.Kind == checkedTerms.InstrumentKind && n.Details.OnDemand == onDemand, "Receipt.NoteKindMismatch",
                "Every note on the receipt follows the receipt's reason and kind of due date.");
            Require(onDemand || n.DueDate is not null, "Instrument.DueDateRequired", "Due date is required.");
            Require(n.Details.Checks?.IsComplete(onDemand) == true, "Receipt.NoteChecksIncomplete",
                $"Note {n.Number} is received only when every item of article 308 is checked on it.");
        }

        return checkedTerms;
    }

    /// <summary>
    /// Correction («ویرایش») of a saved receipt (owner's report no. 6, items 2 and 6: "تا زمانیکه سند ارسال نشده است امکان ویرایش فرم دریافت",
    /// "تا زمانیکه … «تایید سند» حسابداری نشده است بتوان آنرا ویرایش نمود"). A receipt is corrected with the rules it was recorded with,
    /// while it is not cancelled and its voucher is not confirmed. It keeps its kind, its number and so its fiscal year (numbers are given
    /// per year); a replacement receipt keeps its payer, who gave the replaced cheque.
    /// </summary>
    public void CorrectCash(ReceiptHeader header, ReceiptAccountFact cashDesk, Money amount, string userId, bool voucherConfirmed)
    {
        ArgumentNullException.ThrowIfNull(cashDesk);
        RequireCorrectable(ReceiptKind.Cash, header, userId, voucherConfirmed);
        Require(cashDesk.Kind == ReceiptAccountKind.CashDesk, "Receipt.AccountKindWrong", "Cash is received into a cash desk.");
        CheckMoney(cashDesk, amount);
        ApplyHeader(header);
        SetMoney(cashDesk, null, null, amount, null);
        Raise(Corrected(userId));
    }

    /// <inheritdoc cref="CorrectCash"/>
    public void CorrectBankDeposit(ReceiptHeader header, ReceiptAccountFact bankAccount, DepositMethod method, string? trackingNumber, Money amount, string userId,
        string? payerSheba, bool voucherConfirmed)
    {
        RequireCorrectable(ReceiptKind.BankDeposit, header, userId, voucherConfirmed);
        var sheba = CheckDeposit(bankAccount, method, payerSheba);
        CheckMoney(bankAccount, amount);
        ApplyHeader(header);
        SetMoney(bankAccount, method, trackingNumber, amount, sheba);
        Raise(Corrected(userId));
    }

    /// <inheritdoc cref="CorrectCash"/>
    public void CorrectCardDeposit(ReceiptHeader header, ReceiptAccountFact card, DepositMethod method, string? trackingNumber, Money amount, string userId, bool voucherConfirmed)
    {
        RequireCorrectable(ReceiptKind.CardDeposit, header, userId, voucherConfirmed);
        CheckCardDeposit(card, method);
        CheckMoney(card, amount);
        ApplyHeader(header);
        SetMoney(card, method, trackingNumber, amount, null);
        Raise(Corrected(userId));
    }

    /// <summary>
    /// Correction of a cheque receipt: <paramref name="instruments"/> are its cheques as they now are — corrected (which a cheque
    /// allows only while it is in the cash desk and no step was recorded on it), added, left out (removed by the caller), or left as
    /// they are because a step was recorded on them. Owner, 19 Nov 2026 (answer to Q60): "فقط آن رکوردی که صیادی شده قابل ویرایش
    /// نخواهد ماند، بقیه چک‌های داخل آن رسید قابل ویرایش و حذف خواهند بود" — so a cheque that moved on fixes only itself, and with it
    /// the receipt's date and payer, which are written on every cheque of the receipt.
    /// </summary>
    public void CorrectInstruments(ReceiptHeader header, IReadOnlyList<Instrument> instruments, string userId, bool voucherConfirmed)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        RequireCorrectable(ReceiptKind.Instruments, header, userId, voucherConfirmed);
        RequireHeaderKept(header, instruments);
        Require(ReplacedInstrumentId is null || header.Payer.DetailCode == Payer.DetailCode,
            "Receipt.ReplacementPayerDiffers", "The new cheques must come from the payer of the cheque they replace.");
        CheckCheques(instruments, replacement: ReplacedInstrumentId is not null);
        ApplyHeader(header);
        SetInstruments(instruments);
        Raise(Corrected(userId));
    }

    /// <summary>Correction of a receipt of promissory notes, with the rules of <see cref="RecordNotes"/>.</summary>
    public void CorrectNotes(ReceiptHeader header, NoteReceiptTerms terms, IReadOnlyList<Instrument> notes, string userId, bool voucherConfirmed,
        FormSigners? signers = null, IReadOnlyList<Guid>? signerIds = null)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(notes);
        RequireCorrectable(ReceiptKind.Notes, header, userId, voucherConfirmed);
        RequireHeaderKept(header, notes);
        var checkedTerms = CheckNotes(terms, notes, signers, signerIds);
        ApplyHeader(header);
        NoteTerms = checkedTerms;
        SetInstruments(notes);
        Raise(Corrected(userId));
    }

    /// <summary>A receipt is corrected while it is not cancelled and its voucher is not confirmed.</summary>
    /// <param name="voucherConfirmed">The treasurer confirmed the receipt's voucher («تایید سند»).</param>
    public void EnsureCorrectable(bool voucherConfirmed)
    {
        Require(State != ReceiptState.Deleted, "Receipt.Deleted", "The receipt was deleted.");
        Require(State == ReceiptState.Recorded, "Receipt.AlreadyCancelled", "The receipt is already cancelled.");
        Require(!voucherConfirmed, "Receipt.VoucherConfirmed", "The receipt's voucher is confirmed; it can no longer be changed.");
    }

    /// <summary>
    /// The code of the rule that closes «ویرایش» for this receipt, or null while it can be corrected: deleted, cancelled, or voucher
    /// confirmed. A cheque on which a step was recorded no longer closes the correction of its receipt, only its own (answer to Q60).
    /// </summary>
    public string? WhyNotCorrectable(bool voucherConfirmed) =>
        State == ReceiptState.Deleted ? "Receipt.Deleted"
        : State != ReceiptState.Recorded ? "Receipt.AlreadyCancelled"
        : voucherConfirmed ? "Receipt.VoucherConfirmed"
        : null;

    /// <summary>
    /// The code of the rule that closes «حذف» for this receipt, or null while it can be deleted (see <see cref="Delete"/>).
    /// </summary>
    public string? WhyNotDeletable(bool voucherConfirmed, IReadOnlyList<Instrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        return WhyNotCorrectable(voucherConfirmed)
            ?? (ReplacedInstrumentId is not null ? "Receipt.ReplacementNotDeleted"
            : instruments.Any(i => !i.IsAsReceived) ? "Instrument.CorrectionAfterStep"
            : null);
    }

    /// <summary>
    /// Deletion («حذف») of a receipt recorded by mistake, before its voucher is confirmed (owner, 19 Nov 2026, answer to Q59: "امکان حذف
    /// هم لازم است در این مرحله در کنار ویرایش پیش بینی شود"). The receipt becomes «حذف شده» and stays in the receipts list, marked,
    /// with who deleted it and when, so its number is seen to be taken (owner, 20 Nov 2026, answer to Q67: "رسید حذف شده با ترام قرمز
    /// کمرنگ با عبارت بزرگ شده «حذف شده» در ردیف رسیدها"); the caller deletes the cheques or notes on it, which leave the cheque lists.
    /// Not deleted: a cancelled receipt (it stays as the record of its cancellation), a receipt with a cheque on which a step was
    /// recorded (take the other cheques off it with «ویرایش»), and a receipt that replaced a cheque (the replaced cheque names it; its
    /// new cheques are corrected, returned or cancelled instead).
    /// </summary>
    /// <param name="instruments">The cheques or notes of the receipt as stored.</param>
    /// <param name="at">The computer's date and time, kept with the user who deleted it.</param>
    public void Delete(string userId, bool voucherConfirmed, IReadOnlyList<Instrument> instruments, DateTimeOffset at)
    {
        Require(!string.IsNullOrWhiteSpace(userId), "Receipt.UserRequired", "User is required.");
        if (WhyNotDeletable(voucherConfirmed, instruments) is { } code)
        {
            throw new DomainException(code, "The receipt can no longer be deleted.");
        }

        State = ReceiptState.Deleted;
        DeletedBy = userId.Trim();
        DeletedAt = at;
        Raise(new ReceiptDeleted(Id, Kind, FiscalYear, Number, Date, Payer.DetailCode, TotalRial, [.. _instrumentIds], DeletedBy));
    }

    /// <summary>
    /// A cheque or note on which a step was recorded keeps what is written on it, so the receipt keeps the date and payer it shares
    /// with it; everything else of the receipt, and its other cheques, can still be corrected (answer to Q60).
    /// </summary>
    private static void RequireHeaderKept(ReceiptHeader header, IReadOnlyList<Instrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(header);
        Require(instruments.Where(i => !i.IsAsReceived).All(i => i.IssueDate == header.Date && i.Counterparty.DetailCode == header.Payer?.DetailCode),
            "Receipt.FixedByMovedCheque", "A cheque or note of this receipt has a step recorded on it; the receipt's date and payer can no longer change.");
    }

    private void RequireCorrectable(ReceiptKind kind, ReceiptHeader header, string userId, bool voucherConfirmed)
    {
        Require(Kind == kind, "Receipt.CorrectionKindDiffers", "A receipt keeps its kind; cancel it and record a receipt of the other kind.");
        EnsureCorrectable(voucherConfirmed);
        Validate(header, Number, userId);
        Require(header.Date.JalaliYear == FiscalYear, "Receipt.CorrectionOtherYear",
            $"The receipt's number belongs to fiscal year {FiscalYear}; its date stays in that year.");
    }

    /// <summary>
    /// Cancels (ابطال) a cash or bank receipt entered by mistake. Cheques and notes are not cancelled with their receipt: each one is
    /// returned to the payer (عودت) through its own steps, which keeps its history.
    /// </summary>
    public void Cancel(FiscalDate date, string reason, string userId)
    {
        Require(Kind is not (ReceiptKind.Instruments or ReceiptKind.Notes), "Receipt.CancelInstruments", "Return each cheque or note to the payer instead of cancelling the receipt.");
        Require(State != ReceiptState.Deleted, "Receipt.Deleted", "The receipt was deleted.");
        Require(State == ReceiptState.Recorded, "Receipt.AlreadyCancelled", "The receipt is already cancelled.");
        Require(!string.IsNullOrWhiteSpace(reason), "Receipt.CancelReasonRequired", "Give the reason for cancelling.");
        Require(!string.IsNullOrWhiteSpace(userId), "Receipt.UserRequired", "User is required.");
        Require(date >= Date, "Receipt.CancelBeforeReceipt", "The cancellation cannot be dated before the receipt.");
        State = ReceiptState.Cancelled;
        CancelledOn = date;
        CancelReason = reason.Trim();
        Raise(new ReceiptCancelled(Id, Kind, FiscalYear, Number, date, CancelReason, userId));
    }

    public static Receipt Restore(
        Guid id, ReceiptKind kind, int number, FiscalDate date, CounterpartyRef payer, Guid purposeGroupId, string purposeGroupName, string purpose,
        string? reference1, string? reference2, Guid? departmentId, bool salesProceeds, string? note, string recordedBy,
        ReceiptAccountKind? accountKind, Guid? accountId, DepositMethod? method, string? trackingNumber, Money? amount,
        IEnumerable<Guid> instrumentIds, decimal totalRial, ReceiptState state, FiscalDate? cancelledOn, string? cancelReason, long version,
        Guid? replacedInstrumentId = null, string? payerSheba = null, NoteReceiptTerms? noteTerms = null, string? deletedBy = null, DateTimeOffset? deletedAt = null)
    {
        ArgumentNullException.ThrowIfNull(payer);
        var header = new ReceiptHeader(date, new PayerFact(payer.DetailCode ?? string.Empty, payer.DisplayName, true, true),
            new PurposeGroupFact(purposeGroupId, purposeGroupName, true, true), purpose, reference1, reference2, departmentId, salesProceeds, note);
        var r = new Receipt(id, kind, number, header, recordedBy)
        {
            AccountKind = accountKind,
            AccountId = accountId,
            Method = method,
            TrackingNumber = trackingNumber,
            Amount = amount,
            TotalRial = totalRial,
            State = state,
            CancelledOn = cancelledOn,
            CancelReason = cancelReason,
            ReplacedInstrumentId = replacedInstrumentId,
            PayerSheba = payerSheba,
            NoteTerms = noteTerms,
            DeletedBy = deletedBy,
            DeletedAt = deletedAt,
        };
        r._instrumentIds.AddRange(instrumentIds);
        r.SetLoadedVersion(version);
        return r;
    }

    private static Receipt RecordMoney(
        Guid id, ReceiptKind kind, int number, ReceiptHeader header, ReceiptAccountFact account, DepositMethod? method, string? tracking, Money amount, string userId,
        string? payerSheba = null)
    {
        Validate(header, number, userId);
        CheckMoney(account, amount);
        var receipt = new Receipt(id, kind, number, header, userId);
        receipt.SetMoney(account, method, tracking, amount, payerSheba);
        receipt.Raise(receipt.Recorded());
        return receipt;
    }

    private static void CheckMoney(ReceiptAccountFact account, Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        Require(account.IsActive, "Receipt.AccountInactive", "The cash desk, bank account or bank card is switched off.");
        Require(string.Equals(account.CurrencyCode.Trim(), amount.Currency, StringComparison.OrdinalIgnoreCase), "Receipt.CurrencyMismatch",
            $"The amount must be in the account's currency ({account.CurrencyCode}).");
    }

    private void SetMoney(ReceiptAccountFact account, DepositMethod? method, string? tracking, Money amount, string? payerSheba)
    {
        AccountKind = account.Kind;
        AccountId = account.Id;
        Method = method;
        TrackingNumber = Clean(tracking);
        PayerSheba = payerSheba;
        Amount = amount;
        TotalRial = amount.ToRial();
    }

    private ReceiptCorrected Corrected(string userId) => new(
        Id, Kind, FiscalYear, Number, Date, Payer.DetailCode, PurposeGroupId, AccountKind, AccountId, Amount?.Amount, Amount?.Currency, TotalRial,
        [.. _instrumentIds], SalesProceeds, userId);

    private ReceiptRecorded Recorded() => new(
        Id, Kind, FiscalYear, Number, Date, Payer.DetailCode, PurposeGroupId, AccountKind, AccountId, Amount?.Amount, Amount?.Currency, TotalRial,
        [.. _instrumentIds], SalesProceeds, RecordedBy);

    private static void Validate(ReceiptHeader header, int number, string userId)
    {
        ArgumentNullException.ThrowIfNull(header);
        Require(number > 0, "Receipt.NumberInvalid", "The receipt number must be positive.");
        Require(!string.IsNullOrWhiteSpace(userId), "Receipt.UserRequired", "User is required.");
        Require(header.Payer is not null && !string.IsNullOrWhiteSpace(header.Payer.DetailCode), "Receipt.PayerRequired", "Choose the payer (پرداخت کننده).");
        Require(header.Payer!.IsActive, "Receipt.PayerInactive", "The payer's تفصیل is switched off.");
        Require(header.Payer.MayPay, "Receipt.PayerTypeNotAllowed", "The payer must be a customer or a person or company (owner, answer to Q22).");
        Require(header.PurposeGroup is not null, "Receipt.PurposeGroupRequired", "Choose the purpose group (گروه بابت).");
        Require(header.PurposeGroup!.IsForReceipts, "Receipt.PurposeGroupNotForReceipts", "This purpose group is for payments.");
        Require(header.PurposeGroup.IsActive, "Receipt.PurposeGroupInactive", "This purpose group is switched off.");
        Require(!string.IsNullOrWhiteSpace(header.Purpose), "Receipt.PurposeRequired", "Enter the purpose (بابت).");
        Require((header.Note?.Length ?? 0) <= MaxNoteLength, "Receipt.NoteTooLong", "The note is too long.");
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

public sealed record ReceiptRecorded(
    Guid ReceiptId, ReceiptKind Kind, int FiscalYear, int Number, FiscalDate Date, string? PayerDetailCode, Guid PurposeGroupId,
    ReceiptAccountKind? AccountKind, Guid? AccountId, decimal? Amount, string? Currency, decimal TotalRial, IReadOnlyList<Guid> InstrumentIds,
    bool SalesProceeds, string UserId) : DomainEvent;

/// <summary>A saved receipt was corrected before its voucher was confirmed; it carries the receipt as it now is.</summary>
public sealed record ReceiptCorrected(
    Guid ReceiptId, ReceiptKind Kind, int FiscalYear, int Number, FiscalDate Date, string? PayerDetailCode, Guid PurposeGroupId,
    ReceiptAccountKind? AccountKind, Guid? AccountId, decimal? Amount, string? Currency, decimal TotalRial, IReadOnlyList<Guid> InstrumentIds,
    bool SalesProceeds, string UserId) : DomainEvent;

/// <summary>A receipt recorded by mistake was marked «حذف شده», and the cheques or notes on it deleted, before its voucher was confirmed.</summary>
public sealed record ReceiptDeleted(
    Guid ReceiptId, ReceiptKind Kind, int FiscalYear, int Number, FiscalDate Date, string? PayerDetailCode, decimal TotalRial, IReadOnlyList<Guid> InstrumentIds,
    string UserId) : DomainEvent;

public sealed record ReceiptCancelled(Guid ReceiptId, ReceiptKind Kind, int FiscalYear, int Number, FiscalDate Date, string Reason, string UserId) : DomainEvent;
