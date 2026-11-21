using System.Numerics;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Requests.Domain.PaymentRequests;

/// <summary>
/// How the request is to be paid (new-version manual, «نوع پرداخت»: چک نقدی، چک انتظامی، چک مدّت دار، پرداخت نقدی، پرداخت سفته،
/// سفته انتظامی، نقل و انتقال (بین بانکی)، کارت به کارت). One method per request: "جهت هر ذینفع فقط یک نوع پرداخت می تواند انتخاب گردد".
/// Owner, 19 Nov 2026: «ثبت انتقال (خرج چک)» — a received cheque registered in Sayad is handed on to the beneficiary.
/// </summary>
public enum PaymentMethod
{
    CurrentCheque,
    PostDatedCheque,
    GuaranteeCheque,
    Cash,
    PromissoryNote,
    GuaranteeNote,
    Transfer,
    CardToCard,
    ChequeTransfer,

    /// <summary>
    /// «نقل و انتقال بین بانک‌های شرکت» (owner, 21 Nov 2026, answer to Q82: «برای نقل و انتقال از هر نوع لازم است درخواست وجه صادر و مجوز آن
    /// اخذ شود»): money moved from one bank account of the company to another. The request names the two accounts instead of a
    /// beneficiary and a purpose group; treasury pays it with a transfer (electronic, or by Sayad cheque).
    /// </summary>
    OwnAccountTransfer,
}

/// <summary>
/// A received cheque as cashflow-svc describes it, for a request of the kind «ثبت انتقال (خرج چک)» (owner, 19 Nov 2026).
/// </summary>
/// <param name="PayerName">Who gave the cheque to the company («نام واگذارنده چک»).</param>
/// <param name="CanBeTransferred">The cheque may be transferred now: an ordinary cheque in the cash desk whose latest status is
/// «ثبت صیادی شد» (cashflow-svc's lifecycle table decides).</param>
public sealed record ReceivedChequeFact(
    Guid Id, string Number, string? BankName, FiscalDate? DueDate, string PayerName, decimal Amount, string Currency, bool CanBeTransferred, string? SayadId = null);

/// <summary>
/// The cheque a request of the kind «ثبت انتقال (خرج چک)» hands on, as it was when the request was written: its number, due date and the
/// name of who gave it to the company are shown in place of «پرداخت از حساب بانکی» (owner, 19 Nov 2026).
/// </summary>
public sealed record TransferCheque(Guid InstrumentId, string Number, FiscalDate? DueDate, string PayerName, string? BankName = null, string? SayadId = null);

/// <summary>A scanned supporting document of a request (owner's report no. 4, item 12: «تصویر اسکن شده ضمایم پرداخت»).</summary>
/// <param name="ContentType">application/pdf, image/jpeg or image/png, read from the file itself.</param>
public sealed record RequestAttachment(Guid Id, string FileName, string ContentType, int Size, string UploadedBy, DateTimeOffset UploadedAt);

/// <summary>
/// A payment made against an approved request (owner, answer to Q36 of 15 Nov 2026: an endorsed received cheque «خرج چک» pays an approved
/// payment request). Payment screens of Phase E add the other kinds.
/// </summary>
/// <param name="Key">Identifies the payment (e.g. "endorse:{cheque}:{step}"), so a repeated call never pays twice.</param>
/// <param name="Reference">What paid it, as shown to users (e.g. «چک 445566 بانک ملت»).</param>
public sealed record RequestPayment(string Key, string Kind, Guid? InstrumentId, string? Reference, decimal Amount, FiscalDate Date, string UserId);

/// <summary>Registered by the requester, approved for payment, paid by treasury (Phase E), or cancelled.</summary>
public enum PaymentRequestState
{
    Registered,
    Approved,
    Paid,
    Cancelled,
}

public enum PayFromKind
{
    BankAccount,
    CashDesk,
}

/// <summary>A requester of funds (درخواست کننده وجوه) as reference-svc describes it; department and cost centre come with it.</summary>
public sealed record RequesterFact(Guid Id, string Name, Guid? DepartmentId, string? CostCenterCode, bool IsActive);

/// <summary>A beneficiary (ذینفع) from Basic Information, with the تفصیل it is paid to.</summary>
/// <param name="IsBeneficiary">In the group «ذینفعان» of «ذینفعان / متعهدین / ضامنین»: an entry that is only an obligor or a guarantor is
/// not paid (owner, 19 Nov 2026).</param>
public sealed record BeneficiaryFact(Guid Id, string DetailCode, string Name, string? Sheba, bool IsActive, bool IsBeneficiary = true);

