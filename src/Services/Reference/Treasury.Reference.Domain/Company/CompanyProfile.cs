using System.Text.RegularExpressions;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Organization;

namespace Treasury.Reference.Domain.Company;

/// <summary>A city of the Basic Information list as the profile needs it.</summary>
public sealed record CompanyCity(Guid Id, string Name, bool IsActive);

/// <summary>
/// The company's official details (owner, 16 Nov 2026): شناسه ملی, کد اقتصادی, شماره ثبت, تلفن, آدرس سایت and ایمیل. They go on
/// printed forms with the name, logo and addresses. Each is optional; one that is filled in is checked. The address and the postal
/// code he listed with them are the profile's own <see cref="CompanyProfile.Address"/> and <see cref="CompanyProfile.PostalCode"/>.
/// </summary>
/// <param name="NationalId">شناسه ملی of a legal person: 11 digits with a valid check digit.</param>
/// <param name="EconomicCode">کد اقتصادی: 12 digits (the older codes) or 11 (the newer ones, equal to the national ID).</param>
/// <param name="RegistrationNumber">شماره ثبت: up to 10 digits.</param>
public sealed record CompanyDetails(
    string? NationalId = null, string? EconomicCode = null, string? RegistrationNumber = null, string? Phone = null, string? Website = null,
    string? Email = null)
{
    public static readonly CompanyDetails None = new();
}

/// <summary>
/// The company's own particulars («مشخصات شرکت», Basic Information; one per client database). Two requests of the owner, kept on one page:
/// <list type="bullet">
/// <item>16 Nov 2026: the company's name and its logo (آرم), scanned by the user, are shown at the top of every screen, and with the
/// official details (<see cref="CompanyDetails"/>) at the top of printed forms.</item>
/// <item>18 Nov 2026: a received cheque is «خارج از مرکز» when the city of its bank differs from the city of the company's address, so
/// the company's city is kept here. Besides the address there is the legal address («آدرس قانونی»); the city (its own field) and the
/// postal code belong to the legal address.</item>
/// </list>
/// The old Access program and the MVC version kept the company name in their settings, not in a form.
/// </summary>
public sealed partial class CompanyProfile : AggregateRoot<Guid>
{
    /// <summary>There is one profile; it always has this id.</summary>
    public static readonly Guid TheId = new("c0a11e55-0000-4000-8000-000000000001");

    public const int MaxNameLength = 100;
    public const int MaxAddressLength = 300;
    public const int MaxWebsiteLength = 100;
    public const int MaxEmailLength = 100;
    public const int PostalCodeLength = 10;

    /// <summary>A logo is a small picture shown in the bar of every screen; scans of documents (2 MB) are larger.</summary>
    public const int MaxLogoBytes = 1024 * 1024;

    private CompanyProfile(string name, string? address, string? legalAddress, Guid cityId, string? postalCode, CompanyDetails details)
        : base(TheId)
    {
        (Name, Address, LegalAddress, CityId, PostalCode, Details) = (name, address, legalAddress, cityId, postalCode, details);
    }

    public string Name { get; private set; }

    /// <summary>The address the company works at.</summary>
    public string? Address { get; private set; }

    /// <summary>The legal (registered) address; <see cref="CityId"/> and <see cref="PostalCode"/> are its city and postal code.</summary>
    public string? LegalAddress { get; private set; }

    /// <summary>The city of the legal address, from the Basic Information list of cities.</summary>
    public Guid CityId { get; private set; }

    public string? PostalCode { get; private set; }

    public CompanyDetails Details { get; private set; }

    /// <summary>Media type of the stored logo (image/png or image/jpeg); null: no logo yet.</summary>
    public string? LogoType { get; private set; }

    public bool HasLogo => LogoType is not null;

    public static CompanyProfile Register(
        string? name, string? address, string? legalAddress, CompanyCity? city, string? postalCode, CompanyDetails? details = null)
    {
        var (n, a, l, p, d) = Check(name, address, legalAddress, city, postalCode, details);
        var profile = new CompanyProfile(n, a, l, city!.Id, p, d);
        profile.Raise(profile.Changed(city.Name));
        return profile;
    }

    /// <summary>Corrects the whole form at once, as the page shows it; the logo is not part of the form and stays. The same data changes nothing.</summary>
    public void Change(string? name, string? address, string? legalAddress, CompanyCity? city, string? postalCode, CompanyDetails? details = null)
    {
        var (n, a, l, p, d) = Check(name, address, legalAddress, city, postalCode, details);
        if ((n, a, l, city!.Id, p, d) == (Name, Address, LegalAddress, CityId, PostalCode, Details))
        {
            return;
        }

        (Name, Address, LegalAddress, CityId, PostalCode, Details) = (n, a, l, city.Id, p, d);
        Raise(Changed(city.Name));
    }

    /// <summary>Checks a logo file (PNG or JPEG by its first bytes, at most 1 MB) and records its media type.</summary>
    public string AttachLogo(ReadOnlySpan<byte> file)
    {
        Require(file.Length > 0, "Company.LogoInvalid", "The logo file is empty.");
        Require(file.Length <= MaxLogoBytes, "Company.LogoTooLarge", "The logo file is larger than 1 MB.");
        var type = ImageTypeOf(file) ?? throw new DomainException("Company.LogoInvalid", "The logo must be a PNG or JPEG picture.");
        LogoType = type;
        Raise(Changed(null));
        return type;
    }

