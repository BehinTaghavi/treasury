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
        _useCases = new BankAccountUseCases(new PostgresUnitOfWork(_dataSource), new PostgresBankAccountRepository(), new FakeReference(), new PostgresIdempotencyStore(_dataSource),
            new PostgresOpeningBalanceRepository());
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
    public async Task An_account_type_and_its_bank_cards_round_trip_report_4()
    {
        // Card numbers per test run, so the shared test database never sees one twice.
        var seed = Math.Abs(_tag.GetHashCode()) % 100_000_000;
        string Card(int i)
        {
            var body = $"60379{(seed + i) % 100_000_000:D8}00".Substring(0, 15);
            for (var d = 0; d < 10; d++)
            {
                if (Treasury.Accounts.Domain.BankAccounts.BankAccount.IsCardNumber(body + d))
                {
                    return body + d;
                }
            }

            throw new InvalidOperationException();
        }

        var form = Form("7") with
        {
            AccountType = Treasury.Accounts.Domain.BankAccounts.BankAccountType.ShortTermDeposit,
            Term = Treasury.Accounts.Domain.BankAccounts.ShortTermPeriod.ThreeMonths,
            Cards = [new(Card(1), "شرکت نمونه"), new(Card(2), "علی رضایی")],
        };
        var r = await _useCases.RegisterAsync(new(Key(), form), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _useCases.ListAsync(true, CancellationToken.None)).Single(a => a.Id == r.Value);
        Assert.Equal((Treasury.Accounts.Domain.BankAccounts.BankAccountType.ShortTermDeposit, (Treasury.Accounts.Domain.BankAccounts.ShortTermPeriod?)Treasury.Accounts.Domain.BankAccounts.ShortTermPeriod.ThreeMonths, false),
            (v.AccountType, v.Term, v.HasCheques));
        Assert.Equal([Card(1), Card(2)], v.Cards!.Select(c => c.Number));

        Assert.True((await _useCases.ChangeAsync(new(Key(), r.Value, form with { Cards = [new(Card(2), "علی رضایی")] }, v.Version), CancellationToken.None)).IsSuccess);
        Assert.Equal(Card(2), Assert.Single((await _useCases.ListAsync(true, CancellationToken.None)).Single(a => a.Id == r.Value).Cards!).Number);
        Assert.Equal("BankAccount.CardTaken", (await _useCases.RegisterAsync(new(Key(), Form("8") with { Cards = [new(Card(2), "x")] }), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_company_card_without_a_bank_account_round_trips_and_its_number_is_unique()
    {
        // Owner's answer to Q69 (20 Nov 2026); db/postgres/044_accounts_company_cards.sql. A card number per test run.
        var body = $"62198{Math.Abs(_tag.GetHashCode()) % 100_000_000:D8}00"[..15];
        var number = Enumerable.Range(0, 10).Select(d => body + d).First(Treasury.Accounts.Domain.BankAccounts.BankAccount.IsCardNumber);
        var cards = new Treasury.Accounts.Application.CompanyCards.CompanyCardUseCases(
            new PostgresUnitOfWork(_dataSource), new PostgresCompanyCardRepository(), new PostgresBankAccountRepository(), new FakeReference(), new PostgresIdempotencyStore(_dataSource));
        var form = new Treasury.Accounts.Application.CompanyCards.CompanyCardForm(number, "شرکت نمونه " + _tag, FakeReference.Mellat, "1103", "100000");
        var r = await cards.RegisterAsync(new(Key(), form), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Equal("CompanyCard.NumberTaken", (await cards.RegisterAsync(new(Key(), form with { HolderName = "دیگری" }), CancellationToken.None)).ErrorCode);
        var v = (await cards.ListAsync(true, CancellationToken.None)).Single(c => c.Id == r.Value);
        Assert.Equal((number, "شرکت نمونه " + _tag, FakeReference.Mellat, "1103", "100000", true, 1L), (v.CardNumber, v.HolderName, v.BankId, v.LedgerAccountCode, v.DetailCode, v.IsActive, v.Version));

        Assert.True((await cards.ChangeAsync(new(Key(), r.Value, form with { LedgerAccountCode = "130100", DetailCode = null }, 1), CancellationToken.None)).IsSuccess);
        Assert.True((await cards.SetActiveAsync(new(Key(), r.Value, false, 2), CancellationToken.None)).IsSuccess);
        var after = (await cards.ListAsync(true, CancellationToken.None)).Single(c => c.Id == r.Value);
        Assert.Equal(("130100", (string?)null, false, 3L), (after.LedgerAccountCode, after.DetailCode, after.IsActive, after.Version));
        Assert.Equal(3L, await _db.ScalarAsync<long>("SELECT count(*) FROM accounts.outbox WHERE type = 'CompanyCardChanged' AND payload->>'CompanyCardId' = @id", ("id", r.Value.ToString())));
    }

    [Fact]
    public async Task A_cash_desk_round_trips_and_its_name_is_unique()
    {
        var desks = new Treasury.Accounts.Application.CashDesks.CashDeskUseCases(
            new PostgresUnitOfWork(_dataSource), new PostgresCashDeskRepository(), new FakeReference(), new PostgresIdempotencyStore(_dataSource));
        var form = new Treasury.Accounts.Application.CashDesks.CashDeskForm("صندوق " + _tag, "130100", "111078", null, false, null, "1405/08/22");
        var r = await desks.RegisterAsync(new(Key(), form), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Equal("CashDesk.NameTaken", (await desks.RegisterAsync(new(Key(), form with { Name = "صندوق  " + _tag }), CancellationToken.None)).ErrorCode);
        var v = (await desks.ListAsync(true, CancellationToken.None)).Single(d => d.Id == r.Value);
        Assert.Equal(("130100", "111078", "IRR", 1L, "1405/08/22"), (v.LedgerAccountCode, v.HolderDetailCode, v.CurrencyCode, v.Version, v.CreatedOn));
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT count(*) FROM accounts.outbox WHERE type = 'CashDeskChanged' AND payload->>'CashDeskId' = @id", ("id", r.Value.ToString())));
    }

    [Fact]
    public async Task A_petty_cash_round_trips_with_its_amounts_and_purpose_groups()
    {
        var funds = new Treasury.Accounts.Application.PettyCash.PettyCashUseCases(
            new PostgresUnitOfWork(_dataSource), new PostgresPettyCashRepository(), new FakeReference(), new PostgresIdempotencyStore(_dataSource));
        // One petty cash per holder and the test database is kept between runs: register once, then change.
        var form = new Treasury.Accounts.Application.PettyCash.PettyCashForm(
            "111078", "130100", 75_000_000_000, 60_000_000_000, 1_000_000_000, [FakeReference.Expenses], Treasury.Accounts.Domain.PettyCash.PettyCashRecovery.BankCard,
            "1405/08/22", new Treasury.Accounts.Domain.PettyCash.PettyCashKeeping(true, "جاری تنخواه", "0101-555", true, true, "علی رضایی", "6037991234567893"));
        var existing = (await funds.ListAsync(true, CancellationToken.None)).FirstOrDefault(f => f.HolderDetailCode == "111078");
        var r = existing is null
            ? await funds.RegisterAsync(Key(), form, CancellationToken.None)
            : await funds.ChangeAsync(Key(), existing.Id, form, existing.Version, CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await funds.ListAsync(true, CancellationToken.None)).Single(f => f.Id == r.Value);
        Assert.Equal((75_000_000_000m, 60_000_000_000m, 1_000_000_000m), (v.Amount, v.SettleLimit, v.PayCeiling));
        Assert.Equal([FakeReference.Expenses], v.AllowedPurposeGroupIds);
        Assert.Equal(("1405/08/22", "6037991234567893", Treasury.Accounts.Domain.PettyCash.PettyCashRecovery.BankCard), (v.CreatedOn, v.Keeping!.CardNumber, v.Recovery));
        Assert.Equal("PettyCash.HolderTaken", (await funds.RegisterAsync(Key(), form, CancellationToken.None)).ErrorCode);
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

    [Fact]
    public async Task An_opening_balance_round_trips_is_carried_to_the_next_year_and_keeps_the_account_from_deletion()
    {
        var balances = new Treasury.Accounts.Application.OpeningBalances.OpeningBalanceUseCases(
            new PostgresUnitOfWork(_dataSource), new PostgresOpeningBalanceRepository(), new PostgresBankAccountRepository(), new PostgresCashDeskRepository(),
            new PostgresPettyCashRepository(), new PostgresIdempotencyStore(_dataSource));
        var bank = await _useCases.RegisterAsync(new(Key(), Form("ob")), CancellationToken.None);
        Assert.True(bank.IsSuccess, bank.ErrorCode);
        var kind = Treasury.Accounts.Domain.OpeningBalances.BalanceAccountKind.BankAccount;
        var r = await balances.EnterAsync(new(Key(), kind, bank.Value, 1405, "1405/08/24", -2_500_000, null), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.True((await balances.EnterAsync(new(Key(), kind, bank.Value, 1405, "1405/08/25", 3_000_000, null, 1), CancellationToken.None)).IsSuccess);

        var mine = (await balances.ListAsync(1405, CancellationToken.None)).Single(v => v.AccountId == bank.Value);
        Assert.Equal(("1405/08/25", 3_000_000m, 2L), (mine.Entry!.Date, mine.Entry.Amount, mine.Entry.Version));
        Assert.Equal(1405, (await balances.ListAsync(1407, CancellationToken.None)).Single(v => v.AccountId == bank.Value).Carried!.FromYear);
        Assert.Equal("BankAccount.InUse", (await _useCases.DeleteAsync(new(Key(), bank.Value, 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_signatories_of_an_account_round_trip_in_their_order_report_8()
    {
        var form = Form("8") with
        {
            RequiredSignatures = 2,
            Signatories =
            [
                new(FakeReference.Chairman, Treasury.Accounts.Domain.BankAccounts.SignatureKind.Variable),
                new(FakeReference.Ceo, Treasury.Accounts.Domain.BankAccounts.SignatureKind.Fixed),
                new(FakeReference.BoardMember, Treasury.Accounts.Domain.BankAccounts.SignatureKind.Variable),
            ],
        };
        var r = await _useCases.RegisterAsync(new(Key(), form), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _useCases.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(2, v.RequiredSignatures);
        Assert.Equal([FakeReference.Chairman, FakeReference.Ceo, FakeReference.BoardMember], v.Signatories!.Select(s => s.SignatoryId));
        Assert.Equal(Treasury.Accounts.Domain.BankAccounts.SignatureKind.Fixed, v.Signatories![1].Kind);

        // Replaced as a whole by a correction; an account without signatories lists none.
        var changed = await _useCases.ChangeAsync(new(Key(), r.Value, form with { RequiredSignatures = 1, Signatories = [new(FakeReference.Ceo, Treasury.Accounts.Domain.BankAccounts.SignatureKind.Fixed)] }, v.Version), CancellationToken.None);
        Assert.True(changed.IsSuccess, changed.ErrorCode);
        v = (await _useCases.ListAsync(true, CancellationToken.None)).Single(x => x.Id == r.Value);
        Assert.Equal((1, FakeReference.Ceo), (v.RequiredSignatures, Assert.Single(v.Signatories!).SignatoryId));
        var plain = await _useCases.RegisterAsync(new(Key(), Form("9")), CancellationToken.None);
        Assert.True(plain.IsSuccess, plain.ErrorCode);
        Assert.Empty((await _useCases.GetAsync(plain.Value, CancellationToken.None))!.Signatories!);
    }
}
