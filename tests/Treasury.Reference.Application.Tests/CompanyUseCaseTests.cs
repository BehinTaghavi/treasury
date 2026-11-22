using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Application.Company;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

/// <summary>
/// «اطلاعات شرکت» (owner, 16 Nov 2026): one profile per database, with the company's name and logo; and «مشخصات شرکت» (owner,
/// 18 Nov 2026), the same page since the two were joined: the legal address and its city, from the list of cities and kept in use.
/// </summary>
public sealed class CompanyUseCaseTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9];

    private readonly InMemoryReferenceDatabase _db = new();
    private readonly CatalogueUseCases _catalogue;
    private readonly CompanyUseCases _useCases;
    private readonly Guid _city;

    public CompanyUseCaseTests()
    {
        var uow = new InMemoryUnitOfWork(_db);
        var items = new InMemoryCatalogueRepository(_db);
        var idempotency = new InMemoryIdempotencyStore(_db);
        _catalogue = new CatalogueUseCases(uow, items, idempotency);
        _useCases = new CompanyUseCases(uow, new InMemoryCompanyRepository(_db), items, idempotency, _catalogue);
        _city = Add(CatalogueKind.City, "تهران", Add(CatalogueKind.Province, "تهران", Add(CatalogueKind.Country, "ایران")));
    }

    private Guid Add(CatalogueKind kind, string name, Guid? parent = null)
    {
        var r = _catalogue.AddAsync(new AddCatalogueItemCommand(Key(), kind, name, parent), CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private Task<CompanyView> Company() => _useCases.GetAsync(CancellationToken.None);

    [Fact]
    public async Task A_new_database_has_no_company_name_and_no_logo()
    {
        Assert.Equal(new CompanyView(string.Empty, false, 0), await Company());
        Assert.Null(await _useCases.GetLogoAsync(CancellationToken.None));
    }

    [Fact]
    public async Task The_name_is_entered_once_and_corrected_afterwards()
    {
        var first = await _useCases.SaveAsync(Key(), new(" شرکت  ایران ناژو ", CityId: _city), 0, CancellationToken.None);
        Assert.True(first.IsSuccess, first.ErrorCode);
        Assert.Equal(new CompanyView("شرکت ایران ناژو", false, 1, CityId: _city, CityName: "تهران"), await Company());

        var corrected = await _useCases.SaveAsync(Key(), new("شرکت ایران ناژو (سهامی خاص)", CityId: _city), 1, CancellationToken.None);
        Assert.Equal(first.Value, corrected.Value); // still the one profile of the database
        Assert.Equal(new CompanyView("شرکت ایران ناژو (سهامی خاص)", false, 2, CityId: _city, CityName: "تهران"), await Company());

        Assert.Equal("Company.NameRequired", (await _useCases.SaveAsync(Key(), new(" ", CityId: _city), 2, CancellationToken.None)).ErrorCode);
        Assert.Equal(2, _db.OutboxMessages.Count(m => m.Type == "CompanyProfileChanged"));
    }

    [Fact]
    public async Task The_official_details_are_saved_with_the_name_and_survive_a_new_logo_owner_answer_Q38()
    {
        var form = new CompanyForm("شرکت نمونه", "۱۰۱۰۱۰۱۰۱۰۳", "411111111111", "12345", "تهران، خیابان آزادی، پلاک ۱", "1234567890", "021-88880000", "www.example.ir", "info@example.ir", CityId: _city);
        Assert.True((await _useCases.SaveAsync(Key(), form, 0, CancellationToken.None)).IsSuccess);
        var saved = new CompanyView("شرکت نمونه", false, 1, "10101010103", "411111111111", "12345", "تهران، خیابان آزادی، پلاک ۱", "1234567890", "021-88880000", "www.example.ir", "info@example.ir", CityId: _city, CityName: "تهران");
        Assert.Equal(saved, await Company());

        Assert.True((await _useCases.AttachLogoAsync(Key(), Png, 1, CancellationToken.None)).IsSuccess);
        Assert.Equal(saved with { HasLogo = true, Version = 2 }, await Company());

        // A detail that is wrong refuses the whole form; an emptied detail is removed.
        Assert.Equal("Company.EmailInvalid", (await _useCases.SaveAsync(Key(), form with { Name = "دیگر", Email = "info" }, 2, CancellationToken.None)).ErrorCode);
        Assert.Equal(saved with { HasLogo = true, Version = 2 }, await Company());
        Assert.True((await _useCases.SaveAsync(Key(), form with { Website = " ", Email = null }, 2, CancellationToken.None)).IsSuccess);
        Assert.Equal(saved with { HasLogo = true, Version = 3, Website = null, Email = null }, await Company());
    }

    [Fact]
    public async Task A_change_made_on_an_old_copy_of_the_form_is_refused()
    {
        await _useCases.SaveAsync(Key(), new("شرکت نمونه", CityId: _city), 0, CancellationToken.None);
        Assert.Equal("Concurrency.Conflict", (await _useCases.SaveAsync(Key(), new("دیگر", CityId: _city), 0, CancellationToken.None)).ErrorCode);
        Assert.Equal("Concurrency.Conflict", (await _useCases.AttachLogoAsync(Key(), Png, 5, CancellationToken.None)).ErrorCode);
        Assert.Equal("شرکت نمونه", (await Company()).Name);
    }

    [Fact]
    public async Task The_same_request_sent_twice_is_saved_once()
    {
        var key = Key();
        var first = await _useCases.SaveAsync(key, new("شرکت نمونه", CityId: _city), 0, CancellationToken.None);
        var again = await _useCases.SaveAsync(key, new("شرکت نمونه", CityId: _city), 0, CancellationToken.None);
        Assert.Equal(first.Value, again.Value);
        Assert.Equal(1, (await Company()).Version);
        Assert.Equal("Request.IdempotencyKeyRequired", (await _useCases.SaveAsync(" ", new("شرکت نمونه", CityId: _city), 1, CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_logo_follows_the_name_is_replaced_and_survives_a_name_correction()
    {
        Assert.Equal("Company.NameRequired", (await _useCases.AttachLogoAsync(Key(), Png, 0, CancellationToken.None)).ErrorCode);
        await _useCases.SaveAsync(Key(), new("شرکت نمونه", CityId: _city), 0, CancellationToken.None);

        Assert.Equal("Company.LogoInvalid", (await _useCases.AttachLogoAsync(Key(), [1, 2, 3], 1, CancellationToken.None)).ErrorCode);
        Assert.Null(await _useCases.GetLogoAsync(CancellationToken.None));

        Assert.True((await _useCases.AttachLogoAsync(Key(), Png, 1, CancellationToken.None)).IsSuccess);
        Assert.Equal(new CompanyView("شرکت نمونه", true, 2, CityId: _city, CityName: "تهران"), await Company());
        var logo = await _useCases.GetLogoAsync(CancellationToken.None);
        Assert.Equal("image/png", logo!.Value.ContentType);
        Assert.Equal(Png, logo.Value.Data);

        Assert.True((await _useCases.SaveAsync(Key(), new("شرکت نمونه نو", CityId: _city), 2, CancellationToken.None)).IsSuccess);
        Assert.Equal(Png, (await _useCases.GetLogoAsync(CancellationToken.None))!.Value.Data);

        Assert.True((await _useCases.AttachLogoAsync(Key(), Jpeg, 3, CancellationToken.None)).IsSuccess);
        logo = await _useCases.GetLogoAsync(CancellationToken.None);
        Assert.Equal("image/jpeg", logo!.Value.ContentType);
        Assert.Equal(Jpeg, logo.Value.Data);
    }

    [Fact]
    public async Task The_logo_can_be_removed()
    {
        Assert.Equal("Company.NoLogo", (await _useCases.RemoveLogoAsync(Key(), 0, CancellationToken.None)).ErrorCode);
        await _useCases.SaveAsync(Key(), new("شرکت نمونه", CityId: _city), 0, CancellationToken.None);
        Assert.Equal("Company.NoLogo", (await _useCases.RemoveLogoAsync(Key(), 1, CancellationToken.None)).ErrorCode);
        await _useCases.AttachLogoAsync(Key(), Png, 1, CancellationToken.None);

        Assert.True((await _useCases.RemoveLogoAsync(Key(), 2, CancellationToken.None)).IsSuccess);
        Assert.Equal(new CompanyView("شرکت نمونه", false, 3, CityId: _city, CityName: "تهران"), await Company());
        Assert.Null(await _useCases.GetLogoAsync(CancellationToken.None));
    }
}
