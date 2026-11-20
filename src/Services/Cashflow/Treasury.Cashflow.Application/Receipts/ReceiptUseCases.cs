using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Application.Vouchers;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Vouchers;

namespace Treasury.Cashflow.Application.Receipts;

/// <summary>What every receipt form asks. Dates are Jalali text ("1405/08/23"); the payer is a تفصیل code.</summary>
public sealed record ReceiptHeaderInput(
    string Date,
    string PayerDetailCode,
    Guid PurposeGroupId,
    string Purpose,
    string? Reference1 = null,
    string? Reference2 = null,
    Guid? DepartmentId = null,
    bool SalesProceeds = false,
    string? Note = null);

/// <param name="RateToRial">Rial per unit, for a foreign-currency cash desk or account; the currency is the account's own.</param>
public sealed record RecordCashReceiptCommand(string IdempotencyKey, ReceiptHeaderInput Header, Guid CashDeskId, decimal Amount, decimal? RateToRial = null);

public sealed record RecordBankReceiptCommand(
    string IdempotencyKey, ReceiptHeaderInput Header, Guid BankAccountId, DepositMethod Method, string? TrackingNumber, decimal Amount, decimal? RateToRial = null,
    string? PayerSheba = null);

/// <summary>«واریز به کارت بانک»: the card is a petty-cash holder's (<paramref name="CardKind"/> PettyCashCard, the petty cash's id) or a company card.</summary>
public sealed record RecordCardReceiptCommand(
    string IdempotencyKey, ReceiptHeaderInput Header, ReceiptAccountKind CardKind, Guid CardId, DepositMethod Method, string? TrackingNumber, decimal Amount);

/// <summary>One cheque or note on the form. <paramref name="BankId"/> is from the bank-names list (decision of 10 Nov 2026).</summary>
/// <param name="Id">Correction of a receipt: the cheque this line corrects; null for a cheque added to the receipt.</param>
public sealed record ReceivedInstrumentInput(
    InstrumentKind Kind,
    string Number,
    string? DueDate,
    MoneyInput Amount,
    Guid? BankId = null,
    string? BranchName = null,
    string? BranchCode = null,
    string? AccountNumber = null,
    string? AccountHolder = null,
    string? City = null,
    bool OutOfTown = false,
    string? SayadId = null,
    IReadOnlyList<NoteGuarantor>? Guarantors = null,
    Guid? Id = null);

public sealed record RecordInstrumentReceiptCommand(string IdempotencyKey, ReceiptHeaderInput Header, IReadOnlyList<ReceivedInstrumentInput> Instruments);

/// <summary>One promissory note on the note receipt form (owner's form «فرم دریافت سفته», 16 Nov 2026). Amounts are in Rial.</summary>
/// <param name="IssueDate">«تاریخ صدور» written on the note (Jalali text); optional.</param>
/// <param name="DueDate">«تاریخ سررسید»; required with «معین», not given with «عندالمطالبه».</param>
/// <param name="Id">Correction of a receipt: the note this line corrects; null for a note added to the receipt.</param>
public sealed record NoteLineInput(string Number, string? IssueDate, string? DueDate, decimal Amount, NoteChecks? Checks = null, Guid? Id = null);

/// <summary>A guarantor or endorser («ضامن / ظهرنویس») on the note receipt form.</summary>
/// <param name="DetailCode">Its تفصیل: the guarantor is one of the «ضامنین» of Basic Information (owner's answer to Q54, 19 Nov 2026); the
/// name is the one kept there, whatever <paramref name="Name"/> says.</param>
/// <param name="NoteNumber">The note it guarantees («شماره سفته مرتبط»); empty: every note on the receipt.</param>
public sealed record NoteGuarantorInput(string? Name, string? NationalCode = null, string? DetailCode = null, string? Phone = null, string? NoteNumber = null);

/// <summary>The receipt-level part of the note receipt form; dates are Jalali text.</summary>
public sealed record NoteTermsInput(
    NoteReason Reason,
    NoteForm Form,
    NoteMaturity Maturity,
    string? IssuerNationalCode = null,
    string? IssuerPhone = null,
    string? CommitmentSubject = null,
    string? ReturnTerms = null,
    string? CustodyPlace = null,
    bool OriginalReceived = false,
    string? OriginalReceivedOn = null,
    string? Discrepancy = null,
    NoteAttachments? Attachments = null,
    string? DeliveredBy = null);

/// <param name="SignerIds">The authorised signatories chosen by the signature rule of the note receipt (Basic Information).</param>
public sealed record RecordNoteReceiptCommand(
    string IdempotencyKey, ReceiptHeaderInput Header, NoteTermsInput Terms, IReadOnlyList<NoteLineInput> Notes, IReadOnlyList<NoteGuarantorInput>? Guarantors = null,
    IReadOnlyList<Guid>? SignerIds = null);

/// <summary>
/// Correction («ویرایش») of a saved receipt before its voucher is confirmed (owner's report no. 6, items 2 and 6): the form as it now
/// is, with the version the user saw.
/// </summary>
public sealed record CorrectCashReceiptCommand(string IdempotencyKey, Guid Id, long ExpectedVersion, ReceiptHeaderInput Header, Guid CashDeskId, decimal Amount,
    decimal? RateToRial = null);

/// <inheritdoc cref="CorrectCashReceiptCommand"/>
public sealed record CorrectBankReceiptCommand(
    string IdempotencyKey, Guid Id, long ExpectedVersion, ReceiptHeaderInput Header, Guid BankAccountId, DepositMethod Method, string? TrackingNumber, decimal Amount,
    decimal? RateToRial = null, string? PayerSheba = null);

/// <inheritdoc cref="CorrectCashReceiptCommand"/>
public sealed record CorrectCardReceiptCommand(
    string IdempotencyKey, Guid Id, long ExpectedVersion, ReceiptHeaderInput Header, ReceiptAccountKind CardKind, Guid CardId, DepositMethod Method, string? TrackingNumber,
    decimal Amount);

/// <summary>Correction of a cheque receipt: a line with an id corrects that cheque, one without adds a cheque, a cheque left out is removed.</summary>
public sealed record CorrectInstrumentReceiptCommand(string IdempotencyKey, Guid Id, long ExpectedVersion, ReceiptHeaderInput Header,
    IReadOnlyList<ReceivedInstrumentInput> Instruments);

/// <inheritdoc cref="CorrectInstrumentReceiptCommand"/>
public sealed record CorrectNoteReceiptCommand(
    string IdempotencyKey, Guid Id, long ExpectedVersion, ReceiptHeaderInput Header, NoteTermsInput Terms, IReadOnlyList<NoteLineInput> Notes,
    IReadOnlyList<NoteGuarantorInput>? Guarantors = null, IReadOnlyList<Guid>? SignerIds = null);

