using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Application.Receipts;
using Treasury.Cashflow.Application.Vouchers;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Cashflow.Application.Tests;

/// <summary>
/// Correction («ویرایش») of a saved receipt (owner's report no. 6, items 2 and 6): while its voucher is not confirmed, and for a
/// cheque receipt while its cheques are still «نزد صندوق» with no step recorded on them.
/// </summary>
public sealed class ReceiptCorrectionUseCaseTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private readonly InMemoryVoucherStore _store = new();
    private readonly FakeReceiptReference _reference = new();
    private readonly FakeBankAccounts _bank = new();
    private readonly ReceiptUseCases _receipts;
    private readonly InstrumentUseCases _instruments;
    private readonly VoucherUseCases _vouchers;

    public ReceiptCorrectionUseCaseTests()
    {
        var policies = new DevelopmentPolicies().OpenYear(1405).OpenYear(1406);
        _receipts = new ReceiptUseCases(
            new InMemoryUnitOfWork(_db), new InMemoryReceiptRepository(_db), new InMemoryInstrumentRepository(_db), new FakeReceiptAccounts(), _bank, _reference,
            policies, new InMemoryIdempotencyStore(_db), new Clerk(), new InMemoryVoucherTemplateRepository(_store), new InMemoryConfirmedVoucherRepository(_store),
            new FakePaymentRequests());
        _instruments = new InstrumentUseCases(new InMemoryUnitOfWork(_db), new InMemoryInstrumentRepository(_db), new InMemoryChequeBookRepository(_db),
            new InMemoryIdempotencyStore(_db), policies, policies, policies, new Clerk(), _bank);
        _vouchers = new VoucherUseCases(new InMemoryUnitOfWork(_db), new InMemoryVoucherTemplateRepository(_store), new InMemoryConfirmedVoucherRepository(_store),
            new InMemoryReceiptRepository(_db), new InMemoryInstrumentRepository(_db), new FakeReceiptAccounts(), _bank, _reference,
            new InMemoryIdempotencyStore(_db), new Clerk(), TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static ReceiptHeaderInput Header(string payer = "200015", string date = "1405/08/23", string purpose = "فروش محصولات") =>
        new(date, payer, FakeReceiptReference.SalesGroup, purpose);

    private static ReceivedInstrumentInput Cheque(string number, string due = "1405/10/01", decimal amount = 10_000_000, Guid? id = null) =>
        new(InstrumentKind.OrdinaryCheque, number, due, new MoneyInput(amount), FakeReceiptReference.Mellat, SayadId: "1234567890123456", Id: id);

    private async Task<ReceiptView> Receipt(Guid id) => (await _receipts.GetAsync(id, CancellationToken.None))!;

    [Fact]
    public async Task A_cash_receipt_is_corrected_and_keeps_its_number()
    {
        var id = (await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 5_000), CancellationToken.None)).Value;
        Assert.Null((await Receipt(id)).NotCorrectable);

        var r = await _receipts.CorrectCashAsync(new(Key(), id, 1, Header("200016", "1405/08/25", "فروش ضایعات"), FakeReceiptAccounts.Desk, 7_500), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = await Receipt(id);
        Assert.Equal((1, "1405/08/25", "200016", "مشتری دیگر", "فروش ضایعات", 7_500m, 7_500m, 2L),
            (v.Number, v.Date, v.PayerDetailCode, v.PayerName, v.Purpose, v.Amount, v.TotalRial, v.Version));
        Assert.Contains(_db.OutboxMessages, m => m.Type.Contains("ReceiptCorrected", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_correction_needs_the_version_the_user_saw_and_the_rules_of_recording()
    {
        var id = (await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 5_000), CancellationToken.None)).Value;
        async Task<string?> Code(ReceiptHeaderInput header, Guid? desk = null, decimal amount = 5_000, long version = 1) =>
            (await _receipts.CorrectCashAsync(new(Key(), id, version, header, desk ?? FakeReceiptAccounts.Desk, amount), CancellationToken.None)).ErrorCode;

        Assert.Equal("Concurrency.Conflict", await Code(Header(), version: 7));
        Assert.Equal("Receipt.PayerInactive", await Code(Header("200099")));
        Assert.Equal("Receipt.AccountInactive", await Code(Header(), FakeReceiptAccounts.ClosedDesk));
        Assert.Equal("Money.NotPositive", await Code(Header(), amount: 0));
        Assert.Equal("Receipt.PurposeRequired", await Code(Header(purpose: " ")));
        // Its number belongs to its fiscal year.
        Assert.Equal("Receipt.CorrectionOtherYear", await Code(Header(date: "1406/01/10")));
        Assert.Equal("Receipt.NotFound", (await _receipts.CorrectCashAsync(new(Key(), Guid.NewGuid(), 1, Header(), FakeReceiptAccounts.Desk, 1), CancellationToken.None)).ErrorCode);
        // A receipt keeps its kind.
        Assert.Equal("Receipt.CorrectionKindDiffers",
            (await _receipts.CorrectBankDepositAsync(new(Key(), id, 1, Header(), FakeReceiptAccounts.RialBank, DepositMethod.Pos, null, 5_000), CancellationToken.None)).ErrorCode);
        Assert.Equal(1, (await Receipt(id)).Version);
    }

    [Fact]
    public async Task A_bank_deposit_is_corrected_with_the_rules_of_its_kind_of_deposit()
    {
        var id = (await _receipts.RecordBankDepositAsync(new(Key(), Header(), FakeReceiptAccounts.RialBank, DepositMethod.Pos, "T-1", 5_000), CancellationToken.None)).Value;
        Assert.Equal("Receipt.ShebaRequired",
            (await _receipts.CorrectBankDepositAsync(new(Key(), id, 1, Header(), FakeReceiptAccounts.RialBank, DepositMethod.Paya, "T-2", 6_000), CancellationToken.None)).ErrorCode);
        var r = await _receipts.CorrectBankDepositAsync(new(Key(), id, 1, Header(), FakeReceiptAccounts.RialBank, DepositMethod.DepositSlip, "F-9", 6_000), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = await Receipt(id);
        Assert.Equal(("DepositSlip", "F-9", 6_000m, 1), (v.Method, v.TrackingNumber, v.Amount, v.Number));
    }

    [Fact]
    public async Task A_receipt_with_a_confirmed_voucher_or_a_cancelled_receipt_is_no_longer_corrected()
    {
        foreach (var a in new[] { (Domain.Vouchers.FixedAccount.ChequesInCashDesk, "1201") })
        {
            Assert.True((await _vouchers.SetTemplateAccountAsync(Key(), a.Item1, a.Item2, CancellationToken.None)).IsSuccess);
        }

        var confirmed = (await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 5_000), CancellationToken.None)).Value;
        var done = await _vouchers.ConfirmReceiptVoucherAsync(Key(), confirmed, CancellationToken.None);
        Assert.True(done.IsSuccess, done.ErrorCode);
        Assert.Equal("Receipt.VoucherConfirmed", (await Receipt(confirmed)).NotCorrectable);
        Assert.Equal("Receipt.VoucherConfirmed",
            (await _receipts.CorrectCashAsync(new(Key(), confirmed, 1, Header(), FakeReceiptAccounts.Desk, 9_000), CancellationToken.None)).ErrorCode);
        Assert.Equal(5_000m, (await Receipt(confirmed)).Amount);

        var cancelled = (await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 5_000), CancellationToken.None)).Value;
        Assert.True((await _receipts.CancelAsync(new(Key(), cancelled, "1405/08/24", "اشتباه", 1), CancellationToken.None)).IsSuccess);
        Assert.Equal("Receipt.AlreadyCancelled", (await Receipt(cancelled)).NotCorrectable);
        Assert.Equal("Receipt.AlreadyCancelled",
            (await _receipts.CorrectCashAsync(new(Key(), cancelled, 2, Header(), FakeReceiptAccounts.Desk, 9_000), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_cheques_of_a_receipt_are_corrected_added_and_removed_in_one_go()
    {
        var id = (await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("000123"), Cheque("000124"), Cheque("000125")]), CancellationToken.None)).Value;
        var before = (await Receipt(id)).Instruments!;
        Assert.Equal(["000123", "000124", "000125"], before.Select(i => i.Number!)); // in the order of the form
        var keep = before[0];
        var change = before[1];

        var r = await _receipts.CorrectInstrumentsAsync(new(Key(), id, 1, Header(date: "1405/08/24", purpose: "پیش دریافت"),
        [
            Cheque("000123", id: keep.Id),
            Cheque("000199", "1405/12/01", 25_000_000, change.Id),   // number, due date and amount corrected
            Cheque("000300"),                                       // a cheque added
        ]), CancellationToken.None);                                // 000125 left out: removed
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Empty(r.Warnings); // its own cheques are not duplicates of themselves

        var v = await Receipt(id);
        Assert.Equal((1, 3, 45_000_000m, "1405/08/24", "پیش دریافت"), (v.Number, v.InstrumentCount, v.TotalRial, v.Date, v.Purpose));
        var lines = v.Instruments!;
        Assert.Equal(["000123", "000199", "000300"], lines.Select(i => i.Number!));
        Assert.Equal([keep.Id, change.Id], lines.Take(2).Select(i => i.Id));
        var corrected = lines[1];
        Assert.Equal((25_000_000m, "1405/12/01", "1405/08/24", "پیش دریافت", "InCashDesk"), (corrected.Amount, corrected.DueDate, corrected.IssueDate, corrected.Purpose, corrected.State));
        Assert.Equal("1405/08/24", Assert.Single(corrected.History).Date); // the receipt step follows the corrected date
        Assert.Null(await _instruments.GetAsync(before[2].Id, CancellationToken.None));
        Assert.DoesNotContain(await _instruments.ListReceivedAsync(InstrumentState.InCashDesk, CancellationToken.None), i => i.Number == "000125");
        Assert.Contains(_db.OutboxMessages, m => m.Type.Contains("ReceivedInstrumentRemoved", StringComparison.Ordinal));
        Assert.Contains(_db.OutboxMessages, m => m.Type.Contains("ReceivedInstrumentCorrected", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_cheque_that_left_the_cash_desk_closes_the_correction_of_its_receipt()
    {
        var id = (await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("000123"), Cheque("000124")]), CancellationToken.None)).Value;
        var lines = (await Receipt(id)).Instruments!;
        var sayad = await _instruments.ChangeStateAsync(new(Key(), lines[1].Id, InstrumentAction.RegisterSayad, "1405/08/24"), CancellationToken.None);
        Assert.True(sayad.IsSuccess, sayad.ErrorCode);

        Assert.Equal("Instrument.CorrectionAfterStep", (await Receipt(id)).NotCorrectable);
        var r = await _receipts.CorrectInstrumentsAsync(new(Key(), id, 1, Header(), [Cheque("000123", amount: 1, id: lines[0].Id), Cheque("000124", id: lines[1].Id)]), CancellationToken.None);
        Assert.Equal("Instrument.CorrectionAfterStep", r.ErrorCode);
        // Leaving the registered cheque out does not remove it either, and nothing of the failed command is saved.
        Assert.Equal("Instrument.CorrectionAfterStep",
            (await _receipts.CorrectInstrumentsAsync(new(Key(), id, 1, Header(), [Cheque("000123", id: lines[0].Id)]), CancellationToken.None)).ErrorCode);
        Assert.Equal(10_000_000m, (await Receipt(id)).Instruments![0].Amount);
    }

    [Fact]
    public async Task A_line_must_be_a_cheque_of_this_receipt_and_the_same_cheque_twice_is_refused()
    {
        var id = (await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("000123")]), CancellationToken.None)).Value;
        var other = (await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("000900")]), CancellationToken.None)).Value;
        var foreign = (await Receipt(other)).Instruments![0].Id;
        var own = (await Receipt(id)).Instruments![0].Id;
        Assert.Equal("Receipt.InstrumentNotOnReceipt",
            (await _receipts.CorrectInstrumentsAsync(new(Key(), id, 1, Header(), [Cheque("000123", id: foreign)]), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.SameInstrumentTwice",
            (await _receipts.CorrectInstrumentsAsync(new(Key(), id, 1, Header(), [Cheque("000123", id: own), Cheque("000123")]), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.NoInstruments", (await _receipts.CorrectInstrumentsAsync(new(Key(), id, 1, Header(), []), CancellationToken.None)).ErrorCode);
        // A cheque numbered like one of another receipt is saved with the usual warning.
        var r = await _receipts.CorrectInstrumentsAsync(new(Key(), id, 1, Header(), [Cheque("000900", id: own)]), CancellationToken.None);
        Assert.Equal([InstrumentUseCases.PossibleDuplicateWarning], r.Warnings);
    }

    [Fact]
    public async Task A_receipt_of_notes_is_corrected_with_its_terms()
    {
        static NoteLineInput Note(string number, decimal amount = 500_000_000, Guid? id = null) =>
            new(number, "1405/08/20", "1405/11/30", amount, new NoteChecks(true, true, true, true, true), id);
        var terms = new NoteTermsInput(NoteReason.Debt, NoteForm.Paper, NoteMaturity.Fixed);
        var recorded = await _receipts.RecordNotesAsync(new(Key(), Header(date: "1405/08/25"), terms, [Note("N-1"), Note("N-2")]), CancellationToken.None);
        Assert.True(recorded.IsSuccess, recorded.ErrorCode);
        var lines = (await Receipt(recorded.Value)).Instruments!;

        var r = await _receipts.CorrectNotesAsync(new(Key(), recorded.Value, 1, Header(date: "1405/08/25"), terms with { CustodyPlace = "صندوق شرکت" },
            [Note("N-1", 300_000_000, lines[0].Id)], [new(null, DetailCode: "200016")]), CancellationToken.None); // a guarantor of the list (answer to Q54)
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = await Receipt(recorded.Value);
        Assert.Equal((1, 1, 300_000_000m, "صندوق شرکت"), (v.Number, v.InstrumentCount, v.TotalRial, v.NoteTerms!.CustodyPlace));
        Assert.Equal("مشتری دیگر", Assert.Single(Assert.Single(v.Instruments!).Details!.GuarantorList).Name);
    }

    private sealed class Clerk : ICurrentUser
    {
        public string UserId => "clerk-1";
    }
}
