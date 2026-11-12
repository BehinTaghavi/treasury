using Treasury.BuildingBlocks.Domain;
using Npgsql;
using Treasury.BuildingBlocks.Application;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.ChequeBooks;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Infrastructure.InMemory;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Cashflow.Infrastructure.Postgres.Tests;

/// <summary>
/// The use cases of InstrumentUseCaseTests, run through the real PostgreSQL adapters (Npgsql + db/postgres schema)
/// instead of the in-memory ones. Each test uses its own ids, so the tests share one database.
/// </summary>
public sealed class PostgresAdapterTests : IDisposable
{
    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly DevelopmentPolicies _policies = new DevelopmentPolicies().OpenYear(1403).OpenYear(1404).OpenYear(1405);
    private readonly PostgresUnitOfWork _unitOfWork;
    private readonly PostgresInstrumentRepository _instruments = new();
    private readonly InstrumentUseCases _useCases;
    private readonly ChequeBookUseCases _books;
    private readonly Guid _account = Guid.NewGuid();
    private readonly string _bank = "بانک ملی " + Guid.NewGuid().ToString("N")[..8]; // unique per test: duplicate checks stay local

    public PostgresAdapterTests(PostgresDatabase db)
    {
        _db = db;
        _db.Require();
        _dataSource = NpgsqlDataSource.Create(db.Service);
        _unitOfWork = new PostgresUnitOfWork(_dataSource);
        _useCases = new InstrumentUseCases(
            _unitOfWork,
            _instruments,
            new PostgresChequeBookRepository(),
            new PostgresIdempotencyStore(_dataSource),
            _policies,
            _policies,
            _policies,
            new FixedUser("clerk-1"));
        _books = new ChequeBookUseCases(_unitOfWork, new PostgresChequeBookRepository(), new AnyBankAccount(), new PostgresIdempotencyStore(_dataSource));
    }

    /// <summary>accounts-svc stand-in: every bank account exists and is active.</summary>
    private sealed class AnyBankAccount : IBankAccountLookup
    {
        public Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<BankAccountFact?>(new(id, "0101", true));
    }

    public void Dispose() => _dataSource.Dispose();

