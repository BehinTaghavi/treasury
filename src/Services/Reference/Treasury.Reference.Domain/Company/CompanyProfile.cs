using System.Text.RegularExpressions;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Organization;

namespace Treasury.Reference.Domain.Company;

/// <summary>A city of the Basic Information list as the profile needs it.</summary>
public sealed record CompanyCity(Guid Id, string Name, bool IsActive);

/// <summary>
/// The company's official details (owner's answer to Q38, 16 Nov 2026): شناسه ملی, کد اقتصادی, شماره ثبت, نشانی, کد پستی, تلفن,
/// آدرس سایت and ایمیل. They go on printed forms with the name and logo (Q37). Each is optional; one that is filled in is checked.
/// Owner, 18 Nov 2026 («مشخصات شرکت»): besides the address there is the legal address («آدرس قانونی»); the postal code and the city
/// (<see cref="CompanyProfile.CityId"/>) are those of the legal address.
/// </summary>
/// <param name="NationalId">شناسه ملی of a legal person: 11 digits with a valid check digit.</param>
/// <param name="EconomicCode">کد اقتصادی: 12 digits (the older codes) or 11 (the newer ones, equal to the national ID).</param>
/// <param name="RegistrationNumber">شماره ثبت: up to 10 digits.</param>
public sealed record CompanyDetails(
    string? NationalId = null, string? EconomicCode = null, string? RegistrationNumber = null, string? Address = null, string? PostalCode = null,
    string? Phone = null, string? Website = null, string? Email = null, string? LegalAddress = null)
{
    public static readonly CompanyDetails None = new();
}

/// <summary>
/// The company that uses this Treasury database («اطلاعات شرکت», owner, 16 Nov 2026): its name and its logo (آرم), scanned by the
/// user and stored in Basic Information; both are shown at the top of every screen, and with the company's official details at the
/// top of printed forms. One database belongs to one client company, so there is at most one profile. The old Access program and
/// the MVC version kept the company name in their settings, not in a form.
/// The page «مشخصات شرکت» of 18 Nov 2026 (name, address, legal address with its city and postal code) is the same page: the owner
/// confirmed on 18 Nov 2026 that the two become one. The city of the legal address is required, because a received cheque is
/// «خارج از مرکز» when the city of its bank is another city.
/// </summary>
public sealed partial class CompanyProfile : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;
    public const int MaxAddressLength = 300;
    public const int MaxWebsiteLength = 100;
    public const int MaxEmailLength = 100;

    /// <summary>A logo is a small picture shown in the bar of every screen; scans of documents (2 MB) are larger.</summary>
    public const int MaxLogoBytes = 1024 * 1024;

    private CompanyProfile(Guid id, string name, CompanyDetails details, Guid? cityId)
        : base(id) => (Name, Details, CityId) = (name, details, cityId);

    public string Name { get; private set; }

    /// <summary>The city of the legal address, from the Basic Information list of cities. Null only for a profile entered before
    /// the two pages were joined (name and logo only); the next correction asks for it.</summary>
    public Guid? CityId { get; private set; }

    public CompanyDetails Details { get; private set; }

    /// <summary>Media type of the stored logo (image/png or image/jpeg); null: no logo yet.</summary>
    public string? LogoType { get; private set; }

    public bool HasLogo => LogoType is not null;

    public static CompanyProfile Register(Guid id, string? name, CompanyCity? city, CompanyDetails? details = null)
    {
        var validName = ValidName(name);
        var c = new CompanyProfile(id, validName, ValidDetails(details), ValidCity(city));
        c.Raise(CompanyProfileChanged.From(c));
        return c;
    }

    /// <summary>Corrects the name, the city and the official details together, as the form shows them.</summary>
    public void Edit(string? name, CompanyCity? city, CompanyDetails? details)
    {
        var validName = ValidName(name);
        var validCity = ValidCity(city);
        var validDetails = ValidDetails(details);
        if (validName != Name || validDetails != Details || validCity != CityId)
        {
            (Name, Details, CityId) = (validName, validDetails, validCity);
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

    public static CompanyProfile Restore(Guid id, string name, string? logoType, long version, CompanyDetails? details = null, Guid? cityId = null)
    {
        var c = new CompanyProfile(id, name, details ?? CompanyDetails.None, cityId) { LogoType = logoType };
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

    private static Guid ValidCity(CompanyCity? city)
    {
        Require(city is not null, "Company.CityRequired", "Choose the city of the company's legal address.");
        Require(city!.IsActive, "Company.CityInactive", "The city is switched off.");
        return city.Id;
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
        var address = Text(d.Address);
        Require(address is null || address.Length <= MaxAddressLength, "Company.AddressTooLong", $"An address has at most {MaxAddressLength} characters.");
        var legalAddress = Text(d.LegalAddress);
        Require(legalAddress is null || legalAddress.Length <= MaxAddressLength, "Company.AddressTooLong", $"An address has at most {MaxAddressLength} characters.");
        var postal = Digits(d.PostalCode);
        Require(postal is null || (postal.Length == 10 && postal.All(char.IsAsciiDigit)), "Company.PostalCodeInvalid", "A postal code has 10 digits.");
        var phone = Text(d.Phone) is { } p ? PersianText.Key(p).Replace("(", string.Empty, StringComparison.Ordinal).Replace(")", string.Empty, StringComparison.Ordinal) : null;
        Require(phone is null || PhonePattern().IsMatch(phone), "Company.PhoneInvalid", "A phone number has 6 to 20 digits.");
        var website = Text(d.Website)?.Replace(" ", string.Empty, StringComparison.Ordinal);
        Require(website is null || (website.Length <= MaxWebsiteLength && WebsitePattern().IsMatch(website)), "Company.WebsiteInvalid", "The website address is not valid.");
        var email = Text(d.Email)?.Replace(" ", string.Empty, StringComparison.Ordinal);
        Require(email is null || (email.Length <= MaxEmailLength && EmailPattern().IsMatch(email)), "Company.EmailInvalid", "The e-mail address is not valid.");
        return new CompanyDetails(nationalId, economic, registration, address, postal, phone, website, email, legalAddress);
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

public sealed record CompanyProfileChanged(Guid CompanyId, string Name, bool HasLogo, CompanyDetails Details, Guid? CityId) : DomainEvent
{
    public static CompanyProfileChanged From(CompanyProfile c) => new(c.Id, c.Name, c.HasLogo, c.Details, c.CityId);
}
