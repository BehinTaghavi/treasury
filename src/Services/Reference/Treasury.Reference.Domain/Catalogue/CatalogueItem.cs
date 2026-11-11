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
/// One entry of a Basic Information list. An entry that other data refers to is never deleted, only deactivated
/// (it disappears from the lists offered on forms). An entry nothing refers to yet may be deleted, e.g. one typed by mistake
/// (owner, 11 Nov 2026: "بشرط آنکه رکورد مربوطه گردش نداشته باشد").
/// Name uniqueness among siblings is checked by the use case and by a database index on <see cref="NameKey"/>.
/// </summary>
public sealed partial class CatalogueItem : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    /// <summary>The national currency (same code as Money.Rial in cashflow-svc). It can be neither renamed in code nor switched off.</summary>
    public const string RialCode = "IRR";

    /// <summary>The word left out when bank names are compared: "بانک آینده" and "آینده" are the same bank (owner, 11 Nov 2026).</summary>
    public const string BankWord = "بانک";

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

    /// <summary>Country of a province, province of a city; empty for the other lists. Can be corrected (<see cref="Edit"/>).</summary>
    public Guid? ParentId { get; private set; }

    public string Name { get; private set; }

    /// <summary>Comparison key of <see cref="Name"/> (<see cref="PersianText.Key"/>).</summary>
    public string NameKey => PersianText.Key(Name);

    /// <summary>
    /// Key used to find a duplicate: <see cref="NameKey"/>, and for bank names without the word "بانک", so that "آینده"
    /// is refused when "بانک آینده" exists (owner's report of 11 Nov 2026, item 8).
    /// </summary>
    public string MatchKey => Kind == CatalogueKind.Bank && NameKey.Replace(BankWord, string.Empty, StringComparison.Ordinal) is { Length: > 0 } k ? k : NameKey;

    /// <summary>ISO 4217 code of a currency (IRR, USD, EUR…); empty for the other lists. Instruments, payments and bank accounts
    /// store it, so it can be corrected only while nothing uses the currency.</summary>
    public string? Code { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Another service saved data that refers to this entry (a bank account names this bank, city or currency). Never goes back:
    /// an entry once used cannot be deleted and its currency code cannot change.
    /// </summary>
    public bool IsInUse { get; private set; }

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
            currencyCode = ValidCurrencyCode(code);
        }
        else
        {
            Require(string.IsNullOrWhiteSpace(code), "Catalogue.CodeNotAllowed", $"A {kind} has no code.");
        }

        var item = new CatalogueItem(id, kind, parent?.Id, ValidName(name), currencyCode);
        item.Raise(new CatalogueItemChanged(id, kind, item.ParentId, item.Name, currencyCode, true));
        return item;
    }

    public void Rename(string name) => Edit(name);

    /// <summary>
    /// Corrects an entry: its name, the country of a province or the province of a city, the code of a currency.
    /// Owner, 11 Nov 2026 (items 2 and 6): a city renamed from اصفهان to تهران must be able to move to the province تهران,
    /// and a currency entered as QAR must be able to become KWD.
    /// </summary>
    /// <param name="parent">The new parent as loaded by the use case; null keeps the current one.</param>
    /// <param name="code">The new currency code; null keeps the current one.</param>
    public void Edit(string name, CatalogueItem? parent = null, string? code = null)
    {
        var newName = ValidName(name);
        var newParentId = ParentId;
        if (parent is not null && parent.Id != ParentId)
        {
            var parentKind = Kind.ParentKind();
            Require(parentKind is not null, "Catalogue.WrongParent", $"A {Kind} has no parent.");
            Require(parent.Kind == parentKind, "Catalogue.WrongParent", $"The parent of a {Kind} must be a {parentKind}, not a {parent.Kind}.");
            Require(parent.IsActive, "Catalogue.ParentInactive", "The parent entry is inactive.");
            newParentId = parent.Id;
        }

        var newCode = Code;
        if (code is not null)
        {
            Require(Kind == CatalogueKind.Currency, "Catalogue.CodeNotAllowed", $"A {Kind} has no code.");
            newCode = ValidCurrencyCode(code);
            if (newCode != Code)
            {
                Require(Code != RialCode, "Catalogue.RialCodeFixed", "The Rial keeps the code IRR.");
                Require(!IsInUse, "Catalogue.CodeInUse", "The currency is already used; its code can no longer change.");
            }
        }

        if (newName == Name && newParentId == ParentId && newCode == Code)
        {
            return;
        }

        Name = newName;
        ParentId = newParentId;
        Code = newCode;
        Raise(new CatalogueItemChanged(Id, Kind, ParentId, Name, Code, IsActive));
    }

    /// <summary>Records that other data now refers to this entry. Returns false when it was already recorded.</summary>
    public bool MarkInUse()
    {
        if (IsInUse)
        {
            return false;
        }

        IsInUse = true;
        return true;
    }

    /// <summary>
    /// Deletes an entry typed by mistake. Refused when anything refers to it: another service's data, or provinces / cities
    /// under it (<paramref name="hasChildren"/>, checked by the use case). The Rial always stays.
    /// </summary>
    public void Delete(bool hasChildren)
    {
        Require(!(Kind == CatalogueKind.Currency && Code == RialCode), "Catalogue.RialCannotBeDeleted", "The Rial is the base currency and stays.");
        Require(!IsInUse, "Catalogue.InUse", "This entry is already used and cannot be deleted; switch it off instead.");
        Require(!hasChildren, "Catalogue.HasChildren", "Entries under this one must be deleted first.");
        Raise(new CatalogueItemDeleted(Id, Kind));
    }

    public void Activate() => SetActive(true);

    /// <summary>Takes the entry off the lists offered on forms; what already refers to it keeps working.</summary>
    public void Deactivate()
    {
        Require(!(Kind == CatalogueKind.Currency && Code == RialCode), "Catalogue.RialCannotBeDeactivated", "The Rial is the base currency and stays active.");
        SetActive(false);
    }

    /// <summary>Rebuilds an entry from storage. Does not raise events.</summary>
    public static CatalogueItem Restore(Guid id, CatalogueKind kind, Guid? parentId, string name, string? code, bool isActive, long version, bool isInUse = false)
    {
        var item = new CatalogueItem(id, kind, parentId, name, code) { IsActive = isActive, IsInUse = isInUse };
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

    private static string ValidCurrencyCode(string? code)
    {
        var currencyCode = (code ?? string.Empty).Trim().ToUpperInvariant();
        Require(CurrencyCodePattern().IsMatch(currencyCode), "Catalogue.CurrencyCodeInvalid", "A currency code is three Latin letters (ISO 4217), e.g. USD.");
        return currencyCode;
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

/// <summary>An entry typed by mistake was deleted before anything referred to it.</summary>
public sealed record CatalogueItemDeleted(Guid ItemId, CatalogueKind Kind) : DomainEvent;
