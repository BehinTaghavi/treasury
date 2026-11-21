using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Company;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>
/// «مشخصات شرکت» of Basic Information. Owner, 18 Nov 2026: the company's name, address and the city of the legal address, which decides
/// «خارج از مرکز» on received cheques. Owner, 16 Nov 2026: the name and logo are shown at the top of every screen, and the official
/// details go on printed forms. New: the old program and the manual's 14 items have no such page.
/// </summary>
public sealed class CompanyProfileTests
{
    private static readonly CompanyCity Tehran = new(Guid.NewGuid(), "تهران", true);
    private static readonly CompanyCity Shiraz = new(Guid.NewGuid(), "شیراز", true);
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    private static CompanyProfile Sample(CompanyDetails? details = null) => CompanyProfile.Register("شرکت نمونه", null, null, Tehran, null, details);

    [Fact]
    public void The_profile_keeps_the_name_the_address_and_the_city_of_the_address()
    {
        var p = CompanyProfile.Register("  شرکت   نمونه ", " خیابان ولیعصر، پلاک ۱ ", " خیابان  انقلاب، پلاک ۲ ", Tehran, "۱۲۳۴۵-۶۷۸۹۰");
        Assert.Equal(
            (CompanyProfile.TheId, "شرکت نمونه", "خیابان ولیعصر، پلاک ۱", "خیابان انقلاب، پلاک ۲", Tehran.Id, "1234567890"),
            (p.Id, p.Name, p.Address, p.LegalAddress, p.CityId, p.PostalCode));
        Assert.False(p.HasLogo);
        var e = Assert.IsType<CompanyProfileChanged>(Assert.Single(p.PendingEvents));
        Assert.Equal("تهران", e.CityName);
    }

    [Fact]
    public void The_name_and_an_active_city_are_required_and_the_postal_code_has_ten_digits()
    {
        Assert.Equal("Company.NameRequired", Code(() => CompanyProfile.Register(" ", null, null, Tehran, null)));
        Assert.Equal("Company.NameRequired", Code(() => CompanyProfile.Register(null, null, null, Tehran, null)));
        Assert.Equal("Company.CityRequired", Code(() => CompanyProfile.Register("شرکت نمونه", null, null, null, null)));
        Assert.Equal("Company.CityInactive", Code(() => CompanyProfile.Register("شرکت نمونه", null, null, Tehran with { IsActive = false }, null)));
        Assert.Equal("Company.PostalCodeInvalid", Code(() => CompanyProfile.Register("شرکت نمونه", null, null, Tehran, "12345")));
        var p = CompanyProfile.Register("شرکت نمونه", " ", " ", Tehran, " ");
        Assert.Equal(((string?)null, (string?)null, (string?)null), (p.Address, p.LegalAddress, p.PostalCode));
    }

    [Fact]
    public void The_name_has_at_most_100_characters_and_an_address_at_most_300()
    {
        Assert.Equal("Company.NameTooLong", Code(() => CompanyProfile.Register(new string('ش', 101), null, null, Tehran, null)));
        Assert.Equal(100, CompanyProfile.Register(new string('ش', 100), null, null, Tehran, null).Name.Length);
        Assert.Equal("Company.AddressTooLong", Code(() => CompanyProfile.Register("شرکت نمونه", new string('خ', 301), null, Tehran, null)));
        Assert.Equal("Company.AddressTooLong", Code(() => CompanyProfile.Register("شرکت نمونه", null, new string('خ', 301), Tehran, null)));
        Assert.Equal(300, CompanyProfile.Register("شرکت نمونه", new string('خ', 300), null, Tehran, null).Address!.Length);
    }

    [Fact]
    public void The_company_can_move_to_another_city()
    {
        var p = CompanyProfile.Restore("شرکت نمونه", null, null, Tehran.Id, null, 3);
        p.Change("شرکت نمونه (سهامی خاص)", "شیراز، بلوار زند", "شیراز، خیابان قصرالدشت", Shiraz, null);
        Assert.Equal(("شرکت نمونه (سهامی خاص)", Shiraz.Id, 3L), (p.Name, p.CityId, p.Version));
        Assert.Equal("شیراز", ((CompanyProfileChanged)Assert.Single(p.PendingEvents)).CityName);
        Assert.Equal("Company.CityRequired", Code(() => p.Change("شرکت نمونه", null, null, null, null)));
        Assert.Equal(Shiraz.Id, p.CityId);
    }

    [Fact]
    public void The_name_can_be_corrected_and_the_same_form_changes_nothing()
    {
        var p = CompanyProfile.Restore("شرکت نمونه", null, null, Tehran.Id, null, 3);
        p.Change(" شرکت نمونه ", " ", null, Tehran, null);
        Assert.Empty(p.PendingEvents);
        p.Change("شرکت نمونه (سهامی خاص)", null, null, Tehran, null);
        Assert.Equal("شرکت نمونه (سهامی خاص)", ((CompanyProfileChanged)Assert.Single(p.PendingEvents)).Name);
        Assert.Equal("Company.NameRequired", Code(() => p.Change("", null, null, Tehran, null)));
        Assert.Equal("شرکت نمونه (سهامی خاص)", p.Name);
    }

