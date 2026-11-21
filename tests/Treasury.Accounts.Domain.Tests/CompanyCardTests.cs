using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Domain.CompanyCards;
using Treasury.BuildingBlocks.Domain;
using Xunit;

namespace Treasury.Accounts.Domain.Tests;

/// <summary>
/// The company's bank cards without a bank account: owner's report no. 6, item 12 («کارت بانک خاص شرکت که فاقد حساب بانکی است») and his
/// answer to Q69 (20 Nov 2026): card number, name on the card, معین and تفصیل.
/// </summary>
public sealed class CompanyCardTests
{
    private const string Card = "6037991234567893";
    private static readonly Guid Bank = Guid.NewGuid();

    private static readonly LedgerFacts Ledger = new(
        new Dictionary<string, LedgerAccountFact>
        {
            ["1105"] = new("1105", true, true, new HashSet<string> { "بانكها" }, new HashSet<string>()),
            ["1106"] = new("1106", true, true, new HashSet<string>(), new HashSet<string>()),
            ["11"] = new("11", true, false, new HashSet<string>(), new HashSet<string>()),
            ["1199"] = new("1199", false, true, new HashSet<string>(), new HashSet<string>()),
        },
        new Dictionary<string, DetailFact>
        {
            ["100000"] = new("100000", "بانكها", true),
            ["100099"] = new("100099", "بانكها", false),
            ["111078"] = new("111078", "اشخاص وشركتها", true, "Staff"),
        });

    private static CompanyCardData Data(string number = Card, string holder = "شرکت نمونه", string account = "1105", string? detail = "100000", Guid? bank = null) =>
        new(number, holder, bank ?? Bank, account, detail);

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void A_company_card_has_its_number_the_name_on_it_its_bank_and_its_account()
    {
        var c = CompanyCard.Register(Guid.NewGuid(), Data(number: "۶۰۳۷-۹۹۱۲-۳۴۵۶-۷۸۹۳", holder: "  شرکت   نمونه "), Ledger);
        Assert.Equal((Card, "شرکت نمونه", Bank, "1105", "100000"), (c.Data.CardNumber, c.Data.HolderName, c.Data.BankId, c.Data.LedgerAccountCode, c.Data.DetailCode));
        Assert.True(c.IsActive);
        Assert.IsType<CompanyCardChanged>(Assert.Single(c.PendingEvents));
    }

    [Fact]
    public void A_wrong_length_and_a_wrong_check_digit_are_told_apart_report_6_item_17()
    {
        Assert.Equal("CompanyCard.CardNumberInvalid", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(number: "603799123456789"), Ledger)));
        Assert.Equal("CompanyCard.CardCheckDigit", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(number: "6037991234567890"), Ledger)));
    }

    [Fact]
    public void The_name_the_bank_the_account_and_its_detail_are_checked()
    {
        Assert.Equal("CompanyCard.HolderRequired", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(holder: " "), Ledger)));
        Assert.Equal("CompanyCard.BankRequired", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(bank: Guid.Empty), Ledger)));
        Assert.Equal("CompanyCard.LedgerAccountRequired", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(account: ""), Ledger)));
        Assert.Equal("CompanyCard.LedgerAccountNotFound", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(account: "9999"), Ledger)));
        Assert.Equal("CompanyCard.LedgerAccountNotPostable", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(account: "11"), Ledger)));
        Assert.Equal("CompanyCard.LedgerAccountInactive", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(account: "1199", detail: null), Ledger)));

        // An account that lists تفصیل types needs a تفصیل of one of them (owner, 10 Nov 2026); one that lists none takes any or none.
        Assert.Equal("CompanyCard.DetailRequired", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(detail: null), Ledger)));
        Assert.Equal("CompanyCard.DetailNotFound", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(detail: "555555"), Ledger)));
        Assert.Equal("CompanyCard.DetailInactive", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(detail: "100099"), Ledger)));
        Assert.Equal("CompanyCard.DetailTypeNotAllowed", Code(() => CompanyCard.Register(Guid.NewGuid(), Data(detail: "111078"), Ledger)));
        Assert.Null(CompanyCard.Register(Guid.NewGuid(), Data(account: "1106", detail: null), Ledger).Data.DetailCode);
    }

    [Fact]
    public void A_card_is_corrected_and_switched_off_and_on()
    {
        var c = CompanyCard.Register(Guid.NewGuid(), Data(), Ledger);
        c.MarkSaved(1);
        c.Change(Data(), Ledger);
        Assert.Empty(c.PendingEvents); // nothing changed, nothing announced
        c.Change(Data(holder: "شرکت نمونه – فروش", account: "1106", detail: "111078"), Ledger);
        Assert.Equal(("شرکت نمونه – فروش", "1106", "111078"), (c.Data.HolderName, c.Data.LedgerAccountCode, c.Data.DetailCode));
        c.Deactivate();
        Assert.False(c.IsActive);
        c.Activate();
        Assert.True(c.IsActive);
        Assert.Equal(3, c.PendingEvents.Count);
    }
}
