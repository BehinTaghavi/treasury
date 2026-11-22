using Treasury.Reference.Application.Ledger;
using Treasury.Reference.Application.Signatures;
using Treasury.Reference.Domain.Ledger;
using Treasury.Reference.Domain.Signatures;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

/// <summary>«دارندگان مجوزها (فرم‌ها)» (owner's report no. 8, 21 Nov 2026): roles and, per role, the treasury operations.</summary>
public sealed class PermissionHolderUseCaseTests : IDisposable
{
    private readonly InMemoryReferenceDatabase _db = new();
    private readonly PermissionHolderUseCases _useCases;

    public PermissionHolderUseCaseTests()
    {
        var uow = new InMemoryUnitOfWork(_db);
        var idempotency = new InMemoryIdempotencyStore(_db);
        var ledger = new InMemoryLedgerRepository(_db);
        _useCases = new PermissionHolderUseCases(uow, new InMemoryPermissionHolderRepository(_db), ledger, idempotency);
        var ledgerUseCases = new LedgerUseCases(uow, ledger, ledger, idempotency);
        LedgerFixture.Load(ledgerUseCases);
        DetailRow[] details = [new(1, "600046", "علی رضایی", "كاركنان كارخانه", true, false)];
        Assert.True(ledgerUseCases.ImportDetailsAsync("fixture-details", details, CancellationToken.None).GetAwaiter().GetResult().IsSuccess);
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static PermissionRoleForm Role(PermissionRoleKind kind, params TreasuryOperation[] operations) => new(kind, null, operations);

    private static PermissionHolderForm Form(string? name, string? job = "کارشناس خزانه", string? national = null, params PermissionRoleForm[] roles) =>
        new(name, job, roles.Length > 0 ? roles : [Role(PermissionRoleKind.FirstConfirmer, TreasuryOperation.PaymentRequest)], NationalCode: national);

    private async Task<Guid> Add(PermissionHolderForm form)
    {
        var r = await _useCases.AddAsync(Key(), form, CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<PermissionHolderView> Get(Guid id) => (await _useCases.ListAsync(true, CancellationToken.None)).Single(h => h.Id == id);

    [Fact]
    public async Task A_holder_is_listed_with_its_particulars_roles_and_operations()
    {
        var id = await Add(new PermissionHolderForm(null, "مدیر مالی",
            [Role(PermissionRoleKind.FirstApprover, TreasuryOperation.PaymentRequest), new(PermissionRoleKind.Other, "ناظر", [TreasuryOperation.PettyCash, TreasuryOperation.CashPayment])],
            "600046", "0123456789", "09121234567", "021-88990011", "120", "rezaei@nazho.example"));
        var h = await Get(id);
        Assert.Equal(("علی رضایی", "مدیر مالی", "600046", "0123456789", "09121234567", "021-88990011", "120", "rezaei@nazho.example"),
            (h.FullName, h.JobTitle, h.DetailCode, h.NationalCode, h.Mobile, h.Phone, h.Extension, h.Email));
        Assert.Equal((false, true, 1L), (h.HasSignature, h.IsActive, h.Version));
        Assert.Equal([("FirstApprover", "تصویب کننده (اول)"), ("Other", "ناظر")], h.Roles.Select(r => (r.Kind, r.Title)));
        Assert.Equal(["PaymentRequest"], h.Roles[0].Operations);
        Assert.Equal(["CashPayment", "PettyCash"], h.Roles[1].Operations);
    }

    [Fact]
    public async Task The_form_of_an_operation_gets_its_active_holders_in_the_order_of_the_roles()
    {
        var approver = await Add(Form("بهرام کریمی", "مدیر مالی", roles:
            [Role(PermissionRoleKind.FirstApprover, TreasuryOperation.NoteReceipt), Role(PermissionRoleKind.SecondConfirmer, TreasuryOperation.ChequeTransfer)]));
        await Add(Form("سارا احمدی", "رئیس حسابداری", roles: Role(PermissionRoleKind.SecondConfirmer, TreasuryOperation.NoteReceipt)));
        await Add(Form("آرش نادری", roles:
            [Role(PermissionRoleKind.FirstConfirmer, TreasuryOperation.NoteReceipt, TreasuryOperation.CashReceipt), new(PermissionRoleKind.Other, "ناظر", [TreasuryOperation.NoteReceipt])]));
        var off = await Add(Form("کارمند قدیمی", roles: Role(PermissionRoleKind.FirstConfirmer, TreasuryOperation.NoteReceipt)));
        Assert.True((await _useCases.ChangeAsync(new(Key(), off, "deactivate", 1), CancellationToken.None)).IsSuccess);

        var notes = await _useCases.ListForOperationAsync(TreasuryOperation.NoteReceipt, CancellationToken.None);
        Assert.Equal(["آرش نادری", "سارا احمدی", "بهرام کریمی"], notes.Select(h => h.FullName));
        // A person with two roles in the operation is listed once, with both.
        Assert.Equal(["تایید کننده (اول)", "ناظر"], notes[0].Roles);
        Assert.Equal("تایید کننده (اول)، ناظر – کارشناس خزانه", notes[0].Position);
        Assert.Equal("تصویب کننده (اول) – مدیر مالی", notes[2].Position);

        var transfer = Assert.Single(await _useCases.ListForOperationAsync(TreasuryOperation.ChequeTransfer, CancellationToken.None));
        Assert.Equal((approver, "تایید کننده (دوم) – مدیر مالی"), (transfer.Id, transfer.Position));
        Assert.Empty(await _useCases.ListForOperationAsync(TreasuryOperation.PettyCash, CancellationToken.None));
    }

    [Fact]
    public async Task The_same_name_or_national_code_is_not_entered_twice_and_the_form_is_checked()
    {
        await Add(Form("بهرام کریمی", national: "0123456789"));
        Assert.Equal("PermissionHolder.NameTaken", (await _useCases.AddAsync(Key(), Form("بهرام كريمي"), CancellationToken.None)).ErrorCode);
        Assert.Equal("PermissionHolder.NationalCodeTaken", (await _useCases.AddAsync(Key(), Form("دیگری", national: "0123456789"), CancellationToken.None)).ErrorCode);
        Assert.Equal("PermissionHolder.JobTitleRequired", (await _useCases.AddAsync(Key(), Form("دیگری", " "), CancellationToken.None)).ErrorCode);
        Assert.Equal("PermissionHolder.RoleRequired", (await _useCases.AddAsync(Key(), new PermissionHolderForm("دیگری", "کارشناس", []), CancellationToken.None)).ErrorCode);
        Assert.Equal("PermissionHolder.OperationRequired", (await _useCases.AddAsync(Key(), Form("دیگری", roles: Role(PermissionRoleKind.FirstConfirmer)), CancellationToken.None)).ErrorCode);
        Assert.Equal("Person.DetailNotFound",
            (await _useCases.AddAsync(Key(), Form("دیگری") with { DetailCode = "999999" }, CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_holder_is_corrected_with_its_version_gets_a_scan_is_switched_off_and_deleted()
    {
        var id = await Add(Form("بهرام کریمی"));
        Assert.True((await _useCases.EditAsync(Key(), id, Form("بهرام کریمی", "مدیر مالی", roles: Role(PermissionRoleKind.SecondApprover, TreasuryOperation.ChequePayment)), 1, CancellationToken.None)).IsSuccess);
        var h = await Get(id);
        Assert.Equal(("مدیر مالی", "SecondApprover", 2L), (h.JobTitle, Assert.Single(h.Roles).Kind, h.Version));
        Assert.Equal("Concurrency.Conflict", (await _useCases.EditAsync(Key(), id, Form("بهرام کریمی"), 1, CancellationToken.None)).ErrorCode);
        Assert.Equal("PermissionHolder.NotFound", (await _useCases.EditAsync(Key(), Guid.NewGuid(), Form("الف"), 1, CancellationToken.None)).ErrorCode);

        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 5];
        Assert.True((await _useCases.AttachSignatureAsync(Key(), id, png, 2, CancellationToken.None)).IsSuccess);
        Assert.Equal(("image/png", png), ((await _useCases.GetSignatureAsync(id, CancellationToken.None))!.Value.ContentType, (await _useCases.GetSignatureAsync(id, CancellationToken.None))!.Value.Data));
        Assert.True((await _useCases.ListForOperationAsync(TreasuryOperation.ChequePayment, CancellationToken.None))[0].HasSignature);
        Assert.True((await _useCases.RemoveSignatureAsync(Key(), id, 3, CancellationToken.None)).IsSuccess);
        Assert.Null(await _useCases.GetSignatureAsync(id, CancellationToken.None));

        Assert.True((await _useCases.ChangeAsync(new(Key(), id, "deactivate", 4), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListAsync(false, CancellationToken.None));
        Assert.True((await _useCases.ChangeAsync(new(Key(), id, "activate", 5), CancellationToken.None)).IsSuccess);
        Assert.Equal("Request.UnknownChange", (await _useCases.ChangeAsync(new(Key(), id, "retire", 6), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangeAsync(new(Key(), id, "delete", 6), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListAsync(true, CancellationToken.None));
    }

    [Fact]
    public async Task The_same_request_twice_adds_one_holder()
    {
        var key = Key();
        var a = await _useCases.AddAsync(key, Form("بهرام کریمی"), CancellationToken.None);
        var b = await _useCases.AddAsync(key, Form("بهرام کریمی"), CancellationToken.None);
        Assert.Equal(a.Value, b.Value);
        Assert.Single(await _useCases.ListAsync(true, CancellationToken.None));
    }
}
