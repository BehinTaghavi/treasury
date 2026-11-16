using Treasury.BuildingBlocks.Domain;

namespace Treasury.Reference.Domain.Company;

/// <summary>
/// The company that uses this Treasury database («اطلاعات شرکت», owner, 16 Nov 2026): its name and its logo (آرم), scanned by the
/// user and stored in Basic Information; both are shown at the top of every screen. One database belongs to one client company, so
/// there is at most one profile. The old Access program and the MVC version kept the company name in their settings, not in a form.
/// </summary>
public sealed class CompanyProfile : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    /// <summary>A logo is a small picture shown in the bar of every screen; scans of documents (2 MB) are larger.</summary>
    public const int MaxLogoBytes = 1024 * 1024;

    private CompanyProfile(Guid id, string name)
        : base(id) => Name = name;

    public string Name { get; private set; }

    /// <summary>Media type of the stored logo (image/png or image/jpeg); null: no logo yet.</summary>
    public string? LogoType { get; private set; }

    public bool HasLogo => LogoType is not null;

    public static CompanyProfile Register(Guid id, string? name)
    {
        var c = new CompanyProfile(id, ValidName(name));
        c.Raise(CompanyProfileChanged.From(c));
        return c;
    }

    public void Rename(string? name)
    {
        var valid = ValidName(name);
        if (valid != Name)
        {
            Name = valid;
            Raise(CompanyProfileChanged.From(this));
        }
    }

    /// <summary>Checks a logo file (PNG or JPEG by its first bytes, at most 1 MB) and records its media type.</summary>
    public string AttachLogo(ReadOnlySpan<byte> file)
    {
        Require(file.Length > 0, "Company.LogoInvalid", "The logo file is empty.");
        Require(file.Length <= MaxLogoBytes, "Company.LogoTooLarge", "The logo file is larger than 1 MB.");
        var type = ImageTypeOf(file) ?? throw new DomainException("Company.LogoInvalid", "The logo must be a PNG or JPEG picture.");
        LogoType = type;
        Raise(CompanyProfileChanged.From(this));
        return type;
    }

    public void RemoveLogo()
    {
        Require(HasLogo, "Company.NoLogo", "The company has no logo.");
        LogoType = null;
        Raise(CompanyProfileChanged.From(this));
    }

    public static CompanyProfile Restore(Guid id, string name, string? logoType, long version)
    {
        var c = new CompanyProfile(id, name) { LogoType = logoType };
        c.SetLoadedVersion(version);
        return c;
    }

    private static string ValidName(string? name)
    {
        var cleaned = PersianText.Clean(name);
        Require(cleaned.Length > 0, "Company.NameRequired", "The company name is required.");
        Require(cleaned.Length <= MaxNameLength, "Company.NameTooLong", $"At most {MaxNameLength} characters.");
        return cleaned;
    }

    private static string? ImageTypeOf(ReadOnlySpan<byte> b) =>
        b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ? "image/png"
        : b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ? "image/jpeg"
        : null;

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

public sealed record CompanyProfileChanged(Guid CompanyId, string Name, bool HasLogo) : DomainEvent
{
    public static CompanyProfileChanged From(CompanyProfile c) => new(c.Id, c.Name, c.HasLogo);
}
