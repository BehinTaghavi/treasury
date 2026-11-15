using System.Globalization;

namespace Treasury.BuildingBlocks.Domain;

/// <summary>
/// A calendar date. Stored as a real date (never as text); Jalali (Shamsi) is only an input and display format.
/// Replaces the 8-character text dates of the old and MVC systems (rule R2). Shared by every service: the only Jalali parser
/// (CLAUDE.md rule 5); moved here from cashflow-svc when accounts-svc needed it.
/// </summary>
public readonly record struct FiscalDate : IComparable<FiscalDate>
{
    private static readonly PersianCalendar Persian = new();

    public FiscalDate(DateOnly value) => Value = value;

    public DateOnly Value { get; }

    public int JalaliYear => Persian.GetYear(Value.ToDateTime(TimeOnly.MinValue));

    public int JalaliMonth => Persian.GetMonth(Value.ToDateTime(TimeOnly.MinValue));

    public int JalaliDay => Persian.GetDayOfMonth(Value.ToDateTime(TimeOnly.MinValue));

    public static FiscalDate FromJalali(int year, int month, int day)
    {
        if (year is < 1300 or > 1500 || month is < 1 or > 12)
        {
            throw Invalid($"{year:0000}/{month:00}/{day:00}");
        }

        var daysInMonth = Persian.GetDaysInMonth(year, month);
        if (day < 1 || day > daysInMonth)
        {
            throw Invalid($"{year:0000}/{month:00}/{day:00}");
        }

        return new FiscalDate(DateOnly.FromDateTime(Persian.ToDateTime(year, month, day, 0, 0, 0, 0)));
    }

    /// <summary>Parses "14050818" (old and MVC storage format) or "1405/08/18" (display format).</summary>
    public static FiscalDate ParseJalali(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var digits = text.Trim().Replace("/", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        if (digits.Length != 8 || !digits.All(char.IsAsciiDigit))
        {
            throw Invalid(text);
        }

        return FromJalali(
            int.Parse(digits.AsSpan(0, 4), CultureInfo.InvariantCulture),
            int.Parse(digits.AsSpan(4, 2), CultureInfo.InvariantCulture),
            int.Parse(digits.AsSpan(6, 2), CultureInfo.InvariantCulture));
    }

    public static bool TryParseJalali(string? text, out FiscalDate date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            date = ParseJalali(text);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    /// <summary>"1405/08/18".</summary>
    public string ToJalaliString() => $"{JalaliYear:0000}/{JalaliMonth:00}/{JalaliDay:00}";

    /// <summary>"14050818" — only for exchanging data with the old systems.</summary>
    public string ToLegacyString() => $"{JalaliYear:0000}{JalaliMonth:00}{JalaliDay:00}";

    public int CompareTo(FiscalDate other) => Value.CompareTo(other.Value);

    public static bool operator <(FiscalDate left, FiscalDate right) => left.CompareTo(right) < 0;

    public static bool operator >(FiscalDate left, FiscalDate right) => left.CompareTo(right) > 0;

    public static bool operator <=(FiscalDate left, FiscalDate right) => left.CompareTo(right) <= 0;

    public static bool operator >=(FiscalDate left, FiscalDate right) => left.CompareTo(right) >= 0;

    public override string ToString() => ToJalaliString();

    private static DomainException Invalid(string text) => new("FiscalDate.Invalid", $"'{text}' is not a valid Jalali date.");
}
