using Npgsql;
using Treasury.Reference.Application.Company;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Reference.Infrastructure.Postgres.Tests;

/// <summary>«اطلاعات شرکت» through the real PostgreSQL adapter, acting as the reference_svc role (033_reference_company.sql).</summary>
public sealed class CompanyPostgresTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9];

    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly CompanyUseCases _useCases;

    public CompanyPostgresTests(PostgresDatabase db)
    {
        _db = db;
        _db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("reference_svc"));
        _useCases = new CompanyUseCases(new PostgresUnitOfWork(_dataSource), new PostgresCompanyRepository(), new PostgresIdempotencyStore(_dataSource));
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

        var first = await _useCases.SaveNameAsync(Key(), "شرکت نمونه", 0, ct);
        Assert.True(first.IsSuccess, first.ErrorCode);
        Assert.Equal(new CompanyView("شرکت نمونه", false, 1), await _useCases.GetAsync(ct));
        Assert.Equal("Concurrency.Conflict", (await _useCases.SaveNameAsync(Key(), "دیگر", 0, ct)).ErrorCode);

        // A second first entry that slipped past the check is stopped by the table itself.
        await using (var raw = _dataSource.CreateCommand("INSERT INTO reference.company (id, name) VALUES (gen_random_uuid(), 'دوم')"))
        {
            Assert.Equal(PostgresErrorCodes.UniqueViolation, (await Assert.ThrowsAsync<PostgresException>(() => raw.ExecuteNonQueryAsync(ct))).SqlState);
        }

        Assert.True((await _useCases.AttachLogoAsync(Key(), Png, 1, ct)).IsSuccess);
        var logo = await _useCases.GetLogoAsync(ct);
        Assert.Equal("image/png", logo!.Value.ContentType);
        Assert.Equal(Png, logo.Value.Data);

        Assert.True((await _useCases.SaveNameAsync(Key(), "شرکت نمونه (سهامی خاص)", 2, ct)).IsSuccess);
        Assert.True((await _useCases.AttachLogoAsync(Key(), Jpeg, 3, ct)).IsSuccess);
        logo = await _useCases.GetLogoAsync(ct);
        Assert.Equal("image/jpeg", logo!.Value.ContentType);
        Assert.Equal(Jpeg, logo.Value.Data);
        Assert.Equal(new CompanyView("شرکت نمونه (سهامی خاص)", true, 4), await _useCases.GetAsync(ct));
        Assert.Equal("Concurrency.Conflict", (await _useCases.RemoveLogoAsync(Key(), 3, ct)).ErrorCode);

        Assert.True((await _useCases.RemoveLogoAsync(Key(), 4, ct)).IsSuccess);
        Assert.Null(await _useCases.GetLogoAsync(ct));
        Assert.Equal(0, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.company_logo"));
        Assert.Equal(5, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.outbox WHERE type = 'CompanyProfileChanged'"));
    }
}
