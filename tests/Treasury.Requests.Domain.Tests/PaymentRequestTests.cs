using Treasury.BuildingBlocks.Domain;
using Treasury.Requests.Domain.PaymentRequests;
using Xunit;

namespace Treasury.Requests.Domain.Tests;

/// <summary>Payment requests: new-version manual «ب-صدور درخواست وجه», old Access form Porder (rule R9).</summary>
public sealed class PaymentRequestTests
{
    private const string User = "clerk-1";
    private static readonly RequesterFact Requester = new(Guid.NewGuid(), "علی رضایی", Guid.NewGuid(), "810001", true);
    private static readonly BeneficiaryFact Beneficiary = new(Guid.NewGuid(), "300200", "شرکت نمونه بازرگانی", "IR460120000000003123456789", true);
    private static readonly PurposeGroupFact Purchases = new(Guid.NewGuid(), "خرید مواد اولیه", IsForPayments: true, IsActive: true);
    private static readonly PayFromFact Bank = new(PayFromKind.BankAccount, Guid.NewGuid(), "0202-3572", "IRR", true);
    private static readonly PayFromFact Desk = new(PayFromKind.CashDesk, Guid.NewGuid(), "صندوق مرکزی", "IRR", true);

    private static FiscalDate D(string s) => FiscalDate.ParseJalali(s);

    private static PaymentRequestForm Form(PaymentMethod method = PaymentMethod.CurrentCheque, decimal amount = 25_000_000, string currency = "IRR") =>
        new(D("1405/08/24"), Requester, Beneficiary, Purchases, "خرید ورق", amount, currency, method, Reference1: "INV-12");

    private static string Code(Action a) => Assert.Throws<DomainException>(a).Code;

