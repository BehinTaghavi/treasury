using System.Numerics;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Requests.Domain.PaymentRequests;

/// <summary>
/// How the request is to be paid (new-version manual, «نوع پرداخت»: چک نقدی، چک انتظامی، چک مدّت دار، پرداخت نقدی، پرداخت سفته،
/// سفته انتظامی، نقل و انتقال (بین بانکی)، کارت به کارت). One method per request: "جهت هر ذینفع فقط یک نوع پرداخت می تواند انتخاب گردد".
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
}

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
public sealed record BeneficiaryFact(Guid Id, string DetailCode, string Name, string? Sheba, bool IsActive);

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
public sealed record PaymentRequestForm(
    FiscalDate Date,
    RequesterFact Requester,
    BeneficiaryFact Beneficiary,
    PurposeGroupFact PurposeGroup,
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
    string? Note = null);

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

    public FiscalDate? DueDate { get; private set; }

    /// <summary>«بخش»: groups payments by the company's organisational units.</summary>
    public Guid? DepartmentId { get; private set; }

    public bool HasMainBeneficiaries { get; private set; }

    public int Attachments { get; private set; }

    public string? Note { get; private set; }

    public PaymentRequestState State { get; private set; }

    public string? ApprovedBy { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public string? CancelReason { get; private set; }

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

    /// <summary>«تایید پرداخت»: the request goes to the list of requests approved for payment.</summary>
    public void Approve(string userId, DateTimeOffset at)
    {
        Require(State == PaymentRequestState.Registered, "PaymentRequest.NotRegistered", "Only a registered request can be approved.");
        Require(!string.IsNullOrWhiteSpace(userId), "PaymentRequest.UserRequired", "User is required.");
        (State, ApprovedBy, ApprovedAt) = (PaymentRequestState.Approved, userId, at);
        Raise(new PaymentRequestApproved(Id, Number, FiscalYear, Amount, Currency, Method, Priority, userId));
    }

    /// <summary>The approval is taken back (the checkbox unticked) while nothing has been paid.</summary>
    public void WithdrawApproval(string userId)
    {
        Require(State == PaymentRequestState.Approved, "PaymentRequest.NotApproved", "The request is not approved.");
        (State, ApprovedBy, ApprovedAt) = (PaymentRequestState.Registered, null, null);
        Raise(new PaymentRequestApprovalWithdrawn(Id, userId));
    }

    /// <summary>«ابطال درخواست وجه»: refused or no longer needed. A paid request is never cancelled (Porder).</summary>
    public void Cancel(string reason, string userId)
    {
        Require(State is PaymentRequestState.Registered or PaymentRequestState.Approved, "PaymentRequest.CannotCancel",
            "A paid or cancelled request cannot be cancelled.");
        Require(!string.IsNullOrWhiteSpace(reason), "PaymentRequest.CancelReasonRequired", "Give the reason for cancelling.");
        (State, CancelReason) = (PaymentRequestState.Cancelled, reason.Trim());
        Raise(new PaymentRequestCancelled(Id, CancelReason, userId));
    }

    public static PaymentRequest Restore(Guid id, int number, int fiscalYear, string recordedBy, PaymentRequestSnapshotData d, long version)
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
            State = d.State, ApprovedBy = d.ApprovedBy, ApprovedAt = d.ApprovedAt, CancelReason = d.CancelReason,
        };
        r.SetLoadedVersion(version);
        return r;
    }

    public PaymentRequestSnapshotData Data() => new(
        Date, RequesterId, RequesterName, RequesterDepartmentId, CostCenterCode, BeneficiaryId, BeneficiaryDetailCode, BeneficiaryName,
        PurposeGroupId, PurposeGroupName, Purpose, Reference1, Reference2, Amount, Currency, Method, Priority, TransferAccount, CardNumber,
        PayFromKind, PayFromId, DueDate, DepartmentId, HasMainBeneficiaries, Attachments, Note, State, ApprovedBy, ApprovedAt, CancelReason);

    private void Apply(PaymentRequestForm f)
    {
        Require(f.Requester is not null, "PaymentRequest.RequesterRequired", "Choose the requester.");
        Require(f.Requester!.IsActive, "PaymentRequest.RequesterInactive", "The requester is switched off.");
        Require(f.Beneficiary is not null, "PaymentRequest.BeneficiaryRequired", "Choose the beneficiary (old: \"ذينفع نامشخص است\").");
        Require(f.Beneficiary!.IsActive, "PaymentRequest.BeneficiaryInactive", "The beneficiary is switched off.");
        Require(f.PurposeGroup is not null, "PaymentRequest.PurposeGroupRequired", "Choose the purpose group.");
        Require(f.PurposeGroup!.IsForPayments, "PaymentRequest.PurposeGroupNotForPayments", "This purpose group is for receipts.");
        Require(f.PurposeGroup.IsActive, "PaymentRequest.PurposeGroupInactive", "This purpose group is switched off.");
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
                Require(IsSheba(transfer), "PaymentRequest.ShebaInvalid", "The Sheba number is not valid (IR and 24 digits).");
            }
        }
        else if (f.Method == PaymentMethod.CardToCard)
        {
            card = Clean(f.CardNumber)?.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
            Require(card is not null, "PaymentRequest.CardNumberRequired", "Enter the beneficiary's card number.");
            Require(IsCardNumber(card!), "PaymentRequest.CardNumberInvalid", "The card number has 16 digits and a valid check digit.");
        }

        if (f.PayFrom is { } from)
        {
            var wanted = f.Method == PaymentMethod.Cash ? Domain.PaymentRequests.PayFromKind.CashDesk : Domain.PaymentRequests.PayFromKind.BankAccount;
            Require(from.Kind == wanted, "PaymentRequest.PayFromWrongKind", "Cash is paid from a cash desk; everything else from a bank account.");
            Require(from.IsActive, "PaymentRequest.PayFromInactive", "The bank account or cash desk is switched off.");
            Require(string.Equals(from.CurrencyCode.Trim(), currency, StringComparison.OrdinalIgnoreCase), "PaymentRequest.PayFromCurrency",
                "The bank account or cash desk must be in the request's currency.");
        }

        Require(f.Method != PaymentMethod.PostDatedCheque || f.DueDate is not null, "PaymentRequest.DueDateRequired", "A post-dated cheque needs its due date.");
        Require(f.DueDate is null || f.DueDate >= f.Date, "PaymentRequest.DueBeforeRequest", "The due date cannot be before the request date.");

        Date = f.Date;
        (RequesterId, RequesterName, RequesterDepartmentId, CostCenterCode) = (f.Requester.Id, f.Requester.Name, f.Requester.DepartmentId, f.Requester.CostCenterCode);
        (BeneficiaryId, BeneficiaryDetailCode, BeneficiaryName) = (f.Beneficiary.Id, f.Beneficiary.DetailCode, f.Beneficiary.Name);
        (PurposeGroupId, PurposeGroupName, Purpose) = (f.PurposeGroup.Id, f.PurposeGroup.Name, f.Purpose.Trim());
        (Reference1, Reference2, Note) = (Clean(f.Reference1), Clean(f.Reference2), Clean(f.Note));
        (Amount, Currency, Method, Priority) = (f.Amount, currency, f.Method, f.Priority);
        (TransferAccount, CardNumber) = (transfer, card);
        (PayFromKind, PayFromId) = (f.PayFrom?.Kind, f.PayFrom?.Id);
        (DueDate, DepartmentId, HasMainBeneficiaries, Attachments) = (f.DueDate, f.DepartmentId, f.HasMainBeneficiaries, f.Attachments);
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
    PaymentRequestState State, string? ApprovedBy, DateTimeOffset? ApprovedAt, string? CancelReason);

public sealed record PaymentRequestRegistered(
    Guid PaymentRequestId, int Number, int FiscalYear, string BeneficiaryDetailCode, decimal Amount, string Currency, PaymentMethod Method, string UserId) : DomainEvent;

public sealed record PaymentRequestEdited(Guid PaymentRequestId, string UserId) : DomainEvent;

public sealed record PaymentRequestApproved(
    Guid PaymentRequestId, int Number, int FiscalYear, decimal Amount, string Currency, PaymentMethod Method, int Priority, string UserId) : DomainEvent;

public sealed record PaymentRequestApprovalWithdrawn(Guid PaymentRequestId, string UserId) : DomainEvent;

public sealed record PaymentRequestCancelled(Guid PaymentRequestId, string Reason, string UserId) : DomainEvent;
