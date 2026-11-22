using Npgsql;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Application.Company;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Reference.Infrastructure.Postgres.Tests;

/// <summary>
/// «اطلاعات شرکت» / «مشخصات شرکت» (one page since 18 Nov 2026) through the real PostgreSQL adapter, acting as the reference_svc role
/// (033_reference_company.sql, 034, 039: the legal address and its city).
/// </summary>
public sealed class CompanyPostgresTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9];

    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly CompanyUseCases _useCases;
    private readonly Guid _city;
    private readonly string _cityName;

    public CompanyPostgresTests(PostgresDatabase db)
    {
        _db = db;
        _db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("reference_svc"));
        var uow = new PostgresUnitOfWork(_dataSource);
        var items = new PostgresCatalogueRepository();
        var idempotency = new PostgresIdempotencyStore(_dataSource);
        var catalogue = new CatalogueUseCases(uow, items, idempotency);
        _useCases = new CompanyUseCases(uow, new PostgresCompanyRepository(), items, idempotency, catalogue);

        // The city of the legal address (owner, 18 Nov 2026), a city of its own so other tests' lists do not matter.
        Guid Add(CatalogueKind kind, string name, Guid? parent = null)
        {
            var r = catalogue.AddAsync(new AddCatalogueItemCommand(Key(), kind, name, parent), CancellationToken.None).GetAwaiter().GetResult();
            Assert.True(r.IsSuccess, r.ErrorCode);
            return r.Value;
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        _cityName = "شهر " + suffix;
        _city = Add(CatalogueKind.City, _cityName, Add(CatalogueKind.Province, "استان " + suffix, Add(CatalogueKind.Country, "کشور " + suffix)));
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    // One test: the database has one company profile, so the steps run in order on it.
    [Fact]
    public async Task The_company_name_and_logo_round_trip_and_there_is_one_profile()
    {
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal(CompanyView.NotEntered, await _useCases.GetAsync(ct));
        Assert.Null(await _useCases.GetLogoAsync(ct));

        var first = await _useCases.SaveAsync(Key(), new("شرکت نمونه", CityId: _city), 0, ct);
        Assert.True(first.IsSuccess, first.ErrorCode);
        Assert.Equal(new CompanyView("شرکت نمونه", false, 1, CityId: _city, CityName: _cityName), await _useCases.GetAsync(ct));
        Assert.Equal("Concurrency.Conflict", (await _useCases.SaveAsync(Key(), new("دیگر", CityId: _city), 0, ct)).ErrorCode);

        // A second first entry that slipped past the check is stopped by the table itself.
        await using (var raw = _dataSource.CreateCommand("INSERT INTO reference.company (id, name) VALUES (gen_random_uuid(), 'دوم')"))
        {
            Assert.Equal(PostgresErrorCodes.UniqueViolation, (await Assert.ThrowsAsync<PostgresException>(() => raw.ExecuteNonQueryAsync(ct))).SqlState);
        }

        Assert.True((await _useCases.AttachLogoAsync(Key(), Png, 1, ct)).IsSuccess);
        var logo = await _useCases.GetLogoAsync(ct);
        Assert.Equal("image/png", logo!.Value.ContentType);
        Assert.Equal(Png, logo.Value.Data);

        // The official details (Q38) are saved with the name and read back; the logo stays.
        var form = new CompanyForm("شرکت نمونه (سهامی خاص)", "۱۰۱۰۱۰۱۰۱۰۳", "4111-1111-1111", "12345", "تهران، خیابان آزادی، پلاک ۱", "1234567890", "021-88880000", "www.example.ir", "info@example.ir", "تهران، خیابان انقلاب، پلاک ۲", CityId: _city);
        Assert.True((await _useCases.SaveAsync(Key(), form, 2, ct)).IsSuccess);
        Assert.Equal(
            new CompanyView("شرکت نمونه (سهامی خاص)", true, 3, "10101010103", "411111111111", "12345", "تهران، خیابان آزادی، پلاک ۱", "1234567890", "021-88880000", "www.example.ir", "info@example.ir", "تهران، خیابان انقلاب، پلاک ۲", _city, _cityName),
            await _useCases.GetAsync(ct));
        Assert.Equal("Company.PostalCodeInvalid", (await _useCases.SaveAsync(Key(), form with { PostalCode = "123" }, 3, ct)).ErrorCode);
        Assert.True((await _useCases.AttachLogoAsync(Key(), Jpeg, 3, ct)).IsSuccess);
        logo = await _useCases.GetLogoAsync(ct);
        Assert.Equal("image/jpeg", logo!.Value.ContentType);
        Assert.Equal(Jpeg, logo.Value.Data);
        Assert.Equal(("شرکت نمونه (سهامی خاص)", true, 4L, "10101010103"), await _useCases.GetAsync(ct) is var v ? (v.Name, v.HasLogo, v.Version, v.NationalId) : default);
        Assert.Equal("Concurrency.Conflict", (await _useCases.RemoveLogoAsync(Key(), 3, ct)).ErrorCode);

        Assert.True((await _useCases.RemoveLogoAsync(Key(), 4, ct)).IsSuccess);
        Assert.Null(await _useCases.GetLogoAsync(ct));
        Assert.Equal(0, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.company_logo"));
        Assert.Equal(5, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.outbox WHERE type = 'CompanyProfileChanged'"));
    }
}
