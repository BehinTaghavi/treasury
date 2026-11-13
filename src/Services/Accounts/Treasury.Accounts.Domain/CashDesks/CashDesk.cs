using System.Text.RegularExpressions;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Domain.CashDesks;

/// <summary>Everything the user enters on the cash desk form.</summary>
/// <param name="HolderDetailCode">تفصیل of the person who holds the cash desk; their name comes from it.</param>
/// <param name="CreatedOn">Date the cash desk was set up (owner's report no. 2, item 11); empty only on rows saved before it existed.</param>
public sealed record CashDeskData(string Name, string LedgerAccountCode, string HolderDetailCode, string? Detail2Code, string CurrencyCode, FiscalDate? CreatedOn = null);

/// <summary>
/// A cash desk (صندوق) of the company, new-version manual item 10 "تعریف مشخصات دارندگان صندوق": the holder's تفصیل (the
/// holder must already exist in the accounting system; the name comes from the تفصیل) and the معین of the cash desk.
/// MVC CashDesk: description, Account_No, Unique_Account_No (/_two), ExchangeTypeID, IsActive. Users (cashiers) are linked once
/// login exists. Like bank accounts, a cash desk is switched off, not deleted.
/// </summary>
public sealed partial class CashDesk : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    private CashDesk(Guid id, CashDeskData data)
        : base(id)
    {
        Data = data;
        IsActive = true;
    }

    public CashDeskData Data { get; private set; }

    public string NameKey => PersianText.Key(Data.Name);

    public bool IsActive { get; private set; }

    public static CashDesk Register(Guid id, CashDeskData data, LedgerFacts ledger)
    {
        var desk = new CashDesk(id, Validate(data, ledger, null));
        desk.Raise(CashDeskChanged.From(desk));
        return desk;
    }

    public void Change(CashDeskData data, LedgerFacts ledger)
    {
        var clean = Validate(data, ledger, Data);
        if (clean == Data)
        {
            return;
        }

        Data = clean;
        Raise(CashDeskChanged.From(this));
    }

    public void Activate() => SetActive(true);

    /// <summary>The cash desk leaves the lists of receipts and payments; what already uses it keeps working.</summary>
    public void Deactivate() => SetActive(false);

    public static CashDesk Restore(Guid id, CashDeskData data, bool isActive, long version)
    {
        var desk = new CashDesk(id, data) { IsActive = isActive };
        desk.SetLoadedVersion(version);
        return desk;
    }

    private void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
        Raise(CashDeskChanged.From(this));
    }

    /// <summary>Accounts and تفصیل the cash desk already had may stay even if switched off since; new ones must be usable.</summary>
    private static CashDeskData Validate(CashDeskData data, LedgerFacts ledger, CashDeskData? before)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ledger);
        var name = PersianText.Clean(data.Name);
        Require(name.Length > 0, "CashDesk.NameRequired", "The name of the cash desk is required.");
        Require(name.Length <= MaxNameLength, "CashDesk.NameTooLong", $"A name has at most {MaxNameLength} characters.");
        var currency = (data.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        Require(CurrencyPattern().IsMatch(currency), "CashDesk.CurrencyInvalid", "The currency is a three-letter code; the Rial is IRR.");
        Require(data.CreatedOn is not null, "CashDesk.CreatedOnRequired", "The date the cash desk was set up is required.");

        var accountCode = Clean(data.LedgerAccountCode) ?? throw new DomainException("CashDesk.LedgerAccountRequired", "The ledger account (معین) of the cash desk is required.");
        var account = ledger.Account(accountCode);
        Require(account is not null, "CashDesk.LedgerAccountNotFound", $"Account {accountCode} is not in the chart of accounts.");
        Require(account!.IsPostable, "CashDesk.LedgerAccountNotPostable", $"Account {accountCode} has a lower level; choose the lower-level account.");
        Require(account.IsActive || accountCode == before?.LedgerAccountCode, "CashDesk.LedgerAccountInactive", $"Account {accountCode} is inactive.");

        // Manual: "دارندگان صندوق بایستی قبلا در سیستم مالی تعریف شده باشند".
        var holder = Clean(data.HolderDetailCode) ?? throw new DomainException("CashDesk.HolderRequired", "The تفصیل of the holder is required.");
        var d2 = Clean(data.Detail2Code);
        CheckDetail(account.Detail1Types, holder, ledger.Detail(holder), before?.HolderDetailCode, accountCode);
        if (d2 is not null)
        {
            CheckDetail(account.Detail2Types, d2, ledger.Detail(d2), before?.Detail2Code, accountCode);
        }

        return new CashDeskData(name, accountCode, holder, d2, currency, data.CreatedOn);
    }

    /// <summary>Same rule as for bank accounts (owner, 10 Nov 2026): a تفصیل of a type the account does not allow is refused.</summary>
    private static void CheckDetail(IReadOnlySet<string> allowed, string code, DetailFact? fact, string? kept, string accountCode)
    {
        Require(fact is not null, "CashDesk.DetailNotFound", $"تفصیل {code} is not in the list.");
        Require(fact!.IsActive || code == kept, "CashDesk.DetailInactive", $"تفصیل {code} is inactive.");
        Require(allowed.Count == 0 || (fact.Type is not null && allowed.Contains(fact.Type)), "CashDesk.DetailTypeNotAllowed",
            $"A تفصیل of type '{fact.Type}' cannot be used with account {accountCode}.");
    }

    private static string? Clean(string? text) => PersianText.Clean(text) is { Length: > 0 } c ? c : null;

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyPattern();
}

public sealed record CashDeskChanged(Guid CashDeskId, string Name, string LedgerAccountCode, string HolderDetailCode, string? Detail2Code, string CurrencyCode, bool IsActive) : DomainEvent
{
    public static CashDeskChanged From(CashDesk d) =>
        new(d.Id, d.Data.Name, d.Data.LedgerAccountCode, d.Data.HolderDetailCode, d.Data.Detail2Code, d.Data.CurrencyCode, d.IsActive);
}
