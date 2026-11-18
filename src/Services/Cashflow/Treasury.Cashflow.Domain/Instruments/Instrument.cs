using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;

namespace Treasury.Cashflow.Domain.Instruments;

/// <summary>A party on the other side of the instrument (drawer of a received cheque, payee of an issued one).</summary>
/// <param name="DetailCode">The party's تفصیل in the accounting system, when known (payers on receipts).</param>
public sealed record CounterpartyRef(Guid Id, string DisplayName, string? DetailCode = null)
{
    /// <summary>
    /// A party known by its تفصیل code. The id is derived from the code (name-based UUID from SHA-256, RFC 9562 version 8), so every receipt
    /// and instrument of the same تفصیل carries the same id without a separate counterparty table.
    /// </summary>
    public static CounterpartyRef ForDetail(string detailCode, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detailCode);
        var code = detailCode.Trim();
        return new CounterpartyRef(DetailId(code), name.Trim(), code);
    }

    private static readonly Guid DetailNamespace = new("6f1c2a52-7d8e-4b55-9c3a-0e5b7d1f4a21");

    private static Guid DetailId(string code)
    {
        var ns = DetailNamespace.ToByteArray(bigEndian: true);
        var name = System.Text.Encoding.UTF8.GetBytes("detail:" + code);
        var hash = System.Security.Cryptography.SHA256.HashData([.. ns, .. name]);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x80);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}

/// <summary>One of our bank accounts as accounts-svc describes it when a cheque is deposited there for collection.</summary>
public sealed record CollectionAccount(Guid Id, string Title, string CurrencyCode, bool IsActive);

/// <summary>One step in the instrument's life. Replaces the old fixed columns D1–D5 / G1–G5 with a full history.</summary>
/// <param name="Reference">Deposit slip, bounce notice, management's order or bank reference, depending on the step.</param>
/// <param name="Details">What else the step recorded (bank account, receiver, order date…); null for older steps.</param>
public sealed record InstrumentHistoryEntry(
    InstrumentAction Action,
    InstrumentState? From,
    InstrumentState To,
    FiscalDate Date,
    string UserId,
    string? Reference,
    string? Note,
    StepDetails? Details = null);

/// <summary>
/// What a step of a received cheque records besides its date, reference and note (owner's item 14, 15 Nov 2026), so that each step's
/// voucher keeps its own accounts even when the cheque moves on.
/// </summary>
/// <param name="AccountId">Deposit, collected, bounced, returned from the bank: the company bank account of that deposit.</param>
/// <param name="PartyDetailCode">Endorsement: the receiver's تفصیل (any تفصیل, answer to Q32).</param>
/// <param name="DocumentDate">Cancellation: the date of management's order (answer to Q30).</param>
/// <param name="PurposeGroupId">Endorsement: the payment purpose group (its account is debited).</param>
/// <param name="SayadTracking">Endorsement: tracking number of the transfer in the Sayad system (answer to Q32).</param>
/// <param name="ReceiptId">Replacement: the cheque receipt of the new cheques.</param>
/// <param name="PaymentRequestId">Endorsement: the approved payment request the cheque pays (answer to Q36).</param>
/// <param name="TrackingCode">Legal action: the tracking code of the certificate of non-payment (گواهی عدم پرداخت).</param>
public sealed record StepDetails(
    Guid? AccountId = null,
    string? PartyDetailCode = null,
    string? PartyName = null,
    FiscalDate? DocumentDate = null,
    Guid? PurposeGroupId = null,
    string? Purpose = null,
    string? SayadTracking = null,
    Guid? ReceiptId = null,
    Guid? PaymentRequestId = null,
    string? TrackingCode = null);

/// <summary>
/// The bank's certificate of non-payment («گواهی عدم پرداخت», the owner's term of 18 Nov 2026 for what was called the bounce notice): its
/// number, date and tracking code, as entered on the legal-action screen.
/// </summary>
public sealed record NonPaymentCertificate(string? Number, FiscalDate? Date, string? TrackingCode);

