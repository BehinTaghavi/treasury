using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Company;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>
/// «مشخصات شرکت» of Basic Information (owner, 18 Nov 2026): the company's name, address and the city of the address, which decides
/// «خارج از مرکز» on received cheques. New: the old program and the manual's 14 items have no such page.
/// </summary>
public sealed class CompanyProfileTests
{
    private static readonly CompanyCity Tehran = new(Guid.NewGuid(), "تهران", true);
    private static readonly CompanyCity Shiraz = new(Guid.NewGuid(), "شیراز", true);

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void The_profile_keeps_the_name_the_address_and_the_city_of_the_address()
    {
        var p = CompanyProfile.Register("  شرکت   نمونه ", " خیابان ولیعصر، پلاک ۱ ", " خیابان  انقلاب، پلاک ۲ ", Tehran, "۱۲۳۴۵-۶۷۸۹۰");
        Assert.Equal(
            (CompanyProfile.TheId, "شرکت نمونه", "خیابان ولیعصر، پلاک ۱", "خیابان انقلاب، پلاک ۲", Tehran.Id, "1234567890"),
            (p.Id, p.Name, p.Address, p.LegalAddress, p.CityId, p.PostalCode));
        var e = Assert.IsType<CompanyProfileChanged>(Assert.Single(p.PendingEvents));
        Assert.Equal("تهران", e.CityName);
    }

    [Fact]
    public void The_name_and_an_active_city_are_required_and_the_postal_code_has_ten_digits()
    {
        Assert.Equal("Company.NameRequired", Code(() => CompanyProfile.Register(" ", null, null, Tehran, null)));
        Assert.Equal("Company.CityRequired", Code(() => CompanyProfile.Register("شرکت نمونه", null, null, null, null)));
        Assert.Equal("Company.CityInactive", Code(() => CompanyProfile.Register("شرکت نمونه", null, null, Tehran with { IsActive = false }, null)));
        Assert.Equal("Company.PostalCodeInvalid", Code(() => CompanyProfile.Register("شرکت نمونه", null, null, Tehran, "12345")));
        var p = CompanyProfile.Register("شرکت نمونه", " ", " ", Tehran, " ");
        Assert.Equal(((string?)null, (string?)null, (string?)null), (p.Address, p.LegalAddress, p.PostalCode));
    }

    [Fact]
    public void The_company_can_move_to_another_city()
    {
        var p = CompanyProfile.Restore("شرکت نمونه", null, null, Tehran.Id, null, 3);
        p.Change("شرکت نمونه (سهامی خاص)", "شیراز، بلوار زند", "شیراز، خیابان قصرالدشت", Shiraz, null);
        Assert.Equal(("شرکت نمونه (سهامی خاص)", Shiraz.Id, 3L), (p.Name, p.CityId, p.Version));
        Assert.Equal("Company.CityRequired", Code(() => p.Change("شرکت نمونه", null, null, null, null)));
        Assert.Equal(Shiraz.Id, p.CityId);
    }
}
