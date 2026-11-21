using Npgsql;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Application.Company;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Reference.Infrastructure.Postgres.Tests;

/// <summary>
/// «مشخصات شرکت» through the real PostgreSQL adapter, acting as the reference_svc role (037_reference_company_profile.sql,
/// 038_reference_company_details_and_logo.sql).
/// </summary>
public sealed class CompanyPostgresTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9];

    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly CatalogueUseCases _catalogue;
    private readonly CompanyUseCases _useCases;

    public CompanyPostgresTests(PostgresDatabase db)
    {
        _db = db;
        _db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("reference_svc"));
        var uow = new PostgresUnitOfWork(_dataSource);
        var items = new PostgresCatalogueRepository();
        var idempotency = new PostgresIdempotencyStore(_dataSource);
        _catalogue = new CatalogueUseCases(uow, items, idempotency);
        _useCases = new CompanyUseCases(uow, new PostgresCompanyProfileRepository(), items, idempotency, _catalogue);
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private async Task<Guid> Add(CatalogueKind kind, string name, Guid? parent = null)
    {
        var r = await _catalogue.AddAsync(new AddCatalogueItemCommand(Key(), kind, name, parent), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    // One test: the database has one company profile, so the steps run in order on it.
    [Fact]
    public async Task The_company_profile_and_logo_round_trip_and_there_is_one_profile()
    {
        var ct = TestContext.Current.CancellationToken;
        var tag = Guid.NewGuid().ToString("N")[..8];
        var province = await Add(CatalogueKind.Province, $"استان {tag}", await Add(CatalogueKind.Country, $"کشور {tag}"));
        var city = await Add(CatalogueKind.City, $"شهر {tag}", province);
        var other = await Add(CatalogueKind.City, $"شهر دیگر {tag}", province);

        Assert.Equal(CompanyProfileView.NotSet, await _useCases.GetAsync(ct));
        Assert.Null(await _useCases.GetLogoAsync(ct));

        var first = await _useCases.SetAsync(Key(), new("شرکت نمونه", "خیابان ولیعصر", "خیابان انقلاب", city, "1234567890"), 0, ct);
        Assert.True(first.IsSuccess, first.ErrorCode);
        Assert.Equal(
            new CompanyProfileView(true, "شرکت نمونه", "خیابان ولیعصر", "خیابان انقلاب", city, $"شهر {tag}", "1234567890", 1),
            await _useCases.GetAsync(ct));
        Assert.Equal("Concurrency.Conflict", (await _useCases.SetAsync(Key(), new("دیگر", null, null, other, null), 0, ct)).ErrorCode);
        Assert.True((await _catalogue.GetAsync(city, ct))!.IsInUse);

        // A second first entry that slipped past the check is stopped by the table itself.
        await using (var raw = _dataSource.CreateCommand("INSERT INTO reference.company_profile (name, city_id) VALUES ('دوم', gen_random_uuid())"))
        {
            Assert.Equal(PostgresErrorCodes.UniqueViolation, (await Assert.ThrowsAsync<PostgresException>(() => raw.ExecuteNonQueryAsync(ct))).SqlState);
        }

        Assert.True((await _useCases.AttachLogoAsync(Key(), Png, 1, ct)).IsSuccess);
        var logo = await _useCases.GetLogoAsync(ct);
        Assert.Equal("image/png", logo!.Value.ContentType);
        Assert.Equal(Png, logo.Value.Data);

        // The official details are saved with the form and read back; the company moves to another city; the logo stays.
        var form = new CompanyProfileForm(
            "شرکت نمونه (سهامی خاص)", "تهران، خیابان آزادی، پلاک ۱", null, other, null, "۱۰۱۰۱۰۱۰۱۰۳", "4111-1111-1111", "12345", "021-88880000",
            "www.example.ir", "info@example.ir");
        Assert.True((await _useCases.SetAsync(Key(), form, 2, ct)).IsSuccess);
        Assert.Equal(
            new CompanyProfileView(
                true, "شرکت نمونه (سهامی خاص)", "تهران، خیابان آزادی، پلاک ۱", null, other, $"شهر دیگر {tag}", null, 3, true, "10101010103", "411111111111",
                "12345", "021-88880000", "www.example.ir", "info@example.ir"),
            await _useCases.GetAsync(ct));
        Assert.Equal("Company.PostalCodeInvalid", (await _useCases.SetAsync(Key(), form with { PostalCode = "123" }, 3, ct)).ErrorCode);
        Assert.True((await _useCases.AttachLogoAsync(Key(), Jpeg, 3, ct)).IsSuccess);
        logo = await _useCases.GetLogoAsync(ct);
        Assert.Equal("image/jpeg", logo!.Value.ContentType);
        Assert.Equal(Jpeg, logo.Value.Data);
        Assert.Equal(("شرکت نمونه (سهامی خاص)", true, 4L, "10101010103"), await _useCases.GetAsync(ct) is var v ? (v.Name, v.HasLogo, v.Version, v.NationalId) : default);
        Assert.Equal("Concurrency.Conflict", (await _useCases.RemoveLogoAsync(Key(), 3, ct)).ErrorCode);

        Assert.True((await _useCases.RemoveLogoAsync(Key(), 4, ct)).IsSuccess);
        Assert.Null(await _useCases.GetLogoAsync(ct));
        Assert.Equal(0, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.company_profile_logo"));
        Assert.Equal(5, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.outbox WHERE type = 'CompanyProfileChanged'"));
    }
}
