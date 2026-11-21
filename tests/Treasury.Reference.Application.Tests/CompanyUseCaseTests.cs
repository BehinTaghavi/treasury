using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Application.Company;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

/// <summary>«مشخصات شرکت» (owner, 18 Nov 2026): entered once, changed afterwards; its city comes from the list of cities and is kept in use.</summary>
public sealed class CompanyUseCaseTests : IDisposable
{
    private readonly InMemoryReferenceDatabase _db = new();
    private readonly CatalogueUseCases _catalogue;
    private readonly CompanyUseCases _company;

    public CompanyUseCaseTests()
    {
        var uow = new InMemoryUnitOfWork(_db);
        var items = new InMemoryCatalogueRepository(_db);
        var idempotency = new InMemoryIdempotencyStore(_db);
        _catalogue = new CatalogueUseCases(uow, items, idempotency);
        _company = new CompanyUseCases(uow, new InMemoryCompanyProfileRepository(_db), items, idempotency, _catalogue);
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private async Task<Guid> Add(CatalogueKind kind, string name, Guid? parent = null)
    {
        var r = await _catalogue.AddAsync(new AddCatalogueItemCommand(Key(), kind, name, parent), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    [Fact]
    public async Task The_profile_is_entered_once_then_changed_and_its_city_is_kept_in_use()
    {
        Assert.Equal(CompanyProfileView.NotSet, await _company.GetAsync(CancellationToken.None));

        var iran = await Add(CatalogueKind.Country, "ایران");
        var province = await Add(CatalogueKind.Province, "تهران", iran);
        var tehran = await Add(CatalogueKind.City, "تهران", province);
        var shiraz = await Add(CatalogueKind.City, "شیراز", await Add(CatalogueKind.Province, "فارس", iran));

        var first = await _company.SetAsync(Key(), new("شرکت نمونه", "خیابان ولیعصر", "خیابان انقلاب", tehran, "1234567890"), 0, CancellationToken.None);
        Assert.True(first.IsSuccess, first.ErrorCode);
        var v = await _company.GetAsync(CancellationToken.None);
        Assert.Equal(
            (true, "شرکت نمونه", "خیابان ولیعصر", "خیابان انقلاب", (Guid?)tehran, "تهران", "1234567890", 1L),
            (v.IsSet, v.Name, v.Address, v.LegalAddress, v.CityId, v.CityName, v.PostalCode, v.Version));

        // The city named by the profile is in use: it can no longer be deleted from the list of cities.
        Assert.True((await _catalogue.GetAsync(tehran, CancellationToken.None))!.IsInUse);

        // A second "first entry" or a change based on an old copy is refused.
        Assert.Equal("Concurrency.Conflict", (await _company.SetAsync(Key(), new("شرکت دیگر", null, null, shiraz, null), 0, CancellationToken.None)).ErrorCode);
        var moved = await _company.SetAsync(Key(), new("شرکت نمونه", "شیراز، بلوار زند", null, shiraz, null), 1, CancellationToken.None);
        Assert.True(moved.IsSuccess, moved.ErrorCode);
        v = await _company.GetAsync(CancellationToken.None);
        Assert.Equal(("شیراز", 2L, (string?)null, (string?)null), (v.CityName, v.Version, v.PostalCode, v.LegalAddress));
    }

    [Fact]
    public async Task The_city_must_be_a_city_of_the_list_and_the_name_is_required()
    {
        var iran = await Add(CatalogueKind.Country, "ایران");
        var bank = await Add(CatalogueKind.Bank, "بانک نمونه");
        var city = await Add(CatalogueKind.City, "کاشان", await Add(CatalogueKind.Province, "اصفهان", iran));

        Assert.Equal("Company.CityNotFound", (await _company.SetAsync(Key(), new("شرکت نمونه", null, null, Guid.NewGuid(), null), 0, CancellationToken.None)).ErrorCode);
        Assert.Equal("Company.CityNotFound", (await _company.SetAsync(Key(), new("شرکت نمونه", null, null, bank, null), 0, CancellationToken.None)).ErrorCode);
        Assert.Equal("Company.CityRequired", (await _company.SetAsync(Key(), new("شرکت نمونه", null, null, null, null), 0, CancellationToken.None)).ErrorCode);
        Assert.Equal("Company.NameRequired", (await _company.SetAsync(Key(), new(" ", null, null, city, null), 0, CancellationToken.None)).ErrorCode);
        Assert.False((await _company.GetAsync(CancellationToken.None)).IsSet);

        // The same request sent twice enters the profile once.
        var key = Key();
        Assert.True((await _company.SetAsync(key, new("شرکت نمونه", null, null, city, null), 0, CancellationToken.None)).IsSuccess);
        Assert.True((await _company.SetAsync(key, new("شرکت نمونه", null, null, city, null), 0, CancellationToken.None)).IsSuccess);
        Assert.Equal(1L, (await _company.GetAsync(CancellationToken.None)).Version);
    }
}
