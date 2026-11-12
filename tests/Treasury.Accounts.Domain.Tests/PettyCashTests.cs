using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Domain.PettyCash;
using Treasury.BuildingBlocks.Domain;
using Xunit;

namespace Treasury.Accounts.Domain.Tests;

/// <summary>Petty cash holders: new-version manual item 11; MVC PettyCash, PettyCash_BehalfGroups.</summary>
public sealed class PettyCashTests
{
    private static readonly Guid Expenses = Guid.NewGuid();
    private static readonly Guid Sales = Guid.NewGuid();
    private static readonly Guid Closed = Guid.NewGuid();

    private static readonly LedgerFacts Ledger = new(
        new Dictionary<string, LedgerAccountFact> { ["1102"] = new("1102", true, true, new HashSet<string>(), new HashSet<string>()) },
        new Dictionary<string, DetailFact> { ["600046"] = new("600046", "كاركنان كارخانه", true) });

    private static readonly Dictionary<Guid, PurposeGroupFact> Groups = new()
    {
        [Expenses] = new(Expenses, true, true),
        [Sales] = new(Sales, false, true),
        [Closed] = new(Closed, true, false),
    };

    private static PettyCashData Data(decimal amount = 50_000_000, decimal limit = 40_000_000, decimal ceiling = 5_000_000, params Guid[] groups) =>
        new("600046", "1102", amount, limit, ceiling, groups, PettyCashRecovery.Cheque);

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void A_petty_cash_has_a_holder_an_account_an_amount_a_settlement_limit_and_a_ceiling()
    {
        var f = PettyCashFund.Register(Guid.NewGuid(), Data(groups: Expenses), Ledger, Groups);
        Assert.Equal((50_000_000m, 40_000_000m, 5_000_000m, PettyCashRecovery.Cheque), (f.Data.Amount, f.Data.SettleLimit, f.Data.PayCeiling, f.Data.Recovery));
        Assert.Equal([Expenses], f.Data.AllowedPurposeGroupIds);
        Assert.Empty(PettyCashFund.Register(Guid.NewGuid(), Data(), Ledger, Groups).Data.AllowedPurposeGroupIds); // none listed: all groups
    }

    [Theory]
    [InlineData(0, 0, 0, "PettyCash.AmountInvalid")]
    [InlineData(1000.5, 1, 1, "PettyCash.AmountInvalid")]
    [InlineData(1000, 1001, 10, "PettyCash.SettleLimitInvalid")]
    [InlineData(1000, 0, 10, "PettyCash.SettleLimitInvalid")]
    [InlineData(1000, 800, 1001, "PettyCash.PayCeilingInvalid")]
    public void Amounts_must_fit_together(decimal amount, decimal limit, decimal ceiling, string code) =>
        Assert.Equal(code, Code(() => PettyCashFund.Register(Guid.NewGuid(), Data(amount, limit, ceiling), Ledger, Groups)));

    [Fact]
    public void Only_active_payment_purpose_groups_can_be_allowed()
    {
        Assert.Equal("PettyCash.PurposeGroupNotPayment", Code(() => PettyCashFund.Register(Guid.NewGuid(), Data(groups: Sales), Ledger, Groups)));
        Assert.Equal("PettyCash.PurposeGroupInactive", Code(() => PettyCashFund.Register(Guid.NewGuid(), Data(groups: Closed), Ledger, Groups)));
        Assert.Equal("PettyCash.PurposeGroupNotFound", Code(() => PettyCashFund.Register(Guid.NewGuid(), Data(groups: Guid.NewGuid()), Ledger, Groups)));
    }

    [Fact]
    public void Holder_and_account_are_required_and_checked()
    {
        Assert.Equal("PettyCash.HolderRequired", Code(() => PettyCashFund.Register(Guid.NewGuid(), Data() with { HolderDetailCode = " " }, Ledger, Groups)));
        Assert.Equal("PettyCash.DetailNotFound", Code(() => PettyCashFund.Register(Guid.NewGuid(), Data() with { HolderDetailCode = "999999" }, Ledger, Groups)));
        Assert.Equal("PettyCash.LedgerAccountNotFound", Code(() => PettyCashFund.Register(Guid.NewGuid(), Data() with { LedgerAccountCode = "9999" }, Ledger, Groups)));
    }

    [Fact]
    public void An_unchanged_form_raises_nothing_and_a_new_amount_one_event()
    {
        var f = PettyCashFund.Register(Guid.NewGuid(), Data(groups: Expenses), Ledger, Groups);
        f.MarkSaved(1);
        f.Change(Data(groups: Expenses), Ledger, Groups);
        Assert.Empty(f.PendingEvents);
        f.Change(Data(60_000_000, groups: Expenses), Ledger, Groups);
        Assert.IsType<PettyCashChanged>(Assert.Single(f.PendingEvents));
    }
}
