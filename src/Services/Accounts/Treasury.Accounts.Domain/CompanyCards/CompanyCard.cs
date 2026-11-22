using Treasury.Accounts.Domain.BankAccounts;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Domain.CompanyCards;

/// <summary>Everything the user enters on the form «کارت‌های بانک شرکت (بدون حساب بانکی)».</summary>
/// <param name="CardNumber">The 16 digits of the card.</param>
/// <param name="HolderName">The name on the card (عنوان دارندهٔ کارت).</param>
/// <param name="BankId">The bank that issued the card, from the bank-names list.</param>
/// <param name="LedgerAccountCode">The معین a deposit onto the card debits.</param>
/// <param name="DetailCode">Its تفصیل, when the معین takes one.</param>
public sealed record CompanyCardData(string CardNumber, string HolderName, Guid BankId, string LedgerAccountCode, string? DetailCode);

/// <summary>
/// A bank card of the company that has no bank account in the system (owner's report no. 6, item 12: «واریز به حساب کارت بانک خاص شرکت که
/// فاقد حساب بانکی است»; his answer to Q69, 20 Nov 2026: a list of its own in Basic Information with the card number, the name on the
/// card, its معین and تفصیل). Money a payer pays onto it is recorded on «واریز به کارت بانک» and debits its معین and تفصیل.
/// A card that belongs to one of the company's bank accounts is entered on that bank account instead (report no. 4, items 2 and 4).
/// Like cash desks, a card is switched off, not deleted.
/// </summary>
public sealed class CompanyCard : AggregateRoot<Guid>
{
    public const int MaxHolderLength = 100;

    private CompanyCard(Guid id, CompanyCardData data)
        : base(id)
    {
        Data = data;
        IsActive = true;
    }

    public CompanyCardData Data { get; private set; }

    public bool IsActive { get; private set; }

    public static CompanyCard Register(Guid id, CompanyCardData data, LedgerFacts ledger)
    {
        var card = new CompanyCard(id, Validate(data, ledger, null));
        card.Raise(CompanyCardChanged.From(card));
        return card;
    }

    public void Change(CompanyCardData data, LedgerFacts ledger)
    {
        var clean = Validate(data, ledger, Data);
        if (clean == Data)
        {
            return;
        }

        Data = clean;
        Raise(CompanyCardChanged.From(this));
    }

    public void Activate() => SetActive(true);

    /// <summary>The card leaves the list of «واریز به کارت بانک»; receipts already recorded on it keep it.</summary>
    public void Deactivate() => SetActive(false);

    public static CompanyCard Restore(Guid id, CompanyCardData data, bool isActive, long version)
    {
        var card = new CompanyCard(id, data) { IsActive = isActive };
        card.SetLoadedVersion(version);
        return card;
    }

    private void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
        Raise(CompanyCardChanged.From(this));
    }

    /// <summary>The account and تفصیل the card already had may stay even if switched off since; new ones must be usable.</summary>
    private static CompanyCardData Validate(CompanyCardData data, LedgerFacts ledger, CompanyCardData? before)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ledger);

        // The same two refusals as a bank account's card (owner's report no. 6, item 17): a wrong length, or a wrong check digit.
        var number = new string(PersianText.Key(data.CardNumber).Where(char.IsAsciiDigit).ToArray());
        Require(number.Length == 16, "CompanyCard.CardNumberInvalid", "A bank card number has 16 digits.");
        Require(BankAccount.IsCardNumber(number), "CompanyCard.CardCheckDigit", "The check digit of the bank card number does not fit: a digit is wrong.");

        var holder = PersianText.Clean(data.HolderName);
        Require(holder.Length > 0, "CompanyCard.HolderRequired", "Enter the name on the bank card.");
        Require(holder.Length <= MaxHolderLength, "CompanyCard.HolderTooLong", $"The name on the card has at most {MaxHolderLength} characters.");
        Require(data.BankId != Guid.Empty, "CompanyCard.BankRequired", "Choose the bank that issued the card.");

        var accountCode = Clean(data.LedgerAccountCode) ?? throw new DomainException("CompanyCard.LedgerAccountRequired", "The ledger account (معین) of the card is required.");
        var account = ledger.Account(accountCode);
        Require(account is not null, "CompanyCard.LedgerAccountNotFound", $"Account {accountCode} is not in the chart of accounts.");
        Require(account!.IsPostable, "CompanyCard.LedgerAccountNotPostable", $"Account {accountCode} has a lower level; choose the lower-level account.");
        Require(account.IsActive || accountCode == before?.LedgerAccountCode, "CompanyCard.LedgerAccountInactive", $"Account {accountCode} is inactive.");

        // Owner, 10 Nov 2026 (as for bank accounts): an account lists the تفصیل types it allows; then a تفصیل of one of them is required.
        var detail = Clean(data.DetailCode);
        if (detail is null)
        {
            Require(account.Detail1Types.Count == 0, "CompanyCard.DetailRequired", $"Account {accountCode} needs a تفصیل.");
        }
        else
        {
            var fact = ledger.Detail(detail);
            Require(fact is not null, "CompanyCard.DetailNotFound", $"تفصیل {detail} is not in the list.");
            Require(fact!.IsActive || detail == before?.DetailCode, "CompanyCard.DetailInactive", $"تفصیل {detail} is inactive.");
            Require(account.Detail1Types.Count == 0 || (fact.Type is not null && account.Detail1Types.Contains(fact.Type)), "CompanyCard.DetailTypeNotAllowed",
                $"A تفصیل of type '{fact.Type}' cannot be used with account {accountCode}.");
        }

        return new CompanyCardData(number, holder, data.BankId, accountCode, detail);
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

public sealed record CompanyCardChanged(Guid CompanyCardId, string CardNumber, string HolderName, Guid BankId, string LedgerAccountCode, string? DetailCode, bool IsActive) : DomainEvent
{
    public static CompanyCardChanged From(CompanyCard c) =>
        new(c.Id, c.Data.CardNumber, c.Data.HolderName, c.Data.BankId, c.Data.LedgerAccountCode, c.Data.DetailCode, c.IsActive);
}
