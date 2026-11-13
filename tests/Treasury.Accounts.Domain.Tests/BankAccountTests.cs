using Treasury.Accounts.Domain.BankAccounts;
using Treasury.BuildingBlocks.Domain;
using Xunit;

namespace Treasury.Accounts.Domain.Tests;

/// <summary>Bank accounts: manual section 5, Access Form_Bankacc, MVC BanksController; allowed تفصیل types (owner, 10 Nov 2026);
/// owner's report of 11 Nov 2026 (items 10, 12, 14, 15).</summary>
public sealed class BankAccountTests
{
    private static readonly Guid Mellat = Guid.NewGuid();
    private static readonly Guid Melli = Guid.NewGuid();
    private static readonly Guid Tehran = Guid.NewGuid();

    private static readonly LedgerFacts Ledger = new(
        new Dictionary<string, LedgerAccountFact>
        {
            ["1103"] = new("1103", true, true, new HashSet<string> { "بانكها" }, new HashSet<string>()),
            ["1301"] = new("1301", true, false, new HashSet<string>(), new HashSet<string>()),
            ["130100"] = new("130100", true, true, new HashSet<string>(), new HashSet<string>()),
            ["130101"] = new("130101", true, true, new HashSet<string>(), new HashSet<string>()),
            ["1199"] = new("1199", false, true, new HashSet<string>(), new HashSet<string>()),
        },
        new Dictionary<string, DetailFact>
        {
            ["100000"] = new("100000", "بانكها", true),
            ["100001"] = new("100001", "بانكها", true),
            ["111078"] = new("111078", "اشخاص وشركتها", true),
            ["100099"] = new("100099", "بانكها", false),
        });

    private static BankAccountData Data(string number = "0101-123-456", Guid? bank = null, string account = "1103", string? d1 = "100000", string currency = "IRR", int? priority = 1) =>
        new(number, bank ?? Mellat, "شعبه نمونه", "1234", Tehran, FiscalDate.ParseJalali("1400/01/15"), currency,
            new BankLedger(account, d1, null, "130100", "130101"), CollectionPriority: priority, IsFacility: false);

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void A_bank_account_is_registered_active_with_one_event()
    {
        var a = BankAccount.Register(Guid.NewGuid(), Data(), Ledger);
        Assert.True(a.IsActive);
        Assert.False(a.IsBackup);
        Assert.Equal("01011234 56".Replace(" ", "", StringComparison.Ordinal), a.NumberKey);
        Assert.IsType<BankAccountChanged>(Assert.Single(a.PendingEvents));
    }

