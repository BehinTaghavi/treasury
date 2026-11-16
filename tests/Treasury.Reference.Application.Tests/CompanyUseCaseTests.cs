using Treasury.Reference.Application.Company;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

/// <summary>«اطلاعات شرکت» (owner, 16 Nov 2026): one profile per database, with the company's name and logo.</summary>
public sealed class CompanyUseCaseTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9];

    private readonly InMemoryReferenceDatabase _db = new();
    private readonly CompanyUseCases _useCases;

    public CompanyUseCaseTests() =>
        _useCases = new CompanyUseCases(new InMemoryUnitOfWork(_db), new InMemoryCompanyRepository(_db), new InMemoryIdempotencyStore(_db));

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
        var first = await _useCases.SaveNameAsync(Key(), " شرکت  ایران ناژو ", 0, CancellationToken.None);
        Assert.True(first.IsSuccess, first.ErrorCode);
        Assert.Equal(new CompanyView("شرکت ایران ناژو", false, 1), await Company());

        var corrected = await _useCases.SaveNameAsync(Key(), "شرکت ایران ناژو (سهامی خاص)", 1, CancellationToken.None);
        Assert.Equal(first.Value, corrected.Value); // still the one profile of the database
        Assert.Equal(new CompanyView("شرکت ایران ناژو (سهامی خاص)", false, 2), await Company());

        Assert.Equal("Company.NameRequired", (await _useCases.SaveNameAsync(Key(), " ", 2, CancellationToken.None)).ErrorCode);
        Assert.Equal(2, _db.OutboxMessages.Count(m => m.Type == "CompanyProfileChanged"));
    }

    [Fact]
    public async Task A_change_made_on_an_old_copy_of_the_form_is_refused()
    {
        await _useCases.SaveNameAsync(Key(), "شرکت نمونه", 0, CancellationToken.None);
        Assert.Equal("Concurrency.Conflict", (await _useCases.SaveNameAsync(Key(), "دیگر", 0, CancellationToken.None)).ErrorCode);
        Assert.Equal("Concurrency.Conflict", (await _useCases.AttachLogoAsync(Key(), Png, 5, CancellationToken.None)).ErrorCode);
        Assert.Equal("شرکت نمونه", (await Company()).Name);
    }

    [Fact]
    public async Task The_same_request_sent_twice_is_saved_once()
    {
        var key = Key();
        var first = await _useCases.SaveNameAsync(key, "شرکت نمونه", 0, CancellationToken.None);
        var again = await _useCases.SaveNameAsync(key, "شرکت نمونه", 0, CancellationToken.None);
        Assert.Equal(first.Value, again.Value);
        Assert.Equal(1, (await Company()).Version);
        Assert.Equal("Request.IdempotencyKeyRequired", (await _useCases.SaveNameAsync(" ", "شرکت نمونه", 1, CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_logo_follows_the_name_is_replaced_and_survives_a_name_correction()
    {
        Assert.Equal("Company.NameRequired", (await _useCases.AttachLogoAsync(Key(), Png, 0, CancellationToken.None)).ErrorCode);
        await _useCases.SaveNameAsync(Key(), "شرکت نمونه", 0, CancellationToken.None);

        Assert.Equal("Company.LogoInvalid", (await _useCases.AttachLogoAsync(Key(), [1, 2, 3], 1, CancellationToken.None)).ErrorCode);
        Assert.Null(await _useCases.GetLogoAsync(CancellationToken.None));

        Assert.True((await _useCases.AttachLogoAsync(Key(), Png, 1, CancellationToken.None)).IsSuccess);
        Assert.Equal(new CompanyView("شرکت نمونه", true, 2), await Company());
        var logo = await _useCases.GetLogoAsync(CancellationToken.None);
        Assert.Equal("image/png", logo!.Value.ContentType);
        Assert.Equal(Png, logo.Value.Data);

        Assert.True((await _useCases.SaveNameAsync(Key(), "شرکت نمونه نو", 2, CancellationToken.None)).IsSuccess);
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
        await _useCases.SaveNameAsync(Key(), "شرکت نمونه", 0, CancellationToken.None);
        Assert.Equal("Company.NoLogo", (await _useCases.RemoveLogoAsync(Key(), 1, CancellationToken.None)).ErrorCode);
        await _useCases.AttachLogoAsync(Key(), Png, 1, CancellationToken.None);

        Assert.True((await _useCases.RemoveLogoAsync(Key(), 2, CancellationToken.None)).IsSuccess);
        Assert.Equal(new CompanyView("شرکت نمونه", false, 3), await Company());
        Assert.Null(await _useCases.GetLogoAsync(CancellationToken.None));
    }
}
