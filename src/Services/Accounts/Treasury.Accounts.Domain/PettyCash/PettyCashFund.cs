using Treasury.Accounts.Domain.BankAccounts;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Domain.PettyCash;

/// <summary>How a petty cash is topped up after its expenses are settled (manual: "نحوه ترمیم").</summary>
public enum PettyCashRecovery
{
    Cash,
    Cheque,

    /// <summary>Topped up onto the holder's bank card (owner's report no. 2, item 13: "کارت عابر بانک").</summary>
    BankCard,
}

/// <summary>
/// How the holder keeps the petty cash money (owner's report no. 2, item 12): in a bank account (its name and number), in cash,
/// and/or on a bank card (card holder and card number). One or more may be ticked.
/// </summary>
public sealed record PettyCashKeeping(
    bool Bank, string? BankAccountName, string? BankAccountNumber, bool Cash, bool Card, string? CardHolderName, string? CardNumber);

/// <summary>What reference-svc says about a purpose group named on the form; null in place of a fact: no such group.</summary>
public sealed record PurposeGroupFact(Guid Id, bool IsPayment, bool IsActive);

/// <summary>Everything the user enters on the petty cash form. Amounts are in Rial.</summary>
/// <param name="AllowedPurposeGroupIds">Empty: every payment purpose group (manual: "گزینه همه گروه بابت ها").</param>
/// <param name="CreatedOn">Date the petty cash was set up (owner's report no. 2, item 11); empty only on rows saved before it existed.</param>
/// <param name="Keeping">How the money is kept (item 12); empty only on rows saved before it existed.</param>
public sealed record PettyCashData(
    string HolderDetailCode, string LedgerAccountCode, decimal Amount, decimal SettleLimit, decimal PayCeiling,
    IReadOnlyList<Guid> AllowedPurposeGroupIds, PettyCashRecovery Recovery, FiscalDate? CreatedOn = null, PettyCashKeeping? Keeping = null)
{
    public bool Equals(PettyCashData? other) =>
        other is not null && (HolderDetailCode, LedgerAccountCode, Amount, SettleLimit, PayCeiling, Recovery, CreatedOn, Keeping) ==
        (other.HolderDetailCode, other.LedgerAccountCode, other.Amount, other.SettleLimit, other.PayCeiling, other.Recovery, other.CreatedOn, other.Keeping)
        && AllowedPurposeGroupIds.SequenceEqual(other.AllowedPurposeGroupIds);

    public override int GetHashCode() => HashCode.Combine(HolderDetailCode, LedgerAccountCode, Amount, SettleLimit, PayCeiling, Recovery);
}

/// <summary>
/// A petty cash (تنخواه گردان) and its holder, new-version manual item 11; MVC PettyCash, PettyCash_BehalfGroups.
/// The holder is a تفصیل of the accounting system (name from it); the fixed amount, the settlement limit (حد واریز: when the
/// expenses reach it, they are settled and the petty cash topped up) is below the amount, and the payment ceiling (سقف پرداخت:
/// largest single payment) is at most the settlement limit (owner's report no. 2, item 4); the allowed purpose groups are payment
/// groups (none listed: all); top-up in cash, by cheque or onto a bank card. The creation date and how the money is kept are required.
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
        // Owner's report no. 2, item 3: the holder is a member of staff, a cost centre or an organisational unit.
        Require(fact.MayHold || holder == before?.HolderDetailCode, "PettyCash.HolderNotStaff",
            $"تفصیل {holder} is not of a staff, cost-centre or organisational-unit type.");
        Require(account.Detail1Types.Count == 0 || (fact.Type is not null && account.Detail1Types.Contains(fact.Type)), "PettyCash.DetailTypeNotAllowed",
            $"A تفصیل of type '{fact.Type}' cannot be used with account {accountCode}.");

        Require(data.Amount > 0 && data.Amount == decimal.Truncate(data.Amount), "PettyCash.AmountInvalid", "The amount is a whole number of Rials above zero.");
        // Owner's report no. 2, item 4: payment ceiling <= settlement limit < amount.
        Require(data.SettleLimit > 0 && data.SettleLimit < data.Amount, "PettyCash.SettleLimitInvalid", "The settlement limit is above zero and below the amount.");
        Require(data.PayCeiling > 0 && data.PayCeiling <= data.SettleLimit, "PettyCash.PayCeilingInvalid", "The payment ceiling is above zero and at most the settlement limit.");
        Require(data.CreatedOn is not null, "PettyCash.CreatedOnRequired", "The date the petty cash was set up is required.");
        var keeping = CleanKeeping(data.Keeping);

        var ids = (data.AllowedPurposeGroupIds ?? []).Distinct().ToList();
        var kept = before?.AllowedPurposeGroupIds ?? [];
        foreach (var id in ids)
        {
            var group = groups.GetValueOrDefault(id);
            Require(group is not null, "PettyCash.PurposeGroupNotFound", "A purpose group is not in the list.");
            Require(group!.IsPayment, "PettyCash.PurposeGroupNotPayment", "Only payment purpose groups can be paid from a petty cash.");
            Require(group.IsActive || kept.Contains(id), "PettyCash.PurposeGroupInactive", "A purpose group is switched off.");
        }

        return new PettyCashData(holder, accountCode, data.Amount, data.SettleLimit, data.PayCeiling, ids, data.Recovery, data.CreatedOn, keeping);
    }

    private static PettyCashKeeping CleanKeeping(PettyCashKeeping? k)
    {
        Require(k is not null && (k.Bank || k.Cash || k.Card), "PettyCash.KeepingRequired", "Choose how the holder keeps the money: bank, cash and/or bank card.");
        var bankName = k!.Bank ? Clean(k.BankAccountName) : null;
        var bankNumber = k.Bank ? Clean(k.BankAccountNumber) : null;
        Require(!k.Bank || (bankName is not null && bankNumber is not null), "PettyCash.KeepingBankIncomplete", "Enter the name and number of the bank account.");
        Require(bankNumber is null || (bankNumber.Length <= 30 && PersianText.Key(bankNumber).All(c => char.IsAsciiDigit(c) || c is '-' or '.' or '/')),
            "PettyCash.KeepingBankIncomplete", "The bank account number has digits and separators only.");
        var cardHolder = k.Card ? Clean(k.CardHolderName) : null;
        var card = k.Card ? new string(PersianText.Key(k.CardNumber).Where(c => c != '-').ToArray()) : null;
        Require(!k.Card || cardHolder is not null, "PettyCash.KeepingCardIncomplete", "Enter the name of the card holder.");
        Require(card is null || IsCardNumber(card), "PettyCash.CardNumberInvalid", "A bank card number has 16 digits and a valid check digit.");
        return new PettyCashKeeping(k.Bank, bankName, bankNumber is null ? null : PersianText.Key(bankNumber), k.Cash, k.Card, cardHolder, card);
    }

    /// <summary>Iranian bank cards: 16 digits with the Luhn check digit (the same check the banks' card readers use).</summary>
    public static bool IsCardNumber(string digits)
    {
        if (digits.Length != 16 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 16; i++)
        {
            var d = digits[i] - '0';
            if (i % 2 == 0)
            {
                d *= 2;
                if (d > 9)
                {
                    d -= 9;
                }
            }

            sum += d;
        }

        return sum % 10 == 0;
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
