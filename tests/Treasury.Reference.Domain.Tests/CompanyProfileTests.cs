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
        c.Rename(" شرکت نمونه ");
        Assert.Empty(c.PendingEvents);
        c.Rename("شرکت نمونه (سهامی خاص)");
        Assert.Equal("شرکت نمونه (سهامی خاص)", c.Name);
        Assert.Equal("شرکت نمونه (سهامی خاص)", ((CompanyProfileChanged)Assert.Single(c.PendingEvents)).Name);
        Assert.Equal("Company.NameRequired", Code(() => c.Rename("")));
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