/// <summary>
/// An endorsement (خرج چک): it pays an approved payment request (owner, answer to Q36), whose beneficiary receives the cheque and whose
/// payment purpose group and purpose it takes; the Sayad transfer's tracking number is recorded (answer to Q32).
/// </summary>
public sealed record Endorsement(string ReceiverDetailCode, string ReceiverName, Guid PurposeGroupId, string Purpose, string SayadTracking, Guid PaymentRequestId);

/// <summary>
/// A received or issued cheque, promissory note or similar instrument, with its whole life across fiscal years.
/// Old system: one row of dbo.Checks (kept in the shared Cashnajoo86 database). MVC system: Receipt_Detail + Receipt_Detail_PlanState.
/// </summary>
public sealed class Instrument : AggregateRoot<Guid>
{
    private readonly List<InstrumentHistoryEntry> _history = [];

    private Instrument(
        Guid id,
        InstrumentDirection direction,
        InstrumentKind kind,
        Money amount,
        FiscalDate issueDate,
        FiscalDate? dueDate,
        CounterpartyRef counterparty,
        string purpose,
        string? bankName)
        : base(id)
    {
        Direction = direction;
        Kind = kind;
        Amount = amount;
        IssueDate = issueDate;
        DueDate = dueDate;
        Counterparty = counterparty;
        Purpose = purpose;
        BankName = bankName;
    }

    public InstrumentDirection Direction { get; }

    public InstrumentKind Kind { get; }

    /// <summary>Number printed on the instrument. Set at receipt, or when an issued cheque gets its sheet number.</summary>
    public string? Number { get; private set; }

    /// <summary>Drawer's bank for received cheques.</summary>
    public string? BankName { get; }

    /// <summary>Received side: the receipt (رسید دریافت) that brought the instrument in; null for older single entries.</summary>
    public Guid? ReceiptId { get; private set; }

    /// <summary>Received side: branch, account, holder, Sayad ID, guarantors.</summary>
    public ReceivedInstrumentDetails Details { get; private set; } = ReceivedInstrumentDetails.None;

    /// <summary>
    /// Received side: post-dated (مدت‌دار, اسناد دریافتنی) when due after the day it was received; otherwise a current cheque
    /// (چک روز). Manual: "چنانچه تاریخ چک دریافتی روز باشد (مساوی تاریخ کامپیوتر یا کوچکتر از آن) … تیک می خورد".
    /// </summary>
    public bool IsPostDated => Direction == InstrumentDirection.Received && DueDate is { } due && due > IssueDate;

    /// <summary>«سررسید چک» (owner, 18 Nov 2026): see <see cref="ReceivedChequeStatus.HasReachedDueDate"/>.</summary>
    public bool HasReachedDueDate(FiscalDate today) =>
        Direction == InstrumentDirection.Received && ReceivedChequeStatus.HasReachedDueDate(State, IssueDate, DueDate, today);

    /// <summary>Our bank account for issued cheques.</summary>
    public Guid? BankAccountId { get; private set; }

    public Guid? ChequeBookId { get; private set; }

    /// <summary>Payment (in the Disbursements context) this issued instrument settles.</summary>
    public Guid? PaymentId { get; private set; }

    public Money Amount { get; }

    /// <summary>Date written on the instrument (received side: date received).</summary>
    public FiscalDate IssueDate { get; }

    /// <summary>Maturity (سررسید). Null only for guarantee items.</summary>
    public FiscalDate? DueDate { get; }

    public CounterpartyRef Counterparty { get; }

    /// <summary>بابت — what the instrument is for.</summary>
    public string Purpose { get; }

    /// <summary>Collection account (received cheques sent to a bank) or endorsee (received cheques given to others).</summary>
    public string? Destination { get; private set; }

    /// <summary>Received cheques deposited for collection: the company bank account they were deposited at (report no. 4, item 9).</summary>
    public Guid? CollectionAccountId { get; private set; }

    public InstrumentState State { get; private set; }

    /// <summary>Issued post-dated items: maturity has been recorded (old: Ctocash).</summary>
    public bool IsMatured { get; private set; }

