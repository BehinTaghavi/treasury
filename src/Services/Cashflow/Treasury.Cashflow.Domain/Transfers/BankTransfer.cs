using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Domain.Transfers;

/// <summary>Where a transfer between two bank accounts of the company stands.</summary>
public enum TransferState
{
    /// <summary>An electronic transfer, recorded: the money left the source account and reached the destination account.</summary>
    Recorded,

    /// <summary>
    /// A cheque transfer, payment side only («پرداخت ثبت صیاد شد»): the cheque is issued from the source account's cheque book and
    /// registered in Sayad; it is not yet on the receipts side.
    /// </summary>
    ChequeIssued,

    /// <summary>A cheque transfer whose cheque is also on the receipts side, where it follows the steps of a received cheque.</summary>
    ChequeReceived,

    /// <summary>A cheque transfer whose cheque was collected at the destination account: the transfer is finished.</summary>
    Collected,

    /// <summary>«حذف شده»: recorded by mistake and deleted before its voucher was confirmed; the row stays so its number is seen to be taken.</summary>
    Deleted,

    /// <summary>
    /// «ابطال شده»: a cheque transfer whose cheque was cancelled before it was registered in Sayad (owner, 21 Nov 2026, answer to Q79);
    /// its sheet is void, and a new transfer by cheque is a new, independent cheque.
    /// </summary>
    Cancelled,
}

/// <summary>
/// The approved payment request («درخواست وجه») of the kind «نقل و انتقال بین بانک‌های شرکت» that a transfer pays, as requests-svc
/// describes it. Owner, 21 Nov 2026 (answer to Q82): «برای نقل و انتقال از هر نوع لازم است درخواست وجه صادر و مجوز آن اخذ شود».
/// </summary>
/// <param name="IsForOwnTransfer">The request was written for a transfer between the company's own accounts.</param>
/// <param name="RemainingAmount">What is not yet paid on the request (without the transfer being corrected).</param>
public sealed record TransferRequestFact(
    Guid Id, int Number, bool IsApproved, bool IsForOwnTransfer, Guid? SourceAccountId, Guid? DestinationAccountId, decimal RemainingAmount);

/// <summary>
/// Whether the company's account «هزینه کارمزد خدمات بانکی» takes a تفصیل, and of which types (owner, 21 Nov 2026, answer to Q83: the
/// account usually has none, but some companies keep a general cost centre of the finance department for it; when the account has the
/// attribute "with تفصیل" the voucher asks for one, otherwise it is not asked).
/// </summary>
/// <param name="AllowedTypes">The تفصیل types the account takes; empty: no تفصیل. Null account: not set in the template yet.</param>
public sealed record FeeAccountFact(IReadOnlyList<string> AllowedTypes)
{
    public static FeeAccountFact WithoutDetail { get; } = new([]);

    public bool TakesDetail => AllowedTypes.Count > 0;
}

/// <summary>The تفصیل chosen for the fee line, as reference-svc describes it.</summary>
public sealed record FeeDetailFact(string Code, string Name, string? Type, bool IsActive);

/// <summary>The payment request a transfer paid, and the key of that payment on the request (so it can be taken back).</summary>
public sealed record TransferRequest(Guid Id, int Number, string PaymentKey);

/// <summary>One of the company's bank accounts as accounts-svc and reference-svc describe it when a transfer is recorded.</summary>
/// <param name="HasCheques">The kind of account has cheque books (current Qard-al-hasan; owner's report no. 4, item 1).</param>
/// <param name="LedgerAccountCode">The bank account's معین, for the voucher.</param>
/// <param name="DetailCode">The bank account's تفصیل ۱, for the voucher.</param>
public sealed record TransferAccountFact(
    Guid Id, string AccountNumber, Guid BankId, string BankName, string CurrencyCode, bool IsActive, bool HasCheques = true,
    string? LedgerAccountCode = null, string? DetailCode = null, string? BranchName = null, string? BranchCode = null);

/// <summary>A bank account as the transfer keeps it: the names stay as they were on the day, even if the account is renamed later.</summary>
public sealed record TransferAccount(Guid Id, string AccountNumber, Guid BankId, string BankName)
{
    public static TransferAccount From(TransferAccountFact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        return new(fact.Id, fact.AccountNumber.Trim(), fact.BankId, fact.BankName.Trim());
    }

    /// <summary>«بانک ملت – 123456».</summary>
    public string Title => $"{BankName} – {AccountNumber}";
}

