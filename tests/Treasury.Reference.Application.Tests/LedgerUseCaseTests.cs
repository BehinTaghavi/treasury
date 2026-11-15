using Treasury.Reference.Application.Ledger;
using Treasury.Reference.Domain.Ledger;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

public sealed class LedgerUseCaseTests : IDisposable
{
    private readonly InMemoryReferenceDatabase _db = new();
    private readonly LedgerUseCases _useCases;

    public LedgerUseCaseTests()
    {
        var repo = new InMemoryLedgerRepository(_db);
        _useCases = new LedgerUseCases(new InMemoryUnitOfWork(_db), repo, repo, new InMemoryIdempotencyStore(_db));
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static readonly ChartRow[] Chart =
    [
        new(1, "1", "داراییهای جاری"), new(2, "11", "موجودی نقد و بانکها"), new(3, "1103", "موجودی بانکها"),
        new(4, "13", "دریافتنیها"), new(5, "1301", "اسناد دریافتنی"), new(6, "130100", "اسناد دریافتنی تجاری"),
    ];

    private static readonly DetailRow[] Details =
    [
        new(1, "100000", "بانك نمونه شعبه مرکزی - جاری", "بانكها", true, false),
        new(2, "100001", "بانک نمونه شعبه بازار - جاری", "بانكها", true, false),
        new(3, "111078", "شرکت نمونه بازرگانی", "اشخاص وشركتها", true, false),
        new(4, "770100", "تولید نمونه", "مراكز هزينه توليد", true, true),
    ];

    private async Task SetFormat() =>
        Assert.True((await _useCases.SetCodeFormatAsync(new SetCodeFormatCommand(Key(), 1, 2, 4, 6, 6), CancellationToken.None)).IsSuccess);

    [Fact]
    public async Task Nothing_can_be_imported_before_the_code_lengths_are_set()
    {
        Assert.Null(await _useCases.GetCodeFormatAsync(CancellationToken.None));
        Assert.Equal("Ledger.FormatNotSet", (await _useCases.PreviewChartAsync(Chart, CancellationToken.None)).ErrorCode);
        Assert.Equal("Ledger.FormatNotSet", (await _useCases.ImportChartAsync(Key(), Chart, CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_preview_saves_nothing_and_the_import_then_saves_the_chart_with_one_event()
    {
        await SetFormat();
        var preview = await _useCases.PreviewChartAsync(Chart, CancellationToken.None);
        Assert.Equal(6, preview.Value!.Added);
        Assert.Empty(await _useCases.AccountsAsync(false, true, CancellationToken.None));

        var import = await _useCases.ImportChartAsync(Key(), Chart, CancellationToken.None);
        Assert.Equal(6, import.Value!.Added);
        var postable = await _useCases.AccountsAsync(postableOnly: true, includeInactive: false, CancellationToken.None);
        Assert.Equal(["1103", "130100"], postable.Select(a => a.Code));
        Assert.Contains(_db.OutboxMessages, m => m.Type == nameof(ChartOfAccountsImported));
        Assert.True((await _useCases.GetCodeFormatAsync(CancellationToken.None))!.Locked);
    }

    [Fact]
    public async Task The_same_import_key_imports_once_and_returns_the_same_report()
    {
        await SetFormat();
        var key = Key();
        var first = await _useCases.ImportChartAsync(key, Chart, CancellationToken.None);
        var again = await _useCases.ImportChartAsync(key, Chart, CancellationToken.None);
        Assert.Equal(first.Value!.Added, again.Value!.Added);
        Assert.Equal(first.Value.Deactivated, again.Value.Deactivated);
        Assert.Single(_db.OutboxMessages, m => m.Type == nameof(ChartOfAccountsImported));
    }

    [Fact]
    public async Task Detail_accounts_are_searched_by_code_or_name_with_arabic_or_persian_letters_and_by_type()
    {
        await SetFormat();
        Assert.Equal(4, (await _useCases.ImportDetailsAsync(Key(), Details, CancellationToken.None)).Value!.Added);

        Assert.Equal(["100000", "100001"], (await _useCases.SearchDetailsAsync("بانک", null, false, 50, CancellationToken.None)).Select(d => d.Code));
        Assert.Equal(["100000"], (await _useCases.SearchDetailsAsync("مركزي", null, false, 50, CancellationToken.None)).Select(d => d.Code));
        Assert.Equal(["111078"], (await _useCases.SearchDetailsAsync("11107", null, false, 50, CancellationToken.None)).Select(d => d.Code));
        Assert.Equal(["770100"], (await _useCases.SearchDetailsAsync(null, "مراكز هزينه توليد", false, 50, CancellationToken.None)).Select(d => d.Code));
        Assert.Single(await _useCases.SearchDetailsAsync(null, null, false, 1, CancellationToken.None));
        Assert.Equal("بانكها", (await _useCases.DetailTypesAsync(CancellationToken.None))[0].Type);
    }

    [Fact]
    public async Task An_account_gets_the_detail_types_it_needs_from_the_imported_list()
    {
        await SetFormat();
        await _useCases.ImportChartAsync(Key(), Chart, CancellationToken.None);
        await _useCases.ImportDetailsAsync(Key(), Details, CancellationToken.None);

        Assert.True((await _useCases.SetDetailTypesAsync(new(Key(), "1103", ["بانكها"], []), CancellationToken.None)).IsSuccess);
        Assert.Equal("Ledger.UnknownDetailType", (await _useCases.SetDetailTypesAsync(new(Key(), "130100", ["مشتريان"], []), CancellationToken.None)).ErrorCode);
        var bank = (await _useCases.AccountsAsync(true, false, CancellationToken.None)).Single(a => a.Code == "1103");
        Assert.Equal(["بانكها"], bank.Detail1Types);
    }

    [Fact]
    public async Task The_code_lengths_cannot_change_after_an_import()
    {
        await SetFormat();
        await _useCases.ImportChartAsync(Key(), Chart, CancellationToken.None);
        var r = await _useCases.SetCodeFormatAsync(new SetCodeFormatCommand(Key(), 1, 3, 5, 7, 6), CancellationToken.None);
        Assert.Equal("Ledger.FormatLocked", r.ErrorCode);
    }
}