    /// <summary>Jalali fiscal year the instrument was created in. Later state changes belong to the year they happen in.</summary>
    public int OriginFiscalYear { get; private set; }

    public IReadOnlyList<InstrumentHistoryEntry> History => _history;

    public IReadOnlyList<InstrumentAction> AllowedActions() =>
        InstrumentLifecycle.AllowedActions(Direction, State, Kind, IsMatured);

    /// <summary>A cheque or note received into a cash desk (old Stcode 11, form D_Check; rule R1).</summary>
    public static Instrument Receive(
        Guid id,
        InstrumentKind kind,
        string number,
        string bankName,
        Money amount,
        FiscalDate receivedOn,
        FiscalDate? dueDate,
        CounterpartyRef drawer,
        string purpose,
        string userId)
    {
        Require(!string.IsNullOrWhiteSpace(bankName), "Instrument.BankRequired", "Bank is required.");
        return Receive(id, kind, number, bankName, amount, receivedOn, dueDate, drawer, purpose, userId, ReceivedInstrumentDetails.None, null);
    }

    /// <summary>
    /// A cheque or note received on a receipt (رسید دریافت), with what is written on it. A cheque needs its bank (rule R1); a
    /// promissory note has no bank and may name up to two guarantors (new-version manual, form «دریافت چک و اسناد دریافتنی»).
    /// </summary>
    public static Instrument Receive(
        Guid id,
        InstrumentKind kind,
        string number,
        string? bankName,
        Money amount,
        FiscalDate receivedOn,
        FiscalDate? dueDate,
        CounterpartyRef drawer,
        string purpose,
        string userId,
        ReceivedInstrumentDetails details,
        Guid? receiptId)
    {
        ArgumentNullException.ThrowIfNull(details);
        Require(kind.IsValidFor(InstrumentDirection.Received), "Instrument.KindNotValidForDirection", $"{kind} cannot be received.");
        Require(!string.IsNullOrWhiteSpace(number), "Instrument.NumberRequired", "Instrument number is required.");
        Require(!kind.IsCheque() || !string.IsNullOrWhiteSpace(bankName), "Instrument.BankRequired", "Bank is required.");
        ValidateCommon(kind, drawer, purpose, dueDate);
        var checkedDetails = details.Validated(kind);

        var instrument = new Instrument(id, InstrumentDirection.Received, kind, amount, receivedOn, dueDate, drawer, purpose.Trim(),
            string.IsNullOrWhiteSpace(bankName) ? null : bankName.Trim())
        {
            Number = number.Trim(),
            State = InstrumentState.InCashDesk,
            OriginFiscalYear = receivedOn.JalaliYear,
            ReceiptId = receiptId,
            Details = checkedDetails,
        };
        instrument.Record(InstrumentAction.Receive, null, InstrumentState.InCashDesk, receivedOn, userId, null, null);
        return instrument;
    }

    /// <summary>
    /// An instrument we are about to issue for an approved payment (old Stcode 20). It gets its sheet number in <see cref="Issue"/>.
    /// The caller must have checked that the payment is approved (rule R7).
    /// </summary>
    public static Instrument PrepareIssue(
        Guid id,
        InstrumentKind kind,
        Money amount,
        FiscalDate issueDate,
        FiscalDate? dueDate,
        CounterpartyRef payee,
        string purpose,
        Guid bankAccountId,
        Guid paymentId,
        string userId)
    {
        Require(kind.IsValidFor(InstrumentDirection.Issued), "Instrument.KindNotValidForDirection", $"{kind} cannot be issued.");
        ValidateCommon(kind, payee, purpose, dueDate);
        if (kind.RequiresDueNotBeforeIssue() && dueDate < issueDate)
        {
            throw new DomainException("Instrument.DueBeforeIssue", "Due date cannot be earlier than the issue date."); // rule R6
        }

        var instrument = new Instrument(id, InstrumentDirection.Issued, kind, amount, issueDate, dueDate, payee, purpose.Trim(), null)
        {
            BankAccountId = bankAccountId,
            PaymentId = paymentId,
            State = InstrumentState.BeingIssued,
            OriginFiscalYear = issueDate.JalaliYear,
        };
        instrument.Record(InstrumentAction.PrepareIssue, null, InstrumentState.BeingIssued, issueDate, userId, paymentId.ToString(), null);
        return instrument;
    }