/// <summary>A purpose group (گروه بابت); for a payment it decides the debit account of the voucher (instruction 182-99).</summary>
public sealed record PurposeGroupFact(Guid Id, string Name, bool IsForPayments, bool IsActive);

/// <summary>The bank account or cash desk the request is to be paid from (optional at request time).</summary>
public sealed record PayFromFact(PayFromKind Kind, Guid Id, string Name, string CurrencyCode, bool IsActive);

/// <summary>Everything the requester fills in. Fields marked * in the manual are required.</summary>
/// <param name="Reference1">Value of the purpose group's first reference (e.g. invoice no. «شماره صورتحساب»).</param>
/// <param name="TransferAccount">Transfer only: the receiving account or Sheba (manual: «انتقال به حساب بانکی»).</param>
/// <param name="CardNumber">Card-to-card only: the beneficiary's card (manual: «انتقال به شماره کارت»).</param>
/// <param name="HasMainBeneficiaries">«پرداخت جمعی دارد / ذینفعان اصلی دارد»: the beneficiary passes the money on to others.</param>
/// <param name="Attachments">Number of supporting documents («تعداد ضمائم»).</param>
/// <param name="Cheque">«ثبت انتقال (خرج چک)» only: the received cheque to hand on.</param>
/// <param name="TransferTo">«نقل و انتقال بین بانک‌های شرکت» only: the company bank account the money goes to («انتقال گیرنده»);
/// <paramref name="PayFrom"/> is then the account it leaves («انتقال دهنده»), and no beneficiary or purpose group is asked.</param>
public sealed record PaymentRequestForm(
    FiscalDate Date,
    RequesterFact Requester,
    BeneficiaryFact? Beneficiary,
    PurposeGroupFact? PurposeGroup,
    string Purpose,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    int Priority = 1,
    string? Reference1 = null,
    string? Reference2 = null,
    string? TransferAccount = null,
    string? CardNumber = null,
    PayFromFact? PayFrom = null,
    FiscalDate? DueDate = null,
    Guid? DepartmentId = null,
    bool HasMainBeneficiaries = false,
    int Attachments = 0,
    string? Note = null,
    ReceivedChequeFact? Cheque = null,
    PayFromFact? TransferTo = null);

/// <summary>
/// A payment request (درخواست وجه). New-version manual, «ب-صدور درخواست وجه»: every payment starts with a request entered by a
/// department; a person allowed to approve requests ticks «تایید پرداخت», and the request appears in «فهرست درخواست وجه های تایید
/// شده جهت پرداخت» for treasury, sorted by payment method and priority. Old Access form Porder: beneficiary, amount, currency,
/// purpose and date are required (rule R9) and a paid request cannot be deleted ("پرداخت صورت گرفته و امكان حذف وجود ندارد").
/// </summary>
public sealed class PaymentRequest : AggregateRoot<Guid>
{
    public const int MaxPurposeLength = 300;
    public const int MaxAttachments = 999;
    public const int ForeignDecimals = 2;
    public const int MaxAttachmentBytes = 5 * 1024 * 1024;
    public const int MaxAttachmentFiles = 20;

    private readonly List<RequestAttachment> _files = [];
    private readonly List<RequestPayment> _payments = [];

    private PaymentRequest(Guid id, int number, int fiscalYear, string recordedBy)
        : base(id)
    {
        Number = number;
        FiscalYear = fiscalYear;
        RecordedBy = recordedBy;
    }

    /// <summary>Serial number within the fiscal year («شماره سریال درخواست وجه»).</summary>
    public int Number { get; }

    /// <summary>Jalali year of the request date (open question Q5).</summary>
    public int FiscalYear { get; }

    public string RecordedBy { get; }

    public FiscalDate Date { get; private set; }

    public Guid RequesterId { get; private set; }

    public string RequesterName { get; private set; } = string.Empty;

    /// <summary>Taken from the requester («عنوان وشماره واحد درخواست کننده … بصورت اتوماتیکمان پر میشوند»).</summary>
    public Guid? RequesterDepartmentId { get; private set; }

    public string? CostCenterCode { get; private set; }

    public Guid BeneficiaryId { get; private set; }

    public string BeneficiaryDetailCode { get; private set; } = string.Empty;

    public string BeneficiaryName { get; private set; } = string.Empty;