/// <summary>Deletion («حذف») of a receipt recorded by mistake, before its voucher is confirmed (owner's answer to Q59, 19 Nov 2026).</summary>
public sealed record DeleteReceiptCommand(string IdempotencyKey, Guid Id, long ExpectedVersion);

public sealed record CancelReceiptCommand(string IdempotencyKey, Guid Id, string Date, string Reason, long ExpectedVersion);

/// <summary>Replacement of a received cheque (تعویض, owner's item 14 (d)): a cheque receipt of the new cheques, from the same payer.</summary>
public sealed record ReplaceChequeCommand(string IdempotencyKey, Guid InstrumentId, ReceiptHeaderInput Header, IReadOnlyList<ReceivedInstrumentInput> Instruments);

/// <summary>Endorsement of a received cheque (خرج چک, owner's item 14 (g)): it pays an approved payment request (answer to Q36).</summary>
/// <param name="SignerIds">A cheque: the authorised signatories of its payment sheet, chosen by the rule «برگ پرداخت چک» (answer to Q57).</param>
public sealed record EndorseChequeCommand(string IdempotencyKey, Guid InstrumentId, string Date, Guid PaymentRequestId, string SayadTracking, string? Note = null,
    IReadOnlyList<Guid>? SignerIds = null);

/// <summary>The receiver brings an endorsed cheque back bounced (owner's item 14 (h)), with the bank's bounce notice.</summary>
public sealed record EndorsementReturnedCommand(string IdempotencyKey, Guid InstrumentId, string Date, string? NoticeNumber, string? Reason = null, string? TrackingCode = null);

/// <summary>The receipt-level part of a note receipt as the screens and the printed receipt show it.</summary>
public sealed record NoteTermsView(
    string Reason, string Form, string Maturity, string? IssuerNationalCode, string? IssuerPhone, string? CommitmentSubject, string? ReturnTerms,
    string? CustodyPlace, bool OriginalReceived, string? OriginalReceivedOn, string? Discrepancy, NoteAttachments Attachments, string? DeliveredBy,
    IReadOnlyList<ReceiptSigner> Signers)
{
    public static NoteTermsView? From(NoteReceiptTerms? t) => t is null ? null : new(
        t.Reason.ToString(), t.Form.ToString(), t.Maturity.ToString(), t.IssuerNationalCode, t.IssuerPhone, t.CommitmentSubject, t.ReturnTerms, t.CustodyPlace,
        t.OriginalReceived, t.OriginalReceivedOn?.ToJalaliString(), t.Discrepancy, t.Attachments ?? new NoteAttachments(), t.DeliveredBy, t.Signers ?? []);
}

public sealed record ReceiptView(
    Guid Id,
    string Kind,
    int FiscalYear,
    int Number,
    string Date,
    string PayerDetailCode,
    string PayerName,
    Guid PurposeGroupId,
    string PurposeGroupName,
    string Purpose,
    string? Reference1,
    string? Reference2,
    Guid? DepartmentId,
    bool SalesProceeds,
    string? Note,
    string? AccountKind,
    Guid? AccountId,
    string? Method,
    string? TrackingNumber,
    decimal? Amount,
    string? Currency,
    decimal? RateToRial,
    decimal TotalRial,
    int InstrumentCount,
    string State,
    string? CancelledOn,
    string? CancelReason,
    string RecordedBy,
    long Version,
    IReadOnlyList<InstrumentView>? Instruments = null,
    Guid? ReplacedInstrumentId = null,
    IReadOnlyList<ChequeStatusView>? ChequeStatuses = null,
    string? PayerSheba = null,
    NoteTermsView? NoteTerms = null,
    string? NotCorrectable = null,
    string? NotDeletable = null,
    string? DeletedBy = null,
    DateTimeOffset? DeletedAt = null)
{
    /// <param name="statuses">The latest state of the receipt's cheques, for the list (owner, 18 Nov 2026: the «وضعیت» column).</param>
    /// <param name="notCorrectable">Why «ویرایش» is closed (a rule's code), or null when the receipt can still be corrected; only on one receipt, not in lists.</param>
    /// <param name="notDeletable">The same for «حذف».</param>
    public static ReceiptView From(Receipt r, IReadOnlyList<Instrument>? instruments = null, FiscalDate? today = null, IReadOnlyList<ChequeStatusView>? statuses = null,
        string? notCorrectable = null, string? notDeletable = null) => new(
        r.Id, r.Kind.ToString(), r.FiscalYear, r.Number, r.Date.ToJalaliString(), r.Payer.DetailCode ?? string.Empty, r.Payer.DisplayName,
        r.PurposeGroupId, r.PurposeGroupName, r.Purpose, r.Reference1, r.Reference2, r.DepartmentId, r.SalesProceeds, r.Note,
        r.AccountKind?.ToString(), r.AccountId, r.Method?.ToString(), r.TrackingNumber, r.Amount?.Amount, r.Amount?.Currency, r.Amount?.RateToRial,
        r.TotalRial, r.InstrumentIds.Count, r.State.ToString(), r.CancelledOn?.ToJalaliString(), r.CancelReason, r.RecordedBy, r.Version,
        instruments?.Select(i => InstrumentView.From(i, today)).ToList(), r.ReplacedInstrumentId,
        statuses ?? instruments?.Select(i => new ChequeStatusView(i.State.ToString(), today is { } t && i.HasReachedDueDate(t), i.Id, i.Number, i.Kind.ToString())).ToList(), r.PayerSheba, NoteTermsView.From(r.NoteTerms),
        notCorrectable, notDeletable, r.DeletedBy, r.DeletedAt);
}

/// <summary>The latest state of one cheque or note of a receipt; <see cref="ReachedDueDate"/> marks «سررسید چک».</summary>
/// <summary>The latest status of one cheque of a receipt, with the cheque's id, number and kind (owner's report no. 6, items 14 and 20).</summary>
public sealed record ChequeStatusView(string State, bool ReachedDueDate, Guid Id = default, string? Number = null, string? Kind = null);

