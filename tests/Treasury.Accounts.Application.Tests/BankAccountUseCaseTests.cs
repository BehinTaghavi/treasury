using Treasury.Accounts.Application.BankAccounts;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Accounts.Application.Tests;

public sealed class BankAccountUseCaseTests : IDisposable
{
    private readonly InMemoryAccountsDatabase _db = new();
    private readonly FakeReference _reference = new();
    private readonly BankAccountUseCases _useCases;

    public BankAccountUseCaseTests() =>
        _useCases = new BankAccountUseCases(new InMemoryUnitOfWork(_db), new InMemoryBankAccountRepository(_db), _reference, new InMemoryIdempotencyStore(_db));

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static BankAccountForm Form(string number = "0101-123-456", Guid? bank = null, string? d1 = "100000", bool foreign = false, string? currency = null, int? priority = null) =>
        new(number, bank ?? FakeReference.Mellat, "شعبه بازار", "1234", FakeReference.Tehran, "1400/01/15", foreign, currency,
            "1103", d1, null, "130100", "130101", priority, IsFacility: false);

    private async Task<Guid> Register(BankAccountForm form, Guid? main = null)
    {
        var r = await _useCases.RegisterAsync(new RegisterBankAccountCommand(Key(), form, main), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<BankAccountView> View(Guid id) => (await _useCases.ListAsync(true, CancellationToken.None)).Single(v => v.Id == id);

    [Fact]
    public async Task A_bank_account_is_saved_with_its_ledger_accounts_and_one_event()
    {
        var id = await Register(Form());
        var v = await View(id);
        Assert.Equal(("0101-123-456", "IRR", "1103", "100000", "130100", "130101", "1400/01/15"),
            (v.AccountNumber, v.CurrencyCode, v.LedgerAccountCode, v.Detail1Code, v.CollectionAccountCode, v.BouncedAccountCode, v.OpeningDate));
        Assert.Equal(nameof(BankAccountChanged), Assert.Single(_db.OutboxMessages).Type);
    }

    [Fact]
    public async Task The_same_number_is_refused_even_written_differently()
    {
        await Register(Form());
        var r = await _useCases.RegisterAsync(new(Key(), Form(number: "۰۱۰۱ ۱۲۳ ۴۵۶")), CancellationToken.None);
        Assert.Equal("BankAccount.NumberTaken", r.ErrorCode);
    }

    [Theory]
    [InlineData("bank", "BankAccount.BankNotFound")]
    [InlineData("closed", "BankAccount.BankInactive")]
    [InlineData("type", "BankAccount.DetailTypeNotAllowed")]
    [InlineData("currency", "BankAccount.CurrencyNotFound")]
    [InlineData("rial", "BankAccount.ForeignCurrencyRequired")]
    [InlineData("date", "FiscalDate.Invalid")]
    public async Task Each_wrong_field_is_reported_with_its_code(string field, string code)
    {
        var form = field switch
        {
            "bank" => Form(bank: Guid.NewGuid()),
            "closed" => Form(bank: FakeReference.ClosedBank),
            "type" => Form(d1: "111078"),
            "currency" => Form(foreign: true, currency: "GBP"),
            "rial" => Form(foreign: true, currency: "IRR"),
            _ => Form() with { OpeningDate = "1400/13/40" },
        };
        Assert.Equal(code, (await _useCases.RegisterAsync(new(Key(), form), CancellationToken.None)).ErrorCode);
        Assert.Empty(_db.OutboxMessages);
    }

    [Fact]
    public async Task A_foreign_currency_account_keeps_its_currency()
    {
        var id = await Register(Form(foreign: true, currency: "usd"));
        Assert.Equal("USD", (await View(id)).CurrencyCode);
    }

    [Fact]
    public async Task A_main_account_takes_one_backup_account()
    {
        var main = await Register(Form());
        var backup = await Register(Form(number: "0101-999", d1: "100001"), main);
        Assert.Equal(main, (await View(backup)).MainAccountId);
        var second = await _useCases.RegisterAsync(new(Key(), Form(number: "0101-998", d1: "100001"), main), CancellationToken.None);
        Assert.Equal("BankAccount.MainAccountHasBackup", second.ErrorCode);
        var unknown = await _useCases.RegisterAsync(new(Key(), Form(number: "0101-997"), Guid.NewGuid()), CancellationToken.None);
        Assert.Equal("BankAccount.MainAccountNotFound", unknown.ErrorCode);
    }

    [Fact]
    public async Task Accounts_with_collection_priority_come_first_and_switched_off_ones_leave_the_form_lists()
    {
        var plain = await Register(Form(number: "100"));
        var priority = await Register(Form(number: "200", priority: 1));
        var third = await Register(Form(number: "050", priority: 3));
        Assert.Equal([priority, third, plain], (await _useCases.ListAsync(false, CancellationToken.None)).Select(v => v.Id));
        Assert.True((await _useCases.SetActiveAsync(new(Key(), third, false, 1), CancellationToken.None)).IsSuccess);

        Assert.True((await _useCases.SetActiveAsync(new(Key(), plain, false, 1), CancellationToken.None)).IsSuccess);
        Assert.Equal([priority], (await _useCases.ListAsync(false, CancellationToken.None)).Select(v => v.Id));
        Assert.False((await View(plain)).IsActive);
    }

    [Fact]
    public async Task Editing_checks_the_version_the_user_saw_and_the_number_again()
    {
        var a = await Register(Form(number: "100"));
        await Register(Form(number: "200"));
        Assert.Equal("BankAccount.NumberTaken", (await _useCases.ChangeAsync(new(Key(), a, Form(number: "200"), 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangeAsync(new(Key(), a, Form(number: "100") with { BranchName = "شعبه جدید" }, 1), CancellationToken.None)).IsSuccess);
        Assert.Equal("Concurrency.Conflict", (await _useCases.ChangeAsync(new(Key(), a, Form(number: "100"), 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("شعبه جدید", (await View(a)).BranchName);
    }

    [Fact]
    public async Task Ticking_backup_without_a_main_account_saves_nothing_owner_item_10()
    {
        var r = await _useCases.RegisterAsync(new(Key(), Form(), MainAccountId: null, IsBackup: true), CancellationToken.None);
        Assert.Equal("BankAccount.MainAccountRequired", r.ErrorCode);
        Assert.Empty(_db.OutboxMessages);
    }

    [Fact]
    public async Task The_bank_city_and_foreign_currency_are_marked_in_use_in_reference_svc_so_they_cannot_be_deleted()
    {
        await Register(Form(foreign: true, currency: "USD"));
        Assert.Equal(
            [("banks", FakeReference.Mellat), ("cities", FakeReference.Tehran), ("currencies", FakeReference.CurrencyIds["USD"])],
            _reference.MarkedInUse);
    }

    [Fact]
    public async Task A_refused_form_marks_nothing_in_use()
    {
        await _useCases.RegisterAsync(new(Key(), Form(d1: "111078")), CancellationToken.None);
        Assert.Empty(_reference.MarkedInUse);
    }

    [Fact]
    public async Task When_reference_svc_cannot_answer_nothing_is_saved()
    {
        _reference.Down = true;
        Assert.Equal("Reference.Unavailable", (await _useCases.RegisterAsync(new(Key(), Form()), CancellationToken.None)).ErrorCode);
        Assert.Empty(_db.OutboxMessages);
    }

    [Fact]
    public async Task An_unused_account_is_deleted_a_used_one_and_a_main_with_a_backup_are_not_owner_report_3()
    {
        var free = await Register(Form(number: "0101-900"));
        Assert.True((await _useCases.DeleteAsync(new(Key(), free, 1), CancellationToken.None)).IsSuccess);
        Assert.DoesNotContain(await _useCases.ListAsync(true, CancellationToken.None), a => a.Id == free);

        var used = await Register(Form(number: "0101-901"));
        Assert.True((await _useCases.MarkInUseAsync(Key(), used, CancellationToken.None)).IsSuccess);
        var view = (await _useCases.ListAsync(true, CancellationToken.None)).Single(a => a.Id == used);
        Assert.True(view.IsInUse);
        Assert.Equal("BankAccount.InUse", (await _useCases.DeleteAsync(new(Key(), used, view.Version), CancellationToken.None)).ErrorCode);

        var main = await Register(Form(number: "0101-902"));
        await Register(Form(number: "0101-903", d1: "100001"), main);
        Assert.Equal("BankAccount.HasBackup", (await _useCases.DeleteAsync(new(Key(), main, 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_bank_card_belongs_to_one_account_only_report_4_item_2()
    {
        BankAccountForm WithCard(string number) => Form(number) with { Cards = [new BankCard("6037991234567893", "شرکت نمونه")] };
        var first = await _useCases.RegisterAsync(new(Guid.NewGuid().ToString(), WithCard("0101-111")), CancellationToken.None);
        Assert.True(first.IsSuccess, first.ErrorCode);
        Assert.Equal("BankAccount.CardTaken", (await _useCases.RegisterAsync(new(Guid.NewGuid().ToString(), WithCard("0101-222")), CancellationToken.None)).ErrorCode);
        var view = (await _useCases.ListAsync(true, CancellationToken.None)).Single(a => a.Id == first.Value);
        Assert.Equal(("6037991234567893", BankAccountType.CurrentQardHasan, true), (Assert.Single(view.Cards!).Number, view.AccountType, view.HasCheques));
        Assert.True((await _useCases.ChangeAsync(new(Guid.NewGuid().ToString(), first.Value, WithCard("0101-111") with { AccountType = BankAccountType.SavingsQardHasan }, view.Version), CancellationToken.None)).IsSuccess);
        Assert.False((await _useCases.ListAsync(true, CancellationToken.None)).Single(a => a.Id == first.Value).HasCheques);
    }

    [Fact]
    public async Task Report_8_the_signatories_of_an_account_are_checked_with_basic_information_saved_and_marked_in_use()
    {
        var form = Form() with
        {
            RequiredSignatures = 2,
            Signatories = [new(FakeReference.Ceo, SignatureKind.Fixed), new(FakeReference.Chairman, SignatureKind.Variable), new(FakeReference.BoardMember, SignatureKind.Variable)],
        };
        var id = await Register(form);
        var v = await View(id);
        Assert.Equal(2, v.RequiredSignatures);
        Assert.Equal([(FakeReference.Ceo, SignatureKind.Fixed), (FakeReference.Chairman, SignatureKind.Variable), (FakeReference.BoardMember, SignatureKind.Variable)],
            v.Signatories!.Select(s => (s.SignatoryId, s.Kind)));
        // reference-svc is told, so a signatory named by an account is no longer deleted there.
        Assert.Equal([FakeReference.Ceo, FakeReference.Chairman, FakeReference.BoardMember], _reference.MarkedInUse.Where(m => m.List == "signatories").Select(m => m.Id));

        // Another account of the company has its own signatories.
        var other = await Register(Form(number: "0202-999-1") with { RequiredSignatures = 1, Signatories = [new(FakeReference.Chairman, SignatureKind.Fixed)] });
        Assert.Equal(FakeReference.Chairman, Assert.Single((await View(other)).Signatories!).SignatoryId);
        Assert.Equal(3, (await View(id)).Signatories!.Count);

        // Corrected: one signature, either of two.
        var changed = await _useCases.ChangeAsync(new(Key(), id, form with { RequiredSignatures = 1, Signatories = [new(FakeReference.Ceo, SignatureKind.Variable), new(FakeReference.Chairman, SignatureKind.Variable)] }, v.Version), CancellationToken.None);
        Assert.True(changed.IsSuccess, changed.ErrorCode);
        Assert.Equal((1, 2), ((await View(id)).RequiredSignatures, (await View(id)).Signatories!.Count));
    }

    [Theory]
    [InlineData("unknown", "BankAccount.SignatoryNotFound")]
    [InlineData("notes", "BankAccount.SignatoryNotForCheques")]
    [InlineData("retired", "BankAccount.SignatoryInactive")]
    [InlineData("fixed", "BankAccount.TooManyFixedSignatories")]
    [InlineData("few", "BankAccount.TooFewSignatories")]
    [InlineData("count", "BankAccount.SignatureCountInvalid")]
    public async Task Report_8_each_fault_in_the_signatories_is_reported_with_its_code(string fault, string code)
    {
        var form = fault switch
        {
            "unknown" => Form() with { RequiredSignatures = 1, Signatories = [new(Guid.NewGuid(), SignatureKind.Fixed)] },
            "notes" => Form() with { RequiredSignatures = 1, Signatories = [new(FakeReference.NotesOnly, SignatureKind.Fixed)] },
            "retired" => Form() with { RequiredSignatures = 1, Signatories = [new(FakeReference.Retired, SignatureKind.Fixed)] },
            "fixed" => Form() with { RequiredSignatures = 1, Signatories = [new(FakeReference.Ceo, SignatureKind.Fixed), new(FakeReference.Chairman, SignatureKind.Fixed)] },
            "few" => Form() with { RequiredSignatures = 2, Signatories = [new(FakeReference.Ceo, SignatureKind.Fixed)] },
            _ => Form() with { RequiredSignatures = 12, Signatories = [new(FakeReference.Ceo, SignatureKind.Fixed)] },
        };
        Assert.Equal(code, (await _useCases.RegisterAsync(new(Key(), form), CancellationToken.None)).ErrorCode);
        Assert.Empty(await _useCases.ListAsync(true, CancellationToken.None));
    }
}