/// <summary>The cheque of a cheque transfer: the same sheet seen from the payment side (issued) and from the receipts side (received).</summary>
/// <param name="IssuedChequeId">The issued instrument (payment side, «انتقال دهنده»).</param>
/// <param name="Sheet">The sheet of the cheque book, as a number.</param>
/// <param name="Number">The serial as printed, with its leading zeros.</param>
/// <param name="SayadId">The 16-digit Sayad ID of the sheet.</param>
/// <param name="ReceivedChequeId">The received instrument (receipts side, «انتقال گیرنده»), once it is recorded there.</param>
/// <param name="PayerSayadOn">The day the company registered the cheque in Sayad as its payer («پرداخت ثبت صیاد شد»); null: not yet.</param>
/// <param name="MaturedOn">A post-dated cheque: the day its maturity was recorded («سررسید»), when it leaves «اسناد پرداختنی» for the bank.</param>
/// <param name="CancelledOn">The day the cheque was cancelled («ابطال»), with <paramref name="CancelReason"/>.</param>
public sealed record TransferCheque(
    Guid IssuedChequeId, Guid ChequeBookId, long Sheet, string Number, string SayadId, FiscalDate DueDate,
    Guid? ReceivedChequeId = null, FiscalDate? ReceivedOn = null, FiscalDate? CollectedOn = null, FiscalDate? PayerSayadOn = null, FiscalDate? MaturedOn = null,
    FiscalDate? CancelledOn = null, string? CancelReason = null);

/// <summary>What the form of an electronic transfer says.</summary>
/// <param name="Channel">At the branch or remotely; the limits differ.</param>
/// <param name="TrackingNumber">The bank's tracking number of the order (شماره پیگیری), when typed.</param>
/// <param name="Request">The approved payment request the transfer pays (answer to Q82).</param>
/// <param name="FeeRial">The bank's fee for the order («کارمزد خدمات بانکی»), in Rial, when there is one (answer to Q77).</param>
/// <param name="FeeAccount">Whether the fee account takes a تفصیل (answer to Q83); null: the account is not set in the template yet.</param>
/// <param name="FeeDetail">The تفصیل of the fee line, when the fee account takes one.</param>
public sealed record ElectronicTransferForm(
    FiscalDate Date, TransferAccountFact Source, TransferAccountFact Destination, TransferMethod Method, TransferChannel? Channel, Money Amount,
    string? TrackingNumber = null, string? Description = null, TransferRequestFact? Request = null, decimal? FeeRial = null, FeeAccountFact? FeeAccount = null,
    FeeDetailFact? FeeDetail = null);

/// <summary>
/// A transfer between two bank accounts of the company («نقل و انتقال بین بانکی», owner's report no. 7 of 20 Nov 2026).
/// Electronic («روش ساده»): internal (the same bank), Pol, Paya or Satna (two different banks), each within its configurable limits.
/// By Sayad cheque («در صورت نیاز به سند یا … پرداخت مدت‌دار»): the company issues a cheque of the source account and registers it in
/// Sayad («پرداخت ثبت صیاد شد») — the payment side, «نقل و انتقال (انتقال دهنده)» — and with one button the same cheque is recorded
/// on the receipts side, «نقل و انتقال (انتقال گیرنده)», where it goes on like every received cheque (ثبت صیادی, واگذاری به بانک,
/// وصول). Transfers have their own serial number per fiscal year. Not in the old program: Lcash.mdb had no transfer between the
/// company's own accounts (a payment and a receipt were typed separately).
/// </summary>
public sealed class BankTransfer : AggregateRoot<Guid>
{
    public const int MaxDescriptionLength = 500;

    private BankTransfer(Guid id, int number, int fiscalYear, TransferMethod method, string recordedBy)
        : base(id)
    {
        Number = number;
        FiscalYear = fiscalYear;
        Method = method;
        RecordedBy = recordedBy;
    }

    /// <summary>Serial number within the fiscal year, one series for every kind of transfer.</summary>
    public int Number { get; }

    public int FiscalYear { get; }

    /// <summary>Electronic: the day of the order. Cheque: the day the cheque was issued.</summary>
    public FiscalDate Date { get; private set; }

    public TransferMethod Method { get; private set; }

    /// <summary>Electronic transfers only.</summary>
    public TransferChannel? Channel { get; private set; }

    /// <summary>«انتقال دهنده»: the account the money leaves.</summary>
    public TransferAccount Source { get; private set; } = null!;

