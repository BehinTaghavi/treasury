using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Requests.Application.Abstractions;
using Treasury.Requests.Domain.PaymentRequests;

namespace Treasury.Requests.Application.PaymentRequests;

/// <summary>The request form as typed. Dates are Jalali text; requester, beneficiary and purpose group come from Basic Information.</summary>
/// <param name="PayFromId">Optional: a cash desk for cash, otherwise a bank account.</param>
/// <param name="ChequeId">«ثبت انتقال (خرج چک)»: the received cheque to hand on (owner, 19 Nov 2026).</param>
/// <param name="TransferToAccountId">«نقل و انتقال بین حساب‌های شرکت» (owner, 21 Nov 2026): the receiving company bank account; the
/// beneficiary and the purpose group are then left empty and <paramref name="PayFromId"/> is the account the money leaves.</param>
public sealed record PaymentRequestInput(
    string Date,
    Guid RequesterId,
    Guid BeneficiaryId,
    Guid PurposeGroupId,
    string Purpose,
    decimal Amount,
    PaymentMethod Method,
    string Currency = "IRR",
    int Priority = 1,
    string? Reference1 = null,
    string? Reference2 = null,
    string? TransferAccount = null,
    string? CardNumber = null,
    Guid? PayFromId = null,
    string? DueDate = null,
    Guid? DepartmentId = null,
    bool HasMainBeneficiaries = false,
    int Attachments = 0,
    string? Note = null,
    Guid? ChequeId = null,
    Guid? TransferToAccountId = null);

public sealed record RegisterPaymentRequestCommand(string IdempotencyKey, PaymentRequestInput Input);

public sealed record EditPaymentRequestCommand(string IdempotencyKey, Guid Id, PaymentRequestInput Input, long ExpectedVersion);

/// <param name="Change">"approve", "withdraw-approval" or "cancel" (with <paramref name="Reason"/>).</param>
public sealed record ChangePaymentRequestCommand(string IdempotencyKey, Guid Id, string Change, long ExpectedVersion, string? Reason = null);

/// <summary>A payment recorded by treasury against an approved request (answer to Q36: an endorsed cheque).</summary>
public sealed record RecordRequestPaymentCommand(
    string IdempotencyKey, Guid Id, string PaymentKey, string Kind, Guid? InstrumentId, string? Reference, decimal Amount, string Currency, string Date);

public sealed record RequestPaymentView(string Key, string Kind, Guid? InstrumentId, string? Reference, decimal Amount, string Date, string UserId);

/// <summary>The cheque of a request of the kind «ثبت انتقال (خرج چک)»: number, due date and who gave it to the company.</summary>
public sealed record TransferChequeView(Guid InstrumentId, string Number, string? DueDate, string PayerName, string? BankName, string? SayadId);

