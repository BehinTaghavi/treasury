using System.Text.RegularExpressions;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Reference.Domain.Catalogue;

/// <summary>
/// The simple lists of Basic Information (اطلاعات پایه), new-version manual items 1–4 and the bank-name list:
/// currencies, countries, provinces, cities and bank names. Old tables: Bank (Access), BasicBanks, City, ExchangeTypes (MVC).
/// </summary>
public enum CatalogueKind
{
    Currency,
    Country,
    Province,
    City,
    Bank,
}

public static class CatalogueKindRules
{
    /// <summary>Manual: "استان تابعی است از کشور", "شهر تابعی است از استان و کشور".</summary>
    public static CatalogueKind? ParentKind(this CatalogueKind kind) => kind switch
    {
        CatalogueKind.Province => CatalogueKind.Country,
        CatalogueKind.City => CatalogueKind.Province,
        _ => null,
    };
}

/// <summary>
/// One entry of a Basic Information list. Entries are never deleted (old data and other services refer to them);
/// an entry that is no longer used is deactivated and disappears from the lists offered on forms.
/// Name uniqueness among siblings is checked by the use case and by a database index on <see cref="NameKey"/>.
/// </summary>
public sealed partial class CatalogueItem : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    /// <summary>The national currency (same code as Money.Rial in cashflow-svc). It can be neither renamed in code nor switched off.</summary>
    public const string RialCode = "IRR";

    private CatalogueItem(Guid id, CatalogueKind kind, Guid? parentId, string name, string? code)
        : base(id)
    {
        Kind = kind;
        ParentId = parentId;
        Name = name;
        Code = code;
        IsActive = true;
    }

    public CatalogueKind Kind { get; }

    /// <summary>Country of a province, province of a city; empty for the other lists.</summary>
    public Guid? ParentId { get; }

    public string Name { get; private set; }

    /// <summary>Comparison key of <see cref="Name"/> (<see cref="PersianText.Key"/>).</summary>
    public string NameKey => PersianText.Key(Name);

    /// <summary>ISO 4217 code of a currency (IRR, USD, EUR…); empty for the other lists. Never changes once created:
    /// instruments and payments store it.</summary>
    public string? Code { get; }

    public bool IsActive { get; private set; }

    /// <param name="parent">The country of a new province or the province of a new city, as loaded by the use case.</param>
    public static CatalogueItem Add(Guid id, CatalogueKind kind, string name, CatalogueItem? parent = null, string? code = null)
    {
        var expectedParent = kind.ParentKind();
        if (expectedParent is { } parentKind)
        {
            Require(parent is not null, "Catalogue.ParentRequired", $"A {kind} needs its {parentKind}.");
            Require(parent!.Kind == parentKind, "Catalogue.WrongParent", $"The parent of a {kind} must be a {parentKind}, not a {parent.Kind}.");
            Require(parent.IsActive, "Catalogue.ParentInactive", "The parent entry is inactive.");
        }
        else
        {
            Require(parent is null, "Catalogue.WrongParent", $"A {kind} has no parent.");
        }

        string? currencyCode = null;
        if (kind == CatalogueKind.Currency)
        {
            currencyCode = (code ?? string.Empty).Trim().ToUpperInvariant();
            Require(CurrencyCodePattern().IsMatch(currencyCode), "Catalogue.CurrencyCodeInvalid", "A currency code is three Latin letters (ISO 4217), e.g. USD.");
        }
        else
        {
            Require(string.IsNullOrWhiteSpace(code), "Catalogue.CodeNotAllowed", $"A {kind} has no code.");
        }

        var item = new CatalogueItem(id, kind, parent?.Id, ValidName(name), currencyCode);
        item.Raise(new CatalogueItemChanged(id, kind, item.ParentId, item.Name, currencyCode, true));
        return item;
    }

    public void Rename(string name)
    {
        var newName = ValidName(name);
        if (newName == Name)
        {
            return;
        }

        Name = newName;
        Raise(new CatalogueItemChanged(Id, Kind, ParentId, Name, Code, IsActive));
    }

    public void Activate() => SetActive(true);

    /// <summary>Takes the entry off the lists offered on forms; what already refers to it keeps working.</summary>
    public void Deactivate()
    {
        Require(!(Kind == CatalogueKind.Currency && Code == RialCode), "Catalogue.RialCannotBeDeactivated", "The Rial is the base currency and stays active.");
        SetActive(false);
    }

    /// <summary>Rebuilds an entry from storage. Does not raise events.</summary>
    public static CatalogueItem Restore(Guid id, CatalogueKind kind, Guid? parentId, string name, string? code, bool isActive, long version)
    {
        var item = new CatalogueItem(id, kind, parentId, name, code) { IsActive = isActive };
        item.SetLoadedVersion(version);
        return item;
    }

    private void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
        Raise(new CatalogueItemChanged(Id, Kind, ParentId, Name, Code, IsActive));
    }

    private static string ValidName(string? name)
    {
        var cleaned = PersianText.Clean(name);
        Require(cleaned.Length > 0, "Catalogue.NameRequired", "A name is required.");
        Require(cleaned.Length <= MaxNameLength, "Catalogue.NameTooLong", $"A name has at most {MaxNameLength} characters.");
        return cleaned;
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCodePattern();
}

/// <summary>An entry was added, renamed, activated or deactivated. Other services keep their local copies up to date from it.</summary>
public sealed record CatalogueItemChanged(Guid ItemId, CatalogueKind Kind, Guid? ParentId, string Name, string? Code, bool IsActive) : DomainEvent;