    [Fact]
    public async Task A_cheque_book_is_corrected_listed_and_deleted_while_unused_and_kept_once_a_sheet_is_used()
    {
        var unused = await RegisterBook(5001, 5050);
        Assert.True((await _books.CorrectAsync(new(Key(), unused, "A", 6001, 6010, 1), CancellationToken.None)).IsSuccess);
        var view = (await _books.ListAsync(_account, true, CancellationToken.None)).Single(b => b.Id == unused);
        Assert.Equal((6001L, 6010L, 10L, 2L), (view.FirstSheet, view.LastSheet, view.RemainingSheets, view.Version));
        Assert.True((await _books.ChangeStateAsync(new(Key(), unused, "delete", 2), CancellationToken.None)).IsSuccess);
        Assert.Equal(0L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.cheque_book WHERE id = @id", ("id", unused)));

        var used = await RegisterBook(7001, 7010);
        Assert.True((await Issue(used, Approved(), 7001)).IsSuccess);
        var usedView = (await _books.ListAsync(_account, true, CancellationToken.None)).Single(b => b.Id == used);
        Assert.Equal((1L, (long?)7002, true), (usedView.UsedSheets, usedView.NextSheet, usedView.IsInUse));
        Assert.Equal("ChequeBook.InUse", (await _books.ChangeStateAsync(new(Key(), used, "delete", usedView.Version), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_adapters_work_as_the_cashflow_service_role()
    {
        await using var cmd = _dataSource.CreateCommand("SELECT current_user");
        Assert.Equal("cashflow_svc", await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_received_cheque_is_stored_and_read_back_with_its_history_outbox_event_and_idempotency_record()
    {
        var key = Key();
        var result = await _useCases.ReceiveAsync(Receive(key), CancellationToken.None);
        Assert.True(result.IsSuccess, result.ErrorMessage);

        var view = await _useCases.GetAsync(result.Value, CancellationToken.None);
        Assert.NotNull(view);
        Assert.Equal("Received", view!.Direction);
        Assert.Equal("OrdinaryCheque", view.Kind);
        Assert.Equal("555001", view.Number);
        Assert.Equal(_bank, view.BankName);
        Assert.Equal(10_000_000m, view.Amount);
        Assert.Equal("IRR", view.Currency);
        Assert.Equal("1405/08/01", view.IssueDate);
        Assert.Equal("1405/09/01", view.DueDate);
        Assert.Equal("مشتری الف", view.Counterparty);
        Assert.Equal("InCashDesk", view.State);
        Assert.Equal(1405, view.OriginFiscalYear);
        Assert.Equal(1L, view.Version);
        var step = Assert.Single(view.History);
        Assert.Equal("Receive", step.Action);
        Assert.Equal("clerk-1", step.UserId);

        Assert.Equal(1405, await _db.ScalarAsync<int>("SELECT origin_fiscal_year FROM cashflow.instrument_fy1405 WHERE id = @id", ("id", result.Value)));
        Assert.Equal(10_000_000m, await _db.ScalarAsync<decimal>("SELECT amount_rial FROM cashflow.instrument WHERE id = @id", ("id", result.Value)));
        var payload = await _db.ScalarAsync<string>(
            "SELECT payload::text FROM cashflow.outbox WHERE type = 'InstrumentStateChanged' AND payload->>'InstrumentId' = @id", ("id", result.Value.ToString()));
        Assert.Contains("\"2026-10-23\"", payload, StringComparison.Ordinal); // ISO dates in events, not Jalali text
        Assert.Equal(result.Value.ToString(), await _db.ScalarAsync<string>("SELECT result FROM cashflow.idempotency WHERE key = @k", ("k", key)));
    }

    [Fact]
    public async Task A_foreign_currency_cheque_keeps_its_rate_and_rial_amount()
    {
        var command = Receive(Key()) with { Amount = new MoneyInput(1_000, "USD", 600_000) };
        var id = (await _useCases.ReceiveAsync(command, CancellationToken.None)).Value;
        var view = await _useCases.GetAsync(id, CancellationToken.None);
        Assert.Equal("USD", view!.Currency);
        Assert.Equal(1_000m, view.Amount);
        Assert.Equal(600_000_000m, view.AmountInRial);
    }

    [Fact]
    public async Task Reading_an_unknown_instrument_returns_nothing()
    {
        Assert.Null(await _useCases.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task The_same_idempotency_key_does_not_create_a_second_instrument()
    {
        var first = await _useCases.ReceiveAsync(Receive("same-" + Key()), CancellationToken.None);
        var second = await _useCases.ReceiveAsync(Receive(first.Value.ToString()), CancellationToken.None);
        var again = await _useCases.ReceiveAsync(Receive(first.Value.ToString()), CancellationToken.None);
        Assert.Equal(second.Value, again.Value);
        Assert.Equal(2L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.instrument WHERE bank_name = @b", ("b", _bank)));
    }

    [Fact]
    public async Task A_second_cheque_with_the_same_bank_and_number_is_saved_with_a_warning()
    {
        await _useCases.ReceiveAsync(Receive(Key()), CancellationToken.None);
        var duplicate = await _useCases.ReceiveAsync(Receive(Key()), CancellationToken.None);
        Assert.True(duplicate.IsSuccess);
        Assert.Contains(InstrumentUseCases.PossibleDuplicateWarning, duplicate.Warnings);
    }

    [Fact]
    public async Task Collection_path_stores_each_step_once_in_the_fiscal_year_it_happens_in()
    {
        // Received in 1404, deposited in 1404, cleared in 1405 (doc 7.4): the clearing step lives in the 1405 partition.
        var id = (await _useCases.ReceiveAsync(Receive(Key(), date: "1404/12/20") with { DueDate = "1405/01/10" }, CancellationToken.None)).Value;
        Assert.True((await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.DepositForCollection, "1404/12/25", Destination: "حساب جاری ملت"), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.RecordCleared, "1405/01/15", Reference: "R-1"), CancellationToken.None)).IsSuccess);

        var view = await _useCases.GetAsync(id, CancellationToken.None);
        Assert.Equal("Cleared", view!.State);
        Assert.Equal("حساب جاری ملت", view.Destination);
        Assert.Equal(["Receive", "DepositForCollection", "RecordCleared"], view.History.Select(h => h.Action));
        Assert.Equal("R-1", view.History[2].Reference);
        Assert.Equal(3L, view.Version);
        Assert.Equal(2L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.instrument_event_fy1404 WHERE instrument_id = @id", ("id", id)));
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.instrument_event_fy1405 WHERE instrument_id = @id", ("id", id)));
        Assert.Equal(3L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.outbox WHERE payload->>'InstrumentId' = @id", ("id", id.ToString())));
    }

    [Fact]
    public async Task A_rule_violation_returns_its_code_and_saves_nothing()
    {
        var id = (await _useCases.ReceiveAsync(Receive(Key()), CancellationToken.None)).Value;
        var result = await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.RecordCleared, "1405/08/02"), CancellationToken.None);
        Assert.Equal("Instrument.InvalidTransition", result.ErrorCode);
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT version FROM cashflow.instrument WHERE id = @id", ("id", id)));
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.instrument_event WHERE instrument_id = @id", ("id", id)));
    }

    [Fact]
    public async Task A_save_based_on_an_outdated_copy_is_refused()
    {
        var id = (await _useCases.ReceiveAsync(Receive(Key()), CancellationToken.None)).Value;
        var stale = await _unitOfWork.ExecuteAsync(ct => _instruments.GetAsync(id, ct), CancellationToken.None);
        Assert.True((await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.DepositForCollection, "1405/08/03", Destination: "ملت"), CancellationToken.None)).IsSuccess);

        stale!.ReturnToIssuer(FiscalDate.ParseJalali("1405/08/04"), "clerk-2", null);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            _unitOfWork.ExecuteAsync(async ct =>
            {
                await _instruments.SaveAsync(stale, ct);
                return 0;
            }, CancellationToken.None));
        Assert.Equal("DepositedForCollection", (await _useCases.GetAsync(id, CancellationToken.None))!.State);
    }

    [Fact]
    public async Task The_database_refuses_a_step_in_a_closed_fiscal_year_even_if_the_application_allowed_it()
    {
        var id = (await _useCases.ReceiveAsync(Receive(Key(), date: "1403/11/01"), CancellationToken.None)).Value;
        await _db.ExecuteAsync("SELECT cashflow.close_fiscal_year(1403)");

        // The development policy still says 1403 is open; the closed-year trigger is the last line of defence (ADR 0003).
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.DepositForCollection, "1403/11/05", Destination: "ملت"), CancellationToken.None));
        Assert.Equal("P0001", error.SqlState);
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT version FROM cashflow.instrument WHERE id = @id", ("id", id)));
    }

    [Fact]
    public async Task Issuing_reserves_sheets_in_order_and_records_the_cheque()
    {
        var book = await RegisterBook(1001, 1003);
        var first = await Issue(book, Approved(), null);
        var second = await Issue(book, Approved(), null);
        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.Equal("1001", first.Value!.SheetNumber);
        Assert.Equal("1002", second.Value!.SheetNumber);
        Assert.Equal(1L, second.Value.RemainingSheets);

        var cheque = await _useCases.GetAsync(first.Value.InstrumentId, CancellationToken.None);
        Assert.Equal("Issued", cheque!.State);
        Assert.Equal("1001", cheque.Number);
        Assert.Equal(2L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.cheque_sheet WHERE cheque_book_id = @b AND sheet_use = 'Issued'", ("b", book)));
        Assert.Equal(3L, await _db.ScalarAsync<long>("SELECT version FROM cashflow.cheque_book WHERE id = @b", ("b", book)));
    }

    [Fact]
    public async Task The_last_sheet_closes_the_cheque_book()
    {
        var book = await RegisterBook(1, 1);
        var only = await Issue(book, Approved(), null);
        Assert.False(only.Value!.ChequeBookStillActive);
        Assert.False(await _db.ScalarAsync<bool>("SELECT is_active FROM cashflow.cheque_book WHERE id = @b", ("b", book)));
    }

    [Fact]
    public async Task Parallel_issues_from_one_book_never_get_the_same_number()
    {
        // Real row locks (SELECT ... FOR UPDATE) instead of the in-memory store's single lock.
        var book = await RegisterBook(1, 25);
        var results = await Task.WhenAll(Enumerable.Range(0, 25).Select(_ => Issue(book, Approved(), null)));
        Assert.All(results, r => Assert.True(r.IsSuccess, r.ErrorMessage));
        Assert.Equal(25, results.Select(r => r.Value!.SheetNumber).Distinct().Count());
        Assert.Equal(25L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.cheque_sheet WHERE cheque_book_id = @b", ("b", book)));
    }

    [Fact]
    public async Task A_refused_issue_leaves_the_sheet_free()
    {
        var book = await RegisterBook(1, 5);
        var payment = Approved();
        _policies.SetBalance(_account, 1_000);
        Assert.Equal("Payment.InsufficientBalance", (await Issue(book, payment, null)).ErrorCode);
        Assert.Equal("ChequeBook.SheetOutOfRange", (await Issue(book, Approved(), 99)).ErrorCode);

        var next = await Issue(book, Approved(), null);
        Assert.Equal("1", next.Value!.SheetNumber);
    }

    private static string Key() => Guid.NewGuid().ToString();

    private ReceiveInstrumentCommand Receive(string key, string date = "1405/08/01") =>
        new(key, InstrumentKind.OrdinaryCheque, "555001", _bank, new MoneyInput(10_000_000), date, "1405/09/01", Guid.NewGuid(), "مشتری الف", "فروش");

    private Guid Approved()
    {
        var payment = Guid.NewGuid();
        _policies.Approve(payment);
        _policies.SetBalance(_account, 10_000_000_000);
        return payment;
    }

    private async Task<Guid> RegisterBook(long first, long last)
    {
        var result = await _books.RegisterAsync(new RegisterChequeBookCommand(Key(), _account, "A", first, last), CancellationToken.None);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        return result.Value;
    }

    private Task<Result<IssuedChequeView>> Issue(Guid book, Guid payment, long? sheet) =>
        _useCases.IssueChequeAsync(
            new IssueChequeCommand(Key(), payment, _account, book, sheet, InstrumentKind.OrdinaryCheque, new MoneyInput(5_000_000), "1405/08/10", "1405/08/10", Guid.NewGuid(), "تامین کننده", "خرید"),
            CancellationToken.None);

    private sealed class FixedUser(string id) : ICurrentUser
    {
        public string UserId { get; } = id;
    }
}