    /// <summary>«انتقال گیرنده»: the account the money reaches.</summary>
    public TransferAccount Destination { get; private set; } = null!;

    public Money Amount { get; private set; } = null!;

    public string? TrackingNumber { get; private set; }

    public string? Description { get; private set; }

    /// <summary>
    /// The bank's fee of an electronic transfer («کارمزد خدمات بانکی»), in Rial; null when there is none. Owner, 21 Nov 2026 (answer to
    /// Q77): it is debited to one general expense account of the company, set in the voucher template like «مطالبات سوخت شده».
    /// </summary>
    public decimal? FeeRial { get; private set; }

    /// <summary>
    /// The تفصیل of the fee line, kept only when the fee account takes one (owner's answer to Q83: e.g. a general cost centre of the
    /// finance department); null when the account has no تفصیل or there is no fee.
    /// </summary>
    public string? FeeDetailCode { get; private set; }

    /// <summary>
    /// The approved payment request the transfer paid (owner, 21 Nov 2026, answer to Q82). Null only for transfers recorded before that
    /// rule.
    /// </summary>
    public TransferRequest? Request { get; private set; }

    /// <summary>Cheque transfers only.</summary>
    public TransferCheque? Cheque { get; private set; }

    /// <summary>
    /// A cheque transfer whose cheque is due after the day it was issued («مدت‌دار»). Owner, 21 Nov 2026 (answer to Q81): until its due
    /// date it is credited to «اسناد پرداختنی», not to the bank; at maturity it leaves the bank account.
    /// </summary>
    public bool IsPostDated => Cheque is { } c && c.DueDate > Date;

    /// <summary>The key under which this transfer's payment is recorded on its payment request; a correction records a new one.</summary>
    public string NextPaymentKey() => $"transfer:{Id}:{Version + 1}";

    public TransferState State { get; private set; }

    public string RecordedBy { get; }

    public string? DeletedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsCheque => Method == TransferMethod.Cheque;

    /// <summary>An electronic transfer, checked against the rules of its way and the company's limits.</summary>
    /// <param name="usage">What the source account already sent that day by the same way.</param>
    public static BankTransfer RecordElectronic(Guid id, int number, ElectronicTransferForm form, TransferLimits limits, TransferDayUsage usage, string userId)
    {
        CheckElectronic(form, number, userId, limits, usage);
        var transfer = new BankTransfer(id, number, form.Date.JalaliYear, form.Method, userId.Trim()) { State = TransferState.Recorded };
        transfer.Apply(form);
        transfer.Request = new TransferRequest(form.Request!.Id, form.Request.Number, transfer.NextPaymentKey());
        transfer.Raise(new BankTransferRecorded(id, transfer.FiscalYear, number, transfer.Method, transfer.Date, transfer.Source.Id, transfer.Destination.Id,
            transfer.Amount.Amount, transfer.Amount.Currency, transfer.Amount.ToRial(), userId));
        return transfer;
    }

    /// <summary>
    /// Correction («ویرایش») of an electronic transfer while its voucher is not confirmed, with the rules it was recorded with (as the
    /// owner asked for receipts, report no. 6, items 2 and 6). It keeps its number and so its fiscal year.
    /// </summary>
    public void CorrectElectronic(ElectronicTransferForm form, TransferLimits limits, TransferDayUsage usage, string userId, bool voucherConfirmed)
    {
        if (WhyNotCorrectable(voucherConfirmed) is { } code)
        {
            throw new DomainException(code, "The transfer can no longer be corrected.");
        }

        CheckElectronic(form, Number, userId, limits, usage);
        Require(form.Date.JalaliYear == FiscalYear, "Transfer.CorrectionOtherYear", $"The transfer's number belongs to fiscal year {FiscalYear}; its date stays in that year.");
        Method = form.Method;
        Apply(form);
        Request = new TransferRequest(form.Request!.Id, form.Request.Number, NextPaymentKey());
        Raise(new BankTransferCorrected(Id, FiscalYear, Number, Method, Date, Source.Id, Destination.Id, Amount.Amount, Amount.Currency, Amount.ToRial(), userId));
    }

    /// <summary>The code of the rule that closes «ویرایش», or null: only an electronic transfer, not deleted, voucher not confirmed.</summary>
    public string? WhyNotCorrectable(bool voucherConfirmed) =>
        State == TransferState.Deleted ? "Transfer.Deleted"
        : IsCheque ? "Transfer.ChequeNotCorrected"
        : voucherConfirmed ? "Transfer.VoucherConfirmed"
        : null;

