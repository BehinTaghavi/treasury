using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Domain.CashDesks;
using Treasury.BuildingBlocks.Domain;
using Xunit;

namespace Treasury.Accounts.Domain.Tests;

/// <summary>Cash desks: new-version manual item 10 ("دارندگان صندوق بایستی قبلا در سیستم مالی تعریف شده باشند"); MVC CashDesk.</summary>
public sealed class CashDeskTests
{
    private static readonly LedgerFacts Ledger = new(
        new Dictionary<string, LedgerAccountFact>
        {
            ["1101"] = new("1101", true, true, new HashSet<string> { "اشخاص وشركتها" }, new HashSet<string>()),
            ["11"] = new("11", true, false, new HashSet<string>(), new HashSet<string>()),
            ["1199"] = new("1199", false, true, new HashSet<string>(), new HashSet<string>()),
        },
        new Dictionary<string, DetailFact>
        {
            ["111078"] = new("111078", "اشخاص وشركتها", true),
            ["100000"] = new("100000", "بانكها", true),
            ["111099"] = new("111099", "اشخاص وشركتها", false),
        });

    private static CashDeskData Data(string name = "صندوق مرکزی", string account = "1101", string holder = "111078", string currency = "IRR") =>
        new(name, account, holder, null, currency, new FiscalDate(new DateOnly(2026, 11, 13)));

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void A_cash_desk_has_a_name_a_postable_account_and_its_holder()
    {
        var d = CashDesk.Register(Guid.NewGuid(), Data(name: "  صندوق   مرکزی "), Ledger);
        Assert.Equal(("صندوق مرکزی", "1101", "111078", "IRR"), (d.Data.Name, d.Data.LedgerAccountCode, d.Data.HolderDetailCode, d.Data.CurrencyCode));
        Assert.True(d.IsActive);
        Assert.IsType<CashDeskChanged>(Assert.Single(d.PendingEvents));
    }

    [Fact]
    public void Name_account_and_holder_are_required_and_checked()
    {
        Assert.Equal("CashDesk.NameRequired", Code(() => CashDesk.Register(Guid.NewGuid(), Data(name: " "), Ledger)));
        Assert.Equal("CashDesk.LedgerAccountRequired", Code(() => CashDesk.Register(Guid.NewGuid(), Data(account: ""), Ledger)));
        Assert.Equal("CashDesk.LedgerAccountNotFound", Code(() => CashDesk.Register(Guid.NewGuid(), Data(account: "9999"), Ledger)));
        Assert.Equal("CashDesk.LedgerAccountNotPostable", Code(() => CashDesk.Register(Guid.NewGuid(), Data(account: "11"), Ledger)));
        Assert.Equal("CashDesk.LedgerAccountInactive", Code(() => CashDesk.Register(Guid.NewGuid(), Data(account: "1199"), Ledger)));
        Assert.Equal("CashDesk.HolderRequired", Code(() => CashDesk.Register(Guid.NewGuid(), Data(holder: " "), Ledger)));
        Assert.Equal("CashDesk.DetailNotFound", Code(() => CashDesk.Register(Guid.NewGuid(), Data(holder: "555555"), Ledger)));
        Assert.Equal("CashDesk.DetailInactive", Code(() => CashDesk.Register(Guid.NewGuid(), Data(holder: "111099"), Ledger)));
        Assert.Equal("CashDesk.DetailTypeNotAllowed", Code(() => CashDesk.Register(Guid.NewGuid(), Data(holder: "100000"), Ledger)));
        Assert.Equal("CashDesk.CurrencyInvalid", Code(() => CashDesk.Register(Guid.NewGuid(), Data(currency: "US"), Ledger)));
    }

    [Fact]
    public void Editing_validates_again_and_switching_off_and_on_is_allowed()
    {
        var d = CashDesk.Register(Guid.NewGuid(), Data(), Ledger);
        d.MarkSaved(1);
        d.Change(Data(), Ledger);
        Assert.Empty(d.PendingEvents);
        d.Change(Data(name: "صندوق کارخانه"), Ledger);
        Assert.Equal("صندوق کارخانه", d.Data.Name);
        Assert.Equal("CashDesk.DetailTypeNotAllowed", Code(() => d.Change(Data(holder: "100000"), Ledger)));
        d.Deactivate();
        Assert.False(d.IsActive);
        d.Activate();
        Assert.True(d.IsActive);
    }
}
