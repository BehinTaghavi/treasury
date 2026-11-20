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
}

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
public sealed record TransferCheque(
    Guid IssuedChequeId, Guid ChequeBookId, long Sheet, string Number, string SayadId, FiscalDate DueDate,
    Guid? ReceivedChequeId = null, FiscalDate? ReceivedOn = null, FiscalDate? CollectedOn = null);

/// <summary>What the form of an electronic transfer says.</summary>
/// <param name="Channel">At the branch or remotely; the limits differ.</param>
/// <param name="TrackingNumber">The bank's tracking number of the order (شماره پیگیری), when typed.</param>
public sealed record ElectronicTransferForm(
    FiscalDate Date, TransferAccountFact Source, TransferAccountFact Destination, TransferMethod Method, TransferChannel? Channel, Money Amount,
    string? TrackingNumber = null, string? Description = null);

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

    /// <summary>Cheque transfers only.</summary>
    public TransferCheque? Cheque { get; private set; }

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
    public static BankTransfer IssueCheque(
        Guid id, int number, FiscalDate date, TransferAccountFact source, TransferAccountFact destination, Instrument issued, Guid chequeBookId, long sheet,
        string sayadId, string? description, string userId)
    {
        ArgumentNullException.ThrowIfNull(issued);
        CheckCommon(number, userId, source, destination, issued.Amount, description);
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
            Cheque = new TransferCheque(issued.Id, chequeBookId, sheet, issued.Number!, sayadId, issued.DueDate!.Value),
        };
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
        DateTimeOffset? deletedAt, long version)
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
    }

    private static void CheckElectronic(ElectronicTransferForm form, int number, string userId, TransferLimits limits, TransferDayUsage usage)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(limits);
        Require(Enum.IsDefined(form.Method) && form.Method.IsElectronic(), "Transfer.MethodRequired",
            "Choose the way of transfer: internal, Pol, Paya or Satna.");
        CheckCommon(number, userId, form.Source, form.Destination, form.Amount, form.Description);
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
