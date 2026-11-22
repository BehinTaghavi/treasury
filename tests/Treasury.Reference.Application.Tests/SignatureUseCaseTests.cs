using Treasury.Reference.Application.Ledger;
using Treasury.Reference.Application.Signatures;
using Treasury.Reference.Domain.Ledger;
using Treasury.Reference.Domain.Signatures;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

/// <summary>«دارندگان امضای مجاز (اسناد بهادار)» (owner, 16 Nov 2026, answers to Q41 and Q45; his report no. 8 of 21 Nov 2026).</summary>
public sealed class SignatureUseCaseTests : IDisposable
{
    private readonly InMemoryReferenceDatabase _db = new();
    private readonly SignatureUseCases _useCases;

    public SignatureUseCaseTests()
    {
        var uow = new InMemoryUnitOfWork(_db);
        var idempotency = new InMemoryIdempotencyStore(_db);
        var ledger = new InMemoryLedgerRepository(_db);
        _useCases = new SignatureUseCases(uow, new InMemorySignatureRepository(_db), ledger, idempotency);
        var ledgerUseCases = new LedgerUseCases(uow, ledger, ledger, idempotency);
        LedgerFixture.Load(ledgerUseCases);
        DetailRow[] details = [new(1, "600046", "علی رضایی", "كاركنان كارخانه", true, false), new(2, "600047", "کارمند قدیمی", "كاركنان كارخانه", false, false)];
        Assert.True(ledgerUseCases.ImportDetailsAsync("fixture-details", details, CancellationToken.None).GetAwaiter().GetResult().IsSuccess);
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private static SignatoryForm Form(string? name, SignatoryPosition? position = SignatoryPosition.ExecutiveBoardMember, string? national = null, string? detail = null) =>
        new(name, position, NationalCode: national, DetailCode: detail);

    private async Task<Guid> Add(string name, SignatoryPosition position = SignatoryPosition.ExecutiveBoardMember, string? national = null)
    {
        var r = await _useCases.AddSignatoryAsync(Key(), Form(name, position, national), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<SignatoryView> Get(Guid id) => (await _useCases.ListSignatoriesAsync(true, CancellationToken.None)).Single(s => s.Id == id);

    [Fact]
    public async Task Signatories_are_listed_by_name_with_all_their_particulars()
    {
        var r = await _useCases.AddSignatoryAsync(Key(),
            new SignatoryForm(null, SignatoryPosition.ManagingDirector, null, "600046", "0123456789", "09121234567", "021-88990011", "214", "rezaei@nazho.example", true, true),
            CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        await Add("بهرام کریمی", SignatoryPosition.NonBoardManager);
        var list = await _useCases.ListSignatoriesAsync(false, CancellationToken.None);
        Assert.Equal(["بهرام کریمی", "علی رضایی"], list.Select(s => s.FullName));
        var s = list[1];
        Assert.Equal(("ManagingDirector", "مدیر عامل", "600046", "0123456789", "09121234567", "021-88990011", "214", "rezaei@nazho.example"),
            (s.PositionKind, s.Position, s.DetailCode, s.NationalCode, s.Mobile, s.Phone, s.Extension, s.Email));
        Assert.Equal((true, true, false, true, false, 1L), (s.SignsCheques, s.SignsNotes, s.HasSignature, s.IsActive, s.IsInUse, s.Version));
        Assert.Equal("مدیر و مسئول غیر عضو هیئت مدیره", list[0].Position);
        Assert.Equal(s, await _useCases.GetSignatoryAsync(r.Value, CancellationToken.None));
        Assert.Null(await _useCases.GetSignatoryAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task The_same_name_or_national_code_is_not_entered_twice_and_details_are_checked()
    {
        await Add("علی رضایی", national: "0123456789");
        Assert.Equal("Signatory.NameTaken", (await _useCases.AddSignatoryAsync(Key(), Form("علي رضايي"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.NationalCodeTaken", (await _useCases.AddSignatoryAsync(Key(), Form("دیگری", national: "0123456789"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Person.DetailNotFound", (await _useCases.AddSignatoryAsync(Key(), Form("دیگری", detail: "999999"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Person.DetailInactive", (await _useCases.AddSignatoryAsync(Key(), Form("دیگری", detail: "600047"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.PositionRequired", (await _useCases.AddSignatoryAsync(Key(), Form("دیگری", null), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.OtherPositionRequired", (await _useCases.AddSignatoryAsync(Key(), Form("دیگری", SignatoryPosition.Other), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.DocumentRequired",
            (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("دیگری", SignatoryPosition.Chairman, SignsCheques: false, SignsNotes: false), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_signatory_is_corrected_with_its_version_switched_off_and_deleted()
    {
        var id = await Add("علی رضایی");
        var corrected = new SignatoryForm("علی رضایی", SignatoryPosition.Other, "مدیر مالی", Mobile: "09121234567", SignsCheques: false, SignsNotes: true);
        Assert.True((await _useCases.EditSignatoryAsync(Key(), id, corrected, 1, CancellationToken.None)).IsSuccess);
        var s = await Get(id);
        Assert.Equal(("Other", "مدیر مالی", "09121234567", false, true, 2L), (s.PositionKind, s.Position, s.Mobile, s.SignsCheques, s.SignsNotes, s.Version));
        Assert.Equal("Concurrency.Conflict", (await _useCases.EditSignatoryAsync(Key(), id, Form("علی رضایی"), 1, CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.NotFound", (await _useCases.EditSignatoryAsync(Key(), Guid.NewGuid(), Form("الف"), 1, CancellationToken.None)).ErrorCode);

        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), id, "deactivate", 2), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListSignatoriesAsync(false, CancellationToken.None));
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), id, "activate", 3), CancellationToken.None)).IsSuccess);
        Assert.Equal("Request.UnknownChange", (await _useCases.ChangeSignatoryAsync(new(Key(), id, "retire", 4), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), id, "delete", 4), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListSignatoriesAsync(true, CancellationToken.None));
    }

    [Fact]
    public async Task The_scan_of_the_signature_is_stored_replaced_and_removed()
    {
        var id = await Add("علی رضایی");
        Assert.Null(await _useCases.GetSignatureAsync(id, CancellationToken.None));
        Assert.True((await _useCases.AttachSignatureAsync(Key(), id, Png, 1, CancellationToken.None)).IsSuccess);
        Assert.Equal(("image/png", Png.Length), ((await _useCases.GetSignatureAsync(id, CancellationToken.None))!.Value.ContentType, (await _useCases.GetSignatureAsync(id, CancellationToken.None))!.Value.Data.Length));
        Assert.True((await Get(id)).HasSignature);

        // A correction of the form keeps the scan; a new scan replaces it.
        Assert.True((await _useCases.EditSignatoryAsync(Key(), id, Form("علی رضایی", SignatoryPosition.Chairman), 2, CancellationToken.None)).IsSuccess);
        Assert.Equal(Png, (await _useCases.GetSignatureAsync(id, CancellationToken.None))!.Value.Data);
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9];
        Assert.True((await _useCases.AttachSignatureAsync(Key(), id, jpeg, 3, CancellationToken.None)).IsSuccess);
        Assert.Equal(("image/jpeg", jpeg), ((await _useCases.GetSignatureAsync(id, CancellationToken.None))!.Value.ContentType, (await _useCases.GetSignatureAsync(id, CancellationToken.None))!.Value.Data));

        Assert.Equal("Person.SignatureInvalid", (await _useCases.AttachSignatureAsync(Key(), id, [1, 2, 3], 4, CancellationToken.None)).ErrorCode);
        Assert.Equal("Concurrency.Conflict", (await _useCases.AttachSignatureAsync(Key(), id, Png, 1, CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.RemoveSignatureAsync(Key(), id, 4, CancellationToken.None)).IsSuccess);
        Assert.Null(await _useCases.GetSignatureAsync(id, CancellationToken.None));
        Assert.Equal("Person.NoSignature", (await _useCases.RemoveSignatureAsync(Key(), id, 5, CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_signatory_named_by_a_bank_account_is_marked_in_use_and_no_longer_deleted()
    {
        var id = await Add("علی رضایی");
        Assert.True((await _useCases.MarkInUseAsync($"in-use:{id}", id, CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.MarkInUseAsync(Key(), id, CancellationToken.None)).IsSuccess);
        var s = await Get(id);
        Assert.Equal((true, 2L), (s.IsInUse, s.Version));
        Assert.Equal("Signatory.InUse", (await _useCases.ChangeSignatoryAsync(new(Key(), id, "delete", 2), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.ChequesInUse",
            (await _useCases.EditSignatoryAsync(Key(), id, new SignatoryForm("علی رضایی", SignatoryPosition.Chairman, SignsCheques: false, SignsNotes: true), 2, CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), id, "deactivate", 2), CancellationToken.None)).IsSuccess);
        Assert.Equal("Signatory.NotFound", (await _useCases.MarkInUseAsync(Key(), Guid.NewGuid(), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_same_request_twice_adds_one_signatory()
    {
        var key = Key();
        var a = await _useCases.AddSignatoryAsync(key, Form("علی رضایی"), CancellationToken.None);
        var b = await _useCases.AddSignatoryAsync(key, Form("علی رضایی"), CancellationToken.None);
        Assert.Equal(a.Value, b.Value);
        Assert.Single(await _useCases.ListSignatoriesAsync(true, CancellationToken.None));
    }
}