    [Fact]
    public void The_official_details_are_optional_and_kept_in_one_form()
    {
        Assert.Equal(CompanyDetails.None, Sample().Details);
        Assert.Equal(CompanyDetails.None, Sample(new(" ", "", null, "  ")).Details);

        // Typed with a Persian keyboard, dashes and spaces: stored as plain Latin digits and cleaned text.
        var p = Sample(new("۱۰۱۰۱۰۱۰۱۰۳", "4111-1111-1111", "۱۲۳۴۵", "(021) 8888-0000", " www.example.ir ", " info@example.ir "));
        Assert.Equal(new CompanyDetails("10101010103", "411111111111", "12345", "0218888-0000", "www.example.ir", "info@example.ir"), p.Details);
        Assert.Equal(p.Details, ((CompanyProfileChanged)Assert.Single(p.PendingEvents)).Details);
    }

    [Fact]
    public void A_detail_that_is_filled_in_is_checked()
    {
        static string Refused(CompanyDetails d) => Code(() => Sample(d));
        Assert.Equal("Company.NationalIdInvalid", Refused(new(NationalId: "10101010104"))); // wrong check digit
        Assert.Equal("Company.NationalIdInvalid", Refused(new(NationalId: "1234567890"))); // a person's 10-digit code
        Assert.Equal("Company.EconomicCodeInvalid", Refused(new(EconomicCode: "41111")));
        Assert.Equal("Company.EconomicCodeInvalid", Refused(new(EconomicCode: "4111111111111")));
        Assert.Equal("Company.RegistrationNumberInvalid", Refused(new(RegistrationNumber: "12/345")));
        Assert.Equal("Company.RegistrationNumberInvalid", Refused(new(RegistrationNumber: "12345678901")));
        Assert.Equal("Company.PhoneInvalid", Refused(new(Phone: "تلفن")));
        Assert.Equal("Company.PhoneInvalid", Refused(new(Phone: "123")));
        Assert.Equal("Company.WebsiteInvalid", Refused(new(Website: "example")));
        Assert.Equal("Company.WebsiteInvalid", Refused(new(Website: "info@example.ir")));
        Assert.Equal("Company.EmailInvalid", Refused(new(Email: "www.example.ir")));
        Assert.Equal("Company.EmailInvalid", Refused(new(Email: "info@example")));

        // Both forms of the economic code, a website with its scheme and path, a phone with country code.
        var ok = Sample(new(EconomicCode: "10101010103", Phone: "+98-21-88880000", Website: "https://Example.ir/fa"));
        Assert.Equal(("10101010103", "+98-21-88880000", "https://Example.ir/fa"), (ok.Details.EconomicCode, ok.Details.Phone, ok.Details.Website));
    }

    [Fact]
    public void Correcting_the_details_keeps_the_logo_and_the_same_details_change_nothing()
    {
        var details = new CompanyDetails(NationalId: "10101010103", Phone: "021-88880000");
        var p = CompanyProfile.Restore("شرکت نمونه", null, null, Tehran.Id, null, 4, "image/png", details);
        p.Change("شرکت نمونه", null, null, Tehran, null, new(NationalId: "۱۰۱۰۱۰۱۰۱۰۳", Phone: "021-88880000"));
        Assert.Empty(p.PendingEvents);
        p.Change("شرکت نمونه", null, null, Tehran, null, details with { Email = "info@example.ir" });
        Assert.Equal("info@example.ir", p.Details.Email);
        Assert.True(p.HasLogo);
        Assert.Single(p.PendingEvents);
        Assert.Equal("Company.PhoneInvalid", Code(() => p.Change("شرکت نمونه", null, null, Tehran, null, new(Phone: "1"))));
        Assert.Equal("info@example.ir", p.Details.Email); // a refused correction changes nothing
    }

    [Fact]
    public void The_logo_is_a_png_or_jpeg_picture_of_at_most_1_megabyte()
    {
        var p = Sample();
        Assert.Equal("image/png", p.AttachLogo(Png));
        Assert.True(p.HasLogo);
        Assert.Equal("image/jpeg", p.AttachLogo(Jpeg));
        Assert.Equal("image/jpeg", p.LogoType);
        Assert.Equal("Company.LogoInvalid", Code(() => p.AttachLogo([])));
        Assert.Equal("Company.LogoInvalid", Code(() => p.AttachLogo("%PDF-1.7"u8))); // a scan saved as PDF is not a picture
        var large = new byte[CompanyProfile.MaxLogoBytes + 1];
        Png.CopyTo(large, 0);
        Assert.Equal("Company.LogoTooLarge", Code(() => p.AttachLogo(large)));
        Assert.Equal("image/png", p.AttachLogo(large.AsSpan(0, CompanyProfile.MaxLogoBytes)));
    }

    [Fact]
    public void The_logo_can_be_removed_once()
    {
        var p = CompanyProfile.Restore("شرکت نمونه", null, null, Tehran.Id, null, 2, "image/png");
        p.RemoveLogo();
        Assert.False(p.HasLogo);
        Assert.False(((CompanyProfileChanged)Assert.Single(p.PendingEvents)).HasLogo);
        Assert.Equal("Company.NoLogo", Code(p.RemoveLogo));
    }
}
