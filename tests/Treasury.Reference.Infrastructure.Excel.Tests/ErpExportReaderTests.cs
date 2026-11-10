using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Ledger;
using Xunit;

namespace Treasury.Reference.Infrastructure.Excel.Tests;

/// <summary>Reading the accounting system's exports (layouts of Iran Nazho Co.'s files of 10 Nov 2026, made-up content).</summary>
public sealed class ErpExportReaderTests
{
    private static readonly AccountCodeFormat IranNazhoFormat = AccountCodeFormat.Create(1, 2, 4, 6, 6);

    private static FileStream Open(string name) => File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void The_chart_export_gives_one_row_per_account_with_its_excel_row_number()
    {
        using var file = Open("chart.xls");
        var rows = ErpExportReader.ReadChart(file);
        Assert.Equal(["1", "11", "1101", "1103", "13", "1301", "130100"], rows.Select(r => r.Code));
        Assert.Equal("موجودي بانكها", rows[3].Name); // kept as written; compared through PersianText.Key
        Assert.Equal(5, rows[3].RowNumber);
        Assert.Equal("اسناد دريافتني تجاري نمونه", rows[6].Name);
    }

    [Fact]
    public void The_chart_export_imports_without_rejected_rows()
    {
        using var file = Open("chart.xls");
        var chart = ChartOfAccounts.Create(Guid.NewGuid(), IranNazhoFormat);
        var report = chart.Import(ErpExportReader.ReadChart(file));
        Assert.Equal(7, report.Added);
        Assert.Empty(report.Rejected);
    }

    [Fact]
    public void The_detail_export_with_types_gives_type_status_and_cost_centre()
    {
        using var file = Open("details-with-types.xls");
        var rows = ErpExportReader.ReadDetails(file);
        Assert.Equal(["100000", "111078", "600046", "770100", "999999"], rows.Select(r => r.Code));
        Assert.Equal(("بانكها", (bool?)true, (bool?)false), (rows[0].Type, rows[0].IsActive, rows[0].IsCostCenter));
        Assert.Equal((bool?)false, rows[2].IsActive);
        Assert.Equal((bool?)true, rows[3].IsCostCenter);
        Assert.Equal(string.Empty, rows[4].Type);
    }

    [Fact]
    public void The_short_detail_export_has_no_types_and_persian_digits_become_a_normal_code()
    {
        using var file = Open("details-short.xls");
        var rows = ErpExportReader.ReadDetails(file);
        Assert.Equal(["100000", "100001"], rows.Select(r => r.Code));
        Assert.All(rows, r => Assert.Null(r.Type));
        Assert.All(rows, r => Assert.Null(r.IsActive));
    }

    [Theory]
    [InlineData("unrelated.xls")]
    [InlineData("details-with-types.xls")]
    public void A_file_that_is_not_a_chart_export_is_refused_with_a_clear_code(string name)
    {
        using var file = Open(name);
        Assert.Equal("Excel.UnknownLayout", Assert.Throws<DomainException>(() => ErpExportReader.ReadChart(file)).Code);
    }

    [Fact]
    public void A_chart_export_is_not_taken_for_a_detail_list()
    {
        using var file = Open("chart.xls");
        Assert.Equal("Excel.UnknownLayout", Assert.Throws<DomainException>(() => ErpExportReader.ReadDetails(file)).Code);
    }

    [Fact]
    public void A_file_that_is_not_excel_is_refused_with_a_clear_code()
    {
        using var file = new MemoryStream("this is not an Excel file"u8.ToArray());
        Assert.Equal("Excel.Unreadable", Assert.Throws<DomainException>(() => ErpExportReader.ReadChart(file)).Code);
    }

    /// <summary>
    /// Local check against a client's real exports (never committed): TREASURY_REAL_EXPORTS=folder with *tajmiei*.xls and
    /// *mahdodeh*.xls. Skipped when the variable is not set, including on CI.
    /// </summary>
    [Fact]
    public void Real_exports_import_without_rejected_rows_when_provided()
    {
        var folder = Environment.GetEnvironmentVariable("TREASURY_REAL_EXPORTS");
        Assert.SkipWhen(string.IsNullOrWhiteSpace(folder), "Set TREASURY_REAL_EXPORTS to check a client's real exports locally.");
        var chartFile = Directory.GetFiles(folder!, "*tajmiei*.xls").Single();
        var detailFile = Directory.GetFiles(folder!, "*mahdodeh*.xls").Single();

        var chart = ChartOfAccounts.Create(Guid.NewGuid(), IranNazhoFormat);
        using (var f = File.OpenRead(chartFile))
        {
            var report = chart.Import(ErpExportReader.ReadChart(f));
            Assert.Empty(report.Rejected);
            Assert.True(report.Added > 100, $"only {report.Added} accounts");
        }

        var directory = DetailAccountDirectory.Create(Guid.NewGuid());
        using (var f = File.OpenRead(detailFile))
        {
            var report = directory.Import(ErpExportReader.ReadDetails(f), IranNazhoFormat);
            Assert.Empty(report.Rejected);
            Assert.True(report.Added > 1000, $"only {report.Added} تفصیل");
        }

        Assert.Contains("بانكها", directory.ActiveTypes);
        Assert.True(chart.Find("1103")!.IsPostable);
    }
}