    /// <summary>
    /// The payment side of a cheque transfer («نقل و انتقال (انتقال دهنده)»): the cheque was issued from a cheque book of the source
    /// account and registered in Sayad («پرداخت ثبت صیاد شد»). The caller issued <paramref name="issued"/> for this transfer on the
    /// sheet in the same command.
    /// </summary>
    /// <param name="request">The approved payment request the transfer pays (answer to Q82).</param>
    /// <param name="sayadRegistered">The cheque was registered in Sayad on the day it was issued («پرداخت ثبت صیاد شد»); false: it is
    /// registered later with <see cref="RegisterPayerSayad"/>, and until then it can still be cancelled (answer to Q79).</param>
    public static BankTransfer IssueCheque(
        Guid id, int number, FiscalDate date, TransferAccountFact source, TransferAccountFact destination, Instrument issued, Guid chequeBookId, long sheet,
        string sayadId, string? description, string userId, TransferRequestFact? request, bool sayadRegistered = true)
    {
        ArgumentNullException.ThrowIfNull(issued);
        CheckCommon(number, userId, source, destination, issued.Amount, description);
        CheckRequest(request, source, destination, issued.Amount);
        Require(source.HasCheques, "Transfer.SourceHasNoCheques", "The source account has no cheque book; a cheque is issued from a current account.");
        Require(issued.Direction == InstrumentDirection.Issued && issued.State == InstrumentState.Issued && issued.PaymentId == id
                && issued.BankAccountId == source.Id && issued.IssueDate == date && issued.DueDate is not null && !string.IsNullOrWhiteSpace(issued.Number),
            "Transfer.ChequeMismatch", "The cheque must be issued from the source account for this transfer, on the transfer's date.");
        Require(sayadId is { Length: ReceivedInstrumentDetails.SayadIdLength } && sayadId.All(char.IsAsciiDigit), "Transfer.SayadIdRequired",
            "A transfer cheque carries its 16-digit Sayad ID.");
        var transfer = new BankTransfer(id, number, date.JalaliYear, TransferMethod.Cheque, userId.Trim())
        {
            State = TransferState.ChequeIssued,
            Date = date,
            Source = TransferAccount.From(source),
            Destination = TransferAccount.From(destination),
            Amount = issued.Amount,
            Description = Clean(description),
            Cheque = new TransferCheque(issued.Id, chequeBookId, sheet, issued.Number!, sayadId, issued.DueDate!.Value, PayerSayadOn: sayadRegistered ? date : null),
        };
        transfer.Request = new TransferRequest(request!.Id, request.Number, transfer.NextPaymentKey());
        transfer.Raise(new TransferChequeIssued(id, transfer.FiscalYear, number, date, source.Id, destination.Id, issued.Id, issued.Number!, sayadId,
            issued.Amount.Amount, issued.Amount.Currency, issued.Amount.ToRial(), issued.DueDate!.Value, userId));
        return transfer;
    }

    /// <summary>
    /// The receipts side («نقل و انتقال (انتقال گیرنده)»): with the button of the payment form the same cheque is recorded as a
    /// received cheque «نزد صندوق». <paramref name="received"/> was created from the issued cheque in the same command.
    /// </summary>
    public void ReceiveCheque(Instrument received, string userId)
    {
        ArgumentNullException.ThrowIfNull(received);
        Require(!string.IsNullOrWhiteSpace(userId), "Transfer.UserRequired", "User is required.");
        Require(State != TransferState.Deleted, "Transfer.Deleted", "The transfer was deleted.");
        Require(IsCheque && State == TransferState.ChequeIssued && Cheque is not null, "Transfer.ChequeAlreadyReceived",
            "The cheque of this transfer is already recorded on the receipts side.");
        Require(received.Direction == InstrumentDirection.Received && received.Details.TransferId == Id && received.Number == Cheque!.Number
                && received.Amount == Amount && received.DueDate == Cheque.DueDate,
            "Transfer.ChequeMismatch", "The received cheque must be the cheque issued for this transfer.");
        Require(received.IssueDate >= Date, "Transfer.ReceivedBeforeIssue", "The cheque cannot be received before the day it was issued.");
        Cheque = Cheque! with { ReceivedChequeId = received.Id, ReceivedOn = received.IssueDate };
        State = TransferState.ChequeReceived;
        Raise(new TransferChequeReceived(Id, FiscalYear, Number, received.Id, received.IssueDate, userId));
    }

