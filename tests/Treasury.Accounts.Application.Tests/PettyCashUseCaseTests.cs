using Treasury.Accounts.Application.PettyCash;
using Treasury.Accounts.Domain.PettyCash;
using Treasury.Accounts.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Accounts.Application.Tests;

public sealed class PettyCashUseCaseTests : IDisposable
{
    private readonly InMemoryAccountsDatabase _db = new();
    private readonly FakeReference _reference = new();
    private readonly PettyCashUseCases _useCases;

    public PettyCashUseCaseTests() =>
        _useCases = new PettyCashUseCases(new InMemoryUnitOfWork(_db), new InMemoryPettyCashRepository(_db), _reference, new InMemoryIdempotencyStore(_db));

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    // 130100 allows any تفصیل type in FakeReference.
    private static PettyCashForm Form(string holder = "111078", params Guid[] groups) =>
        new(holder, "130100", 50_000_000, 40_000_000, 5_000_000, groups, PettyCashRecovery.Cash, "1405/08/22",
            new PettyCashKeeping(false, null, null, true, false, null, null));

    [Fact]
    public async Task A_petty_cash_is_saved_its_groups_marked_in_use_and_listed_with_the_holders_name()
    {
        var r = await _useCases.RegisterAsync(Key(), Form("111078", FakeReference.Expenses, FakeReference.Advances), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Equal([("purpose-groups", FakeReference.Expenses), ("purpose-groups", FakeReference.Advances)], _reference.MarkedInUse);
        var v = Assert.Single(await _useCases.ListAsync(true, CancellationToken.None));
        Assert.Equal(("تفصیل 111078", 50_000_000m, PettyCashRecovery.Cash), (v.HolderName, v.Amount, v.Recovery));
    }

    [Fact]
    public async Task One_petty_cash_per_holder_and_receipt_groups_are_refused()
    {
        await _useCases.RegisterAsync(Key(), Form(), CancellationToken.None);
        Assert.Equal("PettyCash.HolderTaken", (await _useCases.RegisterAsync(Key(), Form(), CancellationToken.None)).ErrorCode);
        Assert.Equal("PettyCash.PurposeGroupNotPayment", (await _useCases.RegisterAsync(Key(), Form("600046", FakeReference.SalesReceipts), CancellationToken.None)).ErrorCode);
        Assert.Empty(_reference.MarkedInUse);
    }

    [Fact]
    public async Task The_holder_is_staff_a_cost_centre_or_an_organisational_unit_owner_report_2() =>
        Assert.Equal("PettyCash.HolderNotStaff", (await _useCases.RegisterAsync(Key(), Form("100000"), CancellationToken.None)).ErrorCode);
}

