using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Xunit;

namespace Treasury.Cashflow.Domain.Tests;

public sealed class FiscalDateTests
{
    [Theory]
    [InlineData("14030101", 2024, 3, 20)]   // Nowruz 1403
    [InlineData("1404/01/01", 2025, 3, 21)] // Nowruz 1404
    [InlineData("1405/08/18", 2026, 11, 9)]
    public void Parses_old_and_display_formats_to_real_dates(string jalali, int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), FiscalDate.ParseJalali(jalali).Value);

    [Fact]
    public void Round_trips_to_display_and_legacy_text()
    {
        var date = FiscalDate.FromJalali(1405, 8, 18);
        Assert.Equal("1405/08/18", date.ToJalaliString());
        Assert.Equal("14050818", date.ToLegacyString());
        Assert.Equal(1405, date.JalaliYear);
    }

    [Theory]
    [InlineData("1405/13/01")]
    [InlineData("1405/07/31")] // months 7-11 have 30 days
    [InlineData("14051")]
    [InlineData("abcd/ef/gh")]
    [InlineData("0000/00/00")] // placeholder used by the old program
    public void Rejects_invalid_dates(string text) =>
        Assert.Equal("FiscalDate.Invalid", Assert.Throws<DomainException>(() => FiscalDate.ParseJalali(text)).Code);

    [Fact]
    public void Compares_by_calendar_order() =>
        Assert.True(FiscalDate.ParseJalali("1404/12/29") < FiscalDate.ParseJalali("1405/01/01"));
}

public sealed class MoneyTests
{
    [Fact]
    public void Rial_amounts_must_be_whole_and_positive()
    {
        Assert.Equal("Money.RialMustBeWhole", Assert.Throws<DomainException>(() => Money.Rials(10.5m)).Code);
        Assert.Equal("Money.NotPositive", Assert.Throws<DomainException>(() => Money.Rials(0)).Code);
    }

    [Fact]
    public void Foreign_amount_converts_to_rial_rounded_like_the_old_system()
    {
        var usd = Money.Foreign(100.25m, "usd", 1_050_000m);
        Assert.Equal("USD", usd.Currency);
        Assert.Equal(105_262_500m, usd.ToRial());
    }

    [Fact]
    public void Foreign_amount_needs_a_positive_rate() =>
        Assert.Equal("Money.InvalidRate", Assert.Throws<DomainException>(() => Money.Foreign(10, "EUR", 0)).Code);
}