    public Guid PurposeGroupId { get; private set; }

    public string PurposeGroupName { get; private set; } = string.Empty;

    public string Purpose { get; private set; } = string.Empty;

    public string? Reference1 { get; private set; }

    public string? Reference2 { get; private set; }

    public decimal Amount { get; private set; }

    /// <summary>ISO code; Rial (IRR) by default.</summary>
    public string Currency { get; private set; } = "IRR";

    public PaymentMethod Method { get; private set; }

    /// <summary>1 (default) to 3 («اولویت درخواست»).</summary>
    public int Priority { get; private set; } = 1;

    public string? TransferAccount { get; private set; }

    public string? CardNumber { get; private set; }

    public PayFromKind? PayFromKind { get; private set; }

    public Guid? PayFromId { get; private set; }

    /// <summary>«ثبت انتقال (خرج چک)» only: the received cheque the request hands on to its beneficiary.</summary>
    public TransferCheque? Cheque { get; private set; }

    /// <summary>
    /// «نقل و انتقال بین بانک‌های شرکت» only: the company bank account the money goes to. Such a request has no beneficiary and no purpose
    /// group of Basic Information: <see cref="BeneficiaryName"/> shows the receiving account and <see cref="PurposeGroupName"/> the
    /// words «نقل و انتقال بین بانک‌های شرکت», so every list and printout of requests reads the same way.
    /// </summary>
    public Guid? TransferToAccountId { get; private set; }

    /// <summary>The name written in place of a purpose group on a request for a transfer between the company's own accounts.</summary>
    /// <remarks>Owner, 21 Nov 2026: «نقل و انتقال بین بانک‌های شرکت» is the name of this kind of request.</remarks>
    public const string OwnTransferGroupName = "نقل و انتقال بین بانک‌های شرکت";

    public FiscalDate? DueDate { get; private set; }

    /// <summary>«واحد سازمانی» (manual: «بخش»): groups payments by the company's organisational units.</summary>
    public Guid? DepartmentId { get; private set; }

    public bool HasMainBeneficiaries { get; private set; }

    public int Attachments { get; private set; }

    public string? Note { get; private set; }

    public PaymentRequestState State { get; private set; }

