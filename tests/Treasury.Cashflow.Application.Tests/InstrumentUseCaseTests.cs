using Treasury.BuildingBlocks.Application;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.ChequeBooks;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Cashflow.Application.Tests;

public sealed class InstrumentUseCaseTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private readonly DevelopmentPolicies _policies = new DevelopmentPolicies().OpenYear(1405);
    private readonly InstrumentUseCases _useCases;
    private readonly ChequeBookUseCases _books;
    private readonly Guid _account = Guid.NewGuid();

    public InstrumentUseCaseTests()
    {
        _useCases = new InstrumentUseCases(
            new InMemoryUnitOfWork(_db),
            new InMemoryInstrumentRepository(_db),
            new InMemoryChequeBookRepository(_db),
            new InMemoryIdempotencyStore(_db),
            _policies,
            _policies,
            _policies,
            new FixedUser("clerk-1"));
        _books = new ChequeBookUseCases(new InMemoryUnitOfWork(_db), new InMemoryChequeBookRepository(_db), new FakeBankAccounts(), new InMemoryIdempotencyStore(_db));
    }

    public void Dispose() => _db.Dispose();

    private static ReceiveInstrumentCommand Receive(string key, string number = "555001", string date = "1405/08/01") =>
        new(key, InstrumentKind.OrdinaryCheque, number, "بانک ملی", new MoneyInput(10_000_000), date, "1405/09/01", Guid.NewGuid(), "مشتری الف", "فروش");

    [Fact]
    public async Task Receiving_saves_the_instrument_and_one_outbox_event()
    {
        var result = await _useCases.ReceiveAsync(Receive("k1"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var view = await _useCases.GetAsync(result.Value, CancellationToken.None);
        Assert.NotNull(view);
        Assert.Equal("InCashDesk", view!.State);
        Assert.Equal("1405/08/01", view.IssueDate);
        Assert.Contains("DepositForCollection", view.AllowedActions);
        var message = Assert.Single(_db.OutboxMessages);
        Assert.Equal(nameof(InstrumentStateChanged), message.Type);
        Assert.Contains("\"Date\":\"2026-10-23\"", message.Payload); // events carry ISO dates, not Jalali text
    }

    [Fact]
    public async Task The_same_idempotency_key_does_not_create_a_second_instrument()
    {
        var first = await _useCases.ReceiveAsync(Receive("same-key"), CancellationToken.None);
        var second = await _useCases.ReceiveAsync(Receive("same-key"), CancellationToken.None);
        Assert.Equal(first.Value, second.Value);
        Assert.Single(_db.OutboxMessages);
    }

    [Fact]
    public async Task A_second_cheque_with_the_same_bank_and_number_is_saved_with_a_warning()
    {
        await _useCases.ReceiveAsync(Receive("a"), CancellationToken.None);
        var duplicate = await _useCases.ReceiveAsync(Receive("b"), CancellationToken.None);
        Assert.True(duplicate.IsSuccess);
        Assert.Contains(InstrumentUseCases.PossibleDuplicateWarning, duplicate.Warnings);
    }

    [Fact]
    public async Task Operations_in_a_closed_fiscal_year_are_refused()
    {
        _policies.CloseYear(1404);
        var result = await _useCases.ReceiveAsync(Receive("old", date: "1404/12/01"), CancellationToken.None);
        Assert.Equal("FiscalPeriod.Closed", result.ErrorCode);
        Assert.Empty(_db.OutboxMessages);
    }

    [Fact]
    public async Task A_rule_violation_returns_its_code_and_saves_nothing()
    {
        var received = await _useCases.ReceiveAsync(Receive("r"), CancellationToken.None);
        var result = await _useCases.ChangeStateAsync(
            new ChangeInstrumentStateCommand("c1", received.Value, InstrumentAction.RecordCleared, "1405/08/02"), CancellationToken.None);
        Assert.Equal("Instrument.InvalidTransition", result.ErrorCode);
        Assert.Equal("InCashDesk", (await _useCases.GetAsync(received.Value, CancellationToken.None))!.State);
        Assert.Single(_db.OutboxMessages);
    }

    [Fact]
    public async Task Collection_path_through_the_use_cases()
    {
        var id = (await _useCases.ReceiveAsync(Receive("r"), CancellationToken.None)).Value;
        Assert.True((await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.DepositForCollection, "1405/08/03", Destination: "حساب جاری ملت"), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.RecordCleared, "1405/09/02"), CancellationToken.None)).IsSuccess);
        var view = await _useCases.GetAsync(id, CancellationToken.None);
        Assert.Equal("Cleared", view!.State);
        Assert.Equal(3, view.History.Count);
        Assert.Equal(3L, view.Version);
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
    }

    [Fact]
    public async Task Parallel_issues_from_one_book_never_get_the_same_number()
    {
        var book = await RegisterBook(1, 40);
        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => Issue(book, Approved(), null)));
        Assert.True(results.All(r => r.IsSuccess));
        Assert.Equal(40, results.Select(r => r.Value!.SheetNumber).Distinct().Count());
    }

    [Fact]
    public async Task An_unapproved_payment_cannot_be_issued()
    {
        var book = await RegisterBook(1, 5);
        var result = await Issue(book, Guid.NewGuid(), null);
        Assert.Equal("Payment.NotApproved", result.ErrorCode);
    }

    [Fact]
    public async Task An_ordinary_cheque_that_would_overdraw_the_account_is_refused_and_the_sheet_stays_free()
    {
        var book = await RegisterBook(1, 5);
        var payment = Approved();
        _policies.SetBalance(_account, 1_000);
        var refused = await Issue(book, payment, null);
        Assert.Equal("Payment.InsufficientBalance", refused.ErrorCode);

        _policies.SetBalance(_account, 1_000_000_000);
        var next = await Issue(book, Approved(), null);
        Assert.Equal("1", next.Value!.SheetNumber);
    }

    [Fact]
    public async Task A_post_dated_cheque_is_not_blocked_by_the_current_balance()
    {
        var book = await RegisterBook(1, 5);
        var payment = Approved();
        _policies.SetBalance(_account, 0);
        var result = await Issue(book, payment, null, InstrumentKind.PostDatedCheque, due: "1405/10/01");
        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    [Fact]
    public async Task A_typed_sheet_number_outside_the_book_is_refused()
    {
        var book = await RegisterBook(1, 5);
        var result = await Issue(book, Approved(), 99);
        Assert.Equal("ChequeBook.SheetOutOfRange", result.ErrorCode);
    }

    private static string Key() => Guid.NewGuid().ToString();

    private Guid Approved()
    {
        var payment = Guid.NewGuid();
        _policies.Approve(payment);
        _policies.SetBalance(_account, 10_000_000_000);
        return payment;
    }

    private async Task<Guid> RegisterBook(long first, long last) =>
        (await _books.RegisterAsync(new RegisterChequeBookCommand(Key(), _account, "A", first, last), CancellationToken.None)).Value;

    private Task<Result<IssuedChequeView>> Issue(Guid book, Guid payment, long? sheet, InstrumentKind kind = InstrumentKind.OrdinaryCheque, string due = "1405/08/10") =>
        _useCases.IssueChequeAsync(
            new IssueChequeCommand(Key(), payment, _account, book, sheet, kind, new MoneyInput(5_000_000), "1405/08/10", due, Guid.NewGuid(), "تامین کننده", "خرید"),
            CancellationToken.None);

    private sealed class FixedUser(string id) : ICurrentUser
    {
        public string UserId { get; } = id;
    }
}