    /// <summary>
    /// Takes back a receipts-side record made by mistake, while the received cheque is as it was received (no step on it) and the
    /// receiver's voucher is not confirmed; the caller removes the received cheque. The transfer is «پرداخت ثبت صیاد شد» again.
    /// </summary>
    public void UndoReceive(Instrument received, bool receiverVoucherConfirmed, string userId)
    {
        ArgumentNullException.ThrowIfNull(received);
        Require(!string.IsNullOrWhiteSpace(userId), "Transfer.UserRequired", "User is required.");
        Require(State == TransferState.ChequeReceived && Cheque?.ReceivedChequeId == received.Id, "Transfer.ChequeNotReceived",
            "The cheque of this transfer is not on the receipts side.");
        Require(!receiverVoucherConfirmed, "Transfer.VoucherConfirmed", "The voucher is confirmed; the transfer can no longer be changed.");
        Require(received.IsAsReceived, "Transfer.ChequeMovedOn", "A step is recorded on the received cheque; its receipt can no longer be taken back.");
        Cheque = Cheque! with { ReceivedChequeId = null, ReceivedOn = null };
        State = TransferState.ChequeIssued;
        Raise(new TransferChequeReceiptUndone(Id, FiscalYear, Number, received.Id, userId));
    }

    /// <summary>
    /// «پرداخت ثبت صیاد شد»: the company registers the issued cheque in Sayad as its payer, when that was not done on the day of issue.
    /// From then on the cheque is no longer cancelled, and the receipts side may confirm it in Sayad («دریافت ثبت صیاد شد»).
    /// </summary>
    public void RegisterPayerSayad(FiscalDate date, string userId)
    {
        Require(!string.IsNullOrWhiteSpace(userId), "Transfer.UserRequired", "User is required.");
        Require(Cheque is not null && State is TransferState.ChequeIssued or TransferState.ChequeReceived, "Transfer.ChequeNotOpen",
            "This step is recorded on a cheque transfer that is neither collected, cancelled nor deleted.");
        Require(Cheque!.PayerSayadOn is null, "Transfer.PayerSayadAlreadyRegistered", "The cheque is already registered in Sayad by the payer.");
        Require(date >= Date, "Transfer.StepBeforeIssue", "The date cannot be before the day the cheque was issued.");
        Cheque = Cheque with { PayerSayadOn = date };
        Raise(new TransferChequeSayadRegistered(Id, FiscalYear, Number, date, userId));
    }

    /// <summary>
    /// Owner, 21 Nov 2026 (answer to Q79): «اگر ثبت صیادی پرداخت کننده نشده باشد طبعاً ثبت صیادی دریافت کننده هم نشده است» — the
    /// receipts side registers the cheque in Sayad only after the payer did.
    /// </summary>
    public void EnsureReceiverMayRegisterSayad() =>
        Require(Cheque?.PayerSayadOn is not null, "Transfer.PayerSayadRequired",
            "The cheque is not yet registered in Sayad by the payer («پرداخت ثبت صیاد شد»); record that first on the payment side.");

    /// <summary>
    /// «سررسید» of a post-dated transfer cheque (owner, 21 Nov 2026, answer to Q81): «به محض سررسید شدن از حساب بانک خارج» — on or after
    /// its due date the cheque leaves «اسناد پرداختنی» for the source bank, with a voucher of its own.
    /// </summary>
    public void RecordMatured(FiscalDate date, string userId)
    {
        Require(!string.IsNullOrWhiteSpace(userId), "Transfer.UserRequired", "User is required.");
        Require(Cheque is not null && State is TransferState.ChequeIssued or TransferState.ChequeReceived, "Transfer.ChequeNotOpen",
            "This step is recorded on a cheque transfer that is neither collected, cancelled nor deleted.");
        Require(IsPostDated, "Transfer.NotPostDated", "Only a post-dated cheque has a maturity to record.");
        Require(Cheque!.MaturedOn is null, "Transfer.AlreadyMatured", "The maturity of this cheque is already recorded.");
        Require(date >= Cheque.DueDate, "Transfer.MaturedBeforeDueDate", $"The maturity is recorded on or after the due date ({Cheque.DueDate}).");
        Cheque = Cheque with { MaturedOn = date };
        Raise(new TransferChequeMatured(Id, FiscalYear, Number, date, userId));
    }