    public string? ApprovedBy { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public string? CancelReason { get; private set; }

    /// <summary>Scans attached to the request; the number of paper documents is <see cref="Attachments"/>.</summary>
    public IReadOnlyList<RequestAttachment> Files => _files;

    /// <summary>Payments made against the request (an endorsed cheque, answer to Q36); the request is paid when they reach its amount.</summary>
    public IReadOnlyList<RequestPayment> Payments => _payments;

    public decimal PaidAmount => _payments.Sum(p => p.Amount);

    public decimal RemainingAmount => Amount - PaidAmount;

    public static PaymentRequest Register(Guid id, int number, PaymentRequestForm form, string userId)
    {
        ArgumentNullException.ThrowIfNull(form);
        Require(number > 0, "PaymentRequest.NumberInvalid", "The request number must be positive.");
        Require(!string.IsNullOrWhiteSpace(userId), "PaymentRequest.UserRequired", "User is required.");
        var request = new PaymentRequest(id, number, form.Date.JalaliYear, userId);
        request.Apply(form);
        request.State = PaymentRequestState.Registered;
        request.Raise(new PaymentRequestRegistered(id, number, request.FiscalYear, request.BeneficiaryDetailCode, request.Amount, request.Currency, request.Method, userId));
        return request;
    }

    /// <summary>Corrected while it is not yet approved; the date keeps the request in its fiscal year.</summary>
    public void Edit(PaymentRequestForm form, string userId)
    {
        ArgumentNullException.ThrowIfNull(form);
        Require(State == PaymentRequestState.Registered, "PaymentRequest.NotEditable", "Only a request that is not yet approved can be changed.");
        Require(form.Date.JalaliYear == FiscalYear, "PaymentRequest.OtherYear", "The date must stay in the request's fiscal year.");
        Apply(form);
        Raise(new PaymentRequestEdited(Id, userId));
    }

    /// <summary>
    /// Attaches a scan (owner's report no. 4, item 12): PDF, JPEG or PNG, at most 5 MB, 20 files a request; only while the request is
    /// not yet approved, so what was approved is what is paid. Returns the stored attachment.
    /// </summary>
    public RequestAttachment AttachFile(Guid id, string fileName, ReadOnlySpan<byte> content, string userId, DateTimeOffset at)
    {
        Require(State == PaymentRequestState.Registered, "PaymentRequest.NotEditable", "Only a request that is not yet approved can be changed.");
        Require(content.Length > 0, "PaymentRequest.AttachmentEmpty", "The file is empty.");
        Require(content.Length <= MaxAttachmentBytes, "PaymentRequest.AttachmentTooLarge", "A scan is at most 5 MB.");
        Require(_files.Count < MaxAttachmentFiles, "PaymentRequest.TooManyAttachments", "At most 20 scans on one request.");
        var type = FileTypeOf(content) ?? throw new DomainException("PaymentRequest.AttachmentType", "A scan is a PDF, JPEG or PNG file.");
        // Only the file's own name: browsers on Windows may send the whole path, with \ or /.
        var raw = (fileName ?? string.Empty).Trim();
        var name = raw[(raw.LastIndexOfAny(['/', '\\']) + 1)..];
        var file = new RequestAttachment(id, string.IsNullOrWhiteSpace(name) ? "scan" : name[..Math.Min(name.Length, 200)], type, content.Length, userId, at);
        _files.Add(file);
        Raise(new PaymentRequestFileAttached(Id, file.Id, file.FileName, userId));
        return file;
    }

    public void RemoveFile(Guid fileId, string userId)
    {
        Require(State == PaymentRequestState.Registered, "PaymentRequest.NotEditable", "Only a request that is not yet approved can be changed.");
        var i = _files.FindIndex(f => f.Id == fileId);
        Require(i >= 0, "PaymentRequest.AttachmentNotFound", "The scan was not found.");
        _files.RemoveAt(i);
        Raise(new PaymentRequestFileRemoved(Id, fileId, userId));
    }

    /// <summary>«تایید پرداخت»: the request goes to the list of requests approved for payment.</summary>
    public void Approve(string userId, DateTimeOffset at)
    {
        Require(State == PaymentRequestState.Registered, "PaymentRequest.NotRegistered", "Only a registered request can be approved.");
        Require(!string.IsNullOrWhiteSpace(userId), "PaymentRequest.UserRequired", "User is required.");
        (State, ApprovedBy, ApprovedAt) = (PaymentRequestState.Approved, userId, at);
        Raise(new PaymentRequestApproved(Id, Number, FiscalYear, Amount, Currency, Method, Priority, userId));
    }

    /// <summary>
    /// Records a payment against the approved request (answer to Q36). Several payments may share one request (Q29) up to its amount; when
    /// they reach it the request is «پرداخت شده». A payment already recorded under the same key is not recorded again.
    /// </summary>
    public void RecordPayment(RequestPayment payment, string currency)
    {
        ArgumentNullException.ThrowIfNull(payment);
        Require(!string.IsNullOrWhiteSpace(payment.Key), "PaymentRequest.PaymentKeyRequired", "The payment needs its key.");
        if (_payments.Any(p => p.Key == payment.Key))
        {
            return;
        }

        Require(State == PaymentRequestState.Approved, "PaymentRequest.NotApproved", "Only an approved request is paid.");
        Require(string.Equals((currency ?? string.Empty).Trim(), Currency, StringComparison.OrdinalIgnoreCase), "PaymentRequest.PaymentCurrency",
            "The payment must be in the request's currency.");
        // Owner, 19 Nov 2026: a request of the kind «ثبت انتقال (خرج چک)» is paid by the transfer of the cheque it names.
        Require(Cheque is null || payment.InstrumentId == Cheque.InstrumentId, "PaymentRequest.PaymentNotByItsCheque",
            "This request is paid by the transfer of the cheque it names.");

        // Owner, 21 Nov 2026 (answer to Q82): a request for a transfer between the company's own accounts is paid by such a transfer,
        // and a transfer pays no other kind of request.
        Require((Method == PaymentMethod.OwnAccountTransfer) == (payment.Kind == OwnTransferPaymentKind), "PaymentRequest.PaymentKindMismatch",
            "A request for a transfer between the company's accounts is paid by a transfer, and a transfer pays only such a request.");
        Require(payment.Amount > 0, "PaymentRequest.PaymentAmountInvalid", "The payment amount must be positive.");
        Require(payment.Amount <= RemainingAmount, "PaymentRequest.Overpaid", "The payment is more than what remains to be paid on the request.");
        _payments.Add(payment);
        if (RemainingAmount == 0)
        {
            State = PaymentRequestState.Paid;
        }

        Raise(new PaymentRequestPaymentRecorded(Id, payment.Key, payment.Kind, payment.InstrumentId, payment.Amount, payment.Date, State == PaymentRequestState.Paid, payment.UserId));
    }

    /// <summary>A payment came back (an endorsed cheque returned bounced, owner's item 14 (h)): the request is to be paid again.</summary>
    public void ReversePayment(string key, string userId)
    {
        var i = _payments.FindIndex(p => p.Key == key);
        Require(i >= 0, "PaymentRequest.PaymentNotFound", "The payment was not found on the request.");
        var payment = _payments[i];
        _payments.RemoveAt(i);
        if (State == PaymentRequestState.Paid)
        {
            State = PaymentRequestState.Approved;
        }

        Raise(new PaymentRequestPaymentReversed(Id, key, payment.Amount, userId));
    }

    /// <summary>The approval is taken back (the checkbox unticked) while nothing has been paid.</summary>
    public void WithdrawApproval(string userId)
    {
        Require(State == PaymentRequestState.Approved, "PaymentRequest.NotApproved", "The request is not approved.");
        Require(_payments.Count == 0, "PaymentRequest.HasPayments", "Part of the request is paid; its approval cannot be taken back.");
        (State, ApprovedBy, ApprovedAt) = (PaymentRequestState.Registered, null, null);
        Raise(new PaymentRequestApprovalWithdrawn(Id, userId));
    }

    /// <summary>«ابطال درخواست وجه»: refused or no longer needed. A paid request is never cancelled (Porder).</summary>
    public void Cancel(string reason, string userId)
    {
        Require(State is PaymentRequestState.Registered or PaymentRequestState.Approved, "PaymentRequest.CannotCancel",
            "A paid or cancelled request cannot be cancelled.");
        Require(_payments.Count == 0, "PaymentRequest.HasPayments", "Part of the request is paid; it cannot be cancelled.");
        Require(!string.IsNullOrWhiteSpace(reason), "PaymentRequest.CancelReasonRequired", "Give the reason for cancelling.");
        (State, CancelReason) = (PaymentRequestState.Cancelled, reason.Trim());
        Raise(new PaymentRequestCancelled(Id, CancelReason, userId));
    }

    public static PaymentRequest Restore(
        Guid id, int number, int fiscalYear, string recordedBy, PaymentRequestSnapshotData d, long version, IEnumerable<RequestAttachment>? files = null)
    {
        ArgumentNullException.ThrowIfNull(d);
        var r = new PaymentRequest(id, number, fiscalYear, recordedBy)
        {
            Date = d.Date, RequesterId = d.RequesterId, RequesterName = d.RequesterName, RequesterDepartmentId = d.RequesterDepartmentId,
            CostCenterCode = d.CostCenterCode, BeneficiaryId = d.BeneficiaryId, BeneficiaryDetailCode = d.BeneficiaryDetailCode,
            BeneficiaryName = d.BeneficiaryName, PurposeGroupId = d.PurposeGroupId, PurposeGroupName = d.PurposeGroupName, Purpose = d.Purpose,
            Reference1 = d.Reference1, Reference2 = d.Reference2, Amount = d.Amount, Currency = d.Currency, Method = d.Method, Priority = d.Priority,
            TransferAccount = d.TransferAccount, CardNumber = d.CardNumber, PayFromKind = d.PayFromKind, PayFromId = d.PayFromId, DueDate = d.DueDate,
            DepartmentId = d.DepartmentId, HasMainBeneficiaries = d.HasMainBeneficiaries, Attachments = d.Attachments, Note = d.Note,
            State = d.State, ApprovedBy = d.ApprovedBy, ApprovedAt = d.ApprovedAt, CancelReason = d.CancelReason, Cheque = d.Cheque,
            TransferToAccountId = d.TransferToAccountId,
        };
        r._files.AddRange(files ?? []);
        r._payments.AddRange(d.Payments ?? []);
        r.SetLoadedVersion(version);
        return r;
    }

    public PaymentRequestSnapshotData Data() => new(
        Date, RequesterId, RequesterName, RequesterDepartmentId, CostCenterCode, BeneficiaryId, BeneficiaryDetailCode, BeneficiaryName,
        PurposeGroupId, PurposeGroupName, Purpose, Reference1, Reference2, Amount, Currency, Method, Priority, TransferAccount, CardNumber,
        PayFromKind, PayFromId, DueDate, DepartmentId, HasMainBeneficiaries, Attachments, Note, State, ApprovedBy, ApprovedAt, CancelReason, [.. _payments], Cheque,
        TransferToAccountId);

    /// <summary>The kind of payment cashflow-svc records when a transfer between the company's accounts pays its request.</summary>
    public const string OwnTransferPaymentKind = "BankTransfer";

    private void Apply(PaymentRequestForm f)
    {
        Require(f.Requester is not null, "PaymentRequest.RequesterRequired", "Choose the requester.");
        Require(f.Requester!.IsActive, "PaymentRequest.RequesterInactive", "The requester is switched off.");
        var ownTransfer = f.Method == PaymentMethod.OwnAccountTransfer;
        if (!ownTransfer)
        {
            Require(f.Beneficiary is not null, "PaymentRequest.BeneficiaryRequired", "Choose the beneficiary (old: \"ذينفع نامشخص است\").");
            Require(f.Beneficiary!.IsActive, "PaymentRequest.BeneficiaryInactive", "The beneficiary is switched off.");
            Require(f.Beneficiary.IsBeneficiary, "PaymentRequest.NotABeneficiary", "This entry is an obligor or guarantor, not a beneficiary.");
            Require(f.PurposeGroup is not null, "PaymentRequest.PurposeGroupRequired", "Choose the purpose group.");
            Require(f.PurposeGroup!.IsForPayments, "PaymentRequest.PurposeGroupNotForPayments", "This purpose group is for receipts.");
            Require(f.PurposeGroup.IsActive, "PaymentRequest.PurposeGroupInactive", "This purpose group is switched off.");
        }
        Require(!string.IsNullOrWhiteSpace(f.Purpose), "PaymentRequest.PurposeRequired", "Enter the purpose (old: \"بابت نامشخص است\").");
        Require(f.Purpose.Trim().Length <= MaxPurposeLength, "PaymentRequest.PurposeTooLong", "The purpose is too long.");
        Require(Enum.IsDefined(f.Method), "PaymentRequest.MethodRequired", "Choose how it is to be paid.");
        Require(f.Priority is >= 1 and <= 3, "PaymentRequest.PriorityInvalid", "The priority is 1, 2 or 3.");
        Require(f.Attachments is >= 0 and <= MaxAttachments, "PaymentRequest.AttachmentsInvalid", "The number of attachments is not valid.");

        var currency = (f.Currency ?? string.Empty).Trim().ToUpperInvariant();
        Require(currency.Length == 3 && currency.All(char.IsAsciiLetter), "PaymentRequest.CurrencyRequired", "Choose the currency (old: \"نوع ارز نامشخص است\").");
        Require(f.Amount > 0, "PaymentRequest.AmountRequired", "Enter the amount (old: \"مبلغ نامشخص است\").");
        Require(decimal.Round(f.Amount, currency == "IRR" ? 0 : ForeignDecimals) == f.Amount, "PaymentRequest.AmountInvalid",
            currency == "IRR" ? "A Rial amount is a whole number." : "A foreign-currency amount has at most two decimals.");

        // Manual: "در سایر انواع پرداخت این فیلد غیر فعال می باشد" — the transfer account and card number belong to their method only.
        string? transfer = null;
        string? card = null;
        if (f.Method == PaymentMethod.Transfer)
        {
            transfer = Clean(f.TransferAccount);
            Require(transfer is not null, "PaymentRequest.TransferAccountRequired", "Enter the receiving account for a transfer.");
            if (transfer!.StartsWith("IR", StringComparison.OrdinalIgnoreCase))
            {
                transfer = transfer.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
                // Owner's report no. 6, item 17: a wrong length and wrong check digits are told apart.
                Require(transfer.Length == 26 && transfer[2..].All(char.IsAsciiDigit), "PaymentRequest.ShebaInvalid", "A Sheba number is IR and 24 digits.");
                Require(IsSheba(transfer), "PaymentRequest.ShebaCheckDigits", "The check digits of the Sheba number do not fit: a digit is wrong.");
            }
        }
        else if (f.Method == PaymentMethod.CardToCard)
        {
            card = Clean(f.CardNumber)?.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
            Require(card is not null, "PaymentRequest.CardNumberRequired", "Enter the beneficiary's card number.");
            Require(card!.Length == 16 && card.All(char.IsAsciiDigit), "PaymentRequest.CardNumberInvalid", "A card number has 16 digits.");
            Require(IsCardNumber(card), "PaymentRequest.CardCheckDigit", "The check digit of the card number does not fit: a digit is wrong.");
        }

        // Owner, 19 Nov 2026: «ثبت انتقال (خرج چک)» names the received cheque instead of the account paid from. The cheque must have been
        // registered in Sayad («ثبت صیادی شد») and "the amount of the cheque is the amount of the request".
        TransferCheque? cheque = null;
        if (f.Method == PaymentMethod.ChequeTransfer)
        {
            Require(f.Cheque is not null, "PaymentRequest.ChequeRequired", "Choose the received cheque to transfer.");
            var c = f.Cheque!;
            Require(c.CanBeTransferred, "PaymentRequest.ChequeNotTransferable", "Only an ordinary cheque in the cash desk that is registered in Sayad can be transferred.");
            Require(string.Equals(c.Currency.Trim(), currency, StringComparison.OrdinalIgnoreCase), "PaymentRequest.ChequeCurrency",
                "The request must be in the cheque's currency.");
            Require(f.Amount == c.Amount, "PaymentRequest.ChequeAmount", "The amount of the request is the amount of the cheque.");
            cheque = new TransferCheque(c.Id, c.Number.Trim(), c.DueDate, c.PayerName.Trim(), Clean(c.BankName), Clean(c.SayadId));
        }

        // Owner, 21 Nov 2026 (answer to Q82): a transfer between the company's own accounts names both accounts — the one the money
        // leaves («انتقال دهنده») and the one it reaches («انتقال گیرنده») — in the request's currency.
        if (ownTransfer)
        {
            Require(f.PayFrom is not null, "PaymentRequest.TransferFromRequired", "Choose the bank account the money leaves (انتقال دهنده).");
            Require(f.TransferTo is not null, "PaymentRequest.TransferToRequired", "Choose the bank account the money reaches (انتقال گیرنده).");
            Require(f.TransferTo!.Kind == Domain.PaymentRequests.PayFromKind.BankAccount && f.TransferTo.Id != f.PayFrom!.Id, "PaymentRequest.TransferSameAccount",
                "The two bank accounts of a transfer must differ.");
            Require(f.TransferTo.IsActive, "PaymentRequest.PayFromInactive", "The bank account or cash desk is switched off.");
            Require(string.Equals(f.TransferTo.CurrencyCode.Trim(), currency, StringComparison.OrdinalIgnoreCase), "PaymentRequest.PayFromCurrency",
                "The bank account or cash desk must be in the request's currency.");
        }

        if (f.Method != PaymentMethod.ChequeTransfer && f.PayFrom is { } from)
        {
            var wanted = f.Method == PaymentMethod.Cash ? Domain.PaymentRequests.PayFromKind.CashDesk : Domain.PaymentRequests.PayFromKind.BankAccount;
            Require(from.Kind == wanted, "PaymentRequest.PayFromWrongKind", "Cash is paid from a cash desk; everything else from a bank account.");
            Require(from.IsActive, "PaymentRequest.PayFromInactive", "The bank account or cash desk is switched off.");
            Require(string.Equals(from.CurrencyCode.Trim(), currency, StringComparison.OrdinalIgnoreCase), "PaymentRequest.PayFromCurrency",
                "The bank account or cash desk must be in the request's currency.");
        }

        Require(f.Method != PaymentMethod.PostDatedCheque || f.DueDate is not null, "PaymentRequest.DueDateRequired", "A post-dated cheque needs its due date.");
        // A transferred cheque brings its own due date (it may already have passed), so the request has none.
        var due = f.Method == PaymentMethod.ChequeTransfer ? null : f.DueDate;
        Require(due is null || due >= f.Date, "PaymentRequest.DueBeforeRequest", "The due date cannot be before the request date.");

        Date = f.Date;
        (RequesterId, RequesterName, RequesterDepartmentId, CostCenterCode) = (f.Requester.Id, f.Requester.Name, f.Requester.DepartmentId, f.Requester.CostCenterCode);
        (BeneficiaryId, BeneficiaryDetailCode, BeneficiaryName) = ownTransfer
            ? (f.TransferTo!.Id, string.Empty, f.TransferTo.Name.Trim())
            : (f.Beneficiary!.Id, f.Beneficiary.DetailCode, f.Beneficiary.Name);
        (PurposeGroupId, PurposeGroupName, Purpose) = ownTransfer
            ? (Guid.Empty, OwnTransferGroupName, f.Purpose.Trim())
            : (f.PurposeGroup!.Id, f.PurposeGroup.Name, f.Purpose.Trim());
        TransferToAccountId = ownTransfer ? f.TransferTo!.Id : null;
        (Reference1, Reference2, Note) = (Clean(f.Reference1), Clean(f.Reference2), Clean(f.Note));
        (Amount, Currency, Method, Priority) = (f.Amount, currency, f.Method, f.Priority);
        (TransferAccount, CardNumber) = (transfer, card);
        (PayFromKind, PayFromId) = (cheque is null ? f.PayFrom?.Kind : null, cheque is null ? f.PayFrom?.Id : null);
        Cheque = cheque;
        (DueDate, DepartmentId, HasMainBeneficiaries, Attachments) = (due, f.DepartmentId, f.HasMainBeneficiaries, f.Attachments);
    }

    /// <summary>Iranian Sheba (IBAN): IR + 24 digits, mod 97 = 1.</summary>
    public static bool IsSheba(string text)
    {
        if (text.Length != 26 || !text.StartsWith("IR", StringComparison.Ordinal) || !text[2..].All(char.IsAsciiDigit))
        {
            return false;
        }

        var rearranged = text[4..] + "1827" + text[2..4];
        return BigInteger.Parse(rearranged, System.Globalization.CultureInfo.InvariantCulture) % 97 == 1;
    }

    /// <summary>Bank card: 16 digits with the Luhn check digit (as for petty-cash cards in accounts-svc).</summary>
    public static bool IsCardNumber(string digits)
    {
        if (digits.Length != 16 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 16; i++)
        {
            var d = digits[15 - i] - '0';
            if (i % 2 == 1)
            {
                d *= 2;
                if (d > 9)
                {
                    d -= 9;
                }
            }

            sum += d;
        }

        return sum % 10 == 0;
    }

    private static string? FileTypeOf(ReadOnlySpan<byte> b) =>
        b.Length >= 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46 ? "application/pdf"
        : b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ? "image/png"
        : b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ? "image/jpeg"
        : null;

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

/// <summary>Storable state of a request (used by the adapters).</summary>
public sealed record PaymentRequestSnapshotData(
    FiscalDate Date, Guid RequesterId, string RequesterName, Guid? RequesterDepartmentId, string? CostCenterCode, Guid BeneficiaryId,
    string BeneficiaryDetailCode, string BeneficiaryName, Guid PurposeGroupId, string PurposeGroupName, string Purpose, string? Reference1,
    string? Reference2, decimal Amount, string Currency, PaymentMethod Method, int Priority, string? TransferAccount, string? CardNumber,
    PayFromKind? PayFromKind, Guid? PayFromId, FiscalDate? DueDate, Guid? DepartmentId, bool HasMainBeneficiaries, int Attachments, string? Note,
    PaymentRequestState State, string? ApprovedBy, DateTimeOffset? ApprovedAt, string? CancelReason, IReadOnlyList<RequestPayment>? Payments = null,
    TransferCheque? Cheque = null, Guid? TransferToAccountId = null);

public sealed record PaymentRequestRegistered(
    Guid PaymentRequestId, int Number, int FiscalYear, string BeneficiaryDetailCode, decimal Amount, string Currency, PaymentMethod Method, string UserId) : DomainEvent;

public sealed record PaymentRequestEdited(Guid PaymentRequestId, string UserId) : DomainEvent;

public sealed record PaymentRequestApproved(
    Guid PaymentRequestId, int Number, int FiscalYear, decimal Amount, string Currency, PaymentMethod Method, int Priority, string UserId) : DomainEvent;

public sealed record PaymentRequestApprovalWithdrawn(Guid PaymentRequestId, string UserId) : DomainEvent;

public sealed record PaymentRequestFileAttached(Guid PaymentRequestId, Guid FileId, string FileName, string UserId) : DomainEvent;

public sealed record PaymentRequestFileRemoved(Guid PaymentRequestId, Guid FileId, string UserId) : DomainEvent;

public sealed record PaymentRequestCancelled(Guid PaymentRequestId, string Reason, string UserId) : DomainEvent;

public sealed record PaymentRequestPaymentRecorded(
    Guid PaymentRequestId, string Key, string Kind, Guid? InstrumentId, decimal Amount, FiscalDate Date, bool FullyPaid, string UserId) : DomainEvent;

public sealed record PaymentRequestPaymentReversed(Guid PaymentRequestId, string Key, decimal Amount, string UserId) : DomainEvent;
