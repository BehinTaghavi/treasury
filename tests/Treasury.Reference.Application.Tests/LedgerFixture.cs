using Treasury.Reference.Application.Ledger;
using Treasury.Reference.Domain.Ledger;
using Xunit;

namespace Treasury.Reference.Application.Tests;

/// <summary>A small made-up chart of accounts for use-case tests that need ledger accounts.</summary>
internal static class LedgerFixture
{
    /// <summary>Takes entries (no lower level).</summary>
    public const string Postable = "2101";

    /// <summary>Has a lower level, so it does not take entries.</summary>
    public const string Parent = "13";

    public static void Load(LedgerUseCases useCases)
    {
        Assert.True(useCases.SetCodeFormatAsync(new SetCodeFormatCommand("fixture-format", 1, 2, 4, 6, 6), CancellationToken.None).GetAwaiter().GetResult().IsSuccess);
        ChartRow[] chart =
        [
            new(1, "1", "داراییهای جاری"), new(2, "13", "دریافتنیها"), new(3, "1301", "اسناد دریافتنی"),
            new(4, "2", "بدهیهای جاری"), new(5, "21", "پیش دریافتها"), new(6, Postable, "پیش دریافت فروش"),
        ];
        Assert.True(useCases.ImportChartAsync("fixture-chart", chart, CancellationToken.None).GetAwaiter().GetResult().IsSuccess);
    }
}
