using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Application.Receipts;
using Treasury.Cashflow.Application.Vouchers;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Vouchers;
using Treasury.Cashflow.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Cashflow.Application.Tests;

/// <summary>
/// The latest status of a received cheque (owner, 18 Nov 2026): «ثبت صیادی شد» and «حقوقی» are recorded like the other steps, the
/// receipts list carries the latest state of each receipt's cheques with the «سررسید چک» mark, and the return to the payer has its voucher.
/// </summary>
public sealed class ChequeStatusUseCaseTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private readonly InMemoryVoucherStore _store = new();
    private readonly FakeReceiptReference _reference = new();
    private readonly Clock _clock = new("1405/08/27");
    private readonly ReceiptUseCases _receipts;
    private readonly InstrumentUseCases _instruments;
    private readonly VoucherUseCases _vouchers;

    public ChequeStatusUseCaseTests()
    {
        var policies = new DevelopmentPolicies().OpenYear(1405);
        var bank = new Bank();
        _receipts = new ReceiptUseCases(
            new InMemoryUnitOfWork(_db), new InMemoryReceiptRepository(_db), new InMemoryInstrumentRepository(_db), new FakeReceiptAccounts(), bank, _reference,
            policies, new InMemoryIdempotencyStore(_db), new Treasurer(), new InMemoryVoucherTemplateRepository(_store), new InMemoryConfirmedVoucherRepository(_store),
            new FakePaymentRequests(), _clock);
        _instruments = new InstrumentUseCases(new InMemoryUnitOfWork(_db), new InMemoryInstrumentRepository(_db), new InMemoryChequeBookRepository(_db),
            new InMemoryIdempotencyStore(_db), policies, policies, policies, new Treasurer(), bank, _clock);
        _vouchers = new VoucherUseCases(new InMemoryUnitOfWork(_db), new InMemoryVoucherTemplateRepository(_store), new InMemoryConfirmedVoucherRepository(_store),
            new InMemoryReceiptRepository(_db), new InMemoryInstrumentRepository(_db), new FakeReceiptAccounts(), bank, _reference,
            new InMemoryIdempotencyStore(_db), new Treasurer(), _clock);
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static ReceivedInstrumentInput Line(string number, string due) =>
        new(InstrumentKind.OrdinaryCheque, number, due, new MoneyInput(10_000_000), FakeReceiptReference.Mellat, SayadId: "1234567890123456");

    /// <summary>A receipt of 1405/08/23 with one cheque per due date; returns the receipt and its cheques in order.</summary>
    private async Task<(Guid Receipt, IReadOnlyList<Guid> Cheques)> Receipt(params string[] dueDates)
    {
        var r = await _receipts.RecordInstrumentsAsync(new(Key(), new ReceiptHeaderInput("1405/08/23", "200015", FakeReceiptReference.SalesGroup, "فروش محصولات"),
            [.. dueDates.Select((due, i) => Line($"44556{i}", due))]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return (r.Value, (await _receipts.GetAsync(r.Value, CancellationToken.None))!.Instruments!.Select(i => i.Id).ToList());
    }

    private async Task Step(Guid cheque, InstrumentAction action, string date, string? destination = null, string? note = null, string? reference = null)
    {
        var r = await _instruments.ChangeStateAsync(new(Key(), cheque, action, date, destination, note, reference), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
    }

    [Fact]
    public async Task A_cheque_is_registered_in_Sayad_then_deposited_bounced_and_handed_to_the_lawyer()
    {
        var (_, cheques) = await Receipt("1405/08/25");
        await Step(cheques[0], InstrumentAction.RegisterSayad, "1405/08/24", reference: "SY-1");
        var view = (await _instruments.GetAsync(cheques[0], CancellationToken.None))!;
        Assert.Equal(("SayadRegistered", "SY-1"), (view.State, view.History[^1].Details!.SayadTracking));
        Assert.Contains("DepositForCollection", view.AllowedActions);
        Assert.DoesNotContain("RegisterSayad", view.AllowedActions);

        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), cheques[0], Bank.Id, "1405/08/24", "SLIP-1"), CancellationToken.None)).IsSuccess);
        await Step(cheques[0], InstrumentAction.RecordBounced, "1405/08/26", note: "کسری موجودی", reference: "BN-9");
        Assert.Equal("Instrument.InvalidTransition",
            (await _instruments.ChangeStateAsync(new(Key(), cheques[0], InstrumentAction.RegisterSayad, "1405/08/27"), CancellationToken.None)).ErrorCode);
        await Step(cheques[0], InstrumentAction.SendToLegal, "1405/08/27", destination: "دفتر وکالت الف", reference: "پرونده 3");
        view = (await _instruments.GetAsync(cheques[0], CancellationToken.None))!;
        Assert.Equal(("InLegal", "دفتر وکالت الف", "پرونده 3"), (view.State, view.Destination, view.History[^1].Reference));
        Assert.Equal(["Replace", "Cancel"], view.AllowedActions);

        // Registering in Sayad and handing to the lawyer have no voucher; deposit and bounce keep theirs.
        Assert.Equal([3, 4], (await _vouchers.ChequeVouchersAsync(cheques[0], CancellationToken.None)).Select(v => v.Step));
        Assert.Equal("Voucher.NoVoucherForStep", (await _vouchers.ConfirmChequeVoucherAsync(Key(), cheques[0], 5, CancellationToken.None)).ErrorCode);
        Assert.Single(await _instruments.ListReceivedAsync(InstrumentState.InLegal, CancellationToken.None));
    }

    [Fact]
    public async Task The_receipts_list_shows_the_latest_status_of_each_cheque_and_marks_those_that_reached_their_due_date()
    {
        // Today is 1405/08/27: the cheque due 08/25 has reached its due date, the one due 10/01 has not.
        var (receipt, cheques) = await Receipt("1405/08/25", "1405/10/01", "1405/08/26");
        await Step(cheques[2], InstrumentAction.ReturnToIssuer, "1405/08/24");
        var cash = await _receipts.RecordCashAsync(new(Key(), new ReceiptHeaderInput("1405/08/23", "200015", FakeReceiptReference.SalesGroup, "فروش"), FakeReceiptAccounts.Desk, 1_000),
            CancellationToken.None);

        var list = await _receipts.ListAsync(1405, null, CancellationToken.None);
        var row = list.Single(r => r.Id == receipt);
        Assert.Equal([("InCashDesk", true), ("InCashDesk", false), ("ReturnedToIssuer", false)], row.ChequeStatuses!.Select(s => (s.State, s.ReachedDueDate)));
        Assert.Null(row.Instruments);
        Assert.Null(list.Single(r => r.Id == cash.Value).ChequeStatuses);

        // The same on the opened receipt and on the cheque itself; at the bank the mark stays until the bank answers.
        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), cheques[0], Bank.Id, "1405/08/24"), CancellationToken.None)).IsSuccess);
        var opened = (await _receipts.GetAsync(receipt, CancellationToken.None))!;
        Assert.Equal([("DepositedForCollection", true), ("InCashDesk", false), ("ReturnedToIssuer", false)], opened.ChequeStatuses!.Select(s => (s.State, s.ReachedDueDate)));
        Assert.Equal([true, false, false], opened.Instruments!.Select(i => i.ReachedDueDate));
        Assert.True((await _instruments.ListReceivedAsync(InstrumentState.DepositedForCollection, CancellationToken.None)).Single().ReachedDueDate);

        await Step(cheques[0], InstrumentAction.RecordCleared, "1405/08/26");
        Assert.Equal(("Cleared", false), (await _receipts.ListAsync(1405, ReceiptKind.Instruments, CancellationToken.None)).Single().ChequeStatuses!.Select(s => (s.State, s.ReachedDueDate)).First());
    }

    [Fact]
    public async Task Today_is_the_date_in_Iran()
    {
        // 21:30 UTC on 27 Aban is already 01:00 on 28 Aban in Iran (UTC+03:30).
        var late = new Clock("1405/08/27", new TimeSpan(21, 30, 0));
        Assert.Equal("1405/08/28", IranClock.Today(late).ToJalaliString());
        Assert.Equal("1405/08/27", IranClock.Today(new Clock("1405/08/27", new TimeSpan(20, 29, 0))).ToJalaliString());

        _clock.Now = late.Now;
        var (receipt, _) = await Receipt("1405/08/28");
        Assert.True((await _receipts.GetAsync(receipt, CancellationToken.None))!.ChequeStatuses![0].ReachedDueDate);
    }

    [Fact]
    public async Task A_cheque_returned_to_the_payer_gets_a_voucher_that_takes_the_receipt_back()
    {
        foreach (var (a, code) in new[] { (FixedAccount.ChequesInCashDesk, "1201"), (FixedAccount.ChequesInCollection, "1202") })
        {
            Assert.True((await _vouchers.SetTemplateAccountAsync(Key(), a, code, CancellationToken.None)).IsSuccess);
        }

        var (_, cheques) = await Receipt("1405/10/01");
        await Step(cheques[0], InstrumentAction.ReturnToIssuer, "1405/08/25", note: "به درخواست مشتری");
        var v = (await _vouchers.ChequeVouchersAsync(cheques[0], CancellationToken.None)).Single();
        Assert.Equal((2, "عودت چک نزد صندوق", true), (v.Step, v.Operation, v.IsComplete));

        // Debit: the account the receipt credited (the purpose group's, 4101) with the payer; credit: «اسناد نزد صندوق».
        Assert.Equal([(true, "4101", "200015"), (false, "1201", "200015")], v.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.True((await _vouchers.ConfirmChequeVoucherAsync(Key(), cheques[0], 2, CancellationToken.None)).IsSuccess);
    }

    private sealed class Clock(string jalali, TimeSpan? time = null) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(FiscalDate.ParseJalali(jalali).Value.ToDateTime(TimeOnly.FromTimeSpan(time ?? new TimeSpan(8, 0, 0))), TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Bank : IBankAccountLookup
    {
        public static readonly Guid Id = Guid.NewGuid();

        public Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<BankAccountFact?>(new(id, "0101-9", true, true, "IRR", "1109", "600009"));

        public Task MarkInUseAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Treasurer : ICurrentUser
    {
        public string UserId => "treasurer";
    }
}