public sealed record PaymentRequestView(
    Guid Id, int FiscalYear, int Number, string Date, Guid RequesterId, string RequesterName, Guid? RequesterDepartmentId, string? CostCenterCode,
    Guid BeneficiaryId, string BeneficiaryDetailCode, string BeneficiaryName, Guid PurposeGroupId, string PurposeGroupName, string Purpose,
    string? Reference1, string? Reference2, decimal Amount, string Currency, string Method, int Priority, string? TransferAccount, string? CardNumber,
    string? PayFromKind, Guid? PayFromId, string? DueDate, Guid? DepartmentId, bool HasMainBeneficiaries, int Attachments, string? Note,
    string State, string RecordedBy, string? ApprovedBy, DateTimeOffset? ApprovedAt, string? CancelReason, long Version,
    IReadOnlyList<RequestAttachment>? Files = null, decimal PaidAmount = 0, IReadOnlyList<RequestPaymentView>? Payments = null,
    TransferChequeView? Cheque = null, Guid? TransferToAccountId = null, decimal RemainingAmount = 0)
{
    public static PaymentRequestView From(PaymentRequest r) => new(
        r.Id, r.FiscalYear, r.Number, r.Date.ToJalaliString(), r.RequesterId, r.RequesterName, r.RequesterDepartmentId, r.CostCenterCode,
        r.BeneficiaryId, r.BeneficiaryDetailCode, r.BeneficiaryName, r.PurposeGroupId, r.PurposeGroupName, r.Purpose, r.Reference1, r.Reference2,
        r.Amount, r.Currency, r.Method.ToString(), r.Priority, r.TransferAccount, r.CardNumber, r.PayFromKind?.ToString(), r.PayFromId,
        r.DueDate?.ToJalaliString(), r.DepartmentId, r.HasMainBeneficiaries, r.Attachments, r.Note, r.State.ToString(), r.RecordedBy, r.ApprovedBy,
        r.ApprovedAt, r.CancelReason, r.Version, r.Files, r.PaidAmount,
        r.Payments.Select(p => new RequestPaymentView(p.Key, p.Kind, p.InstrumentId, p.Reference, p.Amount, p.Date.ToJalaliString(), p.UserId)).ToList(),
        r.Cheque is { } c ? new TransferChequeView(c.InstrumentId, c.Number, c.DueDate?.ToJalaliString(), c.PayerName, c.BankName, c.SayadId) : null,
        r.TransferToAccountId, r.RemainingAmount);
}