    /// <summary>
    /// The cheque leaves the cash desk («در جریان وصول نزد صندوق») for one of our bank accounts («در جریان وصول نزد بانک»), owner's
    /// report no. 4, item 9; old form D_Check_Vag_m (account and deposit slip «شماره فیش واگذاری»). The account must be switched on and
    /// in the cheque's currency. Rule N3 (owner, 18 Nov 2026): a cheque is deposited only after «ثبت صیادی».
    /// </summary>
    public void DepositForCollection(CollectionAccount account, FiscalDate date, string userId, string? slipNumber = null)
    {
        ArgumentNullException.ThrowIfNull(account);
        RequireSayadRegistration();
        Require(account.IsActive, "Instrument.CollectionAccountInactive", "The bank account is switched off.");
        Require(string.Equals(account.CurrencyCode.Trim(), Amount.Currency, StringComparison.OrdinalIgnoreCase), "Instrument.CollectionAccountCurrency",
            "The cheque is deposited at an account in its own currency.");
        Apply(InstrumentAction.DepositForCollection, date, userId, string.IsNullOrWhiteSpace(slipNumber) ? null : slipNumber.Trim(), null, () =>
        {
            Destination = account.Title.Trim();
            CollectionAccountId = account.Id;
        }, details: new StepDetails(AccountId: account.Id));
    }

    public void DepositForCollection(string collectionAccount, FiscalDate date, string userId, string? reference = null)
    {
        Require(!string.IsNullOrWhiteSpace(collectionAccount), "Instrument.CollectionAccountRequired", "Collection account is required."); // R3
        RequireSayadRegistration();
        Apply(InstrumentAction.DepositForCollection, date, userId, reference, null, () => Destination = collectionAccount.Trim());
    }

    /// <summary>
    /// Rule N3 (owner, 18 Nov 2026: "ثبت صیادی باید اجباری باشد؛ چک بدون آن به بانک واگذار نمی‌شود"): a cheque still «نزد صندوق» is
    /// registered in Sayad first. The old program had no such step (D_Check_Vag_m).
    /// </summary>
    private void RequireSayadRegistration()
    {
        if (Direction == InstrumentDirection.Received && State == InstrumentState.InCashDesk)
        {
            Require(Kind.IsCollectable(), "Instrument.KindNotAllowedForAction", $"'{InstrumentAction.DepositForCollection}' is not allowed for {Kind}."); // R3
            throw new DomainException("Instrument.SayadRegistrationRequired", "Register the cheque in Sayad before depositing it at the bank.");
        }
    }

    /// <summary>
    /// The bank collected the cheque. Rule N2 (CEO, 10 Nov 2026, docs/decisions.md): it cannot be cleared before its due date.
    /// Not in the old program: Form_D_Check_vos_m only checks that a date is given ("تاريخ نامشخص است").
    /// </summary>
    public void RecordCleared(FiscalDate date, string userId, string? reference = null) =>
        Apply(InstrumentAction.RecordCleared, date, userId, reference, null,
            validate: () => RequireNotBeforeDueDate(date, "Instrument.ClearedBeforeDueDate", "Clearing"),
            details: new StepDetails(AccountId: CollectionAccountId));

