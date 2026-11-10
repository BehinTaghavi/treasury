using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

public sealed class CatalogueUseCaseTests : IDisposable
{
    private readonly InMemoryReferenceDatabase _db = new();
    private readonly CatalogueUseCases _useCases;

    public CatalogueUseCaseTests() =>
        _useCases = new CatalogueUseCases(new InMemoryUnitOfWork(_db), new InMemoryCatalogueRepository(_db), new InMemoryIdempotencyStore(_db));

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private async Task<Guid> Add(CatalogueKind kind, string name, Guid? parent = null, string? code = null)
    {
        var r = await _useCases.AddAsync(new AddCatalogueItemCommand(Key(), kind, name, parent, code), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<CatalogueItemView> Get(CatalogueKind kind, Guid id) =>
        (await _useCases.ListAsync(kind, null, includeInactive: true, CancellationToken.None)).Single(v => v.Id == id);

    [Fact]
    public async Task Adding_saves_the_entry_and_one_outbox_event()
    {
        var id = await Add(CatalogueKind.Bank, "بانک ملت");
        var view = await Get(CatalogueKind.Bank, id);
        Assert.Equal("بانک ملت", view.Name);
        Assert.Equal(1L, view.Version);
        Assert.Equal(nameof(CatalogueItemChanged), Assert.Single(_db.OutboxMessages).Type);
    }

    [Fact]
    public async Task A_name_already_in_the_list_is_refused_even_with_arabic_letters()
    {
        await Add(CatalogueKind.Bank, "بانک ملی");
        var duplicate = await _useCases.AddAsync(new AddCatalogueItemCommand(Key(), CatalogueKind.Bank, "بانك ملي"), CancellationToken.None);
        Assert.Equal("Catalogue.NameTaken", duplicate.ErrorCode);
        Assert.Single(_db.OutboxMessages);
    }

    [Fact]
    public async Task The_same_city_name_may_exist_in_two_provinces()
    {
        var iran = await Add(CatalogueKind.Country, "ایران");
        var a = await Add(CatalogueKind.Province, "فارس", iran);
        var b = await Add(CatalogueKind.Province, "اصفهان", iran);
        await Add(CatalogueKind.City, "نیک‌آباد", a);
        await Add(CatalogueKind.City, "نیک‌آباد", b);
        Assert.Equal(2, (await _useCases.ListAsync(CatalogueKind.City, null, false, CancellationToken.None)).Count);
        Assert.Single(await _useCases.ListAsync(CatalogueKind.City, a, false, CancellationToken.None));
    }

    [Fact]
    public async Task An_unknown_parent_is_reported()
    {
        var r = await _useCases.AddAsync(new AddCatalogueItemCommand(Key(), CatalogueKind.City, "کرج", Guid.NewGuid()), CancellationToken.None);
        Assert.Equal("Catalogue.ParentNotFound", r.ErrorCode);
    }

    [Fact]
    public async Task A_currency_code_can_exist_only_once()
    {
        await Add(CatalogueKind.Currency, "دلار آمریکا", code: "USD");
        var r = await _useCases.AddAsync(new AddCatalogueItemCommand(Key(), CatalogueKind.Currency, "دلار", null, "usd"), CancellationToken.None);
        Assert.Equal("Catalogue.CodeTaken", r.ErrorCode);
    }

    [Fact]
    public async Task The_same_idempotency_key_adds_only_once()
    {
        var key = Key();
        var first = await _useCases.AddAsync(new AddCatalogueItemCommand(key, CatalogueKind.Bank, "بانک سپه"), CancellationToken.None);
        var again = await _useCases.AddAsync(new AddCatalogueItemCommand(key, CatalogueKind.Bank, "بانک سپه"), CancellationToken.None);
        Assert.Equal(first.Value, again.Value);
        Assert.Single(await _useCases.ListAsync(CatalogueKind.Bank, null, true, CancellationToken.None));
    }

    [Fact]
    public async Task Renaming_checks_duplicates_and_the_version_the_user_saw()
    {
        var mellat = await Add(CatalogueKind.Bank, "بانک ملت");
        await Add(CatalogueKind.Bank, "بانک ملی");

        Assert.Equal("Catalogue.NameTaken", (await _useCases.RenameAsync(new(Key(), mellat, "بانک ملی", 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.RenameAsync(new(Key(), mellat, "بانک ملت ایران", 1), CancellationToken.None)).IsSuccess);
        Assert.Equal("Concurrency.Conflict", (await _useCases.RenameAsync(new(Key(), mellat, "ملت", 1), CancellationToken.None)).ErrorCode);
        var view = await Get(CatalogueKind.Bank, mellat);
        Assert.Equal("بانک ملت ایران", view.Name);
        Assert.Equal(2L, view.Version);
    }

    [Fact]
    public async Task A_deactivated_entry_leaves_the_form_lists_but_stays_on_the_basic_information_screen()
    {
        var sepah = await Add(CatalogueKind.Bank, "بانک سپه");
        Assert.True((await _useCases.SetActiveAsync(new(Key(), sepah, false, 1), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListAsync(CatalogueKind.Bank, null, includeInactive: false, CancellationToken.None));
        Assert.False((await Get(CatalogueKind.Bank, sepah)).IsActive);
        Assert.True((await _useCases.SetActiveAsync(new(Key(), sepah, true, 2), CancellationToken.None)).IsSuccess);
        Assert.Single(await _useCases.ListAsync(CatalogueKind.Bank, null, includeInactive: false, CancellationToken.None));
    }

    [Fact]
    public async Task The_rial_cannot_be_switched_off()
    {
        var rial = await Add(CatalogueKind.Currency, "ریال", code: "IRR");
        Assert.Equal("Catalogue.RialCannotBeDeactivated", (await _useCases.SetActiveAsync(new(Key(), rial, false, 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Lists_are_in_persian_alphabetical_order()
    {
        foreach (var name in new[] { "بانک ملی", "بانک آینده", "بانک تجارت", "پست بانک" })
        {
            await Add(CatalogueKind.Bank, name);
        }

        var names = (await _useCases.ListAsync(CatalogueKind.Bank, null, false, CancellationToken.None)).Select(v => v.Name).ToList();
        Assert.Equal(["بانک آینده", "بانک تجارت", "بانک ملی", "پست بانک"], names);
    }

    [Fact]
    public async Task A_write_without_an_idempotency_key_is_refused() =>
        Assert.Equal("Request.IdempotencyKeyRequired", (await _useCases.AddAsync(new AddCatalogueItemCommand(" ", CatalogueKind.Bank, "بانک دی"), CancellationToken.None)).ErrorCode);
}

public sealed class DefaultListTests : IDisposable
{
    private readonly InMemoryReferenceDatabase _db = new();
    private readonly CatalogueUseCases _useCases;

    public DefaultListTests() =>
        _useCases = new CatalogueUseCases(new InMemoryUnitOfWork(_db), new InMemoryCatalogueRepository(_db), new InMemoryIdempotencyStore(_db));

    public void Dispose() => _db.Dispose();

    private async Task<int> Count(CatalogueKind kind) => (await _useCases.ListAsync(kind, null, true, CancellationToken.None)).Count;

    [Fact]
    public async Task An_empty_database_gets_the_default_lists_once()
    {
        await DefaultLists.SeedAsync(_useCases, CancellationToken.None);
        await DefaultLists.SeedAsync(_useCases, CancellationToken.None);
        Assert.Equal(8, await Count(CatalogueKind.Currency));
        Assert.Equal(1, await Count(CatalogueKind.Country));
        Assert.Equal(31, await Count(CatalogueKind.Province));
        Assert.Equal(31, await Count(CatalogueKind.City));
        Assert.Equal(DefaultLists.Banks.Count, await Count(CatalogueKind.Bank));
        Assert.Contains(await _useCases.ListAsync(CatalogueKind.Currency, null, false, CancellationToken.None), v => v.Code == "IRR" && v.Name == "ریال");
    }

    [Fact]
    public async Task A_list_the_client_already_filled_is_left_alone()
    {
        await _useCases.AddAsync(new AddCatalogueItemCommand("own", CatalogueKind.Bank, "بانک خودمان"), CancellationToken.None);
        await DefaultLists.SeedAsync(_useCases, CancellationToken.None);
        Assert.Equal(1, await Count(CatalogueKind.Bank));
        Assert.Equal(8, await Count(CatalogueKind.Currency));
    }
}
