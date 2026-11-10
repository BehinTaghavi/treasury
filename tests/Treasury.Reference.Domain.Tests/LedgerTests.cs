using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Ledger;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>
/// Chart of accounts and تفصیل list copied from the accounting system (owner, 10 Nov 2026: four fixed levels with
/// per-company code lengths, floating تفصیل with types). Made-up accounts in the shape of Iran Nazho Co.'s export.
/// </summary>
public sealed class LedgerTests
{
    private static readonly AccountCodeFormat Format = AccountCodeFormat.Create(1, 2, 4, 6, 6);

    private static ChartRow R(int row, string code, string name) => new(row, code, name);

    private static readonly ChartRow[] Sample =
    [
        R(1, "1", "داراییهای جاری"),
        R(2, "11", "موجودی نقد و بانکها"),
        R(3, "1101", "موجودی صندوق"),
        R(4, "1103", "موجودی بانکها"),
        R(5, "13", "حسابها و اسناد دریافتنی"),
        R(6, "1301", "اسناد دریافتنی تجاری"),
        R(7, "130100", "اسناد دریافتنی تجاری - داخلی"),
        R(8, "130101", "سایر اسناد دریافتنی تجاری"),
    ];

    private static ChartOfAccounts Chart() => ChartOfAccounts.Create(Guid.NewGuid(), Format);

    [Theory]
    [InlineData("1", LedgerLevel.Group)]
    [InlineData("11", LedgerLevel.General)]
    [InlineData("1103", LedgerLevel.Subsidiary)]
    [InlineData("110301", LedgerLevel.SubSubsidiary)]
    public void The_level_follows_from_the_code_length(string code, LedgerLevel level) => Assert.Equal(level, Format.LevelOf(code));

    [Theory]
    [InlineData("110")]
    [InlineData("11a3")]
    [InlineData("")]
    public void A_code_of_another_length_or_with_letters_has_no_level(string code) => Assert.Null(Format.LevelOf(code));

    [Theory]
    [InlineData(1, 1, 4, 6, 6)]
    [InlineData(1, 3, 2, 7, 6)]
    [InlineData(0, 2, 4, 6, 6)]
    [InlineData(1, 2, 4, 6, 0)]
    public void Code_lengths_must_grow_level_by_level(int g, int k, int m, int s, int d) =>
        Assert.Equal("Ledger.InvalidCodeFormat", Assert.Throws<DomainException>(() => AccountCodeFormat.Create(g, k, m, s, d)).Code);

    [Fact]
    public void An_import_builds_the_tree_and_marks_where_entries_can_be_made()
    {
        var chart = Chart();
        var report = chart.Import(Sample);
        Assert.Equal(8, report.Added);
        Assert.Empty(report.Rejected);
        Assert.Equal("1103", chart.Find("110301")?.ParentCode ?? chart.Find("1103")!.Code);
        Assert.Equal("1301", chart.Find("130100")!.ParentCode);

        // Old program (Form_D_Check): "حساب داراي سطح معين در معين است" – an account with a lower level takes no entries.
        Assert.True(chart.Find("1103")!.IsPostable);
        Assert.False(chart.Find("1301")!.IsPostable);
        Assert.True(chart.Find("130100")!.IsPostable);
        Assert.False(chart.Find("11")!.IsPostable);
        Assert.False(chart.Find("1")!.IsPostable);
    }

    [Fact]
    public void Rows_that_do_not_fit_are_skipped_and_reported_with_their_row_number()
    {
        var chart = Chart();
        var report = chart.Import(
        [
            R(1, "1", "داراییها"),
            R(2, "11", "نقد"),
            R(3, "110", "سه رقمی"),
            R(4, "2201", "معین بدون کل"),
            R(5, "1101", ""),
            R(6, "11", "نقد تکراری"),
        ]);
        Assert.Equal(2, report.Added);
        Assert.Equal(
            [(3, "Ledger.CodeDoesNotFitFormat"), (4, "Ledger.ParentMissing"), (5, "Ledger.NameRequired"), (6, "Ledger.DuplicateCode")],
            report.Rejected.Select(r => (r.RowNumber, r.Reason)));
    }

    [Fact]
    public void Children_may_come_before_their_parents_in_the_file()
    {
        var chart = Chart();
        var report = chart.Import(Sample.Reverse().ToList());
        Assert.Equal(8, report.Added);
        Assert.Empty(report.Rejected);
    }

    [Fact]
    public void A_new_import_adds_renames_switches_off_and_back_on_and_recomputes_postable()
    {
        var chart = Chart();
        chart.Import(Sample);
        chart.MarkSaved(1);
        chart.ClearChanges();

        // 1101 renamed, 1103 gets a sub-level, the 13 branch disappears.
        var second = chart.Import(
        [
            R(1, "1", "داراییهای جاری"), R(2, "11", "موجودی نقد و بانکها"), R(3, "1101", "صندوق"),
            R(4, "1103", "موجودی بانکها"), R(5, "110301", "بانکهای ریالی"),
        ]);
        Assert.Equal((1, 1, 0, 4, 3), (second.Added, second.Renamed, second.Reactivated, second.Deactivated, second.Unchanged));
        Assert.False(chart.Find("1103")!.IsPostable);
        Assert.True(chart.Find("110301")!.IsPostable);
        Assert.False(chart.Find("1301")!.IsActive);
        Assert.Equal("صندوق", chart.Find("1101")!.Name);

        var third = chart.Import(Sample);
        Assert.Equal(4, third.Reactivated);
        Assert.True(chart.Find("130100")!.IsActive);
    }

    [Fact]
    public void A_preview_changes_nothing()
    {
        var chart = Chart();
        var preview = chart.Preview(Sample);
        Assert.Equal(8, preview.Added);
        Assert.Empty(chart.Accounts);
        Assert.Single(chart.PendingEvents); // only the creation event
    }