    /// <summary>The code of the rule that closes «ابطال چک», or null: only while the cheque is not registered in Sayad (answer to Q79).</summary>
    public string? WhyNotCancellable() =>
        Cheque is null || State is not (TransferState.ChequeIssued or TransferState.ChequeReceived) ? "Transfer.ChequeNotOpen"
        : Cheque.PayerSayadOn is not null ? "Transfer.SayadRegisteredNotCancelled"
        : null;

    /// <summary>
    /// «ابطال» of a transfer cheque (owner, 21 Nov 2026, answer to Q79): «آن چک اگر ثبت صیادی پرداخت کننده نشده باشد … می‌توان آن را
    /// باطل نمود. نیاز به ایجاد امکان تعویض نیست؛ هر زمان چک دیگری برای نقل و انتقال صادر شود به‌عنوان یک چک جدید و مستقل تلقی می‌گردد».
    /// The caller cancels the issued cheque, voids its sheet, cancels the received cheque if it is on the receipts side, and takes the
    /// payment off the payment request. As for received cheques (report no. 6, item 19), a cheque registered in Sayad is not cancelled.
    /// </summary>
    public void CancelCheque(FiscalDate date, string? reason, string userId)
    {
        Require(!string.IsNullOrWhiteSpace(userId), "Transfer.UserRequired", "User is required.");
        if (WhyNotCancellable() is { } code)
        {
            throw new DomainException(code, "The cheque of this transfer can no longer be cancelled.");
        }

        Require(!string.IsNullOrWhiteSpace(reason), "Transfer.CancelReasonRequired", "Give the reason for cancelling the cheque.");
        Require(date >= Date && (Cheque!.ReceivedOn is not { } received || date >= received), "Transfer.StepBeforeIssue",
            "The date cannot be before the day the cheque was issued or received.");
        Cheque = Cheque! with { CancelledOn = date, CancelReason = reason!.Trim() };
        State = TransferState.Cancelled;
        Raise(new TransferChequeCancelled(Id, FiscalYear, Number, date, Cheque.CancelReason!, Cheque.IssuedChequeId, Cheque.ReceivedChequeId, userId));
    }

    /// <summary>The cheque was collected at the destination account: the transfer is finished.</summary>
    public void MarkCollected(FiscalDate date)
    {
        Require(State == TransferState.ChequeReceived && Cheque is not null, "Transfer.ChequeNotReceived", "The cheque of this transfer is not on the receipts side.");
        Cheque = Cheque! with { CollectedOn = date };
        State = TransferState.Collected;
        Raise(new TransferChequeCollected(Id, FiscalYear, Number, date));
    }

    /// <summary>
    /// The code of the rule that closes «حذف», or null. An electronic transfer is deleted while its voucher is not confirmed; a cheque
    /// transfer only while the cheque is not on the receipts side (take that record back first) and the payer's voucher is not confirmed.
    /// </summary>
    public string? WhyNotDeletable(bool voucherConfirmed) =>
        State == TransferState.Deleted ? "Transfer.Deleted"
        : State == TransferState.Cancelled ? "Transfer.ChequeCancelled"
        : voucherConfirmed ? "Transfer.VoucherConfirmed"
        : State is TransferState.ChequeReceived or TransferState.Collected ? "Transfer.ChequeAlreadyReceived"
        : null;

    /// <summary>
    /// Deletion («حذف») of a transfer recorded by mistake, before its voucher is confirmed. It stays in the list as «حذف شده» with who
    /// deleted it and when, as a deleted receipt does (owner's answer to Q67). For a cheque transfer the caller removes the issued cheque
    /// and frees its sheet: the record never happened, so the sheet can be typed again.
    /// </summary>
    public void Delete(string userId, bool voucherConfirmed, DateTimeOffset at)
    {
        Require(!string.IsNullOrWhiteSpace(userId), "Transfer.UserRequired", "User is required.");
        if (WhyNotDeletable(voucherConfirmed) is { } code)
        {
            throw new DomainException(code, "The transfer can no longer be deleted.");
        }

        State = TransferState.Deleted;
        DeletedBy = userId.Trim();
        DeletedAt = at;
        Raise(new BankTransferDeleted(Id, FiscalYear, Number, Method, Date, Amount.ToRial(), Cheque?.IssuedChequeId, DeletedBy));
    }

