using Treasury.BuildingBlocks.Domain;
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
    public async Task A_bank_already_in_the_list_with_the_word_bank_is_refused_owner_item_8()
    {
        await Add(CatalogueKind.Bank, "بانک آینده");
        var r = await _useCases.AddAsync(new AddCatalogueItemCommand(Key(), CatalogueKind.Bank, "آینده"), CancellationToken.None);
        Assert.Equal("Catalogue.SimilarBankName", r.ErrorCode);
        var other = await Add(CatalogueKind.Bank, "بانک دی");
        Assert.Equal("Catalogue.SimilarBankName", (await _useCases.RenameAsync(new(Key(), other, "آينده", 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_city_can_be_renamed_and_moved_to_another_province_in_one_edit_owner_item_6()
    {
        var iran = await Add(CatalogueKind.Country, "ایران");
        var isfahan = await Add(CatalogueKind.Province, "اصفهان", iran);
        var tehranProvince = await Add(CatalogueKind.Province, "تهران", iran);
        var city = await Add(CatalogueKind.City, "اصفهان", isfahan);
        await Add(CatalogueKind.City, "تهران", tehranProvince);

        Assert.Equal("Catalogue.NameTaken", (await _useCases.EditAsync(new(Key(), city, "تهران", 1, tehranProvince), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.EditAsync(new(Key(), city, "ری", 1, tehranProvince), CancellationToken.None)).IsSuccess);
        var view = await Get(CatalogueKind.City, city);
        Assert.Equal(("ری", tehranProvince), (view.Name, view.ParentId));
        Assert.Equal("Catalogue.ParentNotFound", (await _useCases.EditAsync(new(Key(), city, "ری", 2, Guid.NewGuid()), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_currency_code_is_corrected_only_to_a_free_code_and_only_while_unused_owner_item_2()
    {
        await Add(CatalogueKind.Currency, "دلار", code: "USD");
        var qatar = await Add(CatalogueKind.Currency, "ریال قطر", code: "QAR");
        Assert.Equal("Catalogue.CodeTaken", (await _useCases.EditAsync(new(Key(), qatar, "دینار کویت", 1, Code: "USD"), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.EditAsync(new(Key(), qatar, "دینار کویت", 1, Code: "KWD"), CancellationToken.None)).IsSuccess);
        Assert.Equal("KWD", (await Get(CatalogueKind.Currency, qatar)).Code);

        Assert.True((await _useCases.MarkInUseAsync(new(Key(), qatar), CancellationToken.None)).IsSuccess);
        Assert.Equal("Catalogue.CodeInUse", (await _useCases.EditAsync(new(Key(), qatar, "دینار کویت", 3, Code: "QAR"), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task An_unused_entry_is_deleted_a_used_one_or_one_with_entries_under_it_is_not_owner_item_9()
    {
        var mistake = await Add(CatalogueKind.Bank, "بانک اشتباهی");
        Assert.True((await _useCases.DeleteAsync(new(Key(), mistake, 1), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListAsync(CatalogueKind.Bank, null, true, CancellationToken.None));
        Assert.Equal(nameof(CatalogueItemDeleted), _db.OutboxMessages[^1].Type);
        Assert.Equal("Catalogue.NotFound", (await _useCases.DeleteAsync(new(Key(), mistake, 1), CancellationToken.None)).ErrorCode);

        var used = await Add(CatalogueKind.Bank, "بانک ملت");
        Assert.True((await _useCases.MarkInUseAsync(new(Key(), used), CancellationToken.None)).IsSuccess);
        Assert.True((await Get(CatalogueKind.Bank, used)).IsInUse);
        Assert.Equal("Catalogue.InUse", (await _useCases.DeleteAsync(new(Key(), used, 2), CancellationToken.None)).ErrorCode);

        var iran = await Add(CatalogueKind.Country, "ایران");
        var province = await Add(CatalogueKind.Province, "فارس", iran);
        Assert.Equal("Catalogue.HasChildren", (await _useCases.DeleteAsync(new(Key(), iran, 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.DeleteAsync(new(Key(), province, 1), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.DeleteAsync(new(Key(), iran, 1), CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task Marking_in_use_twice_changes_the_entry_once_and_an_unknown_entry_is_reported()
    {
        var bank = await Add(CatalogueKind.Bank, "بانک سینا");
        Assert.True((await _useCases.MarkInUseAsync(new(Key(), bank), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.MarkInUseAsync(new(Key(), bank), CancellationToken.None)).IsSuccess);
        Assert.Equal(2L, (await Get(CatalogueKind.Bank, bank)).Version);
        Assert.Equal("Catalogue.NotFound", (await _useCases.MarkInUseAsync(new(Key(), Guid.NewGuid()), CancellationToken.None)).ErrorCode);
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
        Assert.Equal(DefaultLists.IranCities.Count, await Count(CatalogueKind.City)); // every capital is in the city list too
        Assert.Equal(DefaultLists.Banks.Count, await Count(CatalogueKind.Bank));
        Assert.Equal(4, await Count(CatalogueKind.Reference)); // manual item 8: the fixed references
        Assert.Contains(await _useCases.ListAsync(CatalogueKind.Currency, null, false, CancellationToken.None), v => v.Code == "IRR" && v.Name == "ریال");
    }

    [Fact]
    public void The_list_of_iran_cities_covers_the_31_provinces_with_1354_cities_unique_per_province()
    {
        Assert.Equal(1354, DefaultLists.IranCities.Count);
        Assert.Equal(
            DefaultLists.IranProvinces.Select(p => PersianText.Key(p.Province)).Order(),
            DefaultLists.IranCities.Select(c => PersianText.Key(c.Province)).Distinct().Order());
        Assert.DoesNotContain(DefaultLists.IranCities.GroupBy(c => (PersianText.Key(c.Province), PersianText.Key(c.City))), g => g.Count() > 1);
        Assert.All(DefaultLists.IranProvinces, p => Assert.Contains(DefaultLists.IranCities, c => c.Province == p.Province && PersianText.Key(c.City) == PersianText.Key(p.Capital)));
        Assert.Contains(DefaultLists.IranCities, c => c.City == "کاشان" && c.Province == "اصفهان");
    }

    [Fact]
    public async Task An_older_database_gets_the_missing_cities_and_a_deleted_or_renamed_city_does_not_come_back()
    {
        await DefaultLists.SeedAsync(_useCases, CancellationToken.None);
        var isfahan = (await _useCases.ListAsync(CatalogueKind.Province, null, true, CancellationToken.None)).Single(p => p.Name == "اصفهان");
        var cities = await _useCases.ListAsync(CatalogueKind.City, isfahan.Id, true, CancellationToken.None);
        var kashan = cities.Single(c => c.Name == "کاشان");
        var natanz = cities.Single(c => c.Name == "نطنز");
        Assert.True((await _useCases.DeleteAsync(new("del", kashan.Id, kashan.Version), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.RenameAsync(new("ren", natanz.Id, "نطنز جدید", natanz.Version), CancellationToken.None)).IsSuccess);

        Assert.Equal(0, await DefaultLists.AddIranCitiesAsync(_useCases, CancellationToken.None));
        var after = (await _useCases.ListAsync(CatalogueKind.City, isfahan.Id, true, CancellationToken.None)).Select(c => c.Name).ToList();
        Assert.DoesNotContain("کاشان", after);
        Assert.DoesNotContain("نطنز", after);
        Assert.Contains("نطنز جدید", after);
    }

    [Fact]
    public async Task A_city_the_client_typed_before_is_kept_and_not_added_twice()
    {
        var iran = (await _useCases.AddAsync(new AddCatalogueItemCommand("c", CatalogueKind.Country, "ايران"), CancellationToken.None)).Value;
        var fars = (await _useCases.AddAsync(new AddCatalogueItemCommand("p", CatalogueKind.Province, "فارس", iran), CancellationToken.None)).Value;
        await _useCases.AddAsync(new AddCatalogueItemCommand("k", CatalogueKind.City, "كازرون", fars), CancellationToken.None);

        var added = await DefaultLists.AddIranCitiesAsync(_useCases, CancellationToken.None);
        var farsCities = await _useCases.ListAsync(CatalogueKind.City, fars, true, CancellationToken.None);
        Assert.Equal(DefaultLists.IranCities.Count(c => c.Province == "فارس"), farsCities.Count);
        Assert.Equal(farsCities.Count - 1, added); // only Fars exists here; کازرون was there already
        Assert.Single(farsCities, c => PersianText.Key(c.Name) == PersianText.Key("کازرون"));
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
