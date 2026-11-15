using Treasury.Accounts.Domain.OpeningBalances;
using Treasury.BuildingBlocks.Domain;
using Xunit;

namespace Treasury.Accounts.Domain.Tests;

/// <summary>Opening balances (موجودی اول دوره): Access Form_Accamounts; new-version manual; owner, 14 Nov 2026.</summary>
public sealed class OpeningBalanceTests
{
    private static readonly BalanceAccountFact Bank = new(BalanceAccountKind.BankAccount, Guid.NewGuid(), "IRR", MayBeNegative: true);
    private static readonly BalanceAccountFact DollarBank = new(BalanceAccountKind.BankAccount, Guid.NewGuid(), "USD", MayBeNegative: true);
    private static readonly BalanceAccountFact Desk = new(BalanceAccountKind.CashDesk, Guid.NewGuid(), "IRR", MayBeNegative: false);

    private static FiscalDate D(string jalali) => FiscalDate.ParseJalali(jalali);

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void The_first_balance_may_be_on_the_start_date_in_the_middle_of_the_year()
    {
        var b = OpeningBalance.Enter(Guid.NewGuid(), Bank, 1405, D("1405/08/24"), 1_250_000_000, null);
        Assert.Equal((1405, "1405/08/24", 1_250_000_000m), (b.FiscalYear, b.Date.ToJalaliString(), b.Amount));
        Assert.IsType<OpeningBalanceEntered>(Assert.Single(b.PendingEvents));
    }

    [Fact]
    public void The_date_must_be_in_the_fiscal_year() =>
        Assert.Equal("OpeningBalance.DateOutsideYear", Code(() => OpeningBalance.Enter(Guid.NewGuid(), Bank, 1405, D("1404/12/29"), 1, null)));

    [Fact]
    public void A_bank_account_may_be_overdrawn_but_cash_is_never_below_zero()
    {
        Assert.Equal(-5_000_000m, OpeningBalance.Enter(Guid.NewGuid(), Bank, 1405, D("1405/01/01"), -5_000_000, null).Amount);
        Assert.Equal("OpeningBalance.Negative", Code(() => OpeningBalance.Enter(Guid.NewGuid(), Desk, 1405, D("1405/01/01"), -1, null)));
    }

    [Fact]
    public void A_foreign_currency_balance_carries_its_rial_equivalent_owner_14_nov()
    {
        var b = OpeningBalance.Enter(Guid.NewGuid(), DollarBank, 1405, D("1405/08/24"), 1_500.25m, 1_350_000_000);
        Assert.Equal((1_500.25m, (decimal?)1_350_000_000), (b.Amount, b.RialEquivalent));
        Assert.Equal("OpeningBalance.RialEquivalentRequired", Code(() => OpeningBalance.Enter(Guid.NewGuid(), DollarBank, 1405, D("1405/08/24"), 10, null)));
        Assert.Equal("OpeningBalance.RialEquivalentInvalid", Code(() => OpeningBalance.Enter(Guid.NewGuid(), DollarBank, 1405, D("1405/08/24"), 10, -900_000)));
        Assert.Equal("OpeningBalance.AmountInvalid", Code(() => OpeningBalance.Enter(Guid.NewGuid(), DollarBank, 1405, D("1405/08/24"), 10.123m, 900_000)));
        Assert.Equal("OpeningBalance.RialEquivalentNotNeeded", Code(() => OpeningBalance.Enter(Guid.NewGuid(), Bank, 1405, D("1405/08/24"), 10, 10)));
        Assert.Equal("OpeningBalance.AmountInvalid", Code(() => OpeningBalance.Enter(Guid.NewGuid(), Bank, 1405, D("1405/08/24"), 10.5m, null)));
    }

    [Fact]
    public void A_balance_can_be_corrected_and_removed_owner_14_nov()
    {
        var b = OpeningBalance.Enter(Guid.NewGuid(), Bank, 1406, D("1406/01/01"), 100, null);
        b.MarkSaved(1);
        b.Correct(Bank, D("1406/01/01"), 100, null); // unchanged: nothing
        Assert.Empty(b.PendingEvents);
        b.Correct(Bank, D("1406/01/01"), 250, null);
        Assert.Equal(250m, b.Amount);
        b.Remove();
        Assert.IsType<OpeningBalanceRemoved>(b.PendingEvents[^1]);
        Assert.Equal("OpeningBalance.OtherAccount", Code(() => b.Correct(Desk, D("1406/01/01"), 1, null)));
    }

    [Fact]
    public void The_year_is_a_jalali_year() =>
        Assert.Equal("OpeningBalance.YearInvalid", Code(() => OpeningBalance.Enter(Guid.NewGuid(), Bank, 2026, D("1405/01/01"), 1, null)));
}
