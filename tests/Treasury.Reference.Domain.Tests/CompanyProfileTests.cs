using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Company;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>«اطلاعات شرکت» (owner, 16 Nov 2026): the company's name and logo, shown at the top of every screen.</summary>
public sealed class CompanyProfileTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void The_company_has_a_name_of_at_most_100_characters()
    {
        var c = CompanyProfile.Register(Guid.NewGuid(), "  شرکت   ایران ناژو ");
        Assert.Equal("شرکت ایران ناژو", c.Name);
        Assert.False(c.HasLogo);
        Assert.IsType<CompanyProfileChanged>(Assert.Single(c.PendingEvents));
        Assert.Equal("Company.NameRequired", Code(() => CompanyProfile.Register(Guid.NewGuid(), "  ")));
        Assert.Equal("Company.NameRequired", Code(() => CompanyProfile.Register(Guid.NewGuid(), null)));
        Assert.Equal("Company.NameTooLong", Code(() => CompanyProfile.Register(Guid.NewGuid(), new string('ش', 101))));
        Assert.Equal(100, CompanyProfile.Register(Guid.NewGuid(), new string('ش', 100)).Name.Length);
    }

    [Fact]
    public void The_name_can_be_corrected_and_the_same_name_changes_nothing()
    {
        var c = CompanyProfile.Restore(Guid.NewGuid(), "شرکت نمونه", null, 3);
        c.Edit(" شرکت نمونه ", null);
        Assert.Empty(c.PendingEvents);
        c.Edit("شرکت نمونه (سهامی خاص)", null);
        Assert.Equal("شرکت نمونه (سهامی خاص)", c.Name);
        Assert.Equal("شرکت نمونه (سهامی خاص)", ((CompanyProfileChanged)Assert.Single(c.PendingEvents)).Name);
        Assert.Equal("Company.NameRequired", Code(() => c.Edit("", null)));
    }

    [Fact]
    public void The_official_details_are_optional_and_kept_in_one_form_owner_answer_Q38()
    {
        var none = CompanyProfile.Register(Guid.NewGuid(), "شرکت نمونه");
        Assert.Equal(CompanyDetails.None, none.Details);
        Assert.Equal(CompanyDetails.None, CompanyProfile.Register(Guid.NewGuid(), "شرکت نمونه", new(" ", "", null, "  ")).Details);

        // Typed with a Persian keyboard, dashes and spaces: stored as plain Latin digits and cleaned text.
        var c = CompanyProfile.Register(Guid.NewGuid(), "شرکت نمونه", new(
            "۱۰۱۰۱۰۱۰۱۰۳", "4111-1111-1111", "۱۲۳۴۵", "  تهران،   خیابان آزادی، پلاک ۱ ", "12345-67890", "(021) 8888-0000", " www.example.ir ", " info@example.ir "));
        Assert.Equal(
            new CompanyDetails("10101010103", "411111111111", "12345", "تهران، خیابان آزادی، پلاک ۱", "1234567890", "0218888-0000", "www.example.ir", "info@example.ir"),
            c.Details);
        Assert.Equal(c.Details, ((CompanyProfileChanged)Assert.Single(c.PendingEvents)).Details);
    }

    [Fact]
    public void A_detail_that_is_filled_in_is_checked()
    {
        static string Refused(CompanyDetails d) => Code(() => CompanyProfile.Register(Guid.NewGuid(), "شرکت نمونه", d));
        Assert.Equal("Company.NationalIdInvalid", Refused(new(NationalId: "10101010104"))); // wrong check digit
        Assert.Equal("Company.NationalIdInvalid", Refused(new(NationalId: "1234567890"))); // a person's 10-digit code
        Assert.Equal("Company.EconomicCodeInvalid", Refused(new(EconomicCode: "41111")));
        Assert.Equal("Company.EconomicCodeInvalid", Refused(new(EconomicCode: "4111111111111")));
        Assert.Equal("Company.RegistrationNumberInvalid", Refused(new(RegistrationNumber: "12/345")));
        Assert.Equal("Company.RegistrationNumberInvalid", Refused(new(RegistrationNumber: "12345678901")));
        Assert.Equal("Company.AddressTooLong", Refused(new(Address: new string('خ', 301))));
        Assert.Equal("Company.PostalCodeInvalid", Refused(new(PostalCode: "12345")));
        Assert.Equal("Company.PhoneInvalid", Refused(new(Phone: "تلفن")));
        Assert.Equal("Company.PhoneInvalid", Refused(new(Phone: "123")));
        Assert.Equal("Company.WebsiteInvalid", Refused(new(Website: "example")));
        Assert.Equal("Company.WebsiteInvalid", Refused(new(Website: "info@example.ir")));
        Assert.Equal("Company.EmailInvalid", Refused(new(Email: "www.example.ir")));
        Assert.Equal("Company.EmailInvalid", Refused(new(Email: "info@example")));

        // Both forms of the economic code, a website with its scheme and path, a phone with country code.
        var ok = CompanyProfile.Register(Guid.NewGuid(), "شرکت نمونه", new(EconomicCode: "10101010103", Phone: "+98-21-88880000", Website: "https://Example.ir/fa"));
        Assert.Equal(("10101010103", "+98-21-88880000", "https://Example.ir/fa"), (ok.Details.EconomicCode, ok.Details.Phone, ok.Details.Website));
    }

    [Fact]
    public void Correcting_the_details_keeps_the_logo_and_the_same_details_change_nothing()
    {
        var details = new CompanyDetails(NationalId: "10101010103", Phone: "021-88880000");
        var c = CompanyProfile.Restore(Guid.NewGuid(), "شرکت نمونه", "image/png", 4, details);
        c.Edit("شرکت نمونه", new(NationalId: "۱۰۱۰۱۰۱۰۱۰۳", Phone: "021-88880000"));
        Assert.Empty(c.PendingEvents);
        c.Edit("شرکت نمونه", details with { Email = "info@example.ir" });
        Assert.Equal("info@example.ir", c.Details.Email);
        Assert.True(c.HasLogo);
        Assert.Single(c.PendingEvents);
        Assert.Equal("Company.PostalCodeInvalid", Code(() => c.Edit("شرکت نمونه", new(PostalCode: "1"))));
        Assert.Equal("info@example.ir", c.Details.Email); // a refused correction changes nothing
    }

    [Fact]
    public void The_logo_is_a_png_or_jpeg_picture_of_at_most_1_megabyte()
    {
        var c = CompanyProfile.Register(Guid.NewGuid(), "شرکت نمونه");
        Assert.Equal("image/png", c.AttachLogo(Png));
        Assert.True(c.HasLogo);
        Assert.Equal("image/jpeg", c.AttachLogo(Jpeg));
        Assert.Equal("image/jpeg", c.LogoType);
        Assert.Equal("Company.LogoInvalid", Code(() => c.AttachLogo([])));
        Assert.Equal("Company.LogoInvalid", Code(() => c.AttachLogo("%PDF-1.7"u8))); // a scan saved as PDF is not a picture
        var large = new byte[CompanyProfile.MaxLogoBytes + 1];
        Png.CopyTo(large, 0);
        Assert.Equal("Company.LogoTooLarge", Code(() => c.AttachLogo(large)));
        Assert.Equal("image/png", c.AttachLogo(large.AsSpan(0, CompanyProfile.MaxLogoBytes)));
    }

    [Fact]
    public void The_logo_can_be_removed_once()
    {
        var c = CompanyProfile.Restore(Guid.NewGuid(), "شرکت نمونه", "image/png", 2);
        c.RemoveLogo();
        Assert.False(c.HasLogo);
        Assert.False(((CompanyProfileChanged)Assert.Single(c.PendingEvents)).HasLogo);
        Assert.Equal("Company.NoLogo", Code(c.RemoveLogo));
    }
}