/// <summary>
/// Payment requests (درخواست وجه), new-version manual «ب-صدور درخواست وجه». Each write is one command in one transaction on one
/// aggregate. Requester, beneficiary and purpose group are checked with reference-svc and marked "in use" there, so they are never
/// deleted from Basic Information while a request names them.
/// </summary>
public sealed class PaymentRequestUseCases(
    IUnitOfWork unitOfWork,
    IPaymentRequestRepository requests,
    IRequestReferenceLookup reference,
    IPayFromLookup accounts,
    IReceivedChequeLookup cheques,
    IIdempotencyStore idempotency,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> RegisterAsync(RegisterPaymentRequestCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var form = await FormAsync(command.Input, null, ct).ConfigureAwait(false);
            var number = await requests.NextNumberAsync(form.Date.JalaliYear, ct).ConfigureAwait(false);
            var request = PaymentRequest.Register(Guid.NewGuid(), number, form, currentUser.UserId);
            await MarkInUseAsync(form, ct).ConfigureAwait(false);
            await requests.AddAsync(request, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(request.Id);
        }, cancellationToken);

    public Task<Result<Guid>> EditAsync(EditPaymentRequestCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var request = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var form = await FormAsync(command.Input, request.Id, ct).ConfigureAwait(false);
            request.Edit(form, currentUser.UserId);
            await MarkInUseAsync(form, ct).ConfigureAwait(false);
            await requests.SaveAsync(request, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(request.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeAsync(ChangePaymentRequestCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var request = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var user = currentUser.UserId;
            switch (command.Change)
            {
                case "approve":
                    request.Approve(user, clock.GetUtcNow());
                    break;
                case "withdraw-approval":
                    request.WithdrawApproval(user);
                    break;
                case "cancel":
                    request.Cancel(command.Reason ?? string.Empty, user);
                    break;
                default:
                    return Result<Guid>.Fail("Request.UnknownChange", $"Unknown change '{command.Change}'.");
            }

            await requests.SaveAsync(request, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(request.Id);
        }, cancellationToken);

    /// <summary>
    /// Records a payment against an approved request (called by cashflow-svc when a received cheque is endorsed, answer to Q36). The request is
    /// paid when its payments reach its amount. Repeating the same payment key changes nothing.
    /// </summary>
    public Task<Result<Guid>> RecordPaymentAsync(RecordRequestPaymentCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var request = await requests.GetAsync(command.Id, ct).ConfigureAwait(false)
                ?? throw new DomainException("PaymentRequest.NotFound", "The payment request was not found.");
            request.RecordPayment(
                new RequestPayment(command.PaymentKey, command.Kind, command.InstrumentId, command.Reference, command.Amount, FiscalDate.ParseJalali(command.Date), currentUser.UserId),
                command.Currency);
            await requests.SaveAsync(request, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(request.Id);
        }, cancellationToken);

    /// <summary>Takes a payment back (the endorsed cheque came back bounced): the request is to be paid again.</summary>
    public Task<Result<Guid>> ReversePaymentAsync(string idempotencyKey, Guid id, string paymentKey, CancellationToken cancellationToken) =>
        Run(idempotencyKey, async ct =>
        {
            var request = await requests.GetAsync(id, ct).ConfigureAwait(false)
                ?? throw new DomainException("PaymentRequest.NotFound", "The payment request was not found.");
            request.ReversePayment(paymentKey, currentUser.UserId);
            await requests.SaveAsync(request, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(request.Id);
        }, cancellationToken);

    /// <summary>Attaches a scan to a request that is not yet approved (owner's report no. 4, item 12). Returns the new file's id.</summary>
    public Task<Result<Guid>> AttachFileAsync(string idempotencyKey, Guid id, string fileName, byte[] content, long expectedVersion, CancellationToken cancellationToken) =>
        Run(idempotencyKey, async ct =>
        {
            var request = await LoadAsync(id, expectedVersion, ct).ConfigureAwait(false);
            var file = request.AttachFile(Guid.NewGuid(), fileName, content, currentUser.UserId, clock.GetUtcNow());
            await requests.SaveAsync(request, ct, (file.Id, content)).ConfigureAwait(false);
            return Result<Guid>.Ok(file.Id);
        }, cancellationToken);

    public Task<Result<Guid>> RemoveFileAsync(string idempotencyKey, Guid id, Guid fileId, long expectedVersion, CancellationToken cancellationToken) =>
        Run(idempotencyKey, async ct =>
        {
            var request = await LoadAsync(id, expectedVersion, ct).ConfigureAwait(false);
            request.RemoveFile(fileId, currentUser.UserId);
            await requests.SaveAsync(request, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(request.Id);
        }, cancellationToken);

    public Task<(RequestAttachment File, byte[] Content)?> GetFileAsync(Guid id, Guid fileId, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => requests.GetFileAsync(id, fileId, ct), cancellationToken);

    public Task<PaymentRequestView?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
            await requests.GetAsync(id, ct).ConfigureAwait(false) is { } r ? PaymentRequestView.From(r) : null, cancellationToken);

    /// <summary>
    /// Requests of a year, newest first. The list of requests approved for payment («فهرست درخواست وجه های تایید شده جهت پرداخت») is
    /// sorted by payment method and then priority, as the manual asks.
    /// </summary>
    public Task<IReadOnlyList<PaymentRequestView>> ListAsync(int fiscalYear, PaymentRequestState? state, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<PaymentRequestView>>(async ct =>
        {
            var list = await requests.ListAsync(fiscalYear, state, ct).ConfigureAwait(false);
            var ordered = state == PaymentRequestState.Approved
                ? list.OrderBy(r => r.Method).ThenBy(r => r.Priority).ThenBy(r => r.Date).ThenBy(r => r.Number)
                : list.OrderByDescending(r => r.Date).ThenByDescending(r => r.Number);
            return ordered.Select(PaymentRequestView.From).ToList();
        }, cancellationToken);

    private async Task<PaymentRequest> LoadAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var request = await requests.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("PaymentRequest.NotFound", "The payment request was not found.");
        if (request.Version != expectedVersion)
        {
            throw new ConcurrencyConflictException("The payment request was changed by someone else.");
        }

        return request;
    }

    /// <param name="requestId">The request being corrected; null for a new one.</param>
    private async Task<PaymentRequestForm> FormAsync(PaymentRequestInput input, Guid? requestId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Date))
        {
            throw new DomainException("PaymentRequest.DateRequired", "Enter the date (old: \"تاريخ نامشخص است\").");
        }

        var requester = await reference.RequesterAsync(input.RequesterId, ct).ConfigureAwait(false)
            ?? throw new DomainException("PaymentRequest.RequesterRequired", "Choose the requester.");
        // A transfer between the company's own accounts (answer to Q82) names two bank accounts, not a beneficiary and a purpose group.
        var ownTransfer = input.Method == PaymentMethod.OwnAccountTransfer;
        var beneficiary = ownTransfer ? null : await reference.BeneficiaryAsync(input.BeneficiaryId, ct).ConfigureAwait(false)
            ?? throw new DomainException("PaymentRequest.BeneficiaryRequired", "Choose the beneficiary.");
        var group = ownTransfer ? null : await reference.PurposeGroupAsync(input.PurposeGroupId, ct).ConfigureAwait(false)
            ?? throw new DomainException("PaymentRequest.PurposeGroupRequired", "Choose the purpose group.");
        PayFromFact? transferTo = null;
        if (ownTransfer && input.TransferToAccountId is { } toId && toId != Guid.Empty)
        {
            transferTo = await accounts.BankAccountAsync(toId, ct).ConfigureAwait(false)
                ?? throw new DomainException("PaymentRequest.PayFromNotFound", "The bank account or cash desk was not found.");
        }
        // «ثبت انتقال (خرج چک)»: the cheque is asked from cashflow-svc; a cheque is promised to one open request only.
        ReceivedChequeFact? cheque = null;
        if (input.Method == PaymentMethod.ChequeTransfer && input.ChequeId is { } chequeId && chequeId != Guid.Empty)
        {
            cheque = await cheques.ChequeAsync(chequeId, ct).ConfigureAwait(false)
                ?? throw new DomainException("PaymentRequest.ChequeNotFound", "The received cheque was not found.");
            if (await requests.OpenRequestOfChequeAsync(chequeId, ct).ConfigureAwait(false) is { } other && other.Id != requestId)
            {
                throw new DomainException("PaymentRequest.ChequeOnAnotherRequest", $"Cheque {cheque.Number} is already on payment request {other.Number}.");
            }
        }

        PayFromFact? payFrom = null;
        if (input.Method != PaymentMethod.ChequeTransfer && input.PayFromId is { } fromId)
        {
            payFrom = (input.Method == PaymentMethod.Cash
                    ? await accounts.CashDeskAsync(fromId, ct).ConfigureAwait(false)
                    : await accounts.BankAccountAsync(fromId, ct).ConfigureAwait(false))
                ?? throw new DomainException("PaymentRequest.PayFromNotFound", "The bank account or cash desk was not found.");
        }

        return new PaymentRequestForm(
            FiscalDate.ParseJalali(input.Date), requester, beneficiary, group, input.Purpose ?? string.Empty, input.Amount, input.Currency ?? string.Empty,
            input.Method, input.Priority, input.Reference1, input.Reference2, input.TransferAccount, input.CardNumber, payFrom,
            string.IsNullOrWhiteSpace(input.DueDate) ? null : FiscalDate.ParseJalali(input.DueDate), input.DepartmentId, input.HasMainBeneficiaries,
            input.Attachments, input.Note, cheque, transferTo);
    }

    private async Task MarkInUseAsync(PaymentRequestForm form, CancellationToken ct)
    {
        await reference.MarkInUseAsync("requesters", form.Requester.Id, ct).ConfigureAwait(false);
        if (form.Beneficiary is { } beneficiary)
        {
            await reference.MarkInUseAsync("beneficiaries", beneficiary.Id, ct).ConfigureAwait(false);
        }

        if (form.PurposeGroup is { } group)
        {
            await reference.MarkInUseAsync("purpose-groups", group.Id, ct).ConfigureAwait(false);
        }
    }

    private Task<Result<Guid>> Run(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
