using Npgsql;
using Treasury.Accounts.Application.BankAccounts;
using Treasury.Accounts.Application.Tests;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Accounts.Infrastructure.Postgres.Tests;

/// <summary>Bank accounts through the real PostgreSQL adapters, acting as the accounts_svc role.</summary>
public sealed class BankAccountPostgresTests : IDisposable
{
    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly BankAccountUseCases _useCases;
    private readonly string _tag = Random.Shared.Next(1_000_000, 9_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);

    public BankAccountPostgresTests(PostgresDatabase db)
    {
        _db = db;
        _db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("accounts_svc"));
        _useCases = new BankAccountUseCases(new PostgresUnitOfWork(_dataSource), new PostgresBankAccountRepository(), new FakeReference(), new PostgresIdempotencyStore(_dataSource));
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private BankAccountForm Form(string suffix, string? d1 = "100000", int? priority = null) =>
        new($"0101-{_tag}-{suffix}", FakeReference.Mellat, "شعبه بازار", "1234", FakeReference.Tehran, "1400/01/15", false, null,
            "1103", d1, null, "130100", "130101", priority, IsFacility: true);

    [Fact]
    public async Task The_adapters_act_as_the_accounts_service_role()
    {
        await using var cmd = _dataSource.CreateCommand("SELECT current_user");
        Assert.Equal("accounts_svc", await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_bank_account_round_trips_with_its_dates_codes_and_an_outbox_event()
    {
        var r = await _useCases.RegisterAsync(new(Key(), Form("1")), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _useCases.ListAsync(true, CancellationToken.None)).Single(x => x.Id == r.Value);
        Assert.Equal(("1400/01/15", "IRR", "100000", "130101", true, 1L), (v.OpeningDate, v.CurrencyCode, v.Detail1Code, v.BouncedAccountCode, v.IsFacility, v.Version));
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT count(*) FROM accounts.outbox WHERE payload->>'BankAccountId' = @id", ("id", r.Value.ToString())));
    }

    [Fact]
    public async Task The_collection_priority_is_stored_as_a_number_or_none()
    {
        var first = await _useCases.RegisterAsync(new(Key(), Form("p1", priority: 3)), CancellationToken.None);
        var none = await _useCases.RegisterAsync(new(Key(), Form("p2")), CancellationToken.None);
        Assert.True(first.IsSuccess && none.IsSuccess, first.ErrorCode ?? none.ErrorCode);
        var views = await _useCases.ListAsync(true, CancellationToken.None);
        Assert.Equal((int?)3, views.Single(v => v.Id == first.Value).CollectionPriority);
        Assert.Null(views.Single(v => v.Id == none.Value).CollectionPriority);
    }

    [Fact]
    public async Task Two_users_saving_the_same_number_at_once_get_one_account_and_a_clear_refusal()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => _useCases.RegisterAsync(new(Key(), Form("same")), CancellationToken.None)));
        Assert.Single(results, x => x.IsSuccess);
        Assert.All(results.Where(x => !x.IsSuccess), x => Assert.Equal("BankAccount.NumberTaken", x.ErrorCode));
    }

    [Fact]
    public async Task A_main_account_keeps_one_backup_even_when_two_are_saved_at_once()
    {
        var main = (await _useCases.RegisterAsync(new(Key(), Form("main")), CancellationToken.None)).Value;
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(i =>
            _useCases.RegisterAsync(new(Key(), Form($"backup{i}", d1: "100001"), main), CancellationToken.None)));
        Assert.Single(results, x => x.IsSuccess);
        Assert.All(results.Where(x => !x.IsSuccess), x => Assert.Equal("BankAccount.MainAccountHasBackup", x.ErrorCode));
    }

    [Fact]
    public async Task Editing_and_switching_off_use_the_version_the_user_saw()
    {
        var id = (await _useCases.RegisterAsync(new(Key(), Form("edit")), CancellationToken.None)).Value;
        Assert.True((await _useCases.ChangeAsync(new(Key(), id, Form("edit") with { BranchName = "شعبه مرکزی" }, 1), CancellationToken.None)).IsSuccess);
        Assert.Equal("Concurrency.Conflict", (await _useCases.SetActiveAsync(new(Key(), id, false, 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.SetActiveAsync(new(Key(), id, false, 2), CancellationToken.None)).IsSuccess);
        Assert.DoesNotContain(await _useCases.ListAsync(false, CancellationToken.None), x => x.Id == id);
        Assert.Equal("شعبه مرکزی", (await _useCases.ListAsync(true, CancellationToken.None)).Single(x => x.Id == id).BranchName);
    }
}
