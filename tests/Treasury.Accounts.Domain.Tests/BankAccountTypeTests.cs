using Treasury.Accounts.Domain.BankAccounts;
using Treasury.BuildingBlocks.Domain;
using Xunit;

namespace Treasury.Accounts.Domain.Tests;

/// <summary>Kinds of bank account and their bank cards (owner's report no. 4, items 1, 2 and 4).</summary>
public sealed class BankAccountTypeTests
{
    private const string Card1 = "6037991234567893";
    private const string Card2 = "6037991234567885";

    private static readonly LedgerFacts Ledger = new(
        new Dictionary<string, LedgerAccountFact> { ["1103"] = new("1103", true, true, new HashSet<string>(), new HashSet<string>()) },
        new Dictionary<string, DetailFact> { ["100000"] = new("100000", "بانكها", true) });

    private static BankAccountData Data(BankAccountType type, string currency = "IRR", ShortTermPeriod? term = null, params BankCard[] cards) =>
        new("0101-123-456", Guid.NewGuid(), "مرکزی", "123", Guid.NewGuid(), null, currency, new BankLedger("1103", "100000", null, "1103", "1103"), 1, false,
            type, term, cards);

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void Only_a_current_Qard_al_hasan_account_has_cheque_books()
    {
        Assert.True(BankAccountType.CurrentQardHasan.HasCheques());
        Assert.False(BankAccountType.SavingsQardHasan.HasCheques());
        Assert.False(BankAccountType.ShortTermDeposit.HasCheques());
        Assert.False(BankAccountType.LongTermDeposit.HasCheques());
    }

    [Fact]
    public void A_short_term_deposit_is_ordinary_three_or_six_month_and_in_Rial()
    {
        Assert.Equal("BankAccount.TermRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.ShortTermDeposit), Ledger)));
        var a = BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.ShortTermDeposit, term: ShortTermPeriod.SixMonths), Ledger);
        Assert.Equal(ShortTermPeriod.SixMonths, a.Data.Term);
        Assert.Equal("BankAccount.CurrencyNotForType", Code(() => BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.ShortTermDeposit, "USD", ShortTermPeriod.Ordinary), Ledger)));
        Assert.Null(BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.SavingsQardHasan, term: ShortTermPeriod.SixMonths), Ledger).Data.Term);
    }

    [Theory]
    [InlineData(BankAccountType.CurrentQardHasan)]
    [InlineData(BankAccountType.SavingsQardHasan)]
    [InlineData(BankAccountType.LongTermDeposit)]
    public void Qard_al_hasan_and_long_term_accounts_may_be_in_a_foreign_currency(BankAccountType type) =>
        Assert.Equal("USD", BankAccount.Register(Guid.NewGuid(), Data(type, "USD"), Ledger).Data.CurrencyCode);

    [Fact]
    public void An_account_may_have_several_company_bank_cards_except_a_long_term_deposit()
    {
        var a = BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.CurrentQardHasan, cards: [new("6037-9912-3456-7893", " شرکت نمونه "), new(Card2, "علی رضایی")]), Ledger);
        Assert.Equal([Card1, Card2], a.Data.CardList.Select(c => c.Number));
        Assert.Equal("شرکت نمونه", a.Data.CardList[0].HolderName);
        Assert.Single(BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.ShortTermDeposit, term: ShortTermPeriod.Ordinary, cards: [new(Card1, "x")]), Ledger).Data.CardList);
        Assert.Equal("BankAccount.CardsNotForType", Code(() => BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.LongTermDeposit, cards: [new(Card1, "x")]), Ledger)));
    }

    [Fact]
    public void A_card_has_a_valid_16_digit_number_a_holder_and_is_entered_once()
    {
        Assert.Equal("BankAccount.CardNumberInvalid", Code(() => BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.CurrentQardHasan, cards: [new("6037991234567890", "x")]), Ledger)));
        Assert.Equal("BankAccount.CardHolderRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.CurrentQardHasan, cards: [new(Card1, " ")]), Ledger)));
        Assert.Equal("BankAccount.CardTwice", Code(() => BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.CurrentQardHasan, cards: [new(Card1, "x"), new(Card1, "y")]), Ledger)));
    }

    [Fact]
    public void Changing_only_the_cards_is_a_change()
    {
        var a = BankAccount.Register(Guid.NewGuid(), Data(BankAccountType.CurrentQardHasan, cards: [new(Card1, "x")]), Ledger);
        a.MarkSaved(1);
        a.Change(a.Data with { Cards = [new(Card1, "x")] }, Ledger);
        Assert.Empty(a.PendingEvents);
        a.Change(a.Data with { Cards = [new(Card1, "x"), new(Card2, "y")] }, Ledger);
        Assert.Single(a.PendingEvents);
    }
}