    public static BankTransfer Restore(
        Guid id, int number, int fiscalYear, FiscalDate date, TransferMethod method, TransferChannel? channel, TransferAccount source, TransferAccount destination,
        Money amount, string? trackingNumber, string? description, TransferCheque? cheque, TransferState state, string recordedBy, string? deletedBy,
        DateTimeOffset? deletedAt, long version, decimal? feeRial = null, TransferRequest? request = null, string? feeDetailCode = null)
    {
        var transfer = new BankTransfer(id, number, fiscalYear, method, recordedBy)
        {
            Date = date,
            Channel = channel,
            Source = source,
            Destination = destination,
            Amount = amount,
            TrackingNumber = trackingNumber,
            Description = description,
            Cheque = cheque,
            State = state,
            DeletedBy = deletedBy,
            DeletedAt = deletedAt,
            FeeRial = feeRial,
            FeeDetailCode = feeDetailCode,
            Request = request,
        };
        transfer.SetLoadedVersion(version);
        return transfer;
    }

    private void Apply(ElectronicTransferForm form)
    {
        Date = form.Date;
        Channel = form.Channel;
        Source = TransferAccount.From(form.Source);
        Destination = TransferAccount.From(form.Destination);
        Amount = form.Amount;
        TrackingNumber = Clean(form.TrackingNumber);
        Description = Clean(form.Description);
        FeeRial = form.FeeRial is > 0 ? form.FeeRial : null;
        FeeDetailCode = FeeRial is not null && form.FeeAccount is { TakesDetail: true } ? form.FeeDetail?.Code.Trim() : null;
    }

    private static void CheckElectronic(ElectronicTransferForm form, int number, string userId, TransferLimits limits, TransferDayUsage usage)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(limits);
        Require(Enum.IsDefined(form.Method) && form.Method.IsElectronic(), "Transfer.MethodRequired",
            "Choose the way of transfer: internal, Pol, Paya or Satna.");
        CheckCommon(number, userId, form.Source, form.Destination, form.Amount, form.Description);
        CheckRequest(form.Request, form.Source, form.Destination, form.Amount);
        Require(form.FeeRial is null || (form.FeeRial >= 0 && decimal.Truncate(form.FeeRial.Value) == form.FeeRial), "Transfer.FeeInvalid",
            "The bank's fee is a whole Rial amount, or empty.");
        // Owner's answer to Q83: when the fee account is one "with تفصیل", the fee line names a تفصیل of a type the account takes; when
        // it has none, no تفصیل is asked (one that was sent is not kept).
        if (form.FeeRial is > 0 && form.FeeAccount is { TakesDetail: true } feeAccount)
        {
            Require(form.FeeDetail is not null && !string.IsNullOrWhiteSpace(form.FeeDetail.Code), "Transfer.FeeDetailRequired",
                "The account of bank fees takes a تفصیل; choose the تفصیل of the fee.");
            Require(form.FeeDetail!.IsActive, "Transfer.FeeDetailInactive", "The تفصیل of the fee is switched off.");
            Require(feeAccount.AllowedTypes.Contains(form.FeeDetail.Type ?? string.Empty, StringComparer.Ordinal), "Transfer.FeeDetailTypeNotAllowed",
                "The تفصیل of the fee is not of a type the account of bank fees takes.");
        }

        Require(form.Channel is { } c && Enum.IsDefined(c), "Transfer.ChannelRequired", "Choose whether the order was given at the branch or remotely.");

