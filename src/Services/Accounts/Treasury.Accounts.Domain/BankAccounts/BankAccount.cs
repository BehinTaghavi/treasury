using System.Text.RegularExpressions;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Domain.BankAccounts;

/// <summary>
/// What reference-svc says about a ledger account (copy of the accounting system), as seen when the bank account is saved.
/// Null in place of a fact: the code does not exist.
/// </summary>
public sealed record LedgerAccountFact(string Code, bool IsActive, bool IsPostable, IReadOnlySet<string> Detail1Types, IReadOnlySet<string> Detail2Types);

/// <summary>What reference-svc says about a تفصیل account.</summary>
/// <param name="Role">Staff, CostCentre or OrgUnit as reference-svc reads it from the type (owner, 13 Nov 2026); null otherwise.</param>
public sealed record DetailFact(string Code, string? Type, bool IsActive, string? Role = null)
{
    /// <summary>Who may hold a cash desk or a petty cash: staff, cost centres, organisational units (owner's report no. 2, item 3).</summary>
    public bool MayHold => Role is "Staff" or "CostCentre" or "OrgUnit";
}

/// <summary>
/// The accounting side of a bank account (manual section 5): the معین / معین در معین of the account with its تفصیل 1 and 2,
/// and the accounts used for cheques deposited for collection (واگذاری) and for bounced cheques (برگشتی).
/// </summary>
public sealed record BankLedger(string AccountCode, string? Detail1Code, string? Detail2Code, string? CollectionAccountCode, string? BouncedAccountCode);

/// <summary>Everything the user enters on the bank account form (manual section 5 "معرفی حساب‌های بانکی").</summary>
public sealed record BankAccountData(
    string AccountNumber,
    Guid BankId,
    string? BranchName,
    string? BranchCode,
    Guid? CityId,
    FiscalDate? OpeningDate,
    string CurrencyCode,
    BankLedger Ledger,
    int? CollectionPriority,
    bool IsFacility);

/// <summary>
/// One of the company's bank accounts. Sources: new-version manual section 5; Access Form_Bankacc (required number, bank and
/// معین; number not twice; accounts without a lower level only; Rial by default); MVC BanksController (backup accounts).
/// A backup account (حساب پشتیبان) belongs to a main current account of the same bank, defined first.
/// Owner's report of 11 Nov 2026: branch, branch code, city, تفصیل, collection and bounced-cheque accounts are required too
/// (item 12); collection priority is a number 1–5, 1 the highest (item 15); a backup needs its main account (item 10).
/// </summary>
public sealed partial class BankAccount : AggregateRoot<Guid>
{
    public const string Rial = "IRR";

    /// <summary>Collection priority (اولویت واگذاری): 1 is the highest, 5 the lowest; none for an account not used for collection.</summary>
    public const int HighestPriority = 1;

    public const int LowestPriority = 5;

    private BankAccount(Guid id, BankAccountData data, Guid? mainAccountId)
        : base(id)
    {
        Data = data;
        MainAccountId = mainAccountId;
        IsActive = true;
    }

    public BankAccountData Data { get; private set; }

    /// <summary>The main current account when this is a backup account (پشتیبان); otherwise null.</summary>
    public Guid? MainAccountId { get; }

    public bool IsBackup => MainAccountId is not null;

    public bool IsActive { get; private set; }

    /// <summary>A cheque book, cheque type or cheque names this account (recorded by cashflow-svc); then it is never deleted.</summary>
    public bool IsInUse { get; private set; }

    /// <summary>Comparison key of the account number: digits only, Persian/Arabic digits as 0-9 (Form_Bankacc: "شماره حساب تكراري است").</summary>
    public string NumberKey => NumberKeyOf(Data.AccountNumber);

    /// <param name="main">The main account when registering a backup account, as loaded by the use case.</param>
    /// <param name="mainAlreadyHasBackup">Whether another backup account already points to <paramref name="main"/>.</param>
    /// <param name="isBackup">The user ticked "پشتیبان"; then <paramref name="main"/> is required.</param>
    public static BankAccount Register(
        Guid id, BankAccountData data, LedgerFacts ledger, BankAccount? main = null, bool mainAlreadyHasBackup = false, bool isBackup = false)
    {
        // Owner, 11 Nov 2026 (item 10): "ابتدا حسابجاری مربوط به این حساب پشتیبان را تعریف نموده ..." – never saved as an ordinary account.
        Require(!isBackup || main is not null, "BankAccount.MainAccountRequired", "A backup account needs its main current account, defined first.");
        var clean = Validate(data, ledger);
        if (main is not null)
        {
            // MVC BanksController: the main account is of the same bank, is not itself a backup, and has no other backup.
            Require(main.Id != id, "BankAccount.BackupOfItself", "An account cannot be its own backup.");
            Require(main.IsActive, "BankAccount.MainAccountInactive", "The main account is inactive.");
            Require(!main.IsBackup, "BankAccount.MainAccountIsBackup", "A backup account cannot be the main account of another one.");
            Require(main.Data.BankId == clean.BankId, "BankAccount.MainAccountOtherBank", "The main account must be at the same bank.");
            // Owner, 13 Nov 2026: Rial with Rial, a foreign currency with the same foreign currency.
            Require(main.Data.CurrencyCode == clean.CurrencyCode, "BankAccount.MainAccountOtherCurrency", "A backup account has the currency of its main account.");
            Require(!mainAlreadyHasBackup, "BankAccount.MainAccountHasBackup", "This main account already has a backup account.");
        }

        var account = new BankAccount(id, clean, main?.Id);
        account.Raise(BankAccountChanged.From(account));
        return account;
    }