    public void RemoveLogo()
    {
        Require(HasLogo, "Company.NoLogo", "The company has no logo.");
        LogoType = null;
        Raise(Changed(null));
    }

    public static CompanyProfile Restore(
        string name, string? address, string? legalAddress, Guid cityId, string? postalCode, long version, string? logoType = null,
        CompanyDetails? details = null)
    {
        var profile = new CompanyProfile(name, address, legalAddress, cityId, postalCode, details ?? CompanyDetails.None) { LogoType = logoType };
        profile.SetLoadedVersion(version);
        return profile;
    }

    private CompanyProfileChanged Changed(string? cityName) => new(Name, CityId, cityName, HasLogo, Details);

    private static (string Name, string? Address, string? LegalAddress, string? PostalCode, CompanyDetails Details) Check(
        string? name, string? address, string? legalAddress, CompanyCity? city, string? postalCode, CompanyDetails? details)
    {
        var n = PersianText.Clean(name);
        Require(n.Length > 0, "Company.NameRequired", "Enter the company's name.");
        Require(n.Length <= MaxNameLength, "Company.NameTooLong", $"At most {MaxNameLength} characters.");
        Require(city is not null, "Company.CityRequired", "Choose the city of the company's legal address.");
        Require(city!.IsActive, "Company.CityInactive", "The city is switched off.");
        // Persian digits and a dash in the middle are accepted, as for a beneficiary's postal code.
        var p = Digits(postalCode);
        Require(p is null || (p.Length == PostalCodeLength && p.All(char.IsAsciiDigit)), "Company.PostalCodeInvalid", "The postal code has 10 digits.");
        var a = Text(address);
        var l = Text(legalAddress);
        Require((a?.Length ?? 0) <= MaxAddressLength && (l?.Length ?? 0) <= MaxAddressLength, "Company.AddressTooLong", $"An address has at most {MaxAddressLength} characters.");
        return (n, a, l, p, ValidDetails(details));
    }

    /// <summary>Digits are kept in Latin form whatever keyboard typed them; spaces and dashes inside numbers are dropped.</summary>
    private static CompanyDetails ValidDetails(CompanyDetails? details)
    {
        var d = details ?? CompanyDetails.None;
        var nationalId = Digits(d.NationalId);
        Require(nationalId is null || Beneficiary.IsLegalNationalId(nationalId), "Company.NationalIdInvalid", "The national ID has 11 digits and a valid check digit.");
        var economic = Digits(d.EconomicCode);
        Require(economic is null || (economic.Length is 11 or 12 && economic.All(char.IsAsciiDigit)), "Company.EconomicCodeInvalid", "The economic code has 11 or 12 digits.");
        var registration = Digits(d.RegistrationNumber);
        Require(registration is null || (registration.Length <= 10 && registration.All(char.IsAsciiDigit)), "Company.RegistrationNumberInvalid", "The registration number has at most 10 digits.");
        var phone = Text(d.Phone) is { } p ? PersianText.Key(p).Replace("(", string.Empty, StringComparison.Ordinal).Replace(")", string.Empty, StringComparison.Ordinal) : null;
        Require(phone is null || PhonePattern().IsMatch(phone), "Company.PhoneInvalid", "A phone number has 6 to 20 digits.");
        var website = Text(d.Website)?.Replace(" ", string.Empty, StringComparison.Ordinal);
        Require(website is null || (website.Length <= MaxWebsiteLength && WebsitePattern().IsMatch(website)), "Company.WebsiteInvalid", "The website address is not valid.");
        var email = Text(d.Email)?.Replace(" ", string.Empty, StringComparison.Ordinal);
        Require(email is null || (email.Length <= MaxEmailLength && EmailPattern().IsMatch(email)), "Company.EmailInvalid", "The e-mail address is not valid.");
        return new CompanyDetails(nationalId, economic, registration, phone, website, email);
    }

    private static string? Text(string? text) => PersianText.Clean(text) is { Length: > 0 } c ? c : null;

    private static string? Digits(string? text) => Text(text) is { } t ? PersianText.Key(t).Replace("-", string.Empty, StringComparison.Ordinal) : null;

    [GeneratedRegex("^\\+?[0-9\\-]{6,20}$")]
    private static partial Regex PhonePattern();

    [GeneratedRegex("^(https?://)?[\\p{L}0-9\\-]+(\\.[\\p{L}0-9\\-]+)+(/[^\\s]*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex WebsitePattern();

    [GeneratedRegex("^[^@\\s]+@[^@\\s.]+(\\.[^@\\s.]+)+$")]
    private static partial Regex EmailPattern();

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

/// <param name="CityName">The city's name when the form was saved; null on an event about the logo only.</param>
public sealed record CompanyProfileChanged(string Name, Guid CityId, string? CityName, bool HasLogo, CompanyDetails Details) : DomainEvent;