    /// <summary>
    /// The bounce («برگشت»): at the company's request the bank issued its bounce notice; its number and date are recorded (owner's item 14
    /// (e)). From the bank, or after the bank returned the cheque (answer to Q33). Rule N2 also applies (answer to Q11): not before the due
    /// date. The step's date is the notice's date.
    /// </summary>
    /// <param name="trackingCode">The tracking code of the certificate of non-payment (گواهی عدم پرداخت), when typed (owner, 18 Nov 2026).</param>
    public void RecordBounced(FiscalDate date, string userId, string? reason, string? noticeNumber, string? trackingCode = null)
    {
        var fromBank = State == InstrumentState.DepositedForCollection;
        var code = Clean(trackingCode);
        Apply(InstrumentAction.RecordBounced, date, userId, Clean(noticeNumber), reason,
            validate: () =>
            {
                RequireNotBeforeDueDate(date, "Instrument.BouncedBeforeDueDate", "Bounce");
                Require(!string.IsNullOrWhiteSpace(noticeNumber), "Instrument.BounceNoticeRequired", "Enter the number of the bank's bounce notice.");
            },
            details: fromBank || code is not null ? new StepDetails(AccountId: fromBank ? CollectionAccountId : null, TrackingCode: code) : null);
    }

    /// <summary>
    /// «ثبت صیادی شد» (owner, 18 Nov 2026): the cheque in the cash desk is registered in the banks' Sayad system. The tracking number of
    /// the registration may be recorded. The cheque stays in the cash desk.
    /// </summary>
    public void RegisterSayad(FiscalDate date, string userId, string? trackingNumber = null, string? note = null) =>
        Apply(InstrumentAction.RegisterSayad, date, userId, Clean(trackingNumber), Clean(note),
            details: Clean(trackingNumber) is { } tracking ? new StepDetails(SayadTracking: tracking) : null);

    /// <summary>
    /// «اقدام حقوقی» (owner, 18 Nov 2026): the bounced cheque, with the bank's bounce notice, is handed to the company's lawyer for legal
    /// action and collection; rarely the action is taken on a cheque still in the cash desk (answer to Q38). Recorded on its own screen with the date of the action and its description (شرح اقدام حقوقی); the scans of
    /// its papers are attached afterwards (<see cref="InstrumentAttachment"/>). Old program: the flag GoInlaw, without date or text.
    /// </summary>
    /// <param name="certificate">The certificate of non-payment (گواهی عدم پرداخت): number, date and tracking code. Required for a bounced
    /// ordinary cheque or note (owner, 18 Nov 2026); a guarantee item, or a cheque taken to law from the cash desk, may have none.</param>
    public void SendToLegal(FiscalDate date, string userId, string? description, NonPaymentCertificate? certificate = null)
    {
        var (number, issued, code) = (Clean(certificate?.Number), certificate?.Date, Clean(certificate?.TrackingCode));
        Apply(InstrumentAction.SendToLegal, date, userId, number, Clean(description),
            validate: () =>
            {
                Require(!string.IsNullOrWhiteSpace(description), "Instrument.LegalActionDescriptionRequired", "Enter the description of the legal action.");
                Require(State != InstrumentState.Bounced || Kind.IsGuarantee() || (number is not null && issued is not null && code is not null),
                    "Instrument.NonPaymentCertificateRequired", "Enter the number, date and tracking code of the certificate of non-payment.");
            },
            details: issued is null && code is null ? null : new StepDetails(DocumentDate: issued, TrackingCode: code));
    }

    /// <summary>
    /// The cheque or note in legal action became collectable, during the action or at its end: it is handed back to the cash desk, at the
    /// start of the cycle («نزد صندوق»), and goes on like a new cheque (owner's answer to Q38 (2), 18 Nov 2026).
    /// </summary>
    public void ReturnFromLegal(FiscalDate date, string userId, string? note = null) =>
        Apply(InstrumentAction.ReturnFromLegal, date, userId, null, Clean(note), () => Destination = null);

    /// <summary>The step (1 = received) that put the cheque in legal action, while it is there; scans are attached to it.</summary>
    public int? LegalActionStep => State == InstrumentState.InLegal ? _history.FindLastIndex(h => h.Action == InstrumentAction.SendToLegal) + 1 : null;

    /// <summary>The bank gave the cheque back to the company without collecting it and without a bounce notice (owner's item 14 (c)).</summary>
    public void ReturnFromBank(FiscalDate date, string userId, string? note = null) =>
        Apply(InstrumentAction.ReturnFromBank, date, userId, null, note, details: new StepDetails(AccountId: CollectionAccountId));

