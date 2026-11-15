using Treasury.Accounts.Application.CashDesks;
using Treasury.Accounts.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Accounts.Application.Tests;

public sealed class CashDeskUseCaseTests : IDisposable
{
    private readonly InMemoryAccountsDatabase _db = new();
    private readonly FakeReference _reference = new();
    private readonly CashDeskUseCases _useCases;

    public CashDeskUseCaseTests() =>
        _useCases = new CashDeskUseCases(new InMemoryUnitOfWork(_db), new InMemoryCashDeskRepository(_db), _reference, new InMemoryIdempotencyStore(_db));

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    // 130100 allows any تفصیل type in FakeReference.
    private static CashDeskForm Form(string name = "صندوق مرکزی", string holder = "111078", bool foreign = false, string? currency = null) =>
        new(name, "130100", holder, null, foreign, currency, "1405/08/22");

    [Fact]
    public async Task A_cash_desk_has_its_creation_date_owner_report_2()
    {
        Assert.Equal("CashDesk.CreatedOnRequired", (await _useCases.RegisterAsync(new(Key(), Form() with { CreatedOn = null }), CancellationToken.None)).ErrorCode);
        var id = (await _useCases.RegisterAsync(new(Key(), Form()), CancellationToken.None)).Value;
        Assert.Equal("1405/08/22", (await _useCases.ListAsync(true, CancellationToken.None)).Single(d => d.Id == id).CreatedOn);
    }

    [Fact]
    public async Task A_cash_desk_is_saved_and_listed_with_its_holders_name()
    {
        var r = await _useCases.RegisterAsync(new(Key(), Form()), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = Assert.Single(await _useCases.ListAsync(true, CancellationToken.None));
        Assert.Equal(("صندوق مرکزی", "111078", "تفصیل 111078", "IRR"), (v.Name, v.HolderDetailCode, v.HolderName, v.CurrencyCode));
    }

    [Fact]
    public async Task The_same_name_is_refused_even_written_with_arabic_letters()
    {
        await _useCases.RegisterAsync(new(Key(), Form("صندوق ریالی")), CancellationToken.None);
        Assert.Equal("CashDesk.NameTaken", (await _useCases.RegisterAsync(new(Key(), Form("صندوق ريالي")), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_foreign_currency_cash_desk_marks_its_currency_in_use()
    {
        Assert.True((await _useCases.RegisterAsync(new(Key(), Form("صندوق ارزی", foreign: true, currency: "usd")), CancellationToken.None)).IsSuccess);
        Assert.Equal([("currencies", FakeReference.CurrencyIds["USD"])], _reference.MarkedInUse);
        Assert.Equal("CashDesk.ForeignCurrencyRequired", (await _useCases.RegisterAsync(new(Key(), Form("x", foreign: true, currency: "IRR")), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Switched_off_cash_desks_leave_the_form_lists_and_a_stale_edit_is_refused()
    {
        var id = (await _useCases.RegisterAsync(new(Key(), Form()), CancellationToken.None)).Value;
        Assert.True((await _useCases.SetActiveAsync(new(Key(), id, false, 1), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListAsync(false, CancellationToken.None));
        Assert.Equal("Concurrency.Conflict", (await _useCases.ChangeAsync(new(Key(), id, Form("صندوق جدید"), 1), CancellationToken.None)).ErrorCode);
    }
}
