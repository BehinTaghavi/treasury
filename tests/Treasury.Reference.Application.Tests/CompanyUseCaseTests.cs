using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Application.Company;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

/// <summary>
/// «مشخصات شرکت»: entered once, changed afterwards; its city comes from the list of cities and is kept in use (owner, 18 Nov 2026);
/// one profile per database with the company's logo and official details (owner, 16 Nov 2026).
/// </summary>
public sealed class CompanyUseCaseTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9];

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

    private async Task<Guid> Tehran() =>
        await Add(CatalogueKind.City, "تهران", await Add(CatalogueKind.Province, "تهران", await Add(CatalogueKind.Country, "ایران")));

    private Task<CompanyProfileView> Company() => _company.GetAsync(CancellationToken.None);

    [Fact]
    public async Task The_profile_is_entered_once_then_changed_and_its_city_is_kept_in_use()
    {
        Assert.Equal(CompanyProfileView.NotSet, await Company());
        Assert.Null(await _company.GetLogoAsync(CancellationToken.None));

        var iran = await Add(CatalogueKind.Country, "ایران");
        var province = await Add(CatalogueKind.Province, "تهران", iran);
        var tehran = await Add(CatalogueKind.City, "تهران", province);
        var shiraz = await Add(CatalogueKind.City, "شیراز", await Add(CatalogueKind.Province, "فارس", iran));

        var first = await _company.SetAsync(Key(), new("شرکت نمونه", "خیابان ولیعصر", "خیابان انقلاب", tehran, "1234567890"), 0, CancellationToken.None);
        Assert.True(first.IsSuccess, first.ErrorCode);
        var v = await Company();
        Assert.Equal(
            (true, "شرکت نمونه", "خیابان ولیعصر", "خیابان انقلاب", (Guid?)tehran, "تهران", "1234567890", 1L, false),
            (v.IsSet, v.Name, v.Address, v.LegalAddress, v.CityId, v.CityName, v.PostalCode, v.Version, v.HasLogo));

        // The city named by the profile is in use: it can no longer be deleted from the list of cities.
        Assert.True((await _catalogue.GetAsync(tehran, CancellationToken.None))!.IsInUse);

        // A second "first entry" or a change based on an old copy is refused.
        Assert.Equal("Concurrency.Conflict", (await _company.SetAsync(Key(), new("شرکت دیگر", null, null, shiraz, null), 0, CancellationToken.None)).ErrorCode);
        var moved = await _company.SetAsync(Key(), new("شرکت نمونه", "شیراز، بلوار زند", null, shiraz, null), 1, CancellationToken.None);
        Assert.True(moved.IsSuccess, moved.ErrorCode);
        Assert.Equal(first.Value, moved.Value); // still the one profile of the database
        v = await Company();
        Assert.Equal(("شیراز", 2L, (string?)null, (string?)null), (v.CityName, v.Version, v.PostalCode, v.LegalAddress));
        Assert.Equal(2, _db.OutboxMessages.Count(m => m.Type == "CompanyProfileChanged"));
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
        Assert.False((await Company()).IsSet);

        // The same request sent twice enters the profile once.
        var key = Key();
        Assert.True((await _company.SetAsync(key, new("شرکت نمونه", null, null, city, null), 0, CancellationToken.None)).IsSuccess);
        Assert.True((await _company.SetAsync(key, new("شرکت نمونه", null, null, city, null), 0, CancellationToken.None)).IsSuccess);
        Assert.Equal(1L, (await Company()).Version);
        Assert.Equal("Request.IdempotencyKeyRequired", (await _company.SetAsync(" ", new("شرکت نمونه", null, null, city, null), 1, CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_official_details_are_saved_with_the_form_and_survive_a_new_logo()
    {
        var tehran = await Tehran();
        var form = new CompanyProfileForm(
            "شرکت نمونه", "تهران، خیابان آزادی، پلاک ۱", null, tehran, "1234567890", "۱۰۱۰۱۰۱۰۱۰۳", "411111111111", "12345", "021-88880000", "www.example.ir",
            "info@example.ir");
        Assert.True((await _company.SetAsync(Key(), form, 0, CancellationToken.None)).IsSuccess);
        var saved = new CompanyProfileView(
            true, "شرکت نمونه", "تهران، خیابان آزادی، پلاک ۱", null, tehran, "تهران", "1234567890", 1, false, "10101010103", "411111111111", "12345",
            "021-88880000", "www.example.ir", "info@example.ir");
        Assert.Equal(saved, await Company());

        Assert.True((await _company.AttachLogoAsync(Key(), Png, 1, CancellationToken.None)).IsSuccess);
        Assert.Equal(saved with { HasLogo = true, Version = 2 }, await Company());

        // A detail that is wrong refuses the whole form; an emptied detail is removed.
        Assert.Equal("Company.EmailInvalid", (await _company.SetAsync(Key(), form with { Name = "دیگر", Email = "info" }, 2, CancellationToken.None)).ErrorCode);
        Assert.Equal(saved with { HasLogo = true, Version = 2 }, await Company());
        Assert.True((await _company.SetAsync(Key(), form with { Website = " ", Email = null }, 2, CancellationToken.None)).IsSuccess);
        Assert.Equal(saved with { HasLogo = true, Version = 3, Website = null, Email = null }, await Company());
    }

    [Fact]
    public async Task The_logo_follows_the_profile_is_replaced_and_survives_a_correction_of_the_form()
    {
        Assert.Equal("Company.NameRequired", (await _company.AttachLogoAsync(Key(), Png, 0, CancellationToken.None)).ErrorCode);
        var tehran = await Tehran();
        await _company.SetAsync(Key(), new("شرکت نمونه", null, null, tehran, null), 0, CancellationToken.None);

        Assert.Equal("Company.LogoInvalid", (await _company.AttachLogoAsync(Key(), [1, 2, 3], 1, CancellationToken.None)).ErrorCode);
        Assert.Equal("Concurrency.Conflict", (await _company.AttachLogoAsync(Key(), Png, 5, CancellationToken.None)).ErrorCode);
        Assert.Null(await _company.GetLogoAsync(CancellationToken.None));

        Assert.True((await _company.AttachLogoAsync(Key(), Png, 1, CancellationToken.None)).IsSuccess);
        Assert.Equal((true, 2L), await Company() is var v ? (v.HasLogo, v.Version) : default);
        var logo = await _company.GetLogoAsync(CancellationToken.None);
        Assert.Equal("image/png", logo!.Value.ContentType);
        Assert.Equal(Png, logo.Value.Data);

        Assert.True((await _company.SetAsync(Key(), new("شرکت نمونه نو", null, null, tehran, null), 2, CancellationToken.None)).IsSuccess);
        Assert.Equal(Png, (await _company.GetLogoAsync(CancellationToken.None))!.Value.Data);

        Assert.True((await _company.AttachLogoAsync(Key(), Jpeg, 3, CancellationToken.None)).IsSuccess);
        logo = await _company.GetLogoAsync(CancellationToken.None);
        Assert.Equal("image/jpeg", logo!.Value.ContentType);
        Assert.Equal(Jpeg, logo.Value.Data);
    }

    [Fact]
    public async Task The_logo_can_be_removed()
    {
        Assert.Equal("Company.NoLogo", (await _company.RemoveLogoAsync(Key(), 0, CancellationToken.None)).ErrorCode);
        await _company.SetAsync(Key(), new("شرکت نمونه", null, null, await Tehran(), null), 0, CancellationToken.None);
        Assert.Equal("Company.NoLogo", (await _company.RemoveLogoAsync(Key(), 1, CancellationToken.None)).ErrorCode);
        await _company.AttachLogoAsync(Key(), Png, 1, CancellationToken.None);

        Assert.True((await _company.RemoveLogoAsync(Key(), 2, CancellationToken.None)).IsSuccess);
        Assert.Equal((false, 3L), await Company() is var v ? (v.HasLogo, v.Version) : default);
        Assert.Null(await _company.GetLogoAsync(CancellationToken.None));
    }
}
