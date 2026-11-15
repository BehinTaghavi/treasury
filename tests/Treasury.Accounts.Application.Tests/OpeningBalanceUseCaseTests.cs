using Treasury.Accounts.Application.BankAccounts;
using Treasury.Accounts.Application.CashDesks;
using Treasury.Accounts.Application.OpeningBalances;
using Treasury.Accounts.Domain.OpeningBalances;
using Treasury.Accounts.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Accounts.Application.Tests;

/// <summary>Opening balances of bank accounts, cash desks and petty cash (owner, 14 Nov 2026).</summary>
public sealed class OpeningBalanceUseCaseTests : IDisposable
{
    private readonly InMemoryAccountsDatabase _db = new();
    private readonly FakeReference _reference = new();
    private readonly OpeningBalanceUseCases _balances;
    private readonly BankAccountUseCases _banks;
    private readonly CashDeskUseCases _desks;

    public OpeningBalanceUseCaseTests()
    {
        var uow = new InMemoryUnitOfWork(_db);
        var idem = new InMemoryIdempotencyStore(_db);
        var banks = new InMemoryBankAccountRepository(_db);
        var balances = new InMemoryOpeningBalanceRepository(_db);
        _balances = new OpeningBalanceUseCases(uow, balances, banks, new InMemoryCashDeskRepository(_db), new InMemoryPettyCashRepository(_db), idem);
        _banks = new BankAccountUseCases(uow, banks, _reference, idem, balances);
        _desks = new CashDeskUseCases(uow, new InMemoryCashDeskRepository(_db), _reference, idem);
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private async Task<Guid> Bank(string number = "0101-500", bool foreign = false)
    {
        var r = await _banks.RegisterAsync(new(Key(), new BankAccountForm(number, FakeReference.Mellat, "شعبه بازار", "1234", FakeReference.Tehran, "1400/01/15",
            foreign, foreign ? "USD" : null, "1103", "100000", null, "130100", "130101", null, IsFacility: false)), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    [Fact]
    public async Task A_balance_entered_in_the_first_year_is_carried_to_later_years_until_corrected()
    {
        var bank = await Bank();
        var entered = await _balances.EnterAsync(new(Key(), BalanceAccountKind.BankAccount, bank, 1405, "1405/08/24", 900_000_000, null), CancellationToken.None);
        Assert.True(entered.IsSuccess, entered.ErrorCode);

        var y1405 = Assert.Single(await _balances.ListAsync(1405, CancellationToken.None));
        Assert.Equal(("1405/08/24", 900_000_000m), (y1405.Entry!.Date, y1405.Entry.Amount));
        var y1406 = Assert.Single(await _balances.ListAsync(1406, CancellationToken.None));
        Assert.Equal((1405, 900_000_000m), (y1406.Carried!.FromYear, y1406.Carried.Amount));
        Assert.Null(y1406.Entry);
        Assert.Empty(await _balances.ListAsync(1404, CancellationToken.None));

        // A correction for 1406 wins over the carried balance; removing it brings the carried one back.
        var corrected = await _balances.EnterAsync(new(Key(), BalanceAccountKind.BankAccount, bank, 1406, "1406/01/01", 950_000_000, null), CancellationToken.None);
        Assert.Equal(950_000_000m, Assert.Single(await _balances.ListAsync(1406, CancellationToken.None)).Entry!.Amount);
        Assert.True((await _balances.RemoveAsync(new(Key(), corrected.Value, 1), CancellationToken.None)).IsSuccess);
        Assert.NotNull(Assert.Single(await _balances.ListAsync(1406, CancellationToken.None)).Carried);
    }

    [Fact]
    public async Task Entering_again_corrects_the_same_entry_and_a_stale_version_is_refused()
    {
        var bank = await Bank();
        var id = (await _balances.EnterAsync(new(Key(), BalanceAccountKind.BankAccount, bank, 1405, "1405/08/24", 1, null), CancellationToken.None)).Value;
        Assert.Equal(id, (await _balances.EnterAsync(new(Key(), BalanceAccountKind.BankAccount, bank, 1405, "1405/08/24", 2, null, 1), CancellationToken.None)).Value);
        Assert.Equal("Concurrency.Conflict", (await _balances.EnterAsync(new(Key(), BalanceAccountKind.BankAccount, bank, 1405, "1405/08/24", 3, null, 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("Concurrency.Conflict", (await _balances.EnterAsync(new(Key(), BalanceAccountKind.BankAccount, bank, 1405, "1405/08/24", 3, null), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Foreign_currency_cash_desks_and_unknown_accounts()
    {
        var dollars = await Bank("0101-501", foreign: true);
        Assert.Equal("OpeningBalance.RialEquivalentRequired", (await _balances.EnterAsync(new(Key(), BalanceAccountKind.BankAccount, dollars, 1405, "1405/08/24", 100, null), CancellationToken.None)).ErrorCode);
        Assert.True((await _balances.EnterAsync(new(Key(), BalanceAccountKind.BankAccount, dollars, 1405, "1405/08/24", 100, 90_000_000), CancellationToken.None)).IsSuccess);

        var desk = (await _desks.RegisterAsync(new(Key(), new CashDeskForm("صندوق مرکزی", "130100", "111078", null, false, null, "1405/08/22")), CancellationToken.None)).Value;
        Assert.Equal("OpeningBalance.Negative", (await _balances.EnterAsync(new(Key(), BalanceAccountKind.CashDesk, desk, 1405, "1405/08/24", -1, null), CancellationToken.None)).ErrorCode);
        Assert.True((await _balances.EnterAsync(new(Key(), BalanceAccountKind.CashDesk, desk, 1405, "1405/08/24", 15_000_000, null), CancellationToken.None)).IsSuccess);
        Assert.Equal("OpeningBalance.AccountNotFound", (await _balances.EnterAsync(new(Key(), BalanceAccountKind.PettyCash, Guid.NewGuid(), 1405, "1405/08/24", 1, null), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_bank_account_with_an_opening_balance_is_never_deleted_owner_14_nov()
    {
        var bank = await Bank();
        await _balances.EnterAsync(new(Key(), BalanceAccountKind.BankAccount, bank, 1405, "1405/08/24", 1, null), CancellationToken.None);
        Assert.Equal("BankAccount.InUse", (await _banks.DeleteAsync(new(Key(), bank, 1), CancellationToken.None)).ErrorCode);
    }
}
