using Treasury.BuildingBlocks.Domain;

namespace Treasury.Reference.Domain.Company;

/// <summary>A city of the Basic Information list as the profile needs it.</summary>
public sealed record CompanyCity(Guid Id, string Name, bool IsActive);

/// <summary>
/// The company's own particulars («مشخصات شرکت», Basic Information; one per client database). Owner, 18 Nov 2026: a received cheque is
/// «خارج از مرکز» when the city of its bank differs from the city of the company's address, so the company's city is kept here; the
/// name and address go with it. Same day: besides the address there is the legal address («آدرس قانونی»), and the city (its own field)
/// and the postal code belong to the legal address.
/// </summary>
public sealed class CompanyProfile : AggregateRoot<Guid>
{
    /// <summary>There is one profile; it always has this id.</summary>
    public static readonly Guid TheId = new("c0a11e55-0000-4000-8000-000000000001");

    public const int PostalCodeLength = 10;

    private CompanyProfile(string name, string? address, string? legalAddress, Guid cityId, string? postalCode)
        : base(TheId)
    {
        (Name, Address, LegalAddress, CityId, PostalCode) = (name, address, legalAddress, cityId, postalCode);
    }

    public string Name { get; private set; }

    /// <summary>The address the company works at.</summary>
    public string? Address { get; private set; }

    /// <summary>The legal (registered) address; <see cref="CityId"/> and <see cref="PostalCode"/> are its city and postal code.</summary>
    public string? LegalAddress { get; private set; }

    /// <summary>The city of the legal address, from the Basic Information list of cities.</summary>
    public Guid CityId { get; private set; }

    public string? PostalCode { get; private set; }

    public static CompanyProfile Register(string? name, string? address, string? legalAddress, CompanyCity? city, string? postalCode)
    {
        var (n, a, l, p) = Check(name, address, legalAddress, city, postalCode);
        var profile = new CompanyProfile(n, a, l, city!.Id, p);
        profile.Raise(new CompanyProfileChanged(n, city.Id, city.Name));
        return profile;
    }

    public void Change(string? name, string? address, string? legalAddress, CompanyCity? city, string? postalCode)
    {
        var (n, a, l, p) = Check(name, address, legalAddress, city, postalCode);
        (Name, Address, LegalAddress, CityId, PostalCode) = (n, a, l, city!.Id, p);
        Raise(new CompanyProfileChanged(n, city.Id, city.Name));
    }

    public static CompanyProfile Restore(string name, string? address, string? legalAddress, Guid cityId, string? postalCode, long version)
    {
        var profile = new CompanyProfile(name, address, legalAddress, cityId, postalCode);
        profile.SetLoadedVersion(version);
        return profile;
    }

    private static (string Name, string? Address, string? LegalAddress, string? PostalCode) Check(
        string? name, string? address, string? legalAddress, CompanyCity? city, string? postalCode)
    {
        var n = PersianText.Clean(name);
        Require(n.Length > 0, "Company.NameRequired", "Enter the company's name.");
        Require(city is not null, "Company.CityRequired", "Choose the city of the company's legal address.");
        Require(city!.IsActive, "Company.CityInactive", "The city is switched off.");
        // Persian digits and a dash in the middle are accepted, as for a beneficiary's postal code.
        var p = string.IsNullOrWhiteSpace(postalCode) ? null : PersianText.Key(postalCode).Replace("-", string.Empty, StringComparison.Ordinal);
        Require(p is null || (p.Length == PostalCodeLength && p.All(char.IsAsciiDigit)), "Company.PostalCodeInvalid", "The postal code has 10 digits.");
        var a = PersianText.Clean(address);
        var l = PersianText.Clean(legalAddress);
        return (n, a.Length == 0 ? null : a, l.Length == 0 ? null : l, p);
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

public sealed record CompanyProfileChanged(string Name, Guid CityId, string CityName) : DomainEvent;