    [Fact]
    public void A_file_with_nothing_that_fits_is_refused_and_switches_nothing_off()
    {
        var chart = Chart();
        chart.Import(Sample);
        Assert.Equal("Ledger.NothingToImport", Assert.Throws<DomainException>(() => chart.Import([R(1, "100000", "یک تفصیل")])).Code);
        Assert.All(chart.Accounts, a => Assert.True(a.IsActive));
    }

    [Fact]
    public void The_code_lengths_are_locked_once_accounts_exist()
    {
        var chart = Chart();
        chart.ChangeFormat(AccountCodeFormat.Create(1, 3, 5, 7, 6));
        chart.ChangeFormat(Format);
        chart.Import(Sample);
        Assert.Equal("Ledger.FormatLocked", Assert.Throws<DomainException>(() => chart.ChangeFormat(AccountCodeFormat.Create(1, 3, 5, 7, 6))).Code);
    }

    private static readonly HashSet<string> Types = ["بانكها", "مراكز هزينه", "اشخاص وشركتها", "مشتريان"];

    [Fact]
    public void Detail_types_are_set_only_on_accounts_that_take_entries_and_only_with_known_types()
    {
        var chart = Chart();
        chart.Import(Sample);

        chart.SetDetailTypes("1103", ["بانكها"], [], Types);
        Assert.Equal(["بانكها"], chart.Find("1103")!.Detail1Types);
        Assert.Equal("Ledger.DetailTypesOnlyOnPostable", Assert.Throws<DomainException>(() => chart.SetDetailTypes("1301", ["بانكها"], [], Types)).Code);
        Assert.Equal("Ledger.Detail2WithoutDetail1", Assert.Throws<DomainException>(() => chart.SetDetailTypes("1103", [], ["مراكز هزينه"], Types)).Code);
        Assert.Equal("Ledger.UnknownDetailType", Assert.Throws<DomainException>(() => chart.SetDetailTypes("1103", ["كاركنان"], [], Types)).Code);
        Assert.Equal("Ledger.AccountNotFound", Assert.Throws<DomainException>(() => chart.SetDetailTypes("9999", ["بانكها"], [], Types)).Code);
    }

    [Fact]
    public void An_account_may_allow_several_detail_types_and_refuses_the_others()
    {
        // Owner, 10 Nov 2026: all allowed types are listed on the account; a تفصیل of another type is blocked.
        var chart = Chart();
        chart.Import(Sample);
        chart.SetDetailTypes("130100", ["اشخاص وشركتها", "مشتريان"], ["مراكز هزينه"], Types);
        var account = chart.Find("130100")!;

        account.CheckDetails(true, "مشتريان", true, "مراكز هزينه");
        account.CheckDetails(true, "اشخاص وشركتها", true, "مراكز هزينه");
        Assert.Equal("Ledger.DetailTypeNotAllowed", Assert.Throws<DomainException>(() => account.CheckDetails(true, "بانكها", true, "مراكز هزينه")).Code);
        Assert.Equal("Ledger.DetailTypeNotAllowed", Assert.Throws<DomainException>(() => account.CheckDetails(true, "مشتريان", true, "مشتريان")).Code);
        Assert.Equal("Ledger.DetailRequired", Assert.Throws<DomainException>(() => account.CheckDetails(false, null, true, "مراكز هزينه")).Code);
        Assert.Equal("Ledger.DetailRequired", Assert.Throws<DomainException>(() => account.CheckDetails(true, "مشتريان", false, null)).Code);
    }

    [Fact]
    public void An_account_without_detail_types_takes_no_detail_and_nothing_is_enforced_before_types_are_set()
    {
        var chart = Chart();
        chart.Import(Sample);
        var account = chart.Find("1103")!;
        account.CheckDetails(false, null, false, null);
        Assert.Equal("Ledger.DetailNotAllowed", Assert.Throws<DomainException>(() => account.CheckDetails(true, "بانكها", false, null)).Code);
    }

    [Fact]
    public void Detail_accounts_are_imported_with_type_status_and_cost_centre()
    {
        var directory = DetailAccountDirectory.Create(Guid.NewGuid());
        var report = directory.Import(
        [
            new(1, "100000", "بانک نمونه شعبه مرکزی - جاری", "بانكها", true, false),
            new(2, "600046", "کارمند نمونه", "كاركنان كارخانه", true, false),
            new(3, "770100", "مرکز هزینه تولید نمونه", "مراكز هزينه توليد", false, true),
            new(4, "12345", "کد پنج رقمی", "بانكها", true, false),
        ], Format);
        Assert.Equal(3, report.Added);
        Assert.Equal("Ledger.CodeDoesNotFitFormat", Assert.Single(report.Rejected).Reason);
        Assert.False(directory.Find("770100")!.IsActive);
        Assert.True(directory.Find("770100")!.IsCostCenter);
        Assert.Equal(new HashSet<string> { "بانكها", "كاركنان كارخانه" }, directory.ActiveTypes);
    }

    [Fact]
    public void A_short_export_without_types_keeps_the_types_already_known()
    {
        var directory = DetailAccountDirectory.Create(Guid.NewGuid());
        directory.Import([new(1, "100000", "بانک نمونه", "بانكها", true, false)], Format);
        directory.ClearChanges();
        var report = directory.Import([new(1, "100000", "بانک نمونه", null, null, null), new(2, "100001", "بانک دوم", null, null, null)], Format);
        Assert.Equal((1, 1), (report.Added, report.Unchanged));
        Assert.Equal("بانكها", directory.Find("100000")!.Type);
        Assert.Null(directory.Find("100001")!.Type);
    }
}