    [Fact]
    public void A_request_is_registered_with_the_requesters_department_and_cost_centre()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 7, Form(), User);
        Assert.Equal((PaymentRequestState.Registered, 1405, 7, 1), (r.State, r.FiscalYear, r.Number, r.Priority));
        Assert.Equal((Requester.DepartmentId, "810001", "300200"), (r.RequesterDepartmentId, r.CostCenterCode, r.BeneficiaryDetailCode));
        Assert.IsType<PaymentRequestRegistered>(Assert.Single(r.PendingEvents));
    }

    [Fact]
    public void Old_required_fields_R9_beneficiary_amount_currency_purpose()
    {
        Assert.Equal("PaymentRequest.AmountRequired", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(amount: 0), User)));
        Assert.Equal("PaymentRequest.CurrencyRequired", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(currency: ""), User)));
        Assert.Equal("PaymentRequest.PurposeRequired", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { Purpose = " " }, User)));
        Assert.Equal("PaymentRequest.BeneficiaryInactive", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { Beneficiary = Beneficiary with { IsActive = false } }, User)));
        Assert.Equal("PaymentRequest.RequesterInactive", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { Requester = Requester with { IsActive = false } }, User)));
    }

    [Fact]
    public void Rial_amounts_are_whole_foreign_ones_have_two_decimals()
    {
        Assert.Equal("PaymentRequest.AmountInvalid", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(amount: 10.5m), User)));
        Assert.Equal(1_500.25m, PaymentRequest.Register(Guid.NewGuid(), 1, Form(amount: 1_500.25m, currency: "usd"), User).Amount);
        Assert.Equal("PaymentRequest.AmountInvalid", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(amount: 1.255m, currency: "USD"), User)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Priority_is_1_2_or_3(int priority) =>
        Assert.Equal("PaymentRequest.PriorityInvalid", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { Priority = priority }, User)));

    [Fact]
    public void Only_a_payment_purpose_group()
    {
        Assert.Equal("PaymentRequest.PurposeGroupNotForPayments", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { PurposeGroup = Purchases with { IsForPayments = false } }, User)));
        Assert.Equal("PaymentRequest.PurposeGroupInactive", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { PurposeGroup = Purchases with { IsActive = false } }, User)));
    }

    [Fact]
    public void A_transfer_needs_the_receiving_account_a_Sheba_is_checked()
    {
        Assert.Equal("PaymentRequest.TransferAccountRequired", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.Transfer), User)));
        Assert.Equal("PaymentRequest.ShebaInvalid", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.Transfer) with { TransferAccount = "IR460120000000003123456788" }, User)));
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.Transfer) with { TransferAccount = "ir46 0120 0000 0000 3123 4567 89" }, User);
        Assert.Equal("IR460120000000003123456789", r.TransferAccount);
    }

    [Fact]
    public void Card_to_card_needs_a_valid_card_number()
    {
        Assert.Equal("PaymentRequest.CardNumberRequired", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.CardToCard), User)));
        Assert.Equal("PaymentRequest.CardNumberInvalid", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.CardToCard) with { CardNumber = "6037991234567890" }, User)));
        Assert.Equal("6037991234567893", PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.CardToCard) with { CardNumber = "6037-9912-3456-7893" }, User).CardNumber);
    }

    [Fact]
    public void Transfer_account_and_card_belong_to_their_own_method_only()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { TransferAccount = "x", CardNumber = "y" }, User);
        Assert.Null(r.TransferAccount);
        Assert.Null(r.CardNumber);
    }

    [Fact]
    public void Cash_is_paid_from_a_cash_desk_everything_else_from_a_bank_account()
    {
        Assert.Equal(Desk.Id, PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.Cash) with { PayFrom = Desk }, User).PayFromId);
        Assert.Equal("PaymentRequest.PayFromWrongKind", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.Cash) with { PayFrom = Bank }, User)));
        Assert.Equal("PaymentRequest.PayFromWrongKind", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { PayFrom = Desk }, User)));
        Assert.Equal("PaymentRequest.PayFromCurrency", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(currency: "USD") with { PayFrom = Bank }, User)));
        Assert.Equal("PaymentRequest.PayFromInactive", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { PayFrom = Bank with { IsActive = false } }, User)));
    }

    [Fact]
    public void A_post_dated_cheque_needs_a_due_date_not_before_the_request()
    {
        Assert.Equal("PaymentRequest.DueDateRequired", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.PostDatedCheque), User)));
        Assert.Equal("PaymentRequest.DueBeforeRequest", Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.PostDatedCheque) with { DueDate = D("1405/08/01") }, User)));
        Assert.NotNull(PaymentRequest.Register(Guid.NewGuid(), 1, Form(PaymentMethod.PostDatedCheque) with { DueDate = D("1405/11/30") }, User).DueDate);
    }

    [Fact]
    public void Approved_requests_are_no_longer_edited_and_the_approval_can_be_taken_back()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(), User);
        r.Edit(Form() with { Priority = 3 }, User);
        Assert.Equal(3, r.Priority);
        r.Approve("manager", DateTimeOffset.UnixEpoch);
        Assert.Equal((PaymentRequestState.Approved, "manager"), (r.State, r.ApprovedBy));
        Assert.Equal("PaymentRequest.NotEditable", Code(() => r.Edit(Form(), User)));
        Assert.Equal("PaymentRequest.NotRegistered", Code(() => r.Approve("manager", DateTimeOffset.UnixEpoch)));
        r.WithdrawApproval("manager");
        Assert.Equal((PaymentRequestState.Registered, (string?)null), (r.State, r.ApprovedBy));
    }

    [Fact]
    public void A_request_stays_in_its_fiscal_year()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(), User);
        Assert.Equal("PaymentRequest.OtherYear", Code(() => r.Edit(Form() with { Date = D("1406/01/05") }, User)));
    }

    [Fact]
    public void A_request_is_cancelled_with_a_reason_and_only_once()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(), User);
        r.Approve("manager", DateTimeOffset.UnixEpoch);
        Assert.Equal("PaymentRequest.CancelReasonRequired", Code(() => r.Cancel(" ", User)));
        r.Cancel("خرید منتفی شد", User);
        Assert.Equal(PaymentRequestState.Cancelled, r.State);
        Assert.Equal("PaymentRequest.CannotCancel", Code(() => r.Cancel("again", User)));
    }

    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0];

    [Fact]
    public void Scans_are_attached_as_pdf_jpeg_or_png_report_4_item_12()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(), User);
        var f = r.AttachFile(Guid.NewGuid(), @"C:\scans\invoice 12.pdf", Pdf, User, DateTimeOffset.UnixEpoch);
        Assert.Equal(("invoice 12.pdf", "application/pdf", 8), (f.FileName, f.ContentType, f.Size));
        Assert.Equal("image/png", r.AttachFile(Guid.NewGuid(), "b.png", Png, User, DateTimeOffset.UnixEpoch).ContentType);
        Assert.Equal("PaymentRequest.AttachmentType", Code(() => r.AttachFile(Guid.NewGuid(), "x.docx", [0x50, 0x4B, 3, 4], User, DateTimeOffset.UnixEpoch)));
        Assert.Equal("PaymentRequest.AttachmentEmpty", Code(() => r.AttachFile(Guid.NewGuid(), "x.pdf", [], User, DateTimeOffset.UnixEpoch)));
        Assert.Equal("PaymentRequest.AttachmentTooLarge", Code(() => r.AttachFile(Guid.NewGuid(), "x.pdf", new byte[PaymentRequest.MaxAttachmentBytes + 1], User, DateTimeOffset.UnixEpoch)));
        r.RemoveFile(f.Id, User);
        Assert.Single(r.Files);
        Assert.Equal("PaymentRequest.AttachmentNotFound", Code(() => r.RemoveFile(f.Id, User)));
    }

    [Fact]
    public void An_approved_request_keeps_its_scans()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(), User);
        var f = r.AttachFile(Guid.NewGuid(), "a.pdf", Pdf, User, DateTimeOffset.UnixEpoch);
        r.Approve("manager", DateTimeOffset.UnixEpoch);
        Assert.Equal("PaymentRequest.NotEditable", Code(() => r.AttachFile(Guid.NewGuid(), "b.pdf", Pdf, User, DateTimeOffset.UnixEpoch)));
        Assert.Equal("PaymentRequest.NotEditable", Code(() => r.RemoveFile(f.Id, User)));
    }

    [Fact]
    public void At_most_20_scans()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(), User);
        for (var i = 0; i < PaymentRequest.MaxAttachmentFiles; i++)
        {
            r.AttachFile(Guid.NewGuid(), $"{i}.pdf", Pdf, User, DateTimeOffset.UnixEpoch);
        }

        Assert.Equal("PaymentRequest.TooManyAttachments", Code(() => r.AttachFile(Guid.NewGuid(), "x.pdf", Pdf, User, DateTimeOffset.UnixEpoch)));
    }

    private static RequestPayment Pay(string key, decimal amount) => new(key, "EndorsedCheque", Guid.NewGuid(), "چک 445566", amount, D("1405/08/25"), "treasurer");

    [Fact]
    public void Endorsed_cheques_pay_an_approved_request_in_part_or_in_full_Q36()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(amount: 25_000_000), User);
        Assert.Equal("PaymentRequest.NotApproved", Code(() => r.RecordPayment(Pay("a", 10_000_000), "IRR")));
        r.Approve("manager", DateTimeOffset.UnixEpoch);
        Assert.Equal("PaymentRequest.PaymentCurrency", Code(() => r.RecordPayment(Pay("a", 10_000_000), "USD")));
        Assert.Equal("PaymentRequest.Overpaid", Code(() => r.RecordPayment(Pay("a", 30_000_000), "IRR")));
        r.RecordPayment(Pay("a", 10_000_000), "IRR");
        r.RecordPayment(Pay("a", 10_000_000), "IRR"); // the same payment again changes nothing
        Assert.Equal((PaymentRequestState.Approved, 10_000_000m, 15_000_000m), (r.State, r.PaidAmount, r.RemainingAmount));
        Assert.Equal("PaymentRequest.HasPayments", Code(() => r.WithdrawApproval("manager")));
        Assert.Equal("PaymentRequest.HasPayments", Code(() => r.Cancel("منتفی", "manager")));
        r.RecordPayment(Pay("b", 15_000_000), "IRR");
        Assert.Equal((PaymentRequestState.Paid, 0m), (r.State, r.RemainingAmount));
        Assert.Equal("PaymentRequest.NotApproved", Code(() => r.RecordPayment(Pay("c", 1), "IRR")));
    }

    [Fact]
    public void A_returned_endorsed_cheque_takes_its_payment_back_and_the_request_is_to_be_paid_again()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(amount: 25_000_000), User);
        r.Approve("manager", DateTimeOffset.UnixEpoch);
        r.RecordPayment(Pay("a", 25_000_000), "IRR");
        Assert.Equal(PaymentRequestState.Paid, r.State);
        Assert.Equal("PaymentRequest.PaymentNotFound", Code(() => r.ReversePayment("x", "treasurer")));
        r.ReversePayment("a", "treasurer");
        Assert.Equal((PaymentRequestState.Approved, 0m), (r.State, r.PaidAmount));
        var copy = PaymentRequest.Restore(r.Id, r.Number, r.FiscalYear, r.RecordedBy, r.Data(), 3);
        Assert.Empty(copy.Payments);
    }
}