/// <summary>
/// Receipts (owner, 14 Nov 2026): cash, money paid into a bank account (slip, transfer, POS, card-to-card) and cheques and notes,
/// current or post-dated, in Rial or foreign currency. Each write is one command in one transaction. A receipt of cheques and notes
/// is saved together with the instruments it creates (ADR 0002: a receipt and the new instruments on it), so a half-saved receipt
/// (the MVC version's failure, design document 2.2) cannot happen.
/// </summary>
public sealed class ReceiptUseCases(
    IUnitOfWork unitOfWork,
    IReceiptRepository receipts,
    IInstrumentRepository instruments,
    IReceiptAccountLookup accounts,
    IBankAccountLookup bankAccounts,
    IReceiptReferenceLookup reference,
    IFiscalPeriodPolicy fiscalPeriods,
    IIdempotencyStore idempotency,
    ICurrentUser currentUser,
    IVoucherTemplateRepository templates,
    IConfirmedVoucherRepository vouchers,
    IPaymentRequestLookup paymentRequests,
    TimeProvider? clock = null)
{
    /// <summary>The name reference-svc gives the note receipt among the documents with a signature rule.</summary>
    public const string NoteReceiptDocument = "NoteReceipt";

    /// <summary>The payment sheet of a cheque («برگ پرداخت چک») among the documents with a signature rule (owner's answer to Q57).</summary>
    public const string ChequePaymentSheetDocument = "ChequePaymentSheet";

    /// <summary>At most this many cheques and notes on one receipt.</summary>
    public const int MaxInstruments = 100;

    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> RecordCashAsync(RecordCashReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, _) = await BuildCashAsync(command, preview: false, ct).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.AddAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    public Task<Result<Guid>> RecordBankDepositAsync(RecordBankReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, account) = await BuildBankDepositAsync(command, preview: false, ct).ConfigureAwait(false);

            // A bank account with a receipt is "in use" and can no longer be deleted (owner, 14 Nov 2026).
            await bankAccounts.MarkInUseAsync(account.Id, ct).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.AddAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>«واریز به کارت بانک» (owner's report no. 6, item 12; answers to Q68–Q70): money paid onto a petty-cash holder's card or a company card.</summary>
    public Task<Result<Guid>> RecordCardDepositAsync(RecordCardReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, _) = await BuildCardDepositAsync(command, preview: false, ct).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.AddAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>
    /// Cheques and notes from one payer (old D_Check with «ثبت چک بعدی»). Each becomes an instrument in the cash desk. A cheque with
    /// the same bank and number as one received before is saved with a warning (rule R1, open question Q1); the same cheque twice
    /// on one receipt is refused.
    /// </summary>
    public Task<Result<Guid>> RecordInstrumentsAsync(RecordInstrumentReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, created, duplicate) = await BuildInstrumentsAsync(command, preview: false, ct).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            foreach (var bankId in command.Instruments.Select(l => l.BankId).OfType<Guid>().Distinct())
            {
                await reference.MarkInUseAsync("banks", bankId, ct).ConfigureAwait(false);
            }

            await receipts.AddAsync(receipt, ct).ConfigureAwait(false);
            foreach (var instrument in created)
            {
                await instruments.AddAsync(instrument, ct).ConfigureAwait(false);
            }

            return duplicate ? Result<Guid>.Ok(receipt.Id, InstrumentUseCases.PossibleDuplicateWarning) : Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>
    /// Promissory notes of one issuer on a note receipt (owner's form «فرم دریافت سفته», 16 Nov 2026; report no. 4, item 13), numbered in
    /// a series of their own. Each note becomes an instrument in the cash desk, saved with the receipt in one transaction (ADR 0002).
    /// </summary>
    public Task<Result<Guid>> RecordNotesAsync(RecordNoteReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, created) = await BuildNotesAsync(command, preview: false, ct).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.AddAsync(receipt, ct).ConfigureAwait(false);
            foreach (var note in created)
            {
                await instruments.AddAsync(note, ct).ConfigureAwait(false);
            }

            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    public Task<Result<VoucherView>> PreviewNotesAsync(RecordNoteReceiptCommand command, CancellationToken cancellationToken) =>
        PreviewAsync(async ct =>
        {
            var (receipt, created) = await BuildNotesAsync(command, preview: true, ct).ConfigureAwait(false);
            return (receipt, (IReadOnlyList<Instrument>)created, (LedgerSide?)null, (Replacement?)null);
        }, cancellationToken);

    /// <summary>
    /// Replacement («تعویض», owner's item 14 (d)): the new cheques come on a cheque receipt of their own (cheque receipt numbers) that names
    /// the old cheque, which becomes «تعویض شده». They may differ in amount and number (answer to Q31). The receipt, the new cheques and
    /// the old cheque are saved in one transaction (ADR 0002 amendment).
    /// Owner, 19 Nov 2026 (answer to Q62): only a cheque that was cancelled first («ابطال شده») is replaced; the cheque itself refuses
    /// the step in any other state. Its cancellation debited «مطالبات سوخت شده», which the replacement's voucher credits.
    /// </summary>
    public Task<Result<Guid>> ReplaceAsync(ReplaceChequeCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var old = await ReceivedChequeAsync(command.InstrumentId, ct).ConfigureAwait(false);
            var (receipt, created, duplicate) = await BuildInstrumentsAsync(
                new RecordInstrumentReceiptCommand(command.IdempotencyKey, command.Header, command.Instruments), preview: false, ct, old).ConfigureAwait(false);
            old.Replace(receipt.Id, receipt.Date, currentUser.UserId);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            foreach (var bankId in command.Instruments.Select(l => l.BankId).OfType<Guid>().Distinct())
            {
                await reference.MarkInUseAsync("banks", bankId, ct).ConfigureAwait(false);
            }

            await receipts.AddAsync(receipt, ct).ConfigureAwait(false);
            foreach (var instrument in created)
            {
                await instruments.AddAsync(instrument, ct).ConfigureAwait(false);
            }

            await instruments.SaveAsync(old, ct).ConfigureAwait(false);
            return duplicate ? Result<Guid>.Ok(receipt.Id, InstrumentUseCases.PossibleDuplicateWarning) : Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    public Task<Result<VoucherView>> PreviewReplacementAsync(ReplaceChequeCommand command, CancellationToken cancellationToken) =>
        PreviewAsync(async ct =>
        {
            var old = await ReceivedChequeAsync(command.InstrumentId, ct).ConfigureAwait(false);
            var from = old.State;
            var (receipt, created, _) = await BuildInstrumentsAsync(
                new RecordInstrumentReceiptCommand("preview", command.Header, command.Instruments), preview: true, ct, old).ConfigureAwait(false);
            old.Replace(receipt.Id, receipt.Date, currentUser.UserId); // checks the step is allowed; nothing is saved
            return (receipt, (IReadOnlyList<Instrument>)created, (LedgerSide?)null, new Replacement(old, from));
        }, cancellationToken);

    /// <summary>
    /// Endorsement («خرج چک», owner's item 14 (g)): the cheque pays an approved payment request (answer to Q36). The request's beneficiary
    /// receives it and its payment purpose group (debited on the voucher) and purpose are taken; the cheque may pay the request in full or
    /// in part, never more than remains; the Sayad transfer's tracking number is required (answer to Q32). The payment is recorded on the
    /// request in requests-svc under a key of the cheque and step, so a retried command does not pay twice. An ordinary promissory note
    /// is endorsed the same way («خرج سفته: ظهرنویسی و انتقال به غیر», owner, 16 Nov 2026), without a Sayad transfer.
    /// Owner, 19 Nov 2026 («ثبت انتقال»): a cheque is transferred only when it is «ثبت صیادی شد» and on the request of the kind «ثبت انتقال
    /// (خرج چک)» that was written for it, whose amount is the cheque's (checked by the cheque and by requests-svc).
    /// </summary>
    public Task<Result<Guid>> EndorseAsync(EndorseChequeCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var cheque = await ReceivedChequeAsync(command.InstrumentId, ct).ConfigureAwait(false);
            var date = FiscalDate.ParseJalali(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            if (command.PaymentRequestId == Guid.Empty)
            {
                throw new DomainException("Instrument.PaymentRequestRequired", "Choose the approved payment request the cheque pays.");
            }

            var request = await paymentRequests.GetAsync(command.PaymentRequestId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Instrument.PaymentRequestRequired", "The payment request was not found.");
            if (request.State != "Approved")
            {
                throw new DomainException("Instrument.PaymentRequestNotApproved", $"Payment request {request.Number} is not approved for payment.");
            }

            if (!string.Equals(request.Currency, cheque.Amount.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("Instrument.PaymentRequestCurrency", "The cheque and the payment request are in different currencies.");
            }

            if (cheque.Amount.Amount > request.Amount - request.PaidAmount)
            {
                throw new DomainException("Instrument.PaymentRequestOverpaid", "The cheque is more than what remains to be paid on the request.");
            }

            var group = await reference.PurposeGroupAsync(request.PurposeGroupId, ct).ConfigureAwait(false);
            if (group is null || group.IsForReceipts || !group.IsActive)
            {
                throw new DomainException("Instrument.EndorsementGroupNotForPayments", "The request's purpose group is not an active payment group.");
            }

            // Owner's answers to Q57 (19 Nov 2026): the transfer of a cheque gets a payment sheet with a serial number of its own, signed
            // also by the authorised signatories of the rule «برگ پرداخت چک». The number is taken in this transaction: a refused transfer
            // gives it back.
            int? sheet = null;
            IReadOnlyList<ReceiptSigner> signers = [];
            if (cheque.Kind.IsCheque())
            {
                signers = (await reference.SignatureRuleAsync(ChequePaymentSheetDocument, ct).ConfigureAwait(false) ?? SignatureRuleFact.None).Choose(command.SignerIds);
                sheet = await instruments.NextPaymentSheetNumberAsync(date.JalaliYear, ct).ConfigureAwait(false);
            }

            cheque.Endorse(
                new Endorsement(request.BeneficiaryDetailCode, request.BeneficiaryName, request.PurposeGroupId, request.Purpose, command.SayadTracking, request.Id,
                    request.TransferChequeId, sheet, signers),
                date, currentUser.UserId, command.Note);
            var key = cheque.EndorsementPaymentKey(cheque.History.Count);
            await paymentRequests.RecordPaymentAsync(request.Id,
                new RequestPaymentInput(key, cheque.Id, (cheque.Kind.IsNote() ? $"سفته {cheque.Number}" : $"چک {cheque.Number} {cheque.BankName}").Trim(), cheque.Amount.Amount,
                    cheque.Amount.Currency, date.ToJalaliString()), ct)
                .ConfigureAwait(false);
            await instruments.SaveAsync(cheque, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(cheque.Id);
        }, cancellationToken);

    /// <summary>
    /// The receiver brings the endorsed cheque back bounced («برگشت خرج», owner's item 14 (h)): the cheque is «برگشت شده» again and the
    /// payment it made on the request is taken back, so the request is to be paid again.
    /// </summary>
    public Task<Result<Guid>> EndorsementReturnedAsync(EndorsementReturnedCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var cheque = await ReceivedChequeAsync(command.InstrumentId, ct).ConfigureAwait(false);
            var date = FiscalDate.ParseJalali(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            var step = cheque.History.Count;
            var endorsement = cheque.History[^1];
            cheque.RecordEndorsementReturned(date, currentUser.UserId, command.NoticeNumber, command.Reason, command.TrackingCode);
            if (endorsement.Action == InstrumentAction.Endorse && endorsement.Details?.PaymentRequestId is { } requestId)
            {
                await paymentRequests.ReversePaymentAsync(requestId, cheque.EndorsementPaymentKey(step), ct).ConfigureAwait(false);
            }

            await instruments.SaveAsync(cheque, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(cheque.Id);
        }, cancellationToken);

    private async Task<Instrument> ReceivedChequeAsync(Guid id, CancellationToken ct)
    {
        var cheque = await instruments.GetAsync(id, ct).ConfigureAwait(false);
        if (cheque is null || cheque.Direction != InstrumentDirection.Received)
        {
            throw new DomainException("Instrument.NotFound", "The cheque was not found.");
        }

        return cheque;
    }

    /// <summary>
    /// The voucher the receipt form will produce, shown at the bottom of the form before saving (owner's report no. 4, item 8). Nothing is
    /// saved and no receipt number is taken; an incomplete form answers with the first rule it breaks.
    /// </summary>
    public Task<Result<VoucherView>> PreviewCashAsync(RecordCashReceiptCommand command, CancellationToken cancellationToken) =>
        PreviewAsync(async ct =>
        {
            var (receipt, desk) = await BuildCashAsync(command, preview: true, ct).ConfigureAwait(false);
            return (receipt, (IReadOnlyList<Instrument>)[], new LedgerSide(desk.LedgerAccountCode, desk.DetailCode), (Replacement?)null);
        }, cancellationToken);

    public Task<Result<VoucherView>> PreviewBankDepositAsync(RecordBankReceiptCommand command, CancellationToken cancellationToken) =>
        PreviewAsync(async ct =>
        {
            var (receipt, account) = await BuildBankDepositAsync(command, preview: true, ct).ConfigureAwait(false);
            return (receipt, (IReadOnlyList<Instrument>)[], new LedgerSide(account.LedgerAccountCode, account.DetailCode), (Replacement?)null);
        }, cancellationToken);

    public Task<Result<VoucherView>> PreviewCardDepositAsync(RecordCardReceiptCommand command, CancellationToken cancellationToken) =>
        PreviewAsync(async ct =>
        {
            var (receipt, card) = await BuildCardDepositAsync(command, preview: true, ct).ConfigureAwait(false);
            return (receipt, (IReadOnlyList<Instrument>)[], new LedgerSide(card.LedgerAccountCode, card.DetailCode), (Replacement?)null);
        }, cancellationToken);

    public Task<Result<VoucherView>> PreviewInstrumentsAsync(RecordInstrumentReceiptCommand command, CancellationToken cancellationToken) =>
        PreviewAsync(async ct =>
        {
            var (receipt, created, _) = await BuildInstrumentsAsync(command, preview: true, ct).ConfigureAwait(false);
            return (receipt, (IReadOnlyList<Instrument>)created, (LedgerSide?)null, (Replacement?)null);
        }, cancellationToken);

    private async Task<Result<VoucherView>> PreviewAsync(
        Func<CancellationToken, Task<(Receipt Receipt, IReadOnlyList<Instrument> Instruments, LedgerSide? Account, Replacement? Replacement)>> build,
        CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteAsync(async ct =>
            {
                var (receipt, created, account, replacement) = await build(ct).ConfigureAwait(false);
                var group = await reference.PurposeGroupAsync(receipt.PurposeGroupId, ct).ConfigureAwait(false);
                var template = await templates.GetAsync(ct).ConfigureAwait(false);
                var voucher = ReceiptVouchers.ForReceipt(receipt, created, account, group?.LedgerAccountCode, template, replacement);
                var name = replacement is null ? ReceiptVouchers.OperationName(receipt.Kind) : "تعویض چک";
                return Result<VoucherView>.Ok(VoucherView.From(voucher with { Operation = name }, "Receipt", 0));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return Result<VoucherView>.Fail(ex.Code, ex.Message);
        }
    }

    private async Task<(Receipt Receipt, ReceiptAccountFact Desk)> BuildCashAsync(RecordCashReceiptCommand command, bool preview, CancellationToken ct)
    {
        var desk = await accounts.CashDeskAsync(command.CashDeskId, ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.AccountNotFound", "The cash desk was not found.");
        var (header, date) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
        var number = preview ? 1 : await receipts.NextNumberAsync(date.JalaliYear, ReceiptKind.Cash, ct).ConfigureAwait(false);
        return (Receipt.RecordCash(Guid.NewGuid(), number, header, desk, Amount(command.Amount, desk.CurrencyCode, command.RateToRial), currentUser.UserId), desk);
    }

    private async Task<(Receipt Receipt, ReceiptAccountFact Account)> BuildBankDepositAsync(RecordBankReceiptCommand command, bool preview, CancellationToken ct)
    {
        var account = await accounts.BankAccountAsync(command.BankAccountId, ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.AccountNotFound", "The bank account was not found.");
        var (header, date) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
        var number = preview ? 1 : await receipts.NextNumberAsync(date.JalaliYear, ReceiptKind.BankDeposit, ct).ConfigureAwait(false);
        return (Receipt.RecordBankDeposit(
            Guid.NewGuid(), number, header, account, command.Method, command.TrackingNumber, Amount(command.Amount, account.CurrencyCode, command.RateToRial), currentUser.UserId, command.PayerSheba), account);
    }

    private async Task<(Receipt Receipt, ReceiptAccountFact Card)> BuildCardDepositAsync(RecordCardReceiptCommand command, bool preview, CancellationToken ct)
    {
        var card = await CardAsync(command.CardKind, command.CardId, ct).ConfigureAwait(false);
        var (header, date) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
        var number = preview ? 1 : await receipts.NextNumberAsync(date.JalaliYear, ReceiptKind.CardDeposit, ct).ConfigureAwait(false);
        return (Receipt.RecordCardDeposit(
            Guid.NewGuid(), number, header, card, command.Method, command.TrackingNumber, Amount(command.Amount, card.CurrencyCode, null), currentUser.UserId), card);
    }

    /// <summary>The bank card of «واریز به کارت بانک» as accounts-svc describes it; a cash desk or bank account is not one.</summary>
    private async Task<ReceiptAccountFact> CardAsync(ReceiptAccountKind kind, Guid id, CancellationToken ct)
    {
        if (!kind.IsCard())
        {
            throw new DomainException("Receipt.AccountKindWrong", "A deposit onto a bank card names a petty-cash holder's card or a company card.");
        }

        return await accounts.CardAsync(kind, id, ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.AccountNotFound", "The bank card was not found.");
    }

    private async Task<(Receipt Receipt, List<Instrument> Created, bool Duplicate)> BuildInstrumentsAsync(
        RecordInstrumentReceiptCommand command, bool preview, CancellationToken ct, Instrument? replaces = null, Correction? correcting = null)
    {
        ArgumentNullException.ThrowIfNull(command.Instruments);
        if (command.Instruments.Count > MaxInstruments)
        {
            throw new DomainException("Receipt.TooManyInstruments", $"At most {MaxInstruments} cheques and notes on one receipt.");
        }

        var (header, date) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
        var receiptId = correcting?.Receipt.Id ?? Guid.NewGuid();
        var payer = CounterpartyRef.ForDetail(header.Payer.DetailCode, header.Payer.Name);
        var user = currentUser.UserId;
        var created = new List<Instrument>();
        var seen = new HashSet<(string?, string)>();
        var duplicate = false;
        var own = correcting?.Existing.Select(i => i.Id).ToHashSet() ?? [];

        // Owner, 18 Nov 2026: «خارج از مرکز» follows from the city of the cheque's bank and the city of the company's address.
        var companyCity = command.Instruments.Any(l => !string.IsNullOrWhiteSpace(l.City)) ? await reference.CompanyCityAsync(ct).ConfigureAwait(false) : null;
        foreach (var line in command.Instruments)
        {
            string? bankName = null;
            if (line.BankId is { } bankId)
            {
                bankName = await reference.BankNameAsync(bankId, ct).ConfigureAwait(false)
                    ?? throw new DomainException("Instrument.BankNotFound", "The bank was not found in the bank-names list.");
            }

            FiscalDate? due = string.IsNullOrWhiteSpace(line.DueDate) ? null : FiscalDate.ParseJalali(line.DueDate);
            var details = new ReceivedInstrumentDetails(line.BankId, line.BranchName, line.BranchCode, line.AccountNumber, line.AccountHolder, line.City,
                line.OutOfTown, line.SayadId, line.Guarantors).WithOutOfTownFor(companyCity);
            Instrument instrument;
            if (correcting is not null && line.Id is { } lineId)
            {
                // A cheque on which a step was recorded keeps what is written on it (answer to Q60); the line is shown, not corrected.
                instrument = correcting.Line(lineId);
                if (instrument.IsAsReceived)
                {
                    instrument.CorrectReceived(line.Kind, line.Number, bankName, line.Amount.ToMoney(), date, due, payer, header.Purpose, user, details);
                }
            }
            else
            {
                instrument = Instrument.Receive(Guid.NewGuid(), line.Kind, line.Number, bankName, line.Amount.ToMoney(), date, due, payer, header.Purpose, user, details, receiptId);
            }

            if (!seen.Add((instrument.BankName, instrument.Number!)))
            {
                throw new DomainException("Receipt.SameInstrumentTwice", $"Cheque or note {instrument.Number} is entered twice on this receipt.");
            }

            // The receipt's own cheques, as stored before a correction, are not duplicates of themselves.
            if (instrument.BankName is { } bank && (await instruments.FindReceivedAsync(bank, instrument.Number!, ct).ConfigureAwait(false)).Any(id => !own.Contains(id)))
            {
                duplicate = true;
            }

            created.Add(instrument);
        }

        if (correcting is not null)
        {
            correcting.Receipt.CorrectInstruments(header, created, user, correcting.VoucherConfirmed);
            return (correcting.Receipt, created, duplicate);
        }

        var number = preview ? 1 : await receipts.NextNumberAsync(date.JalaliYear, ReceiptKind.Instruments, ct).ConfigureAwait(false);
        return (Receipt.RecordInstruments(receiptId, number, header, created, user, replaces), created, duplicate);
    }

    private async Task<(Receipt Receipt, List<Instrument> Created)> BuildNotesAsync(RecordNoteReceiptCommand command, bool preview, CancellationToken ct,
        Correction? correcting = null)
    {
        ArgumentNullException.ThrowIfNull(command.Terms);
        ArgumentNullException.ThrowIfNull(command.Notes);
        if (command.Notes.Count > MaxInstruments)
        {
            throw new DomainException("Receipt.TooManyInstruments", $"At most {MaxInstruments} cheques and notes on one receipt.");
        }

        var (header, date) = await HeaderAsync(command.Header, ct, noteReceipt: true).ConfigureAwait(false);
        var t = command.Terms;
        var terms = new NoteReceiptTerms(
            t.Reason, t.Form, t.Maturity, t.IssuerNationalCode, t.IssuerPhone, t.CommitmentSubject, t.ReturnTerms, t.CustodyPlace, t.OriginalReceived,
            string.IsNullOrWhiteSpace(t.OriginalReceivedOn) ? null : FiscalDate.ParseJalali(t.OriginalReceivedOn), t.Discrepancy, t.Attachments, t.DeliveredBy);
        var rule = await reference.SignatureRuleAsync(NoteReceiptDocument, ct).ConfigureAwait(false);

        // Guarantors are chosen from the «ضامنین» of Basic Information only (owner's answer to Q54, 19 Nov 2026; before, a تفصیل or a typed
        // name); each guarantees one note («شماره سفته مرتبط») or all of them.
        var numbers = command.Notes.Select(n => n.Number?.Trim() ?? string.Empty).ToList();
        var guarantors = new List<(NoteGuarantor Guarantor, string? NoteNumber)>();
        foreach (var g in command.Guarantors ?? [])
        {
            var code = string.IsNullOrWhiteSpace(g.DetailCode) ? null : g.DetailCode.Trim();
            var party = code is null ? null : await reference.NotePartyAsync(code, ct).ConfigureAwait(false);
            var guarantor = NoteParties.Guarantor(party, g.NationalCode, g.Phone);
            var noteNumber = string.IsNullOrWhiteSpace(g.NoteNumber) ? null : g.NoteNumber.Trim();
            if (noteNumber is not null && !numbers.Contains(noteNumber))
            {
                throw new DomainException("Receipt.GuarantorNoteNotFound", $"Note {noteNumber} named for a guarantor is not on this receipt.");
            }

            guarantors.Add((guarantor, noteNumber));
        }

        var receiptId = correcting?.Receipt.Id ?? Guid.NewGuid();
        var payer = CounterpartyRef.ForDetail(header.Payer.DetailCode, header.Payer.Name);
        var user = currentUser.UserId;
        var kind = terms.InstrumentKind;
        var onDemand = terms.Maturity == NoteMaturity.OnDemand;
        var created = new List<Instrument>();
        var seen = new HashSet<string>();
        foreach (var line in command.Notes)
        {
            var number = line.Number?.Trim() ?? string.Empty;
            var details = new ReceivedInstrumentDetails(
                Guarantors: guarantors.Where(g => g.NoteNumber is null || g.NoteNumber == number).Select(g => g.Guarantor).ToList(),
                WrittenOn: string.IsNullOrWhiteSpace(line.IssueDate) ? null : FiscalDate.ParseJalali(line.IssueDate).Value,
                OnDemand: onDemand,
                Checks: line.Checks ?? new NoteChecks());
            FiscalDate? due = string.IsNullOrWhiteSpace(line.DueDate) ? null : FiscalDate.ParseJalali(line.DueDate);
            Instrument note;
            if (correcting is not null && line.Id is { } lineId)
            {
                note = correcting.Line(lineId);
                if (note.IsAsReceived)
                {
                    note.CorrectReceived(kind, number, null, new MoneyInput(line.Amount).ToMoney(), date, due, payer, header.Purpose, user, details);
                }
            }
            else
            {
                note = Instrument.Receive(Guid.NewGuid(), kind, number, null, new MoneyInput(line.Amount).ToMoney(), date, due, payer, header.Purpose, user, details, receiptId);
            }

            if (!seen.Add(note.Number!))
            {
                throw new DomainException("Receipt.SameInstrumentTwice", $"Cheque or note {note.Number} is entered twice on this receipt.");
            }

            created.Add(note);
        }

        if (correcting is not null)
        {
            correcting.Receipt.CorrectNotes(header, terms, created, user, correcting.VoucherConfirmed, rule, command.SignerIds);
            return (correcting.Receipt, created);
        }

        var receiptNumber = preview ? 1 : await receipts.NextNumberAsync(date.JalaliYear, ReceiptKind.Notes, ct).ConfigureAwait(false);
        return (Receipt.RecordNotes(receiptId, receiptNumber, header, terms, created, user, rule, command.SignerIds), created);
    }

    /// <summary>
    /// Correction («ویرایش») of a cash receipt before its voucher is confirmed (owner's report no. 6, item 2). One command, one
    /// transaction, the receipt saved once; it keeps its number.
    /// </summary>
    public Task<Result<Guid>> CorrectCashAsync(CorrectCashReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, confirmed) = await LoadForCorrectionAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var desk = await accounts.CashDeskAsync(command.CashDeskId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Receipt.AccountNotFound", "The cash desk was not found.");
            var (header, _) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
            receipt.CorrectCash(header, desk, Amount(command.Amount, desk.CurrencyCode, command.RateToRial), currentUser.UserId, confirmed);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.SaveAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>Correction of a receipt of money paid into a bank account; see <see cref="CorrectCashAsync"/>.</summary>
    public Task<Result<Guid>> CorrectBankDepositAsync(CorrectBankReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, confirmed) = await LoadForCorrectionAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var account = await accounts.BankAccountAsync(command.BankAccountId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Receipt.AccountNotFound", "The bank account was not found.");
            var (header, _) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
            receipt.CorrectBankDeposit(header, account, command.Method, command.TrackingNumber, Amount(command.Amount, account.CurrencyCode, command.RateToRial),
                currentUser.UserId, command.PayerSheba, confirmed);
            await bankAccounts.MarkInUseAsync(account.Id, ct).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.SaveAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>Correction of a deposit onto a bank card; see <see cref="CorrectCashAsync"/>.</summary>
    public Task<Result<Guid>> CorrectCardDepositAsync(CorrectCardReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, confirmed) = await LoadForCorrectionAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var card = await CardAsync(command.CardKind, command.CardId, ct).ConfigureAwait(false);
            var (header, _) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
            receipt.CorrectCardDeposit(header, card, command.Method, command.TrackingNumber, Amount(command.Amount, card.CurrencyCode, null), currentUser.UserId, confirmed);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.SaveAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>
    /// Correction of a cheque receipt (owner's report no. 6, item 6: while the cheque is «نزد صندوق» and its voucher is not confirmed).
    /// A line with an id corrects that cheque, a line without one adds a cheque, and a cheque left out is taken off the receipt. A
    /// cheque on which a step was recorded («ثبت صیادی» …) stays as it is and cannot be taken off; the other cheques of its receipt are
    /// still corrected and removed (owner's answer to Q60). The receipt and its cheques are saved in one transaction (ADR 0002).
    /// </summary>
    public Task<Result<Guid>> CorrectInstrumentsAsync(CorrectInstrumentReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, confirmed) = await LoadForCorrectionAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var existing = await instruments.ListByReceiptAsync(receipt.Id, ct).ConfigureAwait(false);
            var (_, lines, duplicate) = await BuildInstrumentsAsync(
                new RecordInstrumentReceiptCommand(command.IdempotencyKey, command.Header, command.Instruments), preview: false, ct,
                correcting: new Correction(receipt, existing, confirmed)).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            foreach (var bankId in command.Instruments.Select(l => l.BankId).OfType<Guid>().Distinct())
            {
                await reference.MarkInUseAsync("banks", bankId, ct).ConfigureAwait(false);
            }

            await receipts.SaveAsync(receipt, ct).ConfigureAwait(false);
            await SaveCorrectedLinesAsync(existing, lines, ct).ConfigureAwait(false);
            return duplicate ? Result<Guid>.Ok(receipt.Id, InstrumentUseCases.PossibleDuplicateWarning) : Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>Correction of a receipt of promissory notes; see <see cref="CorrectInstrumentsAsync"/>.</summary>
    public Task<Result<Guid>> CorrectNotesAsync(CorrectNoteReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, confirmed) = await LoadForCorrectionAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var existing = await instruments.ListByReceiptAsync(receipt.Id, ct).ConfigureAwait(false);
            var (_, lines) = await BuildNotesAsync(
                new RecordNoteReceiptCommand(command.IdempotencyKey, command.Header, command.Terms, command.Notes, command.Guarantors, command.SignerIds), preview: false, ct,
                new Correction(receipt, existing, confirmed)).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.SaveAsync(receipt, ct).ConfigureAwait(false);
            await SaveCorrectedLinesAsync(existing, lines, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>A receipt being corrected: itself, its cheques or notes as stored, and whether its voucher is confirmed.</summary>
    private sealed record Correction(Receipt Receipt, IReadOnlyList<Instrument> Existing, bool VoucherConfirmed)
    {
        /// <summary>The stored cheque or note a form line corrects.</summary>
        public Instrument Line(Guid id) => Existing.FirstOrDefault(i => i.Id == id)
            ?? throw new DomainException("Receipt.InstrumentNotOnReceipt", "A corrected cheque or note is not on this receipt.");
    }

    private async Task<(Receipt Receipt, bool VoucherConfirmed)> LoadForCorrectionAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var receipt = await receipts.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.NotFound", "The receipt was not found.");
        if (receipt.Version != expectedVersion)
        {
            throw new ConcurrencyConflictException("The receipt was changed by someone else.");
        }

        var confirmed = await vouchers.FindAsync(VoucherSource.Receipt, receipt.Id, 0, ct).ConfigureAwait(false) is not null;
        receipt.EnsureCorrectable(confirmed);
        return (receipt, confirmed);
    }

    /// <summary>
    /// Deletion («حذف») of a receipt recorded by mistake, before its voucher is confirmed (owner's answer to Q59, 19 Nov 2026). The
    /// receipt stays in the list as «حذف شده» with who deleted it and when (answer to Q67, 20 Nov 2026); the cheques or notes on it are
    /// deleted. One transaction (ADR 0002): a deleted receipt whose cheques are still in the cash desk never stays.
    /// </summary>
    public Task<Result<Guid>> DeleteAsync(DeleteReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var receipt = await receipts.GetAsync(command.Id, ct).ConfigureAwait(false)
                ?? throw new DomainException("Receipt.NotFound", "The receipt was not found.");
            if (receipt.Version != command.ExpectedVersion)
            {
                throw new ConcurrencyConflictException("The receipt was changed by someone else.");
            }

            await EnsureOpenAsync(receipt.Date, ct).ConfigureAwait(false);
            var confirmed = await vouchers.FindAsync(VoucherSource.Receipt, receipt.Id, 0, ct).ConfigureAwait(false) is not null;
            var lines = await instruments.ListByReceiptAsync(receipt.Id, ct).ConfigureAwait(false);
            receipt.Delete(currentUser.UserId, confirmed, lines, (clock ?? TimeProvider.System).GetUtcNow());
            foreach (var line in lines)
            {
                line.RemoveFromReceipt(currentUser.UserId);
                await instruments.RemoveAsync(line, ct).ConfigureAwait(false);
            }

            await receipts.SaveAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>
    /// Saves the lines of a corrected receipt: corrected ones, added ones, and removes the ones left out. A line that was left as it
    /// is (a cheque on which a step was recorded) has nothing to save.
    /// </summary>
    private async Task SaveCorrectedLinesAsync(IReadOnlyList<Instrument> existing, IReadOnlyList<Instrument> lines, CancellationToken ct)
    {
        foreach (var removed in existing.Where(e => lines.All(l => l.Id != e.Id)))
        {
            removed.RemoveFromReceipt(currentUser.UserId);
            await instruments.RemoveAsync(removed, ct).ConfigureAwait(false);
        }

        foreach (var line in lines)
        {
            if (existing.Any(e => e.Id == line.Id))
            {
                if (line.PendingEvents.Count > 0)
                {
                    await instruments.SaveCorrectionAsync(line, ct).ConfigureAwait(false);
                }
            }
            else
            {
                await instruments.AddAsync(line, ct).ConfigureAwait(false);
            }
        }
    }

    public Task<Result<Guid>> CancelAsync(CancelReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var receipt = await receipts.GetAsync(command.Id, ct).ConfigureAwait(false);
            if (receipt is null)
            {
                return Result<Guid>.Fail("Receipt.NotFound", "The receipt was not found.");
            }

            if (receipt.Version != command.ExpectedVersion)
            {
                throw new ConcurrencyConflictException("The receipt was changed by someone else.");
            }

            // A confirmed voucher stands: the receipt is no longer cancelled (a reversing voucher comes with the accounting link).
            if (await vouchers.FindAsync(VoucherSource.Receipt, receipt.Id, 0, ct).ConfigureAwait(false) is not null)
            {
                return Result<Guid>.Fail("Receipt.VoucherConfirmed", "The receipt's voucher is confirmed; it can no longer be cancelled.");
            }

            var date = FiscalDate.ParseJalali(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            receipt.Cancel(date, command.Reason, currentUser.UserId);
            await receipts.SaveAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>One receipt with its cheques and notes.</summary>
    public Task<ReceiptView?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var receipt = await receipts.GetAsync(id, ct).ConfigureAwait(false);
            if (receipt is null)
            {
                return null;
            }

            IReadOnlyList<Instrument> lines = receipt.Kind is ReceiptKind.Instruments or ReceiptKind.Notes ? await instruments.ListByReceiptAsync(id, ct).ConfigureAwait(false) : [];

            // In the order of the form, so a correction opens the lines as they were entered.
            var order = receipt.InstrumentIds.Select((x, i) => (x, i)).ToDictionary(p => p.x, p => p.i);
            lines = [.. lines.OrderBy(l => order.GetValueOrDefault(l.Id, int.MaxValue))];
            var confirmed = await vouchers.FindAsync(VoucherSource.Receipt, receipt.Id, 0, ct).ConfigureAwait(false) is not null;
            return ReceiptView.From(receipt, lines, IranClock.Today(clock), notCorrectable: receipt.WhyNotCorrectable(confirmed),
                notDeletable: receipt.WhyNotDeletable(confirmed, lines));
        }, cancellationToken);

    /// <summary>
    /// Receipts of a fiscal year, newest first, each with the latest state of its cheques (owner, 18 Nov 2026: the last column of the
    /// receipts list is the latest status); open a receipt to see the cheques themselves.
    /// </summary>
    public Task<IReadOnlyList<ReceiptView>> ListAsync(int fiscalYear, ReceiptKind? kind, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<ReceiptView>>(async ct =>
        {
            var list = await receipts.ListAsync(fiscalYear, kind, ct).ConfigureAwait(false);
            var today = IranClock.Today(clock);
            var withCheques = list.Where(r => r.Kind is ReceiptKind.Instruments or ReceiptKind.Notes).Select(r => r.Id).ToList();
            var statuses = (await instruments.ListStatusesByReceiptsAsync(withCheques, ct).ConfigureAwait(false)).ToLookup(
                s => s.ReceiptId,
                s => new ChequeStatusView(s.State.ToString(), ReceivedChequeStatus.HasReachedDueDate(s.State, s.ReceivedOn, s.DueDate, today), s.InstrumentId, s.Number, s.Kind.ToString()));
            return list.Select(r => ReceiptView.From(r, statuses: r.Kind is ReceiptKind.Instruments or ReceiptKind.Notes ? statuses[r.Id].ToList() : null)).ToList();
        }, cancellationToken);

    /// <summary>Old D_Naghd / D_variz: date, payer and purpose are required; the date must be in an open fiscal period (rule R2).</summary>
    private async Task<(ReceiptHeader Header, FiscalDate Date)> HeaderAsync(ReceiptHeaderInput input, CancellationToken ct, bool noteReceipt = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Date))
        {
            throw new DomainException("Receipt.DateRequired", "Enter the date (old: \"تاريخ نامشخص است\").");
        }

        var date = FiscalDate.ParseJalali(input.Date);
        await EnsureOpenAsync(date, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(input.PayerDetailCode))
        {
            throw new DomainException("Receipt.PayerRequired", "Choose the payer (پرداخت کننده).");
        }

        var payer = await reference.PayerAsync(input.PayerDetailCode.Trim(), ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.PayerNotFound", "The payer's تفصیل was not found.");
        if (noteReceipt)
        {
            // Owner's answer to Q54 (19 Nov 2026): the obligor of a note is one of the «متعهدین» of Basic Information. That list, not the
            // type of the تفصیل (answer to Q22), now says who may hand a note over: staff and suppliers defined as obligors too.
            NoteParties.Obligor(await reference.NotePartyAsync(payer.DetailCode, ct).ConfigureAwait(false));
            payer = payer with { MayPay = true };
        }

        var group = await reference.PurposeGroupAsync(input.PurposeGroupId, ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.PurposeGroupRequired", "Choose the purpose group (گروه بابت).");
        return (new ReceiptHeader(date, payer, group, input.Purpose ?? string.Empty, input.Reference1, input.Reference2, input.DepartmentId, input.SalesProceeds, input.Note), date);
    }

    private static Money Amount(decimal amount, string currency, decimal? rate) =>
        new MoneyInput(amount, currency, rate).ToMoney();

    private async Task EnsureOpenAsync(FiscalDate date, CancellationToken ct)
    {
        if (!await fiscalPeriods.IsOpenAsync(date, ct).ConfigureAwait(false))
        {
            throw new DomainException("FiscalPeriod.Closed", $"The fiscal period of {date} is closed for new operations.");
        }
    }

    private Task<Result<Guid>> Run(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
