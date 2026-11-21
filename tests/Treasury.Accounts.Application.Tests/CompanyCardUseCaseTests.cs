using Treasury.Accounts.Application.BankAccounts;
using Treasury.Accounts.Application.CompanyCards;
using Treasury.Accounts.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Accounts.Application.Tests;

/// <summary>The company's bank cards without a bank account (owner's report no. 6, item 12; his answer to Q69, 20 Nov 2026).</summary>
public sealed class CompanyCardUseCaseTests : IDisposable
{
    private const string Card = "6037991234567893";
    private const string OtherCard = "6219861034529007";
    private readonly InMemoryAccountsDatabase _db = new();
    private readonly FakeReference _reference = new();
    private readonly CompanyCardUseCases _useCases;
    private readonly BankAccountUseCases _bankAccounts;

    public CompanyCardUseCaseTests()
    {
        var unitOfWork = new InMemoryUnitOfWork(_db);
        var accounts = new InMemoryBankAccountRepository(_db);
        _useCases = new CompanyCardUseCases(unitOfWork, new InMemoryCompanyCardRepository(_db), accounts, _reference, new InMemoryIdempotencyStore(_db));
        _bankAccounts = new BankAccountUseCases(unitOfWork, accounts, _reference, new InMemoryIdempotencyStore(_db));
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    // 1103 takes a تفصیل of type «بانكها»; 130100 takes any or none (FakeReference).
    private static CompanyCardForm Form(string number = Card, string holder = "شرکت نمونه", Guid? bank = null, string account = "1103", string? detail = "100000") =>
        new(number, holder, bank ?? FakeReference.Mellat, account, detail);

    [Fact]
    public async Task A_company_card_is_saved_listed_and_marks_its_bank_in_use()
    {
        var r = await _useCases.RegisterAsync(new(Key(), Form(number: "6037-9912-3456-7893")), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = Assert.Single(await _useCases.ListAsync(false, CancellationToken.None));
        Assert.Equal((Card, "شرکت نمونه", FakeReference.Mellat, "1103", "100000", true), (v.CardNumber, v.HolderName, v.BankId, v.LedgerAccountCode, v.DetailCode, v.IsActive));
        Assert.Contains(("banks", FakeReference.Mellat), _reference.MarkedInUse);
        Assert.Contains(_db.OutboxMessages, m => m.Type == "CompanyCardChanged");
    }

    [Fact]
    public async Task A_card_number_is_entered_once_and_not_when_it_is_on_a_bank_account()
    {
        Assert.True((await _useCases.RegisterAsync(new(Key(), Form()), CancellationToken.None)).IsSuccess);
        Assert.Equal("CompanyCard.NumberTaken", (await _useCases.RegisterAsync(new(Key(), Form(holder: "دیگری")), CancellationToken.None)).ErrorCode);

        // A card of one of the company's bank accounts is entered on that account, not here (report no. 4, items 2 and 4).
        var account = new BankAccountForm("0101-123-456", FakeReference.Mellat, "شعبه بازار", "1234", FakeReference.Tehran, "1400/01/15", false, null,
            "1103", "100000", null, "130100", "130101", null, IsFacility: false, Cards: [new(OtherCard, "شرکت نمونه")]);
        var saved = await _bankAccounts.RegisterAsync(new(Key(), account), CancellationToken.None);
        Assert.True(saved.IsSuccess, saved.ErrorCode);
        Assert.Equal("CompanyCard.OnBankAccount", (await _useCases.RegisterAsync(new(Key(), Form(number: OtherCard)), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_bank_must_be_an_active_bank_of_the_list()
    {
        Assert.Equal("CompanyCard.BankNotFound", (await _useCases.RegisterAsync(new(Key(), Form(bank: Guid.NewGuid())), CancellationToken.None)).ErrorCode);
        Assert.Equal("CompanyCard.BankNotFound", (await _useCases.RegisterAsync(new(Key(), Form(bank: FakeReference.ClosedBank)), CancellationToken.None)).ErrorCode);
        Assert.Equal("CompanyCard.BankRequired", (await _useCases.RegisterAsync(new(Key(), Form(bank: Guid.Empty)), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_card_is_corrected_with_the_version_the_user_saw_and_switched_off()
    {
        var id = (await _useCases.RegisterAsync(new(Key(), Form()), CancellationToken.None)).Value;
        Assert.True((await _useCases.ChangeAsync(new(Key(), id, Form(holder: "شرکت نمونه – فروش", account: "130100", detail: null), 1), CancellationToken.None)).IsSuccess);
        Assert.Equal("Concurrency.Conflict", (await _useCases.ChangeAsync(new(Key(), id, Form(holder: "x"), 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.SetActiveAsync(new(Key(), id, false, 2), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListAsync(false, CancellationToken.None));
        var v = Assert.Single(await _useCases.ListAsync(true, CancellationToken.None));
        Assert.Equal(("شرکت نمونه – فروش", "130100", (string?)null, false, 3L), (v.HolderName, v.LedgerAccountCode, v.DetailCode, v.IsActive, v.Version));
    }
}
