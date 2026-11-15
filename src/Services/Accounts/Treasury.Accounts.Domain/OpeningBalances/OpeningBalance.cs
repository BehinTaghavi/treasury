using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Domain.OpeningBalances;

/// <summary>Which kind of account an opening balance belongs to (owner, 14 Nov 2026: "all" – bank accounts, cash desks, petty cash).</summary>
public enum BalanceAccountKind
{
    BankAccount,
    CashDesk,
    PettyCash,
}

/// <summary>What the account says about itself when a balance is entered: its currency and whether it may go below zero.</summary>
/// <param name="CurrencyCode">IRR for Rial accounts; a foreign currency needs the Rial equivalent too.</param>
/// <param name="MayBeNegative">A bank account may be overdrawn; cash in a cash desk or petty cash cannot be below zero.</param>
public sealed record BalanceAccountFact(BalanceAccountKind Kind, Guid Id, string CurrencyCode, bool MayBeNegative);

/// <summary>
/// The balance of a bank account, cash desk or petty cash at the start of a fiscal year (موجودی اول دوره), Access Form_Accamounts:
/// year, account, amount (stored there as a transaction "موجودي اول دوره"); new-version manual: foreign-currency accounts also
/// carry the Rial equivalent ("مبلغ هم ارز ریالی موجودی اول دوره"). Owner, 14 Nov 2026: the first time the system is used the
/// balance is entered on the start date, which may be in the middle of the year; in later years it is the balance on the first day
/// of the fiscal year, calculated from the previous year, and may be corrected by entering it. Treasury sends no voucher for it.
/// The fiscal year is the Jalali year (open question Q5, assumed yes).
/// </summary>
public sealed class OpeningBalance : AggregateRoot<Guid>
{
    /// <summary>Foreign-currency amounts have at most two decimals; Rial amounts are whole numbers.</summary>
    public const int ForeignDecimals = 2;

    private OpeningBalance(Guid id, BalanceAccountKind kind, Guid accountId, int fiscalYear)
        : base(id)
    {
        AccountKind = kind;
        AccountId = accountId;
        FiscalYear = fiscalYear;
    }

    public BalanceAccountKind AccountKind { get; }

    public Guid AccountId { get; }

    /// <summary>Jalali year, e.g. 1405.</summary>
    public int FiscalYear { get; }

    /// <summary>The date the balance holds on: the start date in the first year, otherwise 1 Farvardin.</summary>
    public FiscalDate Date { get; private set; }

    /// <summary>In the account's currency (Rial for Rial accounts).</summary>
    public decimal Amount { get; private set; }

    /// <summary>Rial equivalent of a foreign-currency balance; null for Rial accounts.</summary>
    public decimal? RialEquivalent { get; private set; }

    public static OpeningBalance Enter(Guid id, BalanceAccountFact account, int fiscalYear, FiscalDate date, decimal amount, decimal? rialEquivalent)
    {
        ArgumentNullException.ThrowIfNull(account);
        var b = new OpeningBalance(id, account.Kind, account.Id, ValidYear(fiscalYear));
        b.Apply(account, date, amount, rialEquivalent);
        return b;
    }

    /// <summary>Corrects the balance (owner, 14 Nov 2026: corrections are allowed).</summary>
    public void Correct(BalanceAccountFact account, FiscalDate date, decimal amount, decimal? rialEquivalent)
    {
        ArgumentNullException.ThrowIfNull(account);
        Require(account.Kind == AccountKind && account.Id == AccountId, "OpeningBalance.OtherAccount", "The balance belongs to another account.");
        if ((date, amount, rialEquivalent) == (Date, Amount, RialEquivalent))
        {
            return;
        }

        Apply(account, date, amount, rialEquivalent);
    }

    /// <summary>A balance typed by mistake can be removed; the year then takes the balance carried from the year before.</summary>
    public void Remove() => Raise(new OpeningBalanceRemoved(Id, AccountKind, AccountId, FiscalYear));

    public static OpeningBalance Restore(
        Guid id, BalanceAccountKind kind, Guid accountId, int fiscalYear, FiscalDate date, decimal amount, decimal? rialEquivalent, long version)
    {
        var b = new OpeningBalance(id, kind, accountId, fiscalYear) { Date = date, Amount = amount, RialEquivalent = rialEquivalent };
        b.SetLoadedVersion(version);
        return b;
    }

    private void Apply(BalanceAccountFact account, FiscalDate date, decimal amount, decimal? rialEquivalent)
    {
        Require(date.JalaliYear == FiscalYear, "OpeningBalance.DateOutsideYear", $"The date must be in fiscal year {FiscalYear}.");
        var rial = string.Equals(account.CurrencyCode, "IRR", StringComparison.Ordinal);
        Require(account.MayBeNegative || amount >= 0, "OpeningBalance.Negative", "Cash in a cash desk or petty cash cannot be below zero.");
        Require(decimal.Round(amount, rial ? 0 : ForeignDecimals) == amount, "OpeningBalance.AmountInvalid",
            rial ? "A Rial amount is a whole number." : "A foreign-currency amount has at most two decimals.");
        if (rial)
        {
            Require(rialEquivalent is null, "OpeningBalance.RialEquivalentNotNeeded", "A Rial account has no Rial equivalent.");
        }
        else
        {
            Require(rialEquivalent is not null, "OpeningBalance.RialEquivalentRequired", "Enter the Rial equivalent of the foreign-currency balance.");
            var r = rialEquivalent!.Value;
            Require(r == decimal.Truncate(r) && Math.Sign(r) == Math.Sign(amount), "OpeningBalance.RialEquivalentInvalid",
                "The Rial equivalent is a whole number of Rials with the sign of the amount.");
        }

        (Date, Amount, RialEquivalent) = (date, amount, rialEquivalent);
        Raise(new OpeningBalanceEntered(Id, AccountKind, AccountId, FiscalYear, date.Value, amount, rialEquivalent));
    }

    private static int ValidYear(int year)
    {
        Require(year is >= 1300 and <= 1500, "OpeningBalance.YearInvalid", "The fiscal year is a Jalali year such as 1405.");
        return year;
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

public sealed record OpeningBalanceEntered(
    Guid OpeningBalanceId, BalanceAccountKind AccountKind, Guid AccountId, int FiscalYear, DateOnly Date, decimal Amount, decimal? RialEquivalent) : DomainEvent;

public sealed record OpeningBalanceRemoved(Guid OpeningBalanceId, BalanceAccountKind AccountKind, Guid AccountId, int FiscalYear) : DomainEvent;
