using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.Common;

/// <summary>
/// An amount in one currency. Rial amounts are whole numbers.
/// Foreign-currency amounts keep the rate used to convert them to Rial (rule R10).
/// </summary>
public sealed record Money
{
    public const string Rial = "IRR";

    private Money(decimal amount, string currency, decimal? rateToRial)
    {
        Amount = amount;
        Currency = currency;
        RateToRial = rateToRial;
    }

    public decimal Amount { get; }

    /// <summary>ISO 4217 code, for example IRR, USD, EUR.</summary>
    public string Currency { get; }

    /// <summary>Rial per one unit of <see cref="Currency"/>; null for Rial amounts.</summary>
    public decimal? RateToRial { get; }

    public bool IsRial => Currency == Rial;

    public static Money Rials(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Money.NotPositive", "Amount must be greater than zero.");
        }

        if (decimal.Truncate(amount) != amount)
        {
            throw new DomainException("Money.RialMustBeWhole", "A Rial amount cannot have decimals.");
        }

        return new Money(amount, Rial, null);
    }

    public static Money Foreign(decimal amount, string currency, decimal rateToRial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var code = currency.Trim().ToUpperInvariant();
        if (code == Rial)
        {
            return Rials(amount);
        }

        if (code.Length != 3)
        {
            throw new DomainException("Money.InvalidCurrency", $"'{currency}' is not a three-letter currency code.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Money.NotPositive", "Amount must be greater than zero.");
        }

        if (rateToRial <= 0)
        {
            throw new DomainException("Money.InvalidRate", "Exchange rate must be greater than zero.");
        }

        return new Money(amount, code, rateToRial);
    }

    /// <summary>Equivalent in Rial, rounded to a whole Rial (old system: Round(Amount * Currate)).</summary>
    public decimal ToRial() => IsRial ? Amount : decimal.Round(Amount * RateToRial!.Value, 0, MidpointRounding.AwayFromZero);

    public override string ToString() => IsRial ? $"{Amount:N0} {Currency}" : $"{Amount:N2} {Currency} @ {RateToRial:N0}";
}
