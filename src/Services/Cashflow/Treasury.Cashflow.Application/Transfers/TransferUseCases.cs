using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.Vouchers;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Transfers;
using Treasury.Cashflow.Domain.Vouchers;

namespace Treasury.Cashflow.Application.Transfers;

/// <summary>The form of an electronic transfer; dates are Jalali text at this edge.</summary>
/// <param name="PaymentRequestId">The approved payment request of the kind «نقل و انتقال بین بانک‌های شرکت» the transfer pays (answer to Q82).</param>
/// <param name="Fee">The bank's fee of the order in Rial («کارمزد خدمات بانکی»), when there is one (answer to Q77).</param>
/// <param name="FeeDetailCode">The تفصیل of the fee, asked only when the fee account takes one (answer to Q83).</param>
public sealed record ElectronicTransferInput(
    string Date, Guid SourceAccountId, Guid DestinationAccountId, TransferMethod Method, TransferChannel? Channel, decimal Amount, decimal? RateToRial = null,
    string? TrackingNumber = null, string? Description = null, Guid? PaymentRequestId = null, decimal? Fee = null, string? FeeDetailCode = null);

/// <summary>Whether the fee line of a transfer asks for a تفصیل, for the form (answer to Q83).</summary>
/// <param name="AccountCode">The company's account «هزینه کارمزد خدمات بانکی»; null: not set in the voucher template yet.</param>
/// <param name="DetailTypes">The تفصیل types the account takes; empty: the fee has no تفصیل.</param>
public sealed record FeeAccountView(string? AccountCode, string? AccountName, IReadOnlyList<string> DetailTypes);

/// <summary>What a source account already sent on a day by one way (answer to Q84: the orders of the day are counted and shown).</summary>
public sealed record TransferDayUsageView(decimal TotalRialSameChannel, int Count);

public sealed record RecordElectronicTransferCommand(string IdempotencyKey, ElectronicTransferInput Form);

public sealed record CorrectElectronicTransferCommand(string IdempotencyKey, Guid TransferId, long ExpectedVersion, ElectronicTransferInput Form);

public sealed record DeleteTransferCommand(string IdempotencyKey, Guid TransferId, long ExpectedVersion);

/// <summary>The payment form «پرداخت چک – نقل و انتقال (انتقال دهنده)».</summary>
/// <param name="Sheet">The sheet of the cheque book; null takes the lowest unused one.</param>
/// <param name="SayadPrefix">The first four digits of the sheet's Sayad ID, which differ on every sheet and are typed when it is issued.</param>
/// <param name="PaymentRequestId">The approved payment request the transfer pays (answer to Q82).</param>
/// <param name="SayadRegistered">The cheque is registered in Sayad as it is issued («پرداخت ثبت صیاد شد»); false: it is registered later
/// on the payment side, and until then it can be cancelled (answer to Q79).</param>
public sealed record TransferChequeInput(
    string Date, Guid SourceAccountId, Guid DestinationAccountId, Guid ChequeBookId, long? Sheet, string SayadPrefix, decimal Amount, string DueDate,
    string? Description = null, decimal? RateToRial = null, Guid? PaymentRequestId = null, bool SayadRegistered = true);

public sealed record IssueTransferChequeCommand(string IdempotencyKey, TransferChequeInput Form);

/// <summary>The button of the payment form: the same cheque recorded on the receipts side.</summary>
/// <param name="Date">The day it is received; empty: the day it was issued («بصورت همزمان»).</param>
public sealed record ReceiveTransferChequeCommand(string IdempotencyKey, Guid TransferId, long ExpectedVersion, string? Date = null);

public sealed record UndoTransferReceiptCommand(string IdempotencyKey, Guid TransferId, long ExpectedVersion);

/// <summary>A dated step of the transfer's cheque on the payment side: «ثبت صیادی پرداخت» or «سررسید». Empty date: today.</summary>
public sealed record TransferChequeStepCommand(string IdempotencyKey, Guid TransferId, long ExpectedVersion, string? Date = null);

/// <summary>«ابطال چک نقل و انتقال» with its reason (answer to Q79). Empty date: today.</summary>
public sealed record CancelTransferChequeCommand(string IdempotencyKey, Guid TransferId, long ExpectedVersion, string? Date, string? Reason);

/// <summary>One row of «سقف‌های انتقال الکترونیکی»; an empty value is "no limit".</summary>
public sealed record TransferLimitView(
    string Method, Guid? BankId, decimal? MinPerOrder, decimal? MaxPerOrderInPerson, decimal? MaxPerOrderRemote, decimal? MaxPerDayInPerson, decimal? MaxPerDayRemote,
    int? MaxCountPerDay)
{
    public static TransferLimitView From(TransferLimit l) =>
        new(l.Method.ToString(), l.BankId, l.MinPerOrder, l.MaxPerOrderInPerson, l.MaxPerOrderRemote, l.MaxPerDayInPerson, l.MaxPerDayRemote, l.MaxCountPerDay);
}

public sealed record SetTransferLimitCommand(
    string IdempotencyKey, TransferMethod Method, Guid? BankId, decimal? MinPerOrder, decimal? MaxPerOrderInPerson, decimal? MaxPerOrderRemote,
    decimal? MaxPerDayInPerson, decimal? MaxPerDayRemote, int? MaxCountPerDay);