    /// <summary>Saves the edited form. Whether it is a backup account, and of which main account, does not change.</summary>
    public void Change(BankAccountData data, LedgerFacts ledger)
    {
        var clean = Validate(data, ledger);
        Require(!IsBackup || clean.BankId == Data.BankId, "BankAccount.MainAccountOtherBank", "A backup account stays at the bank of its main account.");
        Require(!IsBackup || clean.CurrencyCode == Data.CurrencyCode, "BankAccount.MainAccountOtherCurrency", "A backup account keeps the currency of its main account.");
        if (clean == Data)
        {
            return;
        }

        Data = clean;
        Raise(BankAccountChanged.From(this));
    }

    /// <summary>Manual: "غیر فعال سازی … ممکن است به صورت موقتی انجام شود" – the account leaves the lists of receipts and payments.</summary>
    public void Deactivate() => SetActive(false);

    public void Activate() => SetActive(true);

    /// <summary>Marks the account as used by another service's data; false when it already was.</summary>
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
    /// Deletes an account typed by mistake (owner's report no. 3, item 1: "حذف در شرایطی قابل انجام است که حساب مربوطه گردش نداشته
    /// باشد"): only while no cheque book, cheque type or cheque names it, and while no backup account points to it.
    /// </summary>
    /// <param name="hasTransactions">The account has a transaction such as an opening balance (owner, 14 Nov 2026).</param>
    public void Delete(bool hasBackup, bool hasTransactions = false)
    {
        Require(!IsInUse && !hasTransactions, "BankAccount.InUse", "Cheque books, cheques or transactions name this account; switch it off instead.");
        Require(!hasBackup, "BankAccount.HasBackup", "A backup account belongs to this account; delete the backup first.");
        Raise(new BankAccountDeleted(Id));
    }

    public static BankAccount Restore(Guid id, BankAccountData data, Guid? mainAccountId, bool isActive, long version, bool isInUse = false)
    {
        var account = new BankAccount(id, data, mainAccountId) { IsActive = isActive, IsInUse = isInUse };
        account.SetLoadedVersion(version);
        return account;
    }

    public static string NumberKeyOf(string? number) =>
        new(PersianText.Key(number).Where(char.IsAsciiDigit).ToArray());