    public void ReturnToIssuer(FiscalDate date, string userId, string? note = null) =>
        Apply(InstrumentAction.ReturnToIssuer, date, userId, null, note);

    /// <summary>
    /// Endorsement («خرج چک», owner's item 14 (g), kept by the owner): the cheque pays a debt of the company. The receiver is any تفصیل
    /// and the Sayad transfer's tracking number is recorded (answers to Q32).
    /// </summary>
    public void Endorse(Endorsement endorsement, FiscalDate date, string userId, string? note = null)
    {
        ArgumentNullException.ThrowIfNull(endorsement);
        Require(!string.IsNullOrWhiteSpace(endorsement.ReceiverDetailCode) && !string.IsNullOrWhiteSpace(endorsement.ReceiverName),
            "Instrument.EndorseeRequired", "The party receiving the endorsed cheque is required.");
        Require(endorsement.PurposeGroupId != Guid.Empty, "Instrument.EndorsementGroupRequired", "Choose the payment purpose group.");
        Require(!string.IsNullOrWhiteSpace(endorsement.Purpose), "Instrument.PurposeRequired", "Purpose (بابت) is required.");
        Require(!string.IsNullOrWhiteSpace(endorsement.SayadTracking), "Instrument.SayadTrackingRequired", "Enter the tracking number of the Sayad transfer.");
        Require(endorsement.PaymentRequestId != Guid.Empty, "Instrument.PaymentRequestRequired", "Choose the approved payment request the cheque pays.");
        Apply(InstrumentAction.Endorse, date, userId, endorsement.SayadTracking.Trim(), note, () => Destination = endorsement.ReceiverName.Trim(),
            details: new StepDetails(PartyDetailCode: endorsement.ReceiverDetailCode.Trim(), PartyName: endorsement.ReceiverName.Trim(),
                PurposeGroupId: endorsement.PurposeGroupId, Purpose: endorsement.Purpose.Trim(), SayadTracking: endorsement.SayadTracking.Trim(),
                PaymentRequestId: endorsement.PaymentRequestId));
    }

    /// <summary>The key of the payment an endorsement made on its request: the cheque and the endorsement's step (answer to Q36).</summary>
    public string EndorsementPaymentKey(int step)
    {
        Require(step >= 1 && step <= _history.Count && _history[step - 1].Action == InstrumentAction.Endorse, "Instrument.InvalidTransition", "That step is not an endorsement.");
        return $"endorse:{Id}:{step}";
    }

    /// <summary>
    /// The receiver of an endorsed cheque brings it back bounced, with the bank's bounce notice (owner's item 14 (h)). The cheque becomes
    /// «برگشت شده» again in the company's hands; rule N2 applies.
    /// </summary>
    public void RecordEndorsementReturned(FiscalDate date, string userId, string? noticeNumber, string? reason = null, string? trackingCode = null) =>
        Apply(InstrumentAction.RecordEndorsementReturned, date, userId, Clean(noticeNumber), reason,
            details: Clean(trackingCode) is { } code ? new StepDetails(TrackingCode: code) : null,
            validate: () =>
            {
                RequireNotBeforeDueDate(date, "Instrument.BouncedBeforeDueDate", "Bounce");
                Require(!string.IsNullOrWhiteSpace(noticeNumber), "Instrument.BounceNoticeRequired", "Enter the number of the bank's bounce notice.");
            },
            changeDetails: () => Destination = null);

    /// <summary>
    /// Replacement («تعویض», owner's item 14 (d)): the payer gives new cheques for this one; they may differ in amount and number (answer
    /// to Q31) and come on their own cheque receipt.
    /// </summary>
    public void Replace(Guid replacementReceiptId, FiscalDate date, string userId)
    {
        Require(replacementReceiptId != Guid.Empty, "Instrument.ReplacementRequired", "The replacement receipt is required.");
        Apply(InstrumentAction.Replace, date, userId, null, null, details: new StepDetails(ReceiptId: replacementReceiptId));
    }

