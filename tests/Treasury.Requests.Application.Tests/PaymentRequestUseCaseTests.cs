using Treasury.Requests.Application.PaymentRequests;
using Treasury.Requests.Domain.PaymentRequests;
using Treasury.Requests.Infrastructure.InMemory;
using Treasury.Requests.Tests;
using Xunit;

namespace Treasury.Requests.Application.Tests;

/// <summary>Payment requests through the use cases with the in-memory adapters (new-version manual «ب-صدور درخواست وجه»).</summary>
public sealed class PaymentRequestUseCaseTests : IDisposable
{
    private readonly InMemoryRequestsDatabase _db = new();
    private readonly FakeReference _reference = new();
    private readonly FixedUser _user = new("requester-1");
    private readonly PaymentRequestUseCases _requests;

    public PaymentRequestUseCaseTests() =>
        _requests = new PaymentRequestUseCases(
            new InMemoryUnitOfWork(_db), new InMemoryPaymentRequestRepository(_db), _reference, new FakeAccounts(), new InMemoryIdempotencyStore(_db), _user, TimeProvider.System);

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static PaymentRequestInput Input(PaymentMethod method = PaymentMethod.CurrentCheque, int priority = 1, decimal amount = 25_000_000) =>
        new("1405/08/24", FakeReference.Requester, FakeReference.Supplier, FakeReference.Purchases, "خرید ورق", amount, method, Priority: priority, Reference1: "INV-12");