        // Owner's report no. 7: an internal transfer is between two accounts of the same bank (the same or another branch); Pol, Paya
        // and Satna are between two different banks.
        var sameBank = form.Source.BankId == form.Destination.BankId;
        Require(form.Method != TransferMethod.Internal || sameBank, "Transfer.InternalNeedsSameBank",
            "An internal (account to account) transfer is between two accounts of the same bank.");
        Require(!form.Method.IsInterbank() || !sameBank, "Transfer.MethodNeedsOtherBank",
            "Pol, Paya and Satna move money between two different banks; within one bank use the internal transfer.");
        Require(!form.Method.IsInterbank() || form.Amount.IsRial, "Transfer.MethodNotForForeignCurrency", "Pol, Paya and Satna carry Rial only.");
        limits.Check(form.Method, form.Channel!.Value, form.Source.BankId, form.Amount.ToRial(), usage);
    }

    private static void CheckCommon(int number, string userId, TransferAccountFact source, TransferAccountFact destination, Money amount, string? description)
    {
        Require(number > 0, "Transfer.NumberInvalid", "The transfer number must be positive.");
        Require(!string.IsNullOrWhiteSpace(userId), "Transfer.UserRequired", "User is required.");
        Require(source is not null, "Transfer.SourceRequired", "Choose the account the money leaves (انتقال دهنده).");
        Require(destination is not null, "Transfer.DestinationRequired", "Choose the account the money reaches (انتقال گیرنده).");
        Require(source!.Id != destination!.Id, "Transfer.SameAccount", "The two accounts of a transfer must differ.");
        Require(source.IsActive && destination.IsActive, "Transfer.AccountInactive", "A bank account of the transfer is switched off.");
        ArgumentNullException.ThrowIfNull(amount);
        Require(string.Equals(source.CurrencyCode.Trim(), destination.CurrencyCode.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals(source.CurrencyCode.Trim(), amount.Currency, StringComparison.OrdinalIgnoreCase),
            "Transfer.CurrencyMismatch", "Both accounts and the amount must be in the same currency.");
        Require((description?.Trim().Length ?? 0) <= MaxDescriptionLength, "Transfer.DescriptionTooLong", "The description is too long.");
    }

    /// <summary>
    /// Owner, 21 Nov 2026 (answer to Q82): every transfer pays an approved payment request written for a transfer between these two
    /// accounts; like every payment it may pay the request in full or in part, never more than remains (Q29).
    /// </summary>
    private static void CheckRequest(TransferRequestFact? request, TransferAccountFact source, TransferAccountFact destination, Money amount)
    {
        Require(request is not null, "Transfer.RequestRequired", "Choose the approved payment request (درخواست وجه) of this transfer.");
        Require(request!.IsForOwnTransfer, "Transfer.RequestNotForTransfer",
            "This payment request was not written for a transfer between the company's own bank accounts.");
        Require(request.IsApproved, "Transfer.RequestNotApproved", "The payment request of the transfer is not approved.");
        Require(request.SourceAccountId == source.Id && request.DestinationAccountId == destination.Id, "Transfer.RequestOtherAccounts",
            "The two bank accounts of the transfer must be the ones the payment request names.");
        Require(amount.Amount <= request.RemainingAmount, "Transfer.RequestOverpaid", "The amount is more than what remains to be paid on the payment request.");
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

public sealed record BankTransferRecorded(
    Guid TransferId, int FiscalYear, int Number, TransferMethod Method, FiscalDate Date, Guid SourceAccountId, Guid DestinationAccountId,
    decimal Amount, string Currency, decimal AmountRial, string UserId) : DomainEvent;

/// <summary>An electronic transfer was corrected before its voucher was confirmed; it carries the transfer as it now is.</summary>
public sealed record BankTransferCorrected(
    Guid TransferId, int FiscalYear, int Number, TransferMethod Method, FiscalDate Date, Guid SourceAccountId, Guid DestinationAccountId,
    decimal Amount, string Currency, decimal AmountRial, string UserId) : DomainEvent;

public sealed record BankTransferDeleted(
    Guid TransferId, int FiscalYear, int Number, TransferMethod Method, FiscalDate Date, decimal AmountRial, Guid? IssuedChequeId, string UserId) : DomainEvent;

public sealed record TransferChequeIssued(
    Guid TransferId, int FiscalYear, int Number, FiscalDate Date, Guid SourceAccountId, Guid DestinationAccountId, Guid IssuedChequeId, string ChequeNumber,
    string SayadId, decimal Amount, string Currency, decimal AmountRial, FiscalDate DueDate, string UserId) : DomainEvent;

public sealed record TransferChequeReceived(Guid TransferId, int FiscalYear, int Number, Guid ReceivedChequeId, FiscalDate ReceivedOn, string UserId) : DomainEvent;

public sealed record TransferChequeReceiptUndone(Guid TransferId, int FiscalYear, int Number, Guid ReceivedChequeId, string UserId) : DomainEvent;

public sealed record TransferChequeCollected(Guid TransferId, int FiscalYear, int Number, FiscalDate Date) : DomainEvent;

public sealed record TransferChequeSayadRegistered(Guid TransferId, int FiscalYear, int Number, FiscalDate Date, string UserId) : DomainEvent;

public sealed record TransferChequeMatured(Guid TransferId, int FiscalYear, int Number, FiscalDate Date, string UserId) : DomainEvent;

public sealed record TransferChequeCancelled(
    Guid TransferId, int FiscalYear, int Number, FiscalDate Date, string Reason, Guid IssuedChequeId, Guid? ReceivedChequeId, string UserId) : DomainEvent;