    [Fact]
    public void Number_bank_and_ledger_account_are_required_as_in_form_bankacc()
    {
        Assert.Equal("BankAccount.NumberRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data(number: " - "), Ledger)));
        Assert.Equal("BankAccount.BankRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data(bank: Guid.Empty), Ledger)));
        Assert.Equal("BankAccount.LedgerAccountRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data(account: " "), Ledger)));
    }

    [Fact]
    public void The_number_key_ignores_separators_and_persian_digits() =>
        Assert.Equal(BankAccount.NumberKeyOf("0101-123-456"), BankAccount.NumberKeyOf("۰۱۰۱ ۱۲۳ ۴۵۶"));

    [Theory]
    [InlineData("9999", "BankAccount.LedgerAccountNotFound")]
    [InlineData("1199", "BankAccount.LedgerAccountInactive")]
    [InlineData("1301", "BankAccount.LedgerAccountNotPostable")]
    public void The_ledger_account_must_exist_be_active_and_take_entries(string account, string code) =>
        Assert.Equal(code, Code(() => BankAccount.Register(Guid.NewGuid(), Data(account: account, d1: null), Ledger)));

    [Fact]
    public void The_collection_and_bounced_accounts_must_take_entries_too()
    {
        var data = Data() with { Ledger = new BankLedger("1103", "100000", null, "1301", "130101") };
        Assert.Equal("BankAccount.LedgerAccountNotPostable", Code(() => BankAccount.Register(Guid.NewGuid(), data, Ledger)));
    }

    [Fact]
    public void Branch_branch_code_city_detail_collection_and_bounced_accounts_are_required_owner_item_12()
    {
        Assert.Equal("BankAccount.BranchNameRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data() with { BranchName = " " }, Ledger)));
        Assert.Equal("BankAccount.BranchCodeRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data() with { BranchCode = null }, Ledger)));
        Assert.Equal("BankAccount.CityRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data() with { CityId = null }, Ledger)));
        Assert.Equal("BankAccount.DetailRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data(account: "130100", d1: null), Ledger)));
        Assert.Equal("BankAccount.CollectionAccountRequired",
            Code(() => BankAccount.Register(Guid.NewGuid(), Data() with { Ledger = new BankLedger("1103", "100000", null, null, "130101") }, Ledger)));
        Assert.Equal("BankAccount.BouncedAccountRequired",
            Code(() => BankAccount.Register(Guid.NewGuid(), Data() with { Ledger = new BankLedger("1103", "100000", null, "130100", " ") }, Ledger)));
    }

    [Fact]
    public void The_collection_account_may_be_the_bank_account_itself_owner_item_14()
    {
        var data = Data() with { Ledger = new BankLedger("1103", "100000", null, "1103", "130101") };
        Assert.Equal("1103", BankAccount.Register(Guid.NewGuid(), data, Ledger).Data.Ledger.CollectionAccountCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(null)]
    public void The_collection_priority_is_none_or_1_to_5_owner_item_15(int? priority) =>
        Assert.Equal(priority, BankAccount.Register(Guid.NewGuid(), Data(priority: priority), Ledger).Data.CollectionPriority);

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void A_collection_priority_outside_1_to_5_is_refused(int priority) =>
        Assert.Equal("BankAccount.CollectionPriorityInvalid", Code(() => BankAccount.Register(Guid.NewGuid(), Data(priority: priority), Ledger)));

    [Fact]
    public void The_detail_must_be_of_an_allowed_type_and_is_required_when_types_are_listed()
    {
        // 1103 allows only تفصیل of type بانكها.
        Assert.Equal("BankAccount.DetailRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data(d1: null), Ledger)));
        Assert.Equal("BankAccount.DetailTypeNotAllowed", Code(() => BankAccount.Register(Guid.NewGuid(), Data(d1: "111078"), Ledger)));
        Assert.Equal("BankAccount.DetailNotFound", Code(() => BankAccount.Register(Guid.NewGuid(), Data(d1: "555555"), Ledger)));
        Assert.Equal("BankAccount.DetailInactive", Code(() => BankAccount.Register(Guid.NewGuid(), Data(d1: "100099"), Ledger)));
    }

    [Fact]
    public void An_account_without_listed_types_takes_any_active_detail() =>
        BankAccount.Register(Guid.NewGuid(), Data(account: "130100", d1: "111078"), Ledger);

    [Theory]
    [InlineData("US")]
    [InlineData("")]
    public void The_currency_is_a_three_letter_code(string currency) =>
        Assert.Equal("BankAccount.CurrencyInvalid", Code(() => BankAccount.Register(Guid.NewGuid(), Data(currency: currency), Ledger)));

    [Fact]
    public void A_backup_account_belongs_to_an_active_main_account_of_the_same_bank()
    {
        // MVC BanksController: same BasicBankID, HasBackup = false, not already someone's Backup_BankID.
        var main = BankAccount.Register(Guid.NewGuid(), Data(), Ledger);
        var backup = BankAccount.Register(Guid.NewGuid(), Data(number: "0101-999", d1: "100001"), Ledger, main);
        Assert.Equal(main.Id, backup.MainAccountId);

        Assert.Equal("BankAccount.MainAccountHasBackup", Code(() => BankAccount.Register(Guid.NewGuid(), Data(number: "0101-998"), Ledger, main, mainAlreadyHasBackup: true)));
        Assert.Equal("BankAccount.MainAccountIsBackup", Code(() => BankAccount.Register(Guid.NewGuid(), Data(number: "0101-997"), Ledger, backup)));
        Assert.Equal("BankAccount.MainAccountOtherBank", Code(() => BankAccount.Register(Guid.NewGuid(), Data(number: "0202-1", bank: Melli), Ledger, main)));
        main.Deactivate();
        Assert.Equal("BankAccount.MainAccountInactive", Code(() => BankAccount.Register(Guid.NewGuid(), Data(number: "0101-996"), Ledger, main)));
    }

    [Fact]
    public void A_backup_account_has_the_currency_of_its_main_account_owner_13_nov()
    {
        var rial = BankAccount.Register(Guid.NewGuid(), Data(), Ledger);
        var dollar = BankAccount.Register(Guid.NewGuid(), Data(number: "0101-500", currency: "USD"), Ledger);
        Assert.Equal("BankAccount.MainAccountOtherCurrency", Code(() => BankAccount.Register(Guid.NewGuid(), Data(number: "0101-501", currency: "USD"), Ledger, rial)));
        var backup = BankAccount.Register(Guid.NewGuid(), Data(number: "0101-502", currency: "USD", d1: "100001"), Ledger, dollar); // foreign with foreign
        Assert.Equal("BankAccount.MainAccountOtherCurrency", Code(() => backup.Change(Data(number: "0101-502", currency: "EUR", d1: "100001"), Ledger)));
    }

    [Fact]
    public void A_backup_account_is_never_saved_without_its_main_account_owner_item_10() =>
        Assert.Equal("BankAccount.MainAccountRequired", Code(() => BankAccount.Register(Guid.NewGuid(), Data(), Ledger, main: null, isBackup: true)));

    [Fact]
    public void A_backup_account_stays_at_the_bank_of_its_main_account()
    {
        var main = BankAccount.Register(Guid.NewGuid(), Data(), Ledger);
        var backup = BankAccount.Register(Guid.NewGuid(), Data(number: "0101-999", d1: "100001"), Ledger, main);
        Assert.Equal("BankAccount.MainAccountOtherBank", Code(() => backup.Change(Data(number: "0101-999", bank: Melli, d1: "100001"), Ledger)));
    }

    [Fact]
    public void Editing_validates_again_and_an_unchanged_form_raises_nothing()
    {
        var a = BankAccount.Register(Guid.NewGuid(), Data(), Ledger);
        a.MarkSaved(1);
        a.Change(Data(), Ledger);
        Assert.Empty(a.PendingEvents);
        a.Change(Data() with { BranchName = "شعبه جدید" }, Ledger);
        Assert.Single(a.PendingEvents);
        Assert.Equal("BankAccount.DetailTypeNotAllowed", Code(() => a.Change(Data(d1: "111078"), Ledger)));
        Assert.Equal("شعبه جدید", a.Data.BranchName);
    }

    [Fact]
    public void Switching_off_and_on_again_is_allowed_as_in_the_manual()
    {
        var a = BankAccount.Register(Guid.NewGuid(), Data(), Ledger);
        a.Deactivate();
        Assert.False(a.IsActive);
        a.Activate();
        Assert.True(a.IsActive);
    }
}
