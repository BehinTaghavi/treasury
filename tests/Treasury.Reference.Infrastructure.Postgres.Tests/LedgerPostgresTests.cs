using System.Diagnostics;
using Npgsql;
using Treasury.Reference.Application.Ledger;
using Treasury.Reference.Domain.Ledger;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Reference.Infrastructure.Postgres.Tests;

/// <summary>
/// The ledger copy through the real PostgreSQL adapters, as reference_svc. The chart and the تفصیل list are single
/// aggregates per database, so the steps run as one scenario on one shared state.
/// </summary>
public sealed class LedgerPostgresTests : IDisposable
{
    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly LedgerUseCases _useCases;

    public LedgerPostgresTests(PostgresDatabase db)
    {
        _db = db;
        _db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("reference_svc"));
        var repo = new PostgresLedgerRepository();
        _useCases = new LedgerUseCases(new PostgresUnitOfWork(_dataSource), repo, repo, new PostgresIdempotencyStore(_dataSource));
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    [Fact]
    public async Task The_ledger_copy_is_imported_searched_typed_and_reimported()
    {
        await A1_the_chart_is_stored_with_its_code_lengths_and_postable_accounts();
        await A2_ten_thousand_detail_accounts_import_in_one_transaction();
        await A3_search_finds_persian_and_arabic_letters_alike_and_filters_by_type();
        await A4_allowed_detail_types_are_stored_as_sets();
        await A5_a_new_export_switches_off_missing_accounts_and_keeps_their_detail_types();
        await A6_the_code_lengths_are_locked();
    }

    private static readonly ChartRow[] Chart =
    [
        // Children before parents on purpose: the import orders them, and the foreign key is checked per statement.
        new(1, "130100", "اسناد دریافتنی تجاری"), new(2, "1301", "اسناد دریافتنی"), new(3, "13", "دریافتنیها"),
        new(4, "1103", "موجودی بانکها"), new(5, "11", "موجودی نقد و بانکها"), new(6, "1", "داراییهای جاری"),
    ];

    private async Task A1_the_chart_is_stored_with_its_code_lengths_and_postable_accounts()
    {
        Assert.True((await _useCases.SetCodeFormatAsync(new(Key(), 1, 2, 4, 6, 6), CancellationToken.None)).IsSuccess);
        var report = await _useCases.ImportChartAsync(Key(), Chart, CancellationToken.None);
        Assert.True(report.IsSuccess, report.ErrorCode);
        Assert.Equal(6, report.Value!.Added);

        Assert.Equal(["1103", "130100"], (await _useCases.AccountsAsync(true, false, CancellationToken.None)).Select(a => a.Code));
        var format = await _useCases.GetCodeFormatAsync(CancellationToken.None);
        Assert.Equal((1, 2, 4, 6, 6, true), (format!.Group, format.General, format.Subsidiary, format.SubSubsidiary, format.Detail, format.Locked));
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.outbox WHERE type = 'ChartOfAccountsImported'"));
    }

    private async Task A2_ten_thousand_detail_accounts_import_in_one_transaction()
    {
        var rows = Enumerable.Range(0, 10_000)
            .Select(i => new DetailRow(i + 1, (100000 + i).ToString(System.Globalization.CultureInfo.InvariantCulture), i < 60 ? $"بانك نمونه {i}" : $"شخص نمونه {i}", i < 60 ? "بانكها" : "اشخاص وشركتها", i % 500 != 7, false))
            .ToList();
        var clock = Stopwatch.StartNew();
        var report = await _useCases.ImportDetailsAsync(Key(), rows, CancellationToken.None);
        clock.Stop();
        Assert.True(report.IsSuccess, report.ErrorCode);
        Assert.Equal(10_000, report.Value!.Added);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(30), $"import took {clock.Elapsed}");

        var types = await _useCases.DetailTypesAsync(CancellationToken.None);
        // Every 500th row from row 7 is inactive: 20 rows, one of them (row 7) a bank.
        Assert.Equal(("اشخاص وشركتها", 9921), (types[0].Type, types[0].ActiveCount));
        Assert.Equal(("بانكها", 59), (types[1].Type, types[1].ActiveCount));
    }

    private async Task A3_search_finds_persian_and_arabic_letters_alike_and_filters_by_type()
    {
        // Stored "بانك" (Arabic kaf); searched "بانک" (Persian kaf).
        var banks = await _useCases.SearchDetailsAsync("بانک نمونه", "بانكها", false, 200, CancellationToken.None);
        Assert.Equal(59, banks.Count);
        Assert.Equal("100000", banks[0].Code);
        Assert.Single(await _useCases.SearchDetailsAsync("100059", null, false, 200, CancellationToken.None));
        Assert.Equal(200, (await _useCases.SearchDetailsAsync(null, null, false, 5_000, CancellationToken.None)).Count);
        Assert.Empty(await _useCases.SearchDetailsAsync("%", null, false, 200, CancellationToken.None));
    }

    private async Task A4_allowed_detail_types_are_stored_as_sets()
    {
        Assert.True((await _useCases.SetDetailTypesAsync(new(Key(), "130100", ["اشخاص وشركتها", "بانكها"], ["بانكها"]), CancellationToken.None)).IsSuccess);
        var account = (await _useCases.AccountsAsync(true, false, CancellationToken.None)).Single(a => a.Code == "130100");
        Assert.Equal(["اشخاص وشركتها", "بانكها"], account.Detail1Types.Order(StringComparer.Ordinal));
        Assert.Equal(["بانكها"], account.Detail2Types);
        Assert.Empty((await _useCases.AccountsAsync(true, false, CancellationToken.None)).Single(a => a.Code == "1103").Detail1Types);
    }

    private async Task A5_a_new_export_switches_off_missing_accounts_and_keeps_their_detail_types()
    {
        var report = await _useCases.ImportChartAsync(Key(), Chart.Where(r => !r.Code.StartsWith("13", StringComparison.Ordinal)).ToList(), CancellationToken.None);
        Assert.Equal(3, report.Value!.Deactivated);
        Assert.DoesNotContain(await _useCases.AccountsAsync(false, false, CancellationToken.None), a => a.Code == "130100");
        var kept = (await _useCases.AccountsAsync(false, true, CancellationToken.None)).Single(a => a.Code == "130100");
        Assert.False(kept.IsActive);
        Assert.Equal(2, kept.Detail1Types.Count);
    }

    private async Task A6_the_code_lengths_are_locked()
    {
        Assert.Equal("Ledger.FormatLocked", (await _useCases.SetCodeFormatAsync(new(Key(), 1, 3, 5, 7, 6), CancellationToken.None)).ErrorCode);
    }
}
