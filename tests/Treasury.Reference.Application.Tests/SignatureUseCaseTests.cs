using Treasury.Reference.Application.Ledger;
using Treasury.Reference.Application.Signatures;
using Treasury.Reference.Domain.Ledger;
using Treasury.Reference.Domain.Signatures;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

/// <summary>«دارندگان امضای مجاز» and the signature rules (owner, 16 Nov 2026, answers to QN9–QN11).</summary>
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

    private async Task<Guid> Add(string name, string position = "عضو هیئت مدیره", string? national = null)
    {
        var r = await _useCases.AddSignatoryAsync(Key(), new SignatoryForm(name, position, NationalCode: national), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<SignatoryView> Get(Guid id) => (await _useCases.ListSignatoriesAsync(true, CancellationToken.None)).Single(s => s.Id == id);

    [Fact]
    public async Task Signatories_are_listed_by_name_with_all_their_particulars()
    {
        var r = await _useCases.AddSignatoryAsync(Key(), new SignatoryForm(null, "مدیر عامل", "600046", "0123456789", "09121234567", "021-88990011"), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        await Add("بهرام کریمی", "مدیر مالی");
        var list = await _useCases.ListSignatoriesAsync(false, CancellationToken.None);
        Assert.Equal(["بهرام کریمی", "علی رضایی"], list.Select(s => s.FullName));
        var s = list[1];
        Assert.Equal(("مدیر عامل", "600046", "0123456789", "09121234567", "021-88990011", true, false, 1L),
            (s.Position, s.DetailCode, s.NationalCode, s.Mobile, s.Phone, s.IsActive, s.IsInRule, s.Version));
    }

    [Fact]
    public async Task The_same_name_or_national_code_is_not_entered_twice_and_details_are_checked()
    {
        await Add("علی رضایی", national: "0123456789");
        Assert.Equal("Signatory.NameTaken", (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("علي رضايي", "مدیر"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.NationalCodeTaken", (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("دیگری", "مدیر", NationalCode: "0123456789"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.DetailNotFound", (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("دیگری", "مدیر", "999999"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.DetailInactive", (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("دیگری", "مدیر", "600047"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.PositionRequired", (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("دیگری", " "), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_signatory_is_corrected_with_its_version_switched_off_and_deleted()
    {
        var id = await Add("علی رضایی");
        Assert.True((await _useCases.EditSignatoryAsync(Key(), id, new SignatoryForm("علی رضایی", "مدیر عامل", Mobile: "09121234567"), 1, CancellationToken.None)).IsSuccess);
        Assert.Equal(("مدیر عامل", "09121234567", 2L), ((await Get(id)).Position, (await Get(id)).Mobile, (await Get(id)).Version));
        Assert.Equal("Concurrency.Conflict", (await _useCases.EditSignatoryAsync(Key(), id, new SignatoryForm("علی رضایی", "مدیر"), 1, CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.NotFound", (await _useCases.EditSignatoryAsync(Key(), Guid.NewGuid(), new SignatoryForm("الف", "ب"), 1, CancellationToken.None)).ErrorCode);

        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), id, "deactivate", 2), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListSignatoriesAsync(false, CancellationToken.None));
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), id, "activate", 3), CancellationToken.None)).IsSuccess);
        Assert.Equal("Request.UnknownChange", (await _useCases.ChangeSignatoryAsync(new(Key(), id, "retire", 4), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), id, "delete", 4), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListSignatoriesAsync(true, CancellationToken.None));
    }

    [Fact]
    public async Task Each_kind_of_document_has_its_rule_and_a_kind_without_one_needs_no_signature()
    {
        var ceo = await Add("علی رضایی", "مدیر عامل");
        var cfo = await Add("بهرام کریمی", "مدیر مالی");
        var board = await Add("سارا احمدی");

        var rules = await _useCases.ListRulesAsync(CancellationToken.None);
        Assert.Equal(["IssuedCheque", "IssuedNote", "ChequeReceipt", "NoteReceipt", "ChequePaymentSheet", "NotePaymentSheet"], rules.Select(r => r.Document));
        Assert.All(rules, r => Assert.Equal((0, 0L), (r.Required, r.Version)));

        var set = await _useCases.SetRuleAsync(Key(), SignedDocument.NoteReceipt, new SignatureRuleForm(2, [ceo], [cfo, board], 0), CancellationToken.None);
        Assert.True(set.IsSuccess, set.ErrorCode);
        var rule = await _useCases.GetRuleAsync(SignedDocument.NoteReceipt, CancellationToken.None);
        Assert.Equal((2, 1L), (rule.Required, rule.Version));
        Assert.Equal(("علی رضایی", "مدیر عامل"), (Assert.Single(rule.Compulsory).FullName, rule.Compulsory[0].Position));
        Assert.Equal([cfo, board], rule.Optional.Select(s => s.Id));
        Assert.Equal(0, (await _useCases.GetRuleAsync(SignedDocument.IssuedCheque, CancellationToken.None)).Required);
        Assert.True((await Get(ceo)).IsInRule);

        // Changed with its version; an old version is refused.
        Assert.Equal("Concurrency.Conflict", (await _useCases.SetRuleAsync(Key(), SignedDocument.NoteReceipt, new SignatureRuleForm(1, [ceo], [], 0), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.SetRuleAsync(Key(), SignedDocument.NoteReceipt, new SignatureRuleForm(1, [ceo], [], 1), CancellationToken.None)).IsSuccess);
        Assert.Empty((await _useCases.GetRuleAsync(SignedDocument.NoteReceipt, CancellationToken.None)).Optional);
        Assert.False((await Get(board)).IsInRule);
    }

    [Fact]
    public async Task A_rule_is_checked_and_its_signatories_stay_on_the_list()
    {
        var ceo = await Add("علی رضایی", "مدیر عامل");
        var cfo = await Add("بهرام کریمی", "مدیر مالی");
        async Task<string?> Code(int required, Guid[] compulsory, Guid[] optional) =>
            (await _useCases.SetRuleAsync(Key(), SignedDocument.IssuedCheque, new SignatureRuleForm(required, compulsory, optional, 0), CancellationToken.None)).ErrorCode;
        Assert.Equal("SignatureRule.CountTooSmall", await Code(1, [ceo, cfo], []));
        Assert.Equal("SignatureRule.CountTooLarge", await Code(3, [ceo], [cfo]));
        Assert.Equal("SignatureRule.SignatoryTwice", await Code(1, [ceo], [ceo]));
        Assert.Equal("SignatureRule.SignatoryNotFound", await Code(1, [Guid.NewGuid()], []));
        Assert.Null(await Code(2, [ceo, cfo], []));

        Assert.Equal("Signatory.InRule", (await _useCases.ChangeSignatoryAsync(new(Key(), cfo, "deactivate", 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.InRule", (await _useCases.ChangeSignatoryAsync(new(Key(), cfo, "delete", 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.SetRuleAsync(Key(), SignedDocument.IssuedCheque, new SignatureRuleForm(1, [ceo], [], 1), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), cfo, "deactivate", 1), CancellationToken.None)).IsSuccess);
        Assert.Equal("SignatureRule.SignatoryInactive", (await _useCases.SetRuleAsync(Key(), SignedDocument.IssuedCheque, new SignatureRuleForm(2, [ceo], [cfo], 2), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_same_request_twice_adds_one_signatory()
    {
        var key = Key();
        var a = await _useCases.AddSignatoryAsync(key, new SignatoryForm("علی رضایی", "مدیر عامل"), CancellationToken.None);
        var b = await _useCases.AddSignatoryAsync(key, new SignatoryForm("علی رضایی", "مدیر عامل"), CancellationToken.None);
        Assert.Equal(a.Value, b.Value);
        Assert.Single(await _useCases.ListSignatoriesAsync(true, CancellationToken.None));
    }
}