/// <param name="ReceivedState">The latest state of the cheque on the receipts side («نزد صندوق», «ثبت صیادی شد», «واگذار به بانک»…).</param>
/// <param name="PayerSayadOn">The day the payer registered the cheque in Sayad («پرداخت ثبت صیاد شد»); null: not yet.</param>
/// <param name="CanMature">A post-dated cheque whose due date has come and whose maturity («سررسید») is not recorded yet.</param>
/// <param name="WhyNotCancellable">The code of the rule that closes «ابطال چک», or null.</param>
public sealed record TransferChequeView(
    Guid IssuedChequeId, Guid ChequeBookId, string Number, string SayadId, string DueDate, Guid? ReceivedChequeId, string? ReceivedOn, string? CollectedOn,
    string? ReceivedState, bool ReachedDueDate, string? PayerSayadOn = null, bool IsPostDated = false, string? MaturedOn = null, bool CanMature = false,
    string? CancelledOn = null, string? CancelReason = null, string? WhyNotCancellable = null);

/// <param name="State">Recorded, ChequeIssued, ChequeReceived, Collected, Deleted or Cancelled.</param>
/// <param name="ConfirmedSteps">The voucher steps the treasurer confirmed (0 payer or electronic, 1 receiver, 2 maturity, 3 and 4 cancellation).</param>
/// <param name="WhyNotCorrectable">The code of the rule that closes «ویرایش», or null.</param>
/// <param name="WhyNotDeletable">The code of the rule that closes «حذف», or null.</param>
/// <param name="CanUndoReceive">The receipts-side record can still be taken back.</param>
public sealed record TransferView(
    Guid Id, int FiscalYear, int Number, string Date, string Method, string? Channel,
    Guid SourceAccountId, string SourceAccountNumber, Guid SourceBankId, string SourceBankName,
    Guid DestinationAccountId, string DestinationAccountNumber, Guid DestinationBankId, string DestinationBankName,
    decimal Amount, string Currency, decimal? RateToRial, decimal AmountRial, string? TrackingNumber, string? Description, string State, string RecordedBy,
    string? DeletedBy, DateTimeOffset? DeletedAt, TransferChequeView? Cheque, bool PayerVoucherConfirmed, bool ReceiverVoucherConfirmed,
    string? WhyNotCorrectable, string? WhyNotDeletable, bool CanUndoReceive, long Version, decimal? Fee = null, Guid? PaymentRequestId = null,
    int? PaymentRequestNumber = null, IReadOnlyList<int>? ConfirmedSteps = null, string? FeeDetailCode = null);

