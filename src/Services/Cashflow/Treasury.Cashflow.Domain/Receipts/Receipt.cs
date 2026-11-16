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
}

/// <summary>How money reached the bank account (old D_variz «نوع واريز»; manual: «ثبت دریافتهای از محل عابر بانک – POS – (کارت به کارت)»).</summary>
public enum DepositMethod
{
    DepositSlip,
    Transfer,
    Pos,
    CardToCard,
}

public enum ReceiptState
{
    Recorded,
    Cancelled,
}

public enum ReceiptAccountKind
{
    CashDesk,
    BankAccount,
}

/// <summary>A cash desk or one of our bank accounts as accounts-svc describes it when a receipt is recorded.</summary>
/// <param name="LedgerAccountCode">Its معین, for the voucher (owner's report no. 4, item 8).</param>
/// <param name="DetailCode">Its تفصیل: the cash desk holder's, or the bank account's تفصیل 1.</param>
public sealed record ReceiptAccountFact(ReceiptAccountKind Kind, Guid Id, string Name, string CurrencyCode, bool IsActive, string? LedgerAccountCode = null, string? DetailCode = null);

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
        Date = header.Date;
        Payer = CounterpartyRef.ForDetail(header.Payer.DetailCode, header.Payer.Name);
        PurposeGroupId = header.PurposeGroup.Id;
        PurposeGroupName = header.PurposeGroup.Name;
        Purpose = header.Purpose.Trim();
        Reference1 = Clean(header.Reference1);
        Reference2 = Clean(header.Reference2);
        DepartmentId = header.DepartmentId;
        SalesProceeds = header.SalesProceeds;
        Note = Clean(header.Note);
        RecordedBy = userId;
    }

    public ReceiptKind Kind { get; }

    /// <summary>Serial number within the fiscal year and kind (old: Sno = DMax(Sno) + 1 per Scode and Sal).</summary>
    public int Number { get; }

    /// <summary>Jalali year of <see cref="Date"/> (open question Q5: the fiscal year is the Jalali year).</summary>
    public int FiscalYear { get; }

    public FiscalDate Date { get; }

    public CounterpartyRef Payer { get; }

    public Guid PurposeGroupId { get; }

    public string PurposeGroupName { get; }

    /// <summary>بابت, kept as typed even if the purpose list changes later (decision of 13 Nov 2026).</summary>
    public string Purpose { get; }

    public string? Reference1 { get; }

    public string? Reference2 { get; }

    public Guid? DepartmentId { get; }

    public bool SalesProceeds { get; }

    public string? Note { get; }

    public string RecordedBy { get; }

    /// <summary>Cash: the cash desk; bank deposit: our bank account. Null for cheques and notes (they wait in the cash desk as instruments).</summary>
    public ReceiptAccountKind? AccountKind { get; private init; }

    public Guid? AccountId { get; private init; }

    public DepositMethod? Method { get; private init; }

    /// <summary>Deposit slip, transfer tracking or POS reference number.</summary>
    public string? TrackingNumber { get; private init; }

    /// <summary>Cash and bank deposits: the amount, in the currency of the cash desk or bank account.</summary>
    public Money? Amount { get; private init; }

    /// <summary>Cheques and notes received on this receipt.</summary>
    public IReadOnlyList<Guid> InstrumentIds => _instrumentIds;

    /// <summary>A receipt of promissory notes: why and how they were received (owner's form «فرم دریافت سفته»); null for the other kinds.</summary>
    public NoteReceiptTerms? NoteTerms { get; private init; }

    /// <summary>Rial total: the amount, or the sum of the cheques and notes (foreign currency at its rate).</summary>
    public decimal TotalRial { get; private set; }

    public ReceiptState State { get; private set; }

    /// <summary>A cheque receipt that replaces a cheque of the same payer (تعویض, owner's item 14 (d)): that cheque's id.</summary>
    public Guid? ReplacedInstrumentId { get; private init; }

    public FiscalDate? CancelledOn { get; private set; }

    public string? CancelReason { get; private set; }

    /// <summary>Cash received into a cash desk (old Access form D_Naghd, Scode 17).</summary>
    public static Receipt RecordCash(Guid id, int number, ReceiptHeader header, ReceiptAccountFact cashDesk, Money amount, string userId)
    {
        ArgumentNullException.ThrowIfNull(cashDesk);
        Require(cashDesk.Kind == ReceiptAccountKind.CashDesk, "Receipt.AccountKindWrong", "Cash is received into a cash desk.");
        return RecordMoney(id, ReceiptKind.Cash, number, header, cashDesk, null, null, amount, userId);
    }

    /// <summary>Money paid into one of our bank accounts (old Access form D_variz, Scode 16; POS and card-to-card per the manual).</summary>
    public static Receipt RecordBankDeposit(
        Guid id, int number, ReceiptHeader header, ReceiptAccountFact bankAccount, DepositMethod method, string? trackingNumber, Money amount, string userId)
    {
        ArgumentNullException.ThrowIfNull(bankAccount);
        Require(bankAccount.Kind == ReceiptAccountKind.BankAccount, "Receipt.AccountKindWrong", "A deposit is received into a bank account.");
        Require(Enum.IsDefined(method), "Receipt.DepositMethodRequired", "Choose how the money was paid in (old: \"نوع واريز نامشخص است\").");
        // Owner's report no. 4, item 7: a foreign-currency account receives cash deposits and bank transfers (فیش واریزی، حواله).
        Require(string.Equals(bankAccount.CurrencyCode.Trim(), "IRR", StringComparison.OrdinalIgnoreCase) || method is DepositMethod.DepositSlip or DepositMethod.Transfer,
            "Receipt.MethodNotForForeignCurrency", "A foreign-currency account receives cash deposits and bank transfers only.");
        return RecordMoney(id, ReceiptKind.BankDeposit, number, header, bankAccount, method, trackingNumber, amount, userId);
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
            Require(instruments.All(i => i.Kind.IsCollectable()), "Receipt.ReplacementKind", "A cheque is replaced by ordinary or bank cheques.");
        }

        Require(instruments.Count > 0, "Receipt.NoInstruments", "Enter at least one cheque or note.");
        foreach (var i in instruments.Where(i => i.Kind.IsCheque() && i.Details.SayadId is null))
        {
            // Owner, 14 Nov 2026 (answer to Q21): every received cheque carries its 16-digit Sayad ID.
            throw new DomainException("Receipt.SayadIdRequired", $"Enter the Sayad ID of cheque {i.Number}.");
        }

        var receipt = new Receipt(id, ReceiptKind.Instruments, number, header, userId) { ReplacedInstrumentId = replaces?.Id };
        foreach (var i in instruments)
        {
            Require(i.Direction == InstrumentDirection.Received && i.ReceiptId == id && i.IssueDate == header.Date && i.Counterparty.Id == receipt.Payer.Id,
                "Receipt.InstrumentMismatch", "Every cheque and note must be received from this payer on this receipt's date.");
        }

        receipt._instrumentIds.AddRange(instruments.Select(i => i.Id));
        receipt.TotalRial = instruments.Sum(i => i.Amount.ToRial());
        receipt.Raise(receipt.Recorded());
        return receipt;
    }

    /// <summary>
    /// Promissory notes of one issuer on one receipt (owner's form «فرم دریافت سفته», 16 Nov 2026; report no. 4, item 13). Every note
    /// becomes an instrument in the cash desk: a guarantee note when the notes are held as security or in trust, otherwise an ordinary
    /// note. With «معین» every note has its due date; with «عندالمطالبه» none has. The receipt records the handing over of the notes
    /// only; it is not a receipt of money.
    /// </summary>
    public static Receipt RecordNotes(Guid id, int number, ReceiptHeader header, NoteReceiptTerms terms, IReadOnlyList<Instrument> notes, string userId)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(notes);
        Validate(header, number, userId);
        var checkedTerms = terms.Validated();
        Require(notes.Count > 0, "Receipt.NoInstruments", "Enter at least one cheque or note.");
        var onDemand = checkedTerms.Maturity == NoteMaturity.OnDemand;
        var receipt = new Receipt(id, ReceiptKind.Notes, number, header, userId) { NoteTerms = checkedTerms };
        foreach (var n in notes)
        {
            Require(n.Kind == checkedTerms.InstrumentKind && n.Details.OnDemand == onDemand, "Receipt.NoteKindMismatch",
                "Every note on the receipt follows the receipt's reason and kind of due date.");
            Require(onDemand || n.DueDate is not null, "Instrument.DueDateRequired", "Due date is required.");
            Require(n.Direction == InstrumentDirection.Received && n.ReceiptId == id && n.IssueDate == header.Date && n.Counterparty.Id == receipt.Payer.Id,
                "Receipt.InstrumentMismatch", "Every cheque and note must be received from this payer on this receipt's date.");
        }

        receipt._instrumentIds.AddRange(notes.Select(n => n.Id));
        receipt.TotalRial = notes.Sum(n => n.Amount.ToRial());
        receipt.Raise(receipt.Recorded());
        return receipt;
    }

    /// <summary>
    /// Cancels (ابطال) a cash or bank receipt entered by mistake. Cheques and notes are not cancelled with their receipt: each one is
    /// returned to the payer (عودت) through its own steps, which keeps its history.
    /// </summary>
    public void Cancel(FiscalDate date, string reason, string userId)
    {
        Require(Kind is not (ReceiptKind.Instruments or ReceiptKind.Notes), "Receipt.CancelInstruments", "Return each cheque or note to the payer instead of cancelling the receipt.");
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
        Guid? replacedInstrumentId = null, NoteReceiptTerms? noteTerms = null)
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
            NoteTerms = noteTerms,
        };
        r._instrumentIds.AddRange(instrumentIds);
        r.SetLoadedVersion(version);
        return r;
    }

    private static Receipt RecordMoney(
        Guid id, ReceiptKind kind, int number, ReceiptHeader header, ReceiptAccountFact account, DepositMethod? method, string? tracking, Money amount, string userId)
    {
        ArgumentNullException.ThrowIfNull(amount);
        Validate(header, number, userId);
        Require(account.IsActive, "Receipt.AccountInactive", "The cash desk or bank account is switched off.");
        Require(string.Equals(account.CurrencyCode.Trim(), amount.Currency, StringComparison.OrdinalIgnoreCase), "Receipt.CurrencyMismatch",
            $"The amount must be in the account's currency ({account.CurrencyCode}).");
        var receipt = new Receipt(id, kind, number, header, userId)
        {
            AccountKind = account.Kind,
            AccountId = account.Id,
            Method = method,
            TrackingNumber = Clean(tracking),
            Amount = amount,
            TotalRial = amount.ToRial(),
        };
        receipt.Raise(receipt.Recorded());
        return receipt;
    }

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

public sealed record ReceiptCancelled(Guid ReceiptId, ReceiptKind Kind, int FiscalYear, int Number, FiscalDate Date, string Reason, string UserId) : DomainEvent;