    private async Task<Guid> Register(PaymentRequestInput input)
    {
        var r = await _requests.RegisterAsync(new(Key(), input), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<PaymentRequestView> View(Guid id) => (await _requests.GetAsync(id, CancellationToken.None))!;

    [Fact]
    public async Task Requests_are_numbered_from_one_each_year_and_mark_basic_information_in_use()
    {
        var a = await Register(Input());
        var b = await Register(Input());
        Assert.Equal((1, 2), ((await View(a)).Number, (await View(b)).Number));
        var v = await View(a);
        Assert.Equal(("Registered", "علی رضایی", "810001", "شرکت نمونه بازرگانی", "requester-1"), (v.State, v.RequesterName, v.CostCenterCode, v.BeneficiaryName, v.RecordedBy));
        Assert.Contains(("requesters", FakeReference.Requester), _reference.Marked);
        Assert.Contains(("beneficiaries", FakeReference.Supplier), _reference.Marked);
        Assert.Contains(("purpose-groups", FakeReference.Purchases), _reference.Marked);
    }

    /// <summary>Owner's report no. 9 of 22 Nov 2026, items 1 and 2: unit and cost centre chosen on the form, checked with reference-svc.</summary>
    [Fact]
    public async Task Report9_unit_and_cost_centre_are_chosen_on_the_form_and_checked_with_basic_information()
    {
        var v = await View(await Register(Input() with { DepartmentId = FakeReference.Department, CostCenterCode = " 810002 " }));
        Assert.Equal(((Guid?)FakeReference.Department, "810002"), (v.DepartmentId, v.CostCenterCode));
        Assert.Equal("810001", (await View(await Register(Input()))).CostCenterCode); // nothing chosen: the requester's cost centre
        Assert.Equal("PaymentRequest.DepartmentNotFound", (await _requests.RegisterAsync(new(Key(), Input() with { DepartmentId = Guid.NewGuid() }), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.DepartmentInactive", (await _requests.RegisterAsync(new(Key(), Input() with { DepartmentId = FakeReference.OldDepartment }), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.CostCenterNotFound", (await _requests.RegisterAsync(new(Key(), Input() with { CostCenterCode = "999999" }), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.NotCostCenter", (await _requests.RegisterAsync(new(Key(), Input() with { CostCenterCode = "300200" }), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.CostCenterNotInDepartment",
            (await _requests.RegisterAsync(new(Key(), Input() with { DepartmentId = FakeReference.Department, CostCenterCode = "810003" }), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Basic_information_is_checked()
    {
        Assert.Equal("PaymentRequest.BeneficiaryInactive", (await _requests.RegisterAsync(new(Key(), Input() with { BeneficiaryId = FakeReference.OldSupplier }), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.BeneficiaryRequired", (await _requests.RegisterAsync(new(Key(), Input() with { BeneficiaryId = Guid.NewGuid() }), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.PurposeGroupNotForPayments", (await _requests.RegisterAsync(new(Key(), Input() with { PurposeGroupId = FakeReference.Sales }), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.RequesterRequired", (await _requests.RegisterAsync(new(Key(), Input() with { RequesterId = Guid.NewGuid() }), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.PayFromNotFound", (await _requests.RegisterAsync(new(Key(), Input() with { PayFromId = Guid.NewGuid() }), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.DateRequired", (await _requests.RegisterAsync(new(Key(), Input() with { Date = " " }), CancellationToken.None)).ErrorCode);
        _reference.Down = true;
        Assert.Equal("Reference.Unavailable", (await _requests.RegisterAsync(new(Key(), Input()), CancellationToken.None)).ErrorCode);
        Assert.Empty(await _requests.ListAsync(1405, null, CancellationToken.None));
    }

    [Fact]
    public async Task Cash_is_paid_from_a_cash_desk()
    {
        var id = await Register(Input(PaymentMethod.Cash) with { PayFromId = FakeAccounts.Desk });
        Assert.Equal(("CashDesk", (Guid?)FakeAccounts.Desk), ((await View(id)).PayFromKind, (await View(id)).PayFromId));
        Assert.Equal("PaymentRequest.PayFromNotFound", (await _requests.RegisterAsync(new(Key(), Input(PaymentMethod.Cash) with { PayFromId = FakeAccounts.Bank }), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_request_is_corrected_until_approved_then_approved_listed_and_cancelled()
    {
        var id = await Register(Input());
        Assert.True((await _requests.EditAsync(new(Key(), id, Input(priority: 2, amount: 30_000_000), 1), CancellationToken.None)).IsSuccess);
        Assert.Equal((2, 30_000_000m, 2L), ((await View(id)).Priority, (await View(id)).Amount, (await View(id)).Version));
        Assert.Equal("Concurrency.Conflict", (await _requests.ChangeAsync(new(Key(), id, "approve", 1), CancellationToken.None)).ErrorCode);

        _user.UserId = "manager";
        Assert.True((await _requests.ChangeAsync(new(Key(), id, "approve", 2), CancellationToken.None)).IsSuccess);
        var v = await View(id);
        Assert.Equal(("Approved", "manager"), (v.State, v.ApprovedBy));
        Assert.NotNull(v.ApprovedAt);
        Assert.Equal("PaymentRequest.NotEditable", (await _requests.EditAsync(new(Key(), id, Input(), 3), CancellationToken.None)).ErrorCode);
        Assert.Single(await _requests.ListAsync(1405, PaymentRequestState.Approved, CancellationToken.None));

        Assert.Equal("PaymentRequest.CancelReasonRequired", (await _requests.ChangeAsync(new(Key(), id, "cancel", 3), CancellationToken.None)).ErrorCode);
        Assert.True((await _requests.ChangeAsync(new(Key(), id, "cancel", 3, "خرید منتفی شد"), CancellationToken.None)).IsSuccess);
        Assert.Equal(("Cancelled", "خرید منتفی شد"), ((await View(id)).State, (await View(id)).CancelReason));
        Assert.Empty(await _requests.ListAsync(1405, PaymentRequestState.Approved, CancellationToken.None));
        Assert.Equal("Request.UnknownChange", (await _requests.ChangeAsync(new(Key(), id, "pay", 4), CancellationToken.None)).ErrorCode);
        Assert.Equal("PaymentRequest.NotFound", (await _requests.ChangeAsync(new(Key(), Guid.NewGuid(), "approve", 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_approved_list_is_sorted_by_payment_method_then_priority()
    {
        var cash3 = await Register(Input(PaymentMethod.Cash, 3));
        var cheque2 = await Register(Input(PaymentMethod.CurrentCheque, 2));
        var cash1 = await Register(Input(PaymentMethod.Cash, 1));
        var cheque1 = await Register(Input(PaymentMethod.CurrentCheque, 1));
        await Register(Input(PaymentMethod.CurrentCheque, 1)); // not approved
        foreach (var id in new[] { cash3, cheque2, cash1, cheque1 })
        {
            Assert.True((await _requests.ChangeAsync(new(Key(), id, "approve", 1), CancellationToken.None)).IsSuccess);
        }

        var approved = await _requests.ListAsync(1405, PaymentRequestState.Approved, CancellationToken.None);
        Assert.Equal([cheque1, cheque2, cash1, cash3], approved.Select(r => r.Id));
        Assert.Equal(5, (await _requests.ListAsync(1405, null, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task Withdrawing_the_approval_allows_corrections_again()
    {
        var id = await Register(Input());
        await _requests.ChangeAsync(new(Key(), id, "approve", 1), CancellationToken.None);
        Assert.True((await _requests.ChangeAsync(new(Key(), id, "withdraw-approval", 2), CancellationToken.None)).IsSuccess);
        Assert.True((await _requests.EditAsync(new(Key(), id, Input(priority: 3), 3), CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task Scans_are_attached_read_back_and_removed_until_approval()
    {
        var id = await Register(Input());
        byte[] pdf = [0x25, 0x50, 0x44, 0x46, 1, 2, 3];
        var file = await _requests.AttachFileAsync(Key(), id, "invoice.pdf", pdf, 1, CancellationToken.None);
        Assert.True(file.IsSuccess, file.ErrorCode);
        var v = await View(id);
        Assert.Equal(("invoice.pdf", 2L), (Assert.Single(v.Files!).FileName, v.Version));
        var stored = (await _requests.GetFileAsync(id, file.Value, CancellationToken.None))!.Value;
        Assert.Equal(pdf, stored.Content);
        Assert.True((await _requests.RemoveFileAsync(Key(), id, file.Value, 2, CancellationToken.None)).IsSuccess);
        Assert.Null(await _requests.GetFileAsync(id, file.Value, CancellationToken.None));
        Assert.True((await _requests.AttachFileAsync(Key(), id, "b.pdf", pdf, 3, CancellationToken.None)).IsSuccess);
        await _requests.ChangeAsync(new(Key(), id, "approve", 4), CancellationToken.None);
        Assert.Equal("PaymentRequest.NotEditable", (await _requests.AttachFileAsync(Key(), id, "c.pdf", pdf, 5, CancellationToken.None)).ErrorCode);
    }

    /// <summary>Owner's report no. 9, item 4: «تعداد ضمائم» is the number of pages of the scans, kept with each scan.</summary>
    [Fact]
    public async Task Report9_attachments_are_the_pages_of_the_scans_attached()
    {
        var id = await Register(Input());
        var twoPages = System.Text.Encoding.Latin1.GetBytes(
            "%PDF-1.4\n2 0 obj << /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >> endobj\n3 0 obj << /Type /Page >> endobj\n4 0 obj << /Type /Page >> endobj\n");
        Assert.True((await _requests.AttachFileAsync(Key(), id, "invoice.pdf", twoPages, 1, CancellationToken.None)).IsSuccess);
        Assert.True((await _requests.AttachFileAsync(Key(), id, "photo.png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0], 2, CancellationToken.None)).IsSuccess);
        var v = await View(id);
        Assert.Equal((3, "2,1"), (v.Attachments, string.Join(",", v.Files!.Select(f => f.Pages))));
    }

    [Fact]
    public async Task The_same_key_twice_registers_one_request()
    {
        var key = Key();
        var a = await _requests.RegisterAsync(new(key, Input()), CancellationToken.None);
        var b = await _requests.RegisterAsync(new(key, Input()), CancellationToken.None);
        Assert.Equal(a.Value, b.Value);
        Assert.Single(await _requests.ListAsync(1405, null, CancellationToken.None));
    }
}