/// <summary>
/// Transfers between the company's own bank accounts («نقل و انتقال بین بانکی», owner's report no. 7 of 20 Nov 2026): the electronic
/// ways within their configurable limits, and the Sayad cheque with its payment side («انتقال دهنده») and receipts side («انتقال
/// گیرنده»). Each public write is one command in one transaction.
/// </summary>
public sealed class TransferUseCases(
    IUnitOfWork unitOfWork,
    IBankTransferRepository transfers,
    ITransferLimitRepository limits,
    IInstrumentRepository instruments,
    IChequeBookRepository chequeBooks,
    IBankAccountLookup bankAccounts,
    IReceiptReferenceLookup reference,
    IFiscalPeriodPolicy fiscalPeriods,
    IVoucherTemplateRepository templates,
    IConfirmedVoucherRepository vouchers,
    IIdempotencyStore idempotency,
    ICurrentUser currentUser,
    IPaymentRequestLookup paymentRequests,
    TimeProvider? clock = null)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    // ----- limits -----------------------------------------------------------------------------------------------------------------

    public Task<IReadOnlyList<TransferLimitView>> GetLimitsAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<TransferLimitView>>(async ct =>
            (await limits.GetAsync(ct).ConfigureAwait(false)).Rows
                .OrderBy(r => r.Method).ThenBy(r => r.BankId is null ? 0 : 1).ThenBy(r => r.BankId)
                .Select(TransferLimitView.From).ToList(), cancellationToken);

    /// <summary>Sets the limits of one way of transfer, for every bank or for one bank (which must be in the bank-names list).</summary>
    public Task<Result<string>> SetLimitAsync(SetTransferLimitCommand command, CancellationToken cancellationToken) =>
        _runner.RunAsync(command.IdempotencyKey, async ct =>
        {
            if (command.BankId is { } bankId && await reference.BankNameAsync(bankId, ct).ConfigureAwait(false) is null)
            {
                throw new DomainException("TransferLimit.BankNotFound", "The bank is not in the bank-names list.");
            }

            var table = await limits.GetAsync(ct).ConfigureAwait(false);
            table.Set(new TransferLimit(command.Method, command.BankId, command.MinPerOrder, command.MaxPerOrderInPerson, command.MaxPerOrderRemote,
                command.MaxPerDayInPerson, command.MaxPerDayRemote, command.MaxCountPerDay));
            await limits.SaveAsync(table, ct).ConfigureAwait(false);
            return Result<string>.Ok(command.Method.ToString());
        }, s => s, s => s, cancellationToken);

    /// <summary>Removes a bank's own limits; the general limits of the way apply to it again.</summary>
    public Task<Result<string>> RemoveBankLimitAsync(string idempotencyKey, TransferMethod method, Guid bankId, CancellationToken cancellationToken) =>
        _runner.RunAsync(idempotencyKey, async ct =>
        {
            var table = await limits.GetAsync(ct).ConfigureAwait(false);
            table.RemoveBankRow(method, bankId);
            await limits.SaveAsync(table, ct).ConfigureAwait(false);
            return Result<string>.Ok(method.ToString());
        }, s => s, s => s, cancellationToken);

    /// <summary>The account of bank fees as the voucher template names it, and whether it takes a تفصیل (answer to Q83).</summary>
    public Task<FeeAccountView> FeeAccountAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var code = (await templates.GetAsync(ct).ConfigureAwait(false)).CodeOf(FixedAccount.BankFees);
            var account = code is null ? null : await reference.LedgerAccountAsync(code, ct).ConfigureAwait(false);
            return new FeeAccountView(code, account?.Name, account?.Detail1Types ?? []);
        }, cancellationToken);

    /// <summary>
    /// What the source account already sent on a day by one way: the Rial total through the same channel and the number of orders
    /// (answer to Q84: «لازم است در طی روز تعدد پرداخت … در سیستم کنترل شود»). The limits are checked against the same figures.
    /// </summary>
    public Task<TransferDayUsageView> DayUsageAsync(Guid sourceAccountId, string date, TransferMethod method, TransferChannel channel, Guid? exceptId,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var usage = await transfers.DayUsageAsync(sourceAccountId, FiscalDate.ParseJalali(date), method, channel, exceptId, ct).ConfigureAwait(false);
            return new TransferDayUsageView(usage.TotalRialSameChannel, usage.Count);
        }, cancellationToken);

    // ----- electronic transfers ---------------------------------------------------------------------------------------------------

    public Task<Result<Guid>> RecordElectronicAsync(RecordElectronicTransferCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (form, table, usage) = await ElectronicAsync(command.Form, null, ct).ConfigureAwait(false);
            var number = await transfers.NextNumberAsync(form.Date.JalaliYear, ct).ConfigureAwait(false);
            var transfer = BankTransfer.RecordElectronic(Guid.NewGuid(), number, form, table, usage, currentUser.UserId);
            await MarkAccountsAsync(transfer, ct).ConfigureAwait(false);
            await PayRequestAsync(transfer, ct).ConfigureAwait(false);
            await transfers.AddAsync(transfer, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(transfer.Id);
        }, cancellationToken);

    /// <summary>Correction of an electronic transfer while its voucher is not confirmed; the limits are checked again without it.</summary>
    public Task<Result<Guid>> CorrectElectronicAsync(CorrectElectronicTransferCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var transfer = await LoadAsync(command.TransferId, command.ExpectedVersion, ct).ConfigureAwait(false);
            await EnsureOpenAsync(transfer.Date, ct).ConfigureAwait(false);
            var (form, table, usage) = await ElectronicAsync(command.Form, transfer, ct).ConfigureAwait(false);
            var confirmed = await ConfirmedAsync(transfer.Id, TransferVouchers.PayerStep, ct).ConfigureAwait(false);
            var paid = transfer.Request;
            transfer.CorrectElectronic(form, table, usage, currentUser.UserId, confirmed);
            await MarkAccountsAsync(transfer, ct).ConfigureAwait(false);

            // The payment the transfer made on its request is taken back and recorded again as the transfer now is (another amount,
            // perhaps another request).
            await TakeBackPaymentAsync(paid, ct).ConfigureAwait(false);
            await PayRequestAsync(transfer, ct).ConfigureAwait(false);
            await transfers.SaveAsync(transfer, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(transfer.Id);
        }, cancellationToken);

    /// <summary>The voucher of the electronic transfer on the form, before it is saved; an incomplete form answers with the first rule it breaks.</summary>
    public async Task<Result<VoucherView>> PreviewElectronicAsync(ElectronicTransferInput input, Guid? correctingId, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteAsync(async ct =>
            {
                var correcting = correctingId is { } id ? await transfers.GetAsync(id, ct).ConfigureAwait(false) : null;
                var (form, table, usage) = await ElectronicAsync(input, correcting, ct).ConfigureAwait(false);
                var transfer = BankTransfer.RecordElectronic(Guid.NewGuid(), 1, form, table, usage, currentUser.UserId);
                var voucher = TransferVouchers.ForElectronic(transfer, Side(form.Source), Side(form.Destination), await templates.GetAsync(ct).ConfigureAwait(false));
                return Result<VoucherView>.Ok(VoucherView.From(voucher with { Operation = TransferVouchers.OperationName(form.Method) }, "Transfer", TransferVouchers.PayerStep));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return Result<VoucherView>.Fail(ex.Code, ex.Message);
        }
    }

    // ----- cheque transfers -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The payment side («پرداخت چک – نقل و انتقال (انتقال دهنده)»): the cheque is issued on a sheet of a cheque book of the source
    /// account, with its Sayad ID («پرداخت ثبت صیاد شد»). The sheet, the issued cheque and the transfer are saved in one transaction
    /// with the cheque book locked, as every issued cheque is (rule R5; ADR 0002).
    /// </summary>
    public Task<Result<Guid>> IssueChequeAsync(IssueTransferChequeCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (transfer, issued, book) = await BuildChequeAsync(command.Form, preview: false, ct).ConfigureAwait(false);
            await MarkAccountsAsync(transfer, ct).ConfigureAwait(false);
            await PayRequestAsync(transfer, ct).ConfigureAwait(false);
            await chequeBooks.SaveAsync(book, ct).ConfigureAwait(false);
            await instruments.AddAsync(issued, ct).ConfigureAwait(false);
            await transfers.AddAsync(transfer, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(transfer.Id);
        }, cancellationToken);

    /// <summary>The payer's voucher of the cheque on the form, before it is saved; no sheet and no number is taken.</summary>
    public async Task<Result<VoucherView>> PreviewChequeAsync(TransferChequeInput input, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteAsync(async ct =>
            {
                var (transfer, _, _) = await BuildChequeAsync(input, preview: true, ct).ConfigureAwait(false);
                var source = await AccountAsync(transfer.Source.Id, ct).ConfigureAwait(false);
                var template = await templates.GetAsync(ct).ConfigureAwait(false);
                var voucher = TransferVouchers.ForChequeIssued(transfer, Side(source), template);
                return Result<VoucherView>.Ok(VoucherView.From(voucher with { Operation = "پرداخت چک – نقل و انتقال (انتقال دهنده)" }, "Transfer", TransferVouchers.PayerStep));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return Result<VoucherView>.Fail(ex.Code, ex.Message);
        }
    }

    /// <summary>
    /// The receipts side («دریافت چک – نقل و انتقال (انتقال گیرنده)»): the button of the payment form records the same cheque as a
    /// received cheque «نزد صندوق», with the source account as its drawer; from there it follows the steps of every received cheque.
    /// The new received cheque and the transfer are saved together (ADR 0002 amendment of 20 Nov 2026).
    /// </summary>
    public Task<Result<Guid>> ReceiveChequeAsync(ReceiveTransferChequeCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var transfer = await LoadAsync(command.TransferId, command.ExpectedVersion, ct).ConfigureAwait(false);
            if (transfer.Cheque is null)
            {
                throw new DomainException("Transfer.NotACheque", "Only a transfer by cheque has a cheque to receive.");
            }

            if (transfer.State != TransferState.ChequeIssued)
            {
                throw transfer.State == TransferState.Deleted
                    ? new DomainException("Transfer.Deleted", "The transfer was deleted.")
                    : new DomainException("Transfer.ChequeAlreadyReceived", "The cheque of this transfer is already recorded on the receipts side.");
            }

            var date = string.IsNullOrWhiteSpace(command.Date) ? transfer.Date : FiscalDate.ParseJalali(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            var issued = await instruments.GetAsync(transfer.Cheque.IssuedChequeId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Instrument.NotFound", "The issued cheque of the transfer was not found.");
            var source = await AccountAsync(transfer.Source.Id, ct).ConfigureAwait(false);
            var received = Instrument.ReceiveTransferCheque(
                Guid.NewGuid(), issued, transfer.Id, transfer.Destination.Id, Party(source), transfer.Source.BankName,
                new ReceivedInstrumentDetails(source.BankId, source.BranchName, source.BranchCode, source.AccountNumber, SayadId: transfer.Cheque.SayadId),
                date, currentUser.UserId);
            transfer.ReceiveCheque(received, currentUser.UserId);
            await instruments.AddAsync(received, ct).ConfigureAwait(false);
            await transfers.SaveAsync(transfer, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(transfer.Id);
        }, cancellationToken);

    /// <summary>Takes back a receipts-side record made by mistake: the received cheque is removed, the transfer is «پرداخت ثبت صیاد شد» again.</summary>
    public Task<Result<Guid>> UndoReceiveAsync(UndoTransferReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var transfer = await LoadAsync(command.TransferId, command.ExpectedVersion, ct).ConfigureAwait(false);
            var received = transfer.Cheque?.ReceivedChequeId is { } id ? await instruments.GetAsync(id, ct).ConfigureAwait(false) : null;
            if (received is null)
            {
                throw new DomainException("Transfer.ChequeNotReceived", "The cheque of this transfer is not on the receipts side.");
            }

            await EnsureOpenAsync(received.IssueDate, ct).ConfigureAwait(false);
            var confirmed = await ConfirmedAsync(transfer.Id, TransferVouchers.ReceiverStep, ct).ConfigureAwait(false);
            transfer.UndoReceive(received, confirmed, currentUser.UserId);
            received.RemoveFromReceipt(currentUser.UserId);
            await instruments.RemoveAsync(received, ct).ConfigureAwait(false);
            await transfers.SaveAsync(transfer, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(transfer.Id);
        }, cancellationToken);

    /// <summary>
    /// Deletion («حذف») of a transfer recorded by mistake, before its voucher is confirmed; it stays in the list as «حذف شده». A cheque
    /// transfer is deleted only while its cheque is not on the receipts side: the issued cheque is removed and its sheet freed, in the
    /// same transaction, so the sheet can be typed again.
    /// </summary>
    public Task<Result<Guid>> DeleteAsync(DeleteTransferCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var transfer = await LoadAsync(command.TransferId, command.ExpectedVersion, ct).ConfigureAwait(false);
            await EnsureOpenAsync(transfer.Date, ct).ConfigureAwait(false);
            var confirmed = await ConfirmedAsync(transfer.Id, TransferVouchers.PayerStep, ct).ConfigureAwait(false);
            transfer.Delete(currentUser.UserId, confirmed, (clock ?? TimeProvider.System).GetUtcNow());
            if (transfer.Cheque is { } cheque)
            {
                var issued = await instruments.GetAsync(cheque.IssuedChequeId, ct).ConfigureAwait(false)
                    ?? throw new DomainException("Instrument.NotFound", "The issued cheque of the transfer was not found.");
                issued.RemoveIssuedForTransfer(transfer.Id, currentUser.UserId);
                var book = await chequeBooks.GetForUpdateAsync(cheque.ChequeBookId, ct).ConfigureAwait(false)
                    ?? throw new DomainException("ChequeBook.NotFound", "Cheque book was not found.");
                book.Release(cheque.Sheet);
                await chequeBooks.SaveAsync(book, ct).ConfigureAwait(false);
                await instruments.RemoveAsync(issued, ct).ConfigureAwait(false);
            }

            await TakeBackPaymentAsync(transfer.Request, ct).ConfigureAwait(false);
            await transfers.SaveAsync(transfer, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(transfer.Id);
        }, cancellationToken);

    /// <summary>
    /// «ثبت صیادی پرداخت»: the payer's registration of the cheque in Sayad, when it was not done as the cheque was issued. From then on
    /// the cheque is not cancelled and the receipts side may record its own «ثبت صیادی» (answer to Q79).
    /// </summary>
    public Task<Result<Guid>> RegisterPayerSayadAsync(TransferChequeStepCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var transfer = await LoadAsync(command.TransferId, command.ExpectedVersion, ct).ConfigureAwait(false);
            var date = DateOrToday(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            transfer.RegisterPayerSayad(date, currentUser.UserId);
            await transfers.SaveAsync(transfer, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(transfer.Id);
        }, cancellationToken);

    /// <summary>
    /// «سررسید» of a post-dated transfer cheque (answer to Q81): on or after its due date the cheque leaves «اسناد پرداختنی» for the
    /// bank; the issued cheque is marked matured and the transfer gets its maturity voucher.
    /// </summary>
    public Task<Result<Guid>> MatureChequeAsync(TransferChequeStepCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var transfer = await LoadAsync(command.TransferId, command.ExpectedVersion, ct).ConfigureAwait(false);
            var date = DateOrToday(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            transfer.RecordMatured(date, currentUser.UserId);
            var issued = await instruments.GetAsync(transfer.Cheque!.IssuedChequeId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Instrument.NotFound", "The issued cheque of the transfer was not found.");
            issued.RecordMatured(date, currentUser.UserId);
            await instruments.SaveAsync(issued, ct).ConfigureAwait(false);
            await transfers.SaveAsync(transfer, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(transfer.Id);
        }, cancellationToken);

    /// <summary>
    /// «ابطال چک نقل و انتقال» (answer to Q79), only while the cheque is not registered in Sayad: the issued cheque is cancelled and its
    /// sheet voided (it is never used again), the received cheque — if the cheque is already on the receipts side — is cancelled too,
    /// and the payment is taken off the payment request, all in one transaction. A new transfer by cheque is a new cheque.
    /// </summary>
    public Task<Result<Guid>> CancelChequeAsync(CancelTransferChequeCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var transfer = await LoadAsync(command.TransferId, command.ExpectedVersion, ct).ConfigureAwait(false);
            var date = DateOrToday(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            var user = currentUser.UserId;
            transfer.CancelCheque(date, command.Reason, user);
            var cheque = transfer.Cheque!;
            var issued = await instruments.GetAsync(cheque.IssuedChequeId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Instrument.NotFound", "The issued cheque of the transfer was not found.");
            issued.Cancel(date, user, cheque.CancelReason!);
            var book = await chequeBooks.GetForUpdateAsync(cheque.ChequeBookId, ct).ConfigureAwait(false)
                ?? throw new DomainException("ChequeBook.NotFound", "Cheque book was not found.");
            book.Void(cheque.Sheet, cheque.CancelReason!);
            if (cheque.ReceivedChequeId is { } receivedId)
            {
                var received = await instruments.GetAsync(receivedId, ct).ConfigureAwait(false)
                    ?? throw new DomainException("Instrument.NotFound", "The received cheque of the transfer was not found.");
                received.CancelTransferCheque(transfer.Id, date, user, cheque.CancelReason!);
                await instruments.SaveAsync(received, ct).ConfigureAwait(false);
            }

            await TakeBackPaymentAsync(transfer.Request, ct).ConfigureAwait(false);
            await chequeBooks.SaveAsync(book, ct).ConfigureAwait(false);
            await instruments.SaveAsync(issued, ct).ConfigureAwait(false);
            await transfers.SaveAsync(transfer, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(transfer.Id);
        }, cancellationToken);

    // ----- reading ----------------------------------------------------------------------------------------------------------------

    /// <summary>Transfers of a year, newest first; <paramref name="cheque"/> true: by cheque only, false: electronic only, null: all.</summary>
    public Task<IReadOnlyList<TransferView>> ListAsync(int fiscalYear, bool? cheque, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<TransferView>>(async ct =>
        {
            var list = new List<TransferView>();
            foreach (var t in (await transfers.ListAsync(fiscalYear, ct).ConfigureAwait(false)).Where(t => cheque is null || t.IsCheque == cheque))
            {
                list.Add(await ViewAsync(t, ct).ConfigureAwait(false));
            }

            return list;
        }, cancellationToken);

    public Task<TransferView?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
            await transfers.GetAsync(id, ct).ConfigureAwait(false) is { } t ? await ViewAsync(t, ct).ConfigureAwait(false) : null, cancellationToken);

    // ----- vouchers ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The transfer's vouchers, as confirmed or as the system proposes them now: one for an electronic transfer; for a cheque the
    /// payer's («انتقال دهنده») and, once the cheque is on the receipts side, the receiver's («انتقال گیرنده»). A deleted transfer has none.
    /// </summary>
    public Task<IReadOnlyList<VoucherView>> VouchersAsync(Guid transferId, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<VoucherView>>(async ct =>
        {
            var transfer = await transfers.GetAsync(transferId, ct).ConfigureAwait(false);
            if (transfer is null || transfer.State == TransferState.Deleted)
            {
                return [];
            }

            var confirmed = (await vouchers.ListBySourceAsync(transferId, ct).ConfigureAwait(false))
                .Where(v => v.Source == VoucherSource.Transfer).ToDictionary(v => v.Step);
            var list = new List<VoucherView>();
            foreach (var step in TransferVouchers.Steps)
            {
                if (confirmed.TryGetValue(step, out var c))
                {
                    list.Add(VoucherView.From(c.Voucher, "Transfer", step, c));
                }
                else if (await ComputeAsync(transfer, step, ct).ConfigureAwait(false) is { } v)
                {
                    list.Add(VoucherView.From(v, "Transfer", step));
                }
            }

            return list;
        }, cancellationToken);

    /// <summary>The treasurer's «تایید سند» of one voucher of the transfer; from then on the transfer is no longer corrected or deleted.</summary>
    public Task<Result<Guid>> ConfirmVoucherAsync(string idempotencyKey, Guid transferId, int step, CancellationToken cancellationToken) =>
        Run(idempotencyKey, async ct =>
        {
            var transfer = await transfers.GetAsync(transferId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Transfer.NotFound", "The transfer was not found.");
            if (transfer.State == TransferState.Deleted)
            {
                throw new DomainException("Transfer.Deleted", "The transfer was deleted.");
            }

            if (await ConfirmedAsync(transferId, step, ct).ConfigureAwait(false))
            {
                throw new DomainException("Voucher.AlreadyConfirmed", "This voucher is already confirmed.");
            }

            var computed = await ComputeAsync(transfer, step, ct).ConfigureAwait(false)
                ?? throw new DomainException("Voucher.NoVoucherForStep", "This transfer has no such voucher.");
            var voucher = ConfirmedVoucher.Confirm(Guid.NewGuid(), VoucherSource.Transfer, transferId, step, computed, currentUser.UserId, (clock ?? TimeProvider.System).GetUtcNow());
            await vouchers.AddAsync(voucher, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(voucher.Id);
        }, cancellationToken);

    private async Task<Voucher?> ComputeAsync(BankTransfer transfer, int step, CancellationToken ct)
    {
        var template = await templates.GetAsync(ct).ConfigureAwait(false);
        if (step is TransferVouchers.ReceiverStep or TransferVouchers.ReceiverCancelStep)
        {
            // The تفصیل of «اسناد نزد صندوق» is the one kept on the received cheque: the source bank account's (Q78).
            var received = transfer.Cheque?.ReceivedChequeId is { } receivedId ? await instruments.GetAsync(receivedId, ct).ConfigureAwait(false) : null;
            return received is null ? null
                : step == TransferVouchers.ReceiverStep ? TransferVouchers.ForChequeReceived(transfer, received.Counterparty.DetailCode, template)
                : TransferVouchers.ForChequeCancelledByReceiver(transfer, received.Counterparty.DetailCode, template);
        }

        var source = Side(await bankAccounts.GetAsync(transfer.Source.Id, ct).ConfigureAwait(false));
        return step switch
        {
            TransferVouchers.PayerStep when transfer.IsCheque => TransferVouchers.ForChequeIssued(transfer, source, template),
            TransferVouchers.PayerStep =>
                TransferVouchers.ForElectronic(transfer, source, Side(await bankAccounts.GetAsync(transfer.Destination.Id, ct).ConfigureAwait(false)), template),
            TransferVouchers.MaturityStep => TransferVouchers.ForChequeMatured(transfer, source, template),
            TransferVouchers.PayerCancelStep => TransferVouchers.ForChequeCancelledByPayer(transfer, source, template),
            _ => null,
        };
    }

    // ----- helpers ----------------------------------------------------------------------------------------------------------------

    private async Task<TransferView> ViewAsync(BankTransfer t, CancellationToken ct)
    {
        var confirmed = t.State == TransferState.Deleted ? [] : (await vouchers.ListBySourceAsync(t.Id, ct).ConfigureAwait(false))
            .Where(v => v.Source == VoucherSource.Transfer).Select(v => v.Step).ToHashSet();
        var payerConfirmed = confirmed.Contains(TransferVouchers.PayerStep);
        var receiverConfirmed = confirmed.Contains(TransferVouchers.ReceiverStep);
        TransferChequeView? cheque = null;
        var canUndo = false;
        if (t.Cheque is { } c)
        {
            var received = c.ReceivedChequeId is { } id ? await instruments.GetAsync(id, ct).ConfigureAwait(false) : null;
            var today = IranClock.Today(clock);
            var open = t.State is TransferState.ChequeIssued or TransferState.ChequeReceived;
            cheque = new TransferChequeView(c.IssuedChequeId, c.ChequeBookId, c.Number, c.SayadId, c.DueDate.ToJalaliString(), c.ReceivedChequeId,
                c.ReceivedOn?.ToJalaliString(), c.CollectedOn?.ToJalaliString(), received?.State.ToString(), received?.HasReachedDueDate(today) ?? false,
                c.PayerSayadOn?.ToJalaliString(), t.IsPostDated, c.MaturedOn?.ToJalaliString(), open && t.IsPostDated && c.MaturedOn is null && today >= c.DueDate,
                c.CancelledOn?.ToJalaliString(), c.CancelReason, t.WhyNotCancellable());
            canUndo = t.State == TransferState.ChequeReceived && received is { IsAsReceived: true } && !receiverConfirmed;
        }

        return new TransferView(
            t.Id, t.FiscalYear, t.Number, t.Date.ToJalaliString(), t.Method.ToString(), t.Channel?.ToString(),
            t.Source.Id, t.Source.AccountNumber, t.Source.BankId, t.Source.BankName,
            t.Destination.Id, t.Destination.AccountNumber, t.Destination.BankId, t.Destination.BankName,
            t.Amount.Amount, t.Amount.Currency, t.Amount.RateToRial, t.Amount.ToRial(), t.TrackingNumber, t.Description, t.State.ToString(), t.RecordedBy,
            t.DeletedBy, t.DeletedAt, cheque, payerConfirmed, receiverConfirmed, t.WhyNotCorrectable(payerConfirmed), t.WhyNotDeletable(payerConfirmed), canUndo, t.Version,
            t.FeeRial, t.Request?.Id, t.Request?.Number, [.. confirmed.Order()], t.FeeDetailCode);
    }

    /// <param name="correcting">The transfer being corrected; null for a new one.</param>
    private async Task<(ElectronicTransferForm Form, TransferLimits Limits, TransferDayUsage Usage)> ElectronicAsync(
        ElectronicTransferInput input, BankTransfer? correcting, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var date = ParseDate(input.Date);
        await EnsureOpenAsync(date, ct).ConfigureAwait(false);
        var source = await AccountAsync(input.SourceAccountId, ct).ConfigureAwait(false);
        var destination = await AccountAsync(input.DestinationAccountId, ct).ConfigureAwait(false);
        var amount = ToMoney(input.Amount, source.CurrencyCode, input.RateToRial);
        var request = await RequestAsync(input.PaymentRequestId, correcting, ct).ConfigureAwait(false);
        // The fee line asks for a تفصیل only when the company's fee account takes one (answer to Q83).
        FeeAccountFact? feeAccount = null;
        FeeDetailFact? feeDetail = null;
        if (input.Fee is > 0 && (await templates.GetAsync(ct).ConfigureAwait(false)).CodeOf(FixedAccount.BankFees) is { } feeCode)
        {
            feeAccount = new FeeAccountFact((await reference.LedgerAccountAsync(feeCode, ct).ConfigureAwait(false))?.Detail1Types ?? []);
            if (feeAccount.TakesDetail && !string.IsNullOrWhiteSpace(input.FeeDetailCode))
            {
                feeDetail = await reference.DetailAsync(input.FeeDetailCode.Trim(), ct).ConfigureAwait(false)
                    ?? throw new DomainException("Transfer.FeeDetailNotFound", "The تفصیل of the fee was not found.");
            }
        }

        var form = new ElectronicTransferForm(date, source, destination, input.Method, input.Channel, amount, input.TrackingNumber, input.Description, request, input.Fee,
            feeAccount, feeDetail);
        var usage = input.Channel is { } channel && Enum.IsDefined(input.Method)
            ? await transfers.DayUsageAsync(source.Id, date, input.Method, channel, correcting?.Id, ct).ConfigureAwait(false)
            : TransferDayUsage.None;
        return (form, await limits.GetAsync(ct).ConfigureAwait(false), usage);
    }

    private async Task<(BankTransfer Transfer, Instrument Issued, Domain.ChequeBooks.ChequeBook Book)> BuildChequeAsync(TransferChequeInput input, bool preview, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var date = ParseDate(input.Date);
        await EnsureOpenAsync(date, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(input.DueDate))
        {
            throw new DomainException("Instrument.DueDateRequired", "Due date is required.");
        }

        var due = FiscalDate.ParseJalali(input.DueDate);
        var source = await AccountAsync(input.SourceAccountId, ct).ConfigureAwait(false);
        var destination = await AccountAsync(input.DestinationAccountId, ct).ConfigureAwait(false);
        var amount = ToMoney(input.Amount, source.CurrencyCode, input.RateToRial);
        var book = await chequeBooks.GetForUpdateAsync(input.ChequeBookId, ct).ConfigureAwait(false)
            ?? throw new DomainException("ChequeBook.NotFound", "Cheque book was not found.");
        if (book.BankAccountId != source.Id)
        {
            throw new DomainException("ChequeBook.WrongAccount", "The cheque book does not belong to this bank account.");
        }

        long sheet;
        if (input.Sheet is { } requested)
        {
            book.Reserve(requested);
            sheet = requested;
        }
        else
        {
            sheet = book.ReserveNext();
        }

        var sayadId = book.SayadIdOf(sheet, input.SayadPrefix);
        var transferId = Guid.NewGuid();
        var user = currentUser.UserId;

        // A cheque dated the day it is written is an ordinary cheque; with a later due date it is post-dated («مدت‌دار»).
        var kind = due == date ? InstrumentKind.OrdinaryCheque : InstrumentKind.PostDatedCheque;
        var purpose = string.IsNullOrWhiteSpace(input.Description) ? $"نقل و انتقال بانکی به {TransferAccount.From(destination).Title}" : input.Description.Trim();
        var issued = Instrument.PrepareIssue(Guid.NewGuid(), kind, amount, date, due, Party(destination), purpose, source.Id, transferId, user);

        // The cheque number keeps the leading zeros of the printed serial (owner, 12 Nov 2026).
        issued.Issue(book.Id, book.SerialText(sheet), date, user);
        var number = preview ? 1 : await transfers.NextNumberAsync(date.JalaliYear, ct).ConfigureAwait(false);
        var request = await RequestAsync(input.PaymentRequestId, null, ct).ConfigureAwait(false);
        var transfer = BankTransfer.IssueCheque(transferId, number, date, source, destination, issued, book.Id, sheet, sayadId, input.Description, user, request,
            input.SayadRegistered);
        return (transfer, issued, book);
    }

    /// <summary>
    /// The payment request the transfer is to pay, as requests-svc describes it (answer to Q82); null when none was chosen (the domain
    /// then asks for one). For a correction, what the transfer already paid on the same request counts as still to be paid.
    /// </summary>
    private async Task<TransferRequestFact?> RequestAsync(Guid? requestId, BankTransfer? correcting, CancellationToken ct)
    {
        if (requestId is not { } id || id == Guid.Empty)
        {
            return null;
        }

        var r = await paymentRequests.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("Transfer.RequestNotFound", "The payment request of the transfer was not found.");
        var own = correcting is { Request: { } paid } && paid.Id == r.Id ? correcting.Amount.Amount : 0;
        return new TransferRequestFact(r.Id, r.Number, r.State == "Approved" || (own > 0 && r.State == "Paid"), r.Method == "OwnAccountTransfer", r.PayFromId,
            r.TransferToAccountId, r.Amount - r.PaidAmount + own);
    }

    /// <summary>
    /// Records the transfer as a payment on its request in requests-svc, under the key the transfer keeps; a refusal comes back as the
    /// request's own error code and nothing of the transfer is saved.
    /// </summary>
    private async Task PayRequestAsync(BankTransfer transfer, CancellationToken ct)
    {
        if (transfer.Request is not { } request)
        {
            return;
        }

        var what = transfer.Cheque is { } c ? $"چک {c.Number} {transfer.Source.BankName}" : TransferVouchers.OperationName(transfer.Method);
        await paymentRequests.RecordPaymentAsync(request.Id,
            new RequestPaymentInput(request.PaymentKey, transfer.Cheque?.IssuedChequeId, $"نقل و انتقال شماره {transfer.Number} – {what}", transfer.Amount.Amount,
                transfer.Amount.Currency, transfer.Date.ToJalaliString(), "BankTransfer"), ct).ConfigureAwait(false);
    }

    /// <summary>Takes the transfer's payment off its request (a deleted or corrected transfer, a cancelled cheque): the request is to be paid again.</summary>
    private async Task TakeBackPaymentAsync(TransferRequest? request, CancellationToken ct)
    {
        if (request is not null)
        {
            await paymentRequests.ReversePaymentAsync(request.Id, request.PaymentKey, ct).ConfigureAwait(false);
        }
    }

    private FiscalDate DateOrToday(string? text) => string.IsNullOrWhiteSpace(text) ? IranClock.Today(clock) : FiscalDate.ParseJalali(text);

    /// <summary>The bank account as accounts-svc describes it, with its bank's name from reference-svc.</summary>
    private async Task<TransferAccountFact> AccountAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Transfer.AccountRequired", "Choose both bank accounts of the transfer.");
        }

        var account = await bankAccounts.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("Transfer.AccountNotFound", "The bank account is not one of the company's bank accounts.");
        var bankId = account.BankId ?? throw new DomainException("Transfer.AccountNotFound", "The bank of the bank account is not known.");
        var bankName = await reference.BankNameAsync(bankId, ct).ConfigureAwait(false) ?? string.Empty;
        return new TransferAccountFact(account.Id, account.AccountNumber, bankId, bankName, account.CurrencyCode, account.IsActive, account.HasCheques,
            account.LedgerAccountCode, account.Detail1Code, account.BranchName, account.BranchCode);
    }

    /// <summary>The company's own account as the other party of the transfer's cheque, known by the bank account's تفصیل.</summary>
    private static CounterpartyRef Party(TransferAccountFact account)
    {
        var name = $"حساب {account.AccountNumber} {account.BankName}".Trim();
        return string.IsNullOrWhiteSpace(account.DetailCode) ? new CounterpartyRef(account.Id, name) : CounterpartyRef.ForDetail(account.DetailCode, name);
    }

    private static LedgerSide Side(TransferAccountFact account) => new(account.LedgerAccountCode, account.DetailCode);

    private static LedgerSide Side(BankAccountFact? account) => new(account?.LedgerAccountCode, account?.Detail1Code);

    private static Money ToMoney(decimal amount, string currency, decimal? rate) =>
        string.Equals(currency.Trim(), Money.Rial, StringComparison.OrdinalIgnoreCase)
            ? Money.Rials(amount)
            : Money.Foreign(amount, currency, rate ?? throw new DomainException("Money.RateRequired", "Exchange rate is required for foreign currency."));

    private static FiscalDate ParseDate(string? text) =>
        string.IsNullOrWhiteSpace(text) ? throw new DomainException("Transfer.DateRequired", "Enter the date of the transfer.") : FiscalDate.ParseJalali(text);

    private async Task<BankTransfer> LoadAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var transfer = await transfers.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("Transfer.NotFound", "The transfer was not found.");
        if (transfer.Version != expectedVersion)
        {
            throw new ConcurrencyConflictException("The transfer was changed by someone else.");
        }

        return transfer;
    }

    private async Task<bool> ConfirmedAsync(Guid transferId, int step, CancellationToken ct) =>
        await vouchers.FindAsync(VoucherSource.Transfer, transferId, step, ct).ConfigureAwait(false) is not null;

    /// <summary>Tells accounts-svc both accounts are used, so they are never deleted (a deletion of the transfer does not undo it).</summary>
    private async Task MarkAccountsAsync(BankTransfer transfer, CancellationToken ct)
    {
        await bankAccounts.MarkInUseAsync(transfer.Source.Id, ct).ConfigureAwait(false);
        await bankAccounts.MarkInUseAsync(transfer.Destination.Id, ct).ConfigureAwait(false);
    }

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
