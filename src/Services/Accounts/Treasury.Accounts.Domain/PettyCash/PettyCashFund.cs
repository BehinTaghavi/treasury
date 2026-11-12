using Treasury.Accounts.Domain.BankAccounts;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Domain.PettyCash;

/// <summary>How a petty cash is topped up after its expenses are settled (manual: "نحوه ترمیم").</summary>
public enum PettyCashRecovery
{
    Cash,
    Cheque,
}

/// <summary>What reference-svc says about a purpose group named on the form; null in place of a fact: no such group.</summary>
public sealed record PurposeGroupFact(Guid Id, bool IsPayment, bool IsActive);

/// <summary>Everything the user enters on the petty cash form. Amounts are in Rial.</summary>
/// <param name="AllowedPurposeGroupIds">Empty: every payment purpose group (manual: "گزینه همه گروه بابت ها").</param>
public sealed record PettyCashData(
    string HolderDetailCode, string LedgerAccountCode, decimal Amount, decimal SettleLimit, decimal PayCeiling,
    IReadOnlyList<Guid> AllowedPurposeGroupIds, PettyCashRecovery Recovery)
{
    public bool Equals(PettyCashData? other) =>
        other is not null && (HolderDetailCode, LedgerAccountCode, Amount, SettleLimit, PayCeiling, Recovery) ==
        (other.HolderDetailCode, other.LedgerAccountCode, other.Amount, other.SettleLimit, other.PayCeiling, other.Recovery)
        && AllowedPurposeGroupIds.SequenceEqual(other.AllowedPurposeGroupIds);

    public override int GetHashCode() => HashCode.Combine(HolderDetailCode, LedgerAccountCode, Amount, SettleLimit, PayCeiling, Recovery);
}

/// <summary>
/// A petty cash (تنخواه گردان) and its holder, new-version manual item 11; MVC PettyCash, PettyCash_BehalfGroups.
/// The holder is a تفصیل of the accounting system (name from it); the fixed amount, the settlement limit (حد واریز: when the
/// expenses reach it, they are settled and the petty cash topped up) and the payment ceiling (سقف پرداخت: largest single payment)
/// are at most the amount; the allowed purpose groups are payment groups (none listed: all); top-up in cash or by cheque.
/// </summary>
public sealed class PettyCashFund : AggregateRoot<Guid>
{
    private PettyCashFund(Guid id, PettyCashData data)
        : base(id)
    {
        Data = data;
        IsActive = true;
    }

    public PettyCashData Data { get; private set; }

    public bool IsActive { get; private set; }

    /// <param name="groups">The purpose groups named on the form as reference-svc described them, by id.</param>
    public static PettyCashFund Register(Guid id, PettyCashData data, LedgerFacts ledger, IReadOnlyDictionary<Guid, PurposeGroupFact> groups)
    {
        var fund = new PettyCashFund(id, Validate(data, ledger, groups, null));
        fund.Raise(PettyCashChanged.From(fund));
        return fund;
    }

    public void Change(PettyCashData data, LedgerFacts ledger, IReadOnlyDictionary<Guid, PurposeGroupFact> groups)
    {
        var clean = Validate(data, ledger, groups, Data);
        if (clean.Equals(Data))
        {
            return;
        }

        Data = clean;
        Raise(PettyCashChanged.From(this));
    }

    public void Activate() => SetActive(true);

    public void Deactivate() => SetActive(false);

    public static PettyCashFund Restore(Guid id, PettyCashData data, bool isActive, long version)
    {
        var fund = new PettyCashFund(id, data) { IsActive = isActive };
        fund.SetLoadedVersion(version);
        return fund;
    }

    private void SetActive(bool active)
    {
        if (IsActive != active)
        {
            IsActive = active;
            Raise(PettyCashChanged.From(this));
        }
    }

    private static PettyCashData Validate(PettyCashData data, LedgerFacts ledger, IReadOnlyDictionary<Guid, PurposeGroupFact> groups, PettyCashData? before)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(groups);
        var accountCode = Clean(data.LedgerAccountCode) ?? throw new DomainException("PettyCash.LedgerAccountRequired", "The ledger account of the petty cash is required.");
        var account = ledger.Account(accountCode);
        Require(account is not null, "PettyCash.LedgerAccountNotFound", $"Account {accountCode} is not in the chart of accounts.");
        Require(account!.IsPostable, "PettyCash.LedgerAccountNotPostable", $"Account {accountCode} has a lower level; choose the lower-level account.");
        Require(account.IsActive || accountCode == before?.LedgerAccountCode, "PettyCash.LedgerAccountInactive", $"Account {accountCode} is inactive.");

        // Manual: "دارندگان تنخواه یا صندوق بایستی قبلا در سیستم مالی تعریف شده باشند".
        var holder = Clean(data.HolderDetailCode) ?? throw new DomainException("PettyCash.HolderRequired", "The تفصیل of the holder is required.");
        var fact = ledger.Detail(holder);
        Require(fact is not null, "PettyCash.DetailNotFound", $"تفصیل {holder} is not in the list.");
        Require(fact!.IsActive || holder == before?.HolderDetailCode, "PettyCash.DetailInactive", $"تفصیل {holder} is inactive.");
        Require(account.Detail1Types.Count == 0 || (fact.Type is not null && account.Detail1Types.Contains(fact.Type)), "PettyCash.DetailTypeNotAllowed",
            $"A تفصیل of type '{fact.Type}' cannot be used with account {accountCode}.");

        Require(data.Amount > 0 && data.Amount == decimal.Truncate(data.Amount), "PettyCash.AmountInvalid", "The amount is a whole number of Rials above zero.");
        Require(data.SettleLimit > 0 && data.SettleLimit <= data.Amount, "PettyCash.SettleLimitInvalid", "The settlement limit is above zero and at most the amount.");
        Require(data.PayCeiling > 0 && data.PayCeiling <= data.Amount, "PettyCash.PayCeilingInvalid", "The payment ceiling is above zero and at most the amount.");

        var ids = (data.AllowedPurposeGroupIds ?? []).Distinct().ToList();
        var kept = before?.AllowedPurposeGroupIds ?? [];
        foreach (var id in ids)
        {
            var group = groups.GetValueOrDefault(id);
            Require(group is not null, "PettyCash.PurposeGroupNotFound", "A purpose group is not in the list.");
            Require(group!.IsPayment, "PettyCash.PurposeGroupNotPayment", "Only payment purpose groups can be paid from a petty cash.");
            Require(group.IsActive || kept.Contains(id), "PettyCash.PurposeGroupInactive", "A purpose group is switched off.");
        }

        return new PettyCashData(holder, accountCode, data.Amount, data.SettleLimit, data.PayCeiling, ids, data.Recovery);
    }

    private static string? Clean(string? text) => PersianText.Clean(text) is { Length: > 0 } c ? c : null;

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

public sealed record PettyCashChanged(
    Guid PettyCashId, string HolderDetailCode, string LedgerAccountCode, decimal Amount, decimal SettleLimit, decimal PayCeiling,
    IReadOnlyList<Guid> AllowedPurposeGroupIds, PettyCashRecovery Recovery, bool IsActive) : DomainEvent
{
    public static PettyCashChanged From(PettyCashFund f) => new(
        f.Id, f.Data.HolderDetailCode, f.Data.LedgerAccountCode, f.Data.Amount, f.Data.SettleLimit, f.Data.PayCeiling, f.Data.AllowedPurposeGroupIds, f.Data.Recovery, f.IsActive);
}
