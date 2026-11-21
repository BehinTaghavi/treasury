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
    private readonly FakeBankAccounts _bankAccounts = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 11, 15, 7, 5, 0, TimeSpan.Zero));

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
            new FixedUser("clerk-1"),
            _bankAccounts,
            _clock);
        _books = new ChequeBookUseCases(new InMemoryUnitOfWork(_db), new InMemoryChequeBookRepository(_db), new FakeBankAccounts(), new InMemoryIdempotencyStore(_db), new InMemoryChequeFormatRepository(_db));
    }

    [Fact]
    public async Task A_cheque_in_the_cash_desk_is_deposited_at_a_company_account_report_4_item_9()
    {
        var id = (await _useCases.ReceiveAsync(Receive(Guid.NewGuid().ToString(), "770001"), CancellationToken.None)).Value;
        Assert.Contains(await _useCases.ListReceivedAsync(InstrumentState.InCashDesk, CancellationToken.None), i => i.Id == id);

        // Rule N3 (owner, 18 Nov 2026): not before «ثبت صیادی».
        Assert.Equal("Instrument.SayadRegistrationRequired",
            (await _useCases.DepositForCollectionAsync(new(Guid.NewGuid().ToString(), id, Guid.NewGuid(), "1405/08/05"), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangeStateAsync(new(Guid.NewGuid().ToString(), id, InstrumentAction.RegisterSayad, "1405/08/02"), CancellationToken.None)).IsSuccess);
        Assert.Equal("Instrument.CollectionAccountNotFound",
            (await _useCases.DepositForCollectionAsync(new(Guid.NewGuid().ToString(), id, FakeBankAccounts.Unknown, "1405/08/05"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Instrument.CollectionAccountInactive",
            (await _useCases.DepositForCollectionAsync(new(Guid.NewGuid().ToString(), id, FakeBankAccounts.Closed, "1405/08/05"), CancellationToken.None)).ErrorCode);
        var bank = Guid.NewGuid();
        Assert.True((await _useCases.DepositForCollectionAsync(new(Guid.NewGuid().ToString(), id, bank, "1405/08/05", "SLIP-1"), CancellationToken.None)).IsSuccess);
        var v = (await _useCases.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(("DepositedForCollection", (Guid?)bank), (v.State, v.CollectionAccountId));
        Assert.Contains(bank, _bankAccounts.InUse);
        Assert.DoesNotContain(await _useCases.ListReceivedAsync(InstrumentState.InCashDesk, CancellationToken.None), i => i.Id == id);
        Assert.Contains(await _useCases.ListReceivedAsync(InstrumentState.DepositedForCollection, CancellationToken.None), i => i.Id == id);
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
        Assert.Contains("RegisterSayad", view.AllowedActions);
        Assert.DoesNotContain("DepositForCollection", view.AllowedActions); // rule N3: only after «ثبت صیادی»
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
        Assert.True((await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.RegisterSayad, "1405/08/02"), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.DepositForCollection, "1405/08/03", Destination: "حساب جاری ملت"), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.RecordCleared, "1405/09/02"), CancellationToken.None)).IsSuccess);
        var view = await _useCases.GetAsync(id, CancellationToken.None);
        Assert.Equal("Cleared", view!.State);
        Assert.Equal(4, view.History.Count);
        Assert.Equal(4L, view.Version);
    }

    [Fact]
    public async Task Issuing_reserves_sheets_in_order_and_records_the_cheque()
    {
        var book = await RegisterBook(1001, 1003);
        var first = await Issue(book, Approved(), null);
        var second = await Issue(book, Approved(), null);
        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.Equal("101001", first.Value!.SheetNumber);
        Assert.Equal("101002", second.Value!.SheetNumber);
        Assert.Equal(1L, second.Value.RemainingSheets);
        var cheque = await _useCases.GetAsync(first.Value.InstrumentId, CancellationToken.None);
        Assert.Equal("Issued", cheque!.State);
    }

    [Fact]
    public async Task A_cheque_from_a_book_whose_serial_starts_with_0_keeps_the_zero_owner_12_nov()
    {
        var registered = await _books.RegisterAsync(
            new RegisterChequeBookCommand(Key(), _account, "1234", 56_701, 56_725, null, "12345678", "1", SerialDigits: 6), CancellationToken.None);
        Assert.True(registered.IsSuccess, registered.ErrorCode);
        Assert.Equal("056701", (await Issue(registered.Value, Approved(), null)).Value!.SheetNumber);
        Assert.Equal("056710", (await Issue(registered.Value, Approved(), 56_710)).Value!.SheetNumber);
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
        Assert.Equal("100001", next.Value!.SheetNumber);
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
        var result = await Issue(book, Approved(), Base + 99);
        Assert.Equal("ChequeBook.SheetOutOfRange", result.ErrorCode);
    }

    [Fact]
    public async Task An_issued_cheque_is_not_handed_to_its_beneficiary_before_sabt_sayadi_is_ticked_owner_15_Nov_2026()
    {
        var issued = await Issue(await RegisterBook(1, 5), Approved(), null);
        Assert.True(issued.IsSuccess, issued.ErrorCode);
        var id = issued.Value!.InstrumentId;
        var before = (await _useCases.GetAsync(id, CancellationToken.None))!;
        Assert.Equal((true, true, (DateTimeOffset?)null), (before.SayadRegistrationRequired, before.CanChangeSayadRegistration, before.SayadRegisteredAt));
        Assert.DoesNotContain("Deliver", before.AllowedActions);
        Assert.Equal("Instrument.SayadNotRegistered",
            (await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.Deliver, "1405/08/11", Note: "آقای رضایی"), CancellationToken.None)).ErrorCode);

        // The tick keeps the computer's date and time and the user; it is not a step of the cheque.
        Assert.True((await _useCases.SetSayadRegistrationAsync(new(Key(), id, true), CancellationToken.None)).IsSuccess);
        var ticked = (await _useCases.GetAsync(id, CancellationToken.None))!;
        Assert.Equal((_clock.GetUtcNow(), "clerk-1"), (ticked.SayadRegisteredAt!.Value, ticked.SayadRegisteredBy));
        Assert.Contains("Deliver", ticked.AllowedActions);
        Assert.Equal(before.History.Count, ticked.History.Count);
        Assert.Contains(_db.OutboxMessages, m => m.Type == nameof(InstrumentSayadRegistrationChanged) && m.Payload.Contains("\"Registered\":true", StringComparison.Ordinal));
        Assert.Equal("Instrument.SayadAlreadyRegistered", (await _useCases.SetSayadRegistrationAsync(new(Key(), id, true), CancellationToken.None)).ErrorCode);

        Assert.True((await _useCases.ChangeStateAsync(new(Key(), id, InstrumentAction.Deliver, "1405/08/11", Note: "آقای رضایی"), CancellationToken.None)).IsSuccess);
        var delivered = (await _useCases.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(("Delivered", (DateTimeOffset?)_clock.GetUtcNow(), false), (delivered.State, delivered.SayadRegisteredAt, delivered.CanChangeSayadRegistration));
        Assert.Equal("Instrument.SayadRegistrationClosed", (await _useCases.SetSayadRegistrationAsync(new(Key(), id, false), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_tick_set_by_mistake_is_removed_while_the_issued_cheque_is_not_delivered()
    {
        var id = (await Issue(await RegisterBook(1, 5), Approved(), null)).Value!.InstrumentId;
        Assert.Equal("Instrument.SayadNotRegistered", (await _useCases.SetSayadRegistrationAsync(new(Key(), id, false), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.SetSayadRegistrationAsync(new(Key(), id, true), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.SetSayadRegistrationAsync(new(Key(), id, false), CancellationToken.None)).IsSuccess);
        var view = (await _useCases.GetAsync(id, CancellationToken.None))!;
        Assert.Null(view.SayadRegisteredAt);
        Assert.Null(view.SayadRegisteredBy);
        Assert.DoesNotContain("Deliver", view.AllowedActions);
        Assert.Equal("Instrument.NotFound", (await _useCases.SetSayadRegistrationAsync(new(Key(), Guid.NewGuid(), true), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_received_cheque_is_registered_in_sayad_by_its_step_and_refuses_the_tick_N3()
    {
        // Owner, 18 Nov 2026 (rule N3): the step «ثبت صیادی» and its status are for received cheques; the tick of 15 Nov is for issued ones.
        var id = (await _useCases.ReceiveAsync(Receive(Key(), "880001"), CancellationToken.None)).Value;
        var view = (await _useCases.GetAsync(id, CancellationToken.None))!;
        Assert.Equal((false, false), (view.SayadRegistrationRequired, view.CanChangeSayadRegistration));
        Assert.Contains("RegisterSayad", view.AllowedActions);
        Assert.Equal("Instrument.SayadRegistrationIsAStep", (await _useCases.SetSayadRegistrationAsync(new(Key(), id, true), CancellationToken.None)).ErrorCode);
        Assert.Equal("Instrument.SayadRegistrationRequired",
            (await _useCases.DepositForCollectionAsync(new(Key(), id, Guid.NewGuid(), "1405/08/05", "SLIP-1"), CancellationToken.None)).ErrorCode);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
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
        (await _books.RegisterAsync(new RegisterChequeBookCommand(Key(), _account, "1234", Base + first, Base + last, null, "12345678", "1", PartlyUsed: true), CancellationToken.None)).Value;

    /// <summary>Serials have 6 to 8 digits (owner, 12 Nov 2026): the tests count sheets from here.</summary>
    private const long Base = 100_000;

    private Task<Result<IssuedChequeView>> Issue(Guid book, Guid payment, long? sheet, InstrumentKind kind = InstrumentKind.OrdinaryCheque, string due = "1405/08/10") =>
        _useCases.IssueChequeAsync(
            new IssueChequeCommand(Key(), payment, _account, book, sheet, kind, new MoneyInput(5_000_000), "1405/08/10", due, Guid.NewGuid(), "تامین کننده", "خرید"),
            CancellationToken.None);

    private sealed class FixedUser(string id) : ICurrentUser
    {
        public string UserId { get; } = id;
    }
}