    /// <summary>
    /// Cancellation of a received cheque («ابطال», owner's item 14 (f)): in the cash desk, back from the bank or bounced. The treasurer
    /// cancels it and types the number and date of management's order (answer to Q30), with the reason.
    /// </summary>
    public void CancelReceived(FiscalDate date, string userId, string? orderNumber, FiscalDate? orderDate, string? reason)
    {
        Require(Direction == InstrumentDirection.Received, "Instrument.InvalidTransition", "Only a received cheque is cancelled this way.");
        Require(!string.IsNullOrWhiteSpace(orderNumber) && orderDate is not null, "Instrument.CancelOrderRequired",
            "Enter the number and date of management's order to cancel.");
        Require(orderDate is null || orderDate <= date, "Instrument.CancelOrderAfterCancel", "Management's order cannot be dated after the cancellation.");
        Require(!string.IsNullOrWhiteSpace(reason), "Instrument.CancelReasonRequired", "A reason is required to cancel.");
        Apply(InstrumentAction.Cancel, date, userId, orderNumber!.Trim(), reason!.Trim(), details: new StepDetails(DocumentDate: orderDate));
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>Gives the instrument the sheet number reserved from <paramref name="chequeBookId"/> (old Stcode 21).</summary>
    public void Issue(Guid chequeBookId, string sheetNumber, FiscalDate date, string userId)
    {
        Require(!string.IsNullOrWhiteSpace(sheetNumber), "Instrument.NumberRequired", "Cheque number is required.");
        Apply(InstrumentAction.Issue, date, userId, sheetNumber, null, () =>
        {
            ChequeBookId = chequeBookId;
            Number = sheetNumber;
        });
    }

    public void Deliver(FiscalDate date, string userId, string? receivedBy = null) =>
        Apply(InstrumentAction.Deliver, date, userId, null, receivedBy);

    public void Cancel(FiscalDate date, string userId, string reason)
    {
        // A received cheque is cancelled with management's order (CancelReceived).
        Require(Direction == InstrumentDirection.Issued, "Instrument.CancelOrderRequired", "Enter the number and date of management's order to cancel.");
        Require(!string.IsNullOrWhiteSpace(reason), "Instrument.CancelReasonRequired", "A reason is required to cancel.");
        Apply(InstrumentAction.Cancel, date, userId, null, reason);
    }

    /// <summary>A post-dated cheque, instalment or note we issued has reached maturity (old: Ctocash).</summary>
    public void RecordMatured(FiscalDate date, string userId)
    {
        if (IsMatured)
        {
            throw new DomainException("Instrument.AlreadyMatured", "Maturity has already been recorded.");
        }

        Apply(InstrumentAction.RecordMatured, date, userId, null, null, () => IsMatured = true);
    }

    /// <summary>Our bank paid the cheque we issued (old: Vos + RegVos on an issued cheque).</summary>
    public void RecordPaidByBank(FiscalDate date, string userId, string? reference = null) =>
        Apply(InstrumentAction.RecordPaidByBank, date, userId, reference, null);

    /// <summary>Our bank returned the cheque we issued (old: Backed + RegVos on an issued cheque).</summary>
    public void RecordBouncedAtBank(FiscalDate date, string userId, string? reason = null) =>
        Apply(InstrumentAction.RecordBouncedAtBank, date, userId, null, reason);

    /// <summary>Rebuilds an instrument from storage or from migrated data. Does not raise events.</summary>
    public static Instrument Restore(
        Guid id,
        InstrumentDirection direction,
        InstrumentKind kind,
        string? number,
        string? bankName,
        Guid? bankAccountId,
        Guid? chequeBookId,
        Guid? paymentId,
        Money amount,
        FiscalDate issueDate,
        FiscalDate? dueDate,
        CounterpartyRef counterparty,
        string purpose,
        string? destination,
        InstrumentState state,
        int originFiscalYear,
        IEnumerable<InstrumentHistoryEntry> history,
        long version,
        Guid? receiptId = null,
        ReceivedInstrumentDetails? details = null,
        Guid? collectionAccountId = null)
    {
        var historyList = history.ToList();
        var instrument = new Instrument(id, direction, kind, amount, issueDate, dueDate, counterparty, purpose, bankName)
        {
            Number = number,
            BankAccountId = bankAccountId,
            ChequeBookId = chequeBookId,
            PaymentId = paymentId,
            Destination = destination,
            State = state,
            OriginFiscalYear = originFiscalYear,
            IsMatured = historyList.Any(h => h.Action == InstrumentAction.RecordMatured),
            ReceiptId = receiptId,
            Details = details ?? ReceivedInstrumentDetails.None,
            CollectionAccountId = collectionAccountId,
        };
        instrument._history.AddRange(historyList);
        instrument.SetLoadedVersion(version);
        return instrument;
    }

    /// <param name="validate">Extra rule of this action, checked after the transition and date rules, before anything changes.</param>
    private void Apply(InstrumentAction action, FiscalDate date, string userId, string? reference, string? note, Action? changeDetails = null, Action? validate = null,
        StepDetails? details = null)
    {
        Require(!string.IsNullOrWhiteSpace(userId), "Instrument.UserRequired", "User is required.");
        var transition = InstrumentLifecycle.Find(Direction, State, action)
            ?? throw new DomainException(
                "Instrument.InvalidTransition",
                $"'{action}' is not allowed for a {Direction.ToString().ToLowerInvariant()} instrument in state {State}."); // R4 "order of operations is wrong"

        if (!transition.KindAllowed(Kind))
        {
            throw new DomainException("Instrument.KindNotAllowedForAction", $"'{action}' is not allowed for {Kind}."); // R3
        }

        var lastDate = _history[^1].Date;
        if (date < lastDate)
        {
            throw new DomainException(
                "Instrument.DateBeforePreviousStep",
                $"Date {date} is earlier than the previous step ({lastDate})."); // N1 (new rule, to be confirmed)
        }

        validate?.Invoke();
        var from = State;
        changeDetails?.Invoke();
        State = transition.To;
        Record(action, from, transition.To, date, userId, reference, note, details);
    }

    private void Record(InstrumentAction action, InstrumentState? from, InstrumentState to, FiscalDate date, string userId, string? reference, string? note,
        StepDetails? details = null)
    {
        Require(!string.IsNullOrWhiteSpace(userId), "Instrument.UserRequired", "User is required.");
        _history.Add(new InstrumentHistoryEntry(action, from, to, date, userId, reference, note, details));
        Raise(new InstrumentStateChanged(Id, Direction, Kind, action, from, to, date, Amount, Counterparty.Id, Number, Destination, userId));
    }

    private void RequireNotBeforeDueDate(FiscalDate date, string code, string what)
    {
        if (DueDate is { } due && date < due)
        {
            throw new DomainException(code, $"{what} date {date} is earlier than the due date ({due}).");
        }
    }

    private static void ValidateCommon(InstrumentKind kind, CounterpartyRef counterparty, string purpose, FiscalDate? dueDate)
    {
        ArgumentNullException.ThrowIfNull(counterparty);
        Require(!string.IsNullOrWhiteSpace(counterparty.DisplayName), "Instrument.CounterpartyRequired", "Drawer or payee is required.");
        Require(!string.IsNullOrWhiteSpace(purpose), "Instrument.PurposeRequired", "Purpose (بابت) is required.");
        Require(kind.IsGuarantee() || dueDate is not null, "Instrument.DueDateRequired", "Due date is required.");
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

/// <summary>
/// Raised for every step of an instrument's life. The Accounting Integration context turns these into vouchers using posting rules;
/// the instrument itself knows nothing about accounts.
/// </summary>
public sealed record InstrumentStateChanged(
    Guid InstrumentId,
    InstrumentDirection Direction,
    InstrumentKind Kind,
    InstrumentAction Action,
    InstrumentState? From,
    InstrumentState To,
    FiscalDate Date,
    Money Amount,
    Guid CounterpartyId,
    string? Number,
    string? Destination,
    string UserId) : DomainEvent;