    private void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
        Raise(BankAccountChanged.From(this));
    }

    private static BankAccountData Validate(BankAccountData data, LedgerFacts ledger)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ledger);
        var number = PersianText.Clean(data.AccountNumber);
        Require(NumberKeyOf(number).Length > 0, "BankAccount.NumberRequired", "The account number is required."); // "شماره حساب نامشخص است"
        Require(data.BankId != Guid.Empty, "BankAccount.BankRequired", "The bank is required."); // "نام بانك نامشخص است"
        Require(Clean(data.BranchName) is not null, "BankAccount.BranchNameRequired", "The branch name is required.");
        Require(Clean(data.BranchCode) is not null, "BankAccount.BranchCodeRequired", "The branch code is required.");
        Require(data.CityId is { } city && city != Guid.Empty, "BankAccount.CityRequired", "The city is required.");
        Require(data.CollectionPriority is null or (>= HighestPriority and <= LowestPriority), "BankAccount.CollectionPriorityInvalid",
            $"The collection priority is a number from {HighestPriority} (highest) to {LowestPriority}.");
        var currency = (data.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        Require(CurrencyPattern().IsMatch(currency), "BankAccount.CurrencyInvalid", "The currency is a three-letter code; the Rial is IRR.");

        var l = data.Ledger ?? throw new DomainException("BankAccount.LedgerAccountRequired", "The ledger account is required.");
        var accountCode = Clean(l.AccountCode) ?? throw new DomainException("BankAccount.LedgerAccountRequired", "The ledger account (معین) is required."); // "حساب معين نامشخص است"
        var d1 = Clean(l.Detail1Code);
        var d2 = Clean(l.Detail2Code);
        Require(d2 is null || d1 is not null, "BankAccount.Detail2WithoutDetail1", "تفصیل دوم needs تفصیل اول.");

        var account = Postable(ledger.Account(accountCode), accountCode);
        Require(d1 is not null, "BankAccount.DetailRequired", "تفصیل اول (usually the bank account's own تفصیل) is required.");
        CheckDetail(1, account.Detail1Types, d1, d1 is null ? null : ledger.Detail(d1), accountCode);
        CheckDetail(2, account.Detail2Types, d2, d2 is null ? null : ledger.Detail(d2), accountCode);

        // The collection account may be the bank's own account (owner, 11 Nov 2026, item 14); nothing compares them.
        var collection = Clean(l.CollectionAccountCode) ?? throw new DomainException("BankAccount.CollectionAccountRequired", "The collection account (معین واگذاری) is required.");
        var bounced = Clean(l.BouncedAccountCode) ?? throw new DomainException("BankAccount.BouncedAccountRequired", "The bounced-cheque account (معین برگشتی) is required.");
        Postable(ledger.Account(collection), collection);
        Postable(ledger.Account(bounced), bounced);

        return data with
        {
            AccountNumber = number,
            BranchName = Clean(data.BranchName),
            BranchCode = Clean(data.BranchCode),
            CurrencyCode = currency,
            Ledger = new BankLedger(accountCode, d1, d2, collection, bounced),
        };
    }

    private static LedgerAccountFact Postable(LedgerAccountFact? fact, string code)
    {
        Require(fact is not null, "BankAccount.LedgerAccountNotFound", $"Account {code} is not in the chart of accounts.");
        Require(fact!.IsActive, "BankAccount.LedgerAccountInactive", $"Account {code} is inactive.");
        // Form_Bankacc lists only accounts without a lower level (Has_Sub_Subsidery = False).
        Require(fact.IsPostable, "BankAccount.LedgerAccountNotPostable", $"Account {code} has a lower level; choose the lower-level account.");
        return fact;
    }

    /// <summary>
    /// Owner, 10 Nov 2026: an account lists the تفصیل types it allows; another type is refused, and when types are listed the
    /// تفصیل is required. Same rule as LedgerAccount.CheckDetails in reference-svc, applied here to the facts it returned.
    /// </summary>
    private static void CheckDetail(int position, IReadOnlySet<string> allowed, string? code, DetailFact? fact, string accountCode)
    {
        if (code is null)
        {
            Require(allowed.Count == 0, "BankAccount.DetailRequired", $"Account {accountCode} needs a تفصیل {position}."); // "حساب تفصيل نامشخص است"
            return;
        }

        Require(fact is not null, "BankAccount.DetailNotFound", $"تفصیل {code} is not in the list.");
        Require(fact!.IsActive, "BankAccount.DetailInactive", $"تفصیل {code} is inactive.");
        Require(allowed.Count == 0 || (fact.Type is not null && allowed.Contains(fact.Type)), "BankAccount.DetailTypeNotAllowed",
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

/// <summary>The ledger facts the use case looked up for one save (accounts and تفصیل named on the form).</summary>
public sealed class LedgerFacts(IReadOnlyDictionary<string, LedgerAccountFact> accounts, IReadOnlyDictionary<string, DetailFact> details)
{
    public static LedgerFacts None { get; } = new(new Dictionary<string, LedgerAccountFact>(), new Dictionary<string, DetailFact>());

    public LedgerAccountFact? Account(string code) => accounts.GetValueOrDefault(code);

    public DetailFact? Detail(string code) => details.GetValueOrDefault(code);
}

/// <summary>A bank account was registered, edited, switched off or on. Cashflow and posting keep their copies from it.</summary>
public sealed record BankAccountChanged(
    Guid BankAccountId, string AccountNumber, Guid BankId, string CurrencyCode, Guid? MainAccountId, string LedgerAccountCode,
    string? Detail1Code, string? Detail2Code, string? CollectionAccountCode, string? BouncedAccountCode, int? CollectionPriority, bool IsActive) : DomainEvent
{
    public static BankAccountChanged From(BankAccount a) => new(
        a.Id, a.Data.AccountNumber, a.Data.BankId, a.Data.CurrencyCode, a.MainAccountId, a.Data.Ledger.AccountCode,
        a.Data.Ledger.Detail1Code, a.Data.Ledger.Detail2Code, a.Data.Ledger.CollectionAccountCode, a.Data.Ledger.BouncedAccountCode,
        a.Data.CollectionPriority, a.IsActive);
}

public sealed record BankAccountDeleted(Guid BankAccountId) : DomainEvent;
