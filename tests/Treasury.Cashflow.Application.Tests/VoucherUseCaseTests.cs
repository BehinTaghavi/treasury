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

/// <summary>Vouchers of receipts and cheque steps, the template page and the treasurer's confirmation (owner's report no. 4, item 8).</summary>
public sealed class VoucherUseCaseTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private readonly InMemoryVoucherStore _store = new();
    private readonly FakeReceiptReference _reference = new();
    private readonly LedgerBank _bank = new();
    private readonly FakePaymentRequests _requests = new();
    private readonly ReceiptUseCases _receipts;
    private readonly InstrumentUseCases _instruments;
    private readonly VoucherUseCases _vouchers;

    public VoucherUseCaseTests()
    {
        var policies = new DevelopmentPolicies().OpenYear(1405);
        _receipts = new ReceiptUseCases(
            new InMemoryUnitOfWork(_db), new InMemoryReceiptRepository(_db), new InMemoryInstrumentRepository(_db), new FakeReceiptAccounts(), _bank, _reference,
            policies, new InMemoryIdempotencyStore(_db), new Treasurer(), new InMemoryVoucherTemplateRepository(_store), new InMemoryConfirmedVoucherRepository(_store), _requests);
        _instruments = new InstrumentUseCases(new InMemoryUnitOfWork(_db), new InMemoryInstrumentRepository(_db), new InMemoryChequeBookRepository(_db),
            new InMemoryIdempotencyStore(_db), policies, policies, policies, new Treasurer(), _bank);
        _vouchers = new VoucherUseCases(new InMemoryUnitOfWork(_db), new InMemoryVoucherTemplateRepository(_store), new InMemoryConfirmedVoucherRepository(_store),
            new InMemoryReceiptRepository(_db), new InMemoryInstrumentRepository(_db), new FakeReceiptAccounts(), _bank, _reference,
            new InMemoryIdempotencyStore(_db), new Treasurer(), TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static ReceiptHeaderInput Header() => new("1405/08/23", "200015", FakeReceiptReference.SalesGroup, "فروش محصولات");

    private async Task SetTemplate()
    {
        foreach (var (a, code) in new[] { (FixedAccount.ChequesInCashDesk, "1201"), (FixedAccount.ChequesInCollection, "1202"), (FixedAccount.ProtestedCheques, "1203") })
        {
            var r = await _vouchers.SetTemplateAccountAsync(Key(), a, code, CancellationToken.None);
            Assert.True(r.IsSuccess, r.ErrorCode);
        }
    }

    private async Task<Guid> ChequeReceipt()
    {
        var r = await _receipts.RecordInstrumentsAsync(new(Key(), Header(),
            [new(InstrumentKind.OrdinaryCheque, "445566", "1405/10/01", new MoneyInput(10_000_000), FakeReceiptReference.Mellat, SayadId: "1234567890123456")]),
            CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    [Fact]
    public async Task The_template_page_lists_every_fixed_account_and_checks_the_chart()
    {
        await SetTemplate();
        var t = await _vouchers.GetTemplateAsync(CancellationToken.None);
        Assert.Equal(Enum.GetNames<FixedAccount>(), t.Select(a => a.Account));
        Assert.Equal("1202", t.Single(a => a.Account == "ChequesInCollection").Code);
        Assert.Null(t.Single(a => a.Account == "GuaranteeCounterpart").Code);
        Assert.Equal("VoucherTemplate.AccountNotPostable", (await _vouchers.SetTemplateAccountAsync(Key(), FixedAccount.GuaranteeCounterpart, "12", CancellationToken.None)).ErrorCode);
        Assert.Equal("VoucherTemplate.AccountNotFound", (await _vouchers.SetTemplateAccountAsync(Key(), FixedAccount.GuaranteeCounterpart, "777", CancellationToken.None)).ErrorCode);
        Assert.True((await _vouchers.SetTemplateAccountAsync(Key(), FixedAccount.ChequesInCollection, "", CancellationToken.None)).IsSuccess);
        Assert.Null((await _vouchers.GetTemplateAsync(CancellationToken.None)).Single(a => a.Account == "ChequesInCollection").Code);
    }

    [Fact]
    public async Task The_form_shows_the_voucher_before_saving_without_taking_a_number()
    {
        var preview = await _receipts.PreviewCashAsync(new("preview", Header(), FakeReceiptAccounts.Desk, 5_000_000), CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.ErrorCode);
        Assert.Equal([(true, "1101", "500001"), (false, "4101", "200015")], preview.Value!.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal("دریافت نقدی", preview.Value.Operation);
        Assert.Empty(await _receipts.ListAsync(1405, ReceiptKind.Cash, CancellationToken.None));

        var saved = await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 5_000_000), CancellationToken.None);
        Assert.Equal(1, (await _receipts.GetAsync(saved.Value, CancellationToken.None))!.Number);

        // An incomplete form answers with the rule it breaks, as a save would.
        Assert.Equal("Receipt.AccountNotFound", (await _receipts.PreviewCashAsync(new("preview", Header(), Guid.NewGuid(), 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_cheque_receipt_without_the_template_cannot_be_confirmed_until_the_account_is_set()
    {
        var id = await ChequeReceipt();
        var v = (await _vouchers.ReceiptVoucherAsync(id, CancellationToken.None))!;
        Assert.False(v.IsComplete);
        Assert.Equal("ChequesInCashDesk", v.Lines[0].Missing);
        Assert.Equal("Voucher.Incomplete", (await _vouchers.ConfirmReceiptVoucherAsync(Key(), id, CancellationToken.None)).ErrorCode);

        await SetTemplate();
        var confirmed = await _vouchers.ConfirmReceiptVoucherAsync(Key(), id, CancellationToken.None);
        Assert.True(confirmed.IsSuccess, confirmed.ErrorCode);
        v = (await _vouchers.ReceiptVoucherAsync(id, CancellationToken.None))!;
        Assert.Equal(("treasurer", "1201"), (v.ConfirmedBy, v.Lines[0].AccountCode));
        Assert.Equal("Voucher.AlreadyConfirmed", (await _vouchers.ConfirmReceiptVoucherAsync(Key(), id, CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_confirmed_voucher_keeps_its_lines_when_the_template_changes()
    {
        await SetTemplate();
        var id = await ChequeReceipt();
        Assert.True((await _vouchers.ConfirmReceiptVoucherAsync(Key(), id, CancellationToken.None)).IsSuccess);
        Assert.True((await _vouchers.SetTemplateAccountAsync(Key(), FixedAccount.ChequesInCashDesk, "1203", CancellationToken.None)).IsSuccess);
        Assert.Equal("1201", (await _vouchers.ReceiptVoucherAsync(id, CancellationToken.None))!.Lines[0].AccountCode);
    }

    [Fact]
    public async Task A_receipt_with_a_confirmed_voucher_is_no_longer_cancelled()
    {
        var r = await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 1_000_000), CancellationToken.None);
        Assert.True((await _vouchers.ConfirmReceiptVoucherAsync(Key(), r.Value, CancellationToken.None)).IsSuccess);
        Assert.Equal("Receipt.VoucherConfirmed", (await _receipts.CancelAsync(new(Key(), r.Value, "1405/08/24", "اشتباه", 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_cancelled_receipt_gets_no_voucher()
    {
        var r = await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 1_000_000), CancellationToken.None);
        Assert.True((await _receipts.CancelAsync(new(Key(), r.Value, "1405/08/24", "اشتباه", 1), CancellationToken.None)).IsSuccess);
        Assert.Equal("Voucher.SourceCancelled", (await _vouchers.ConfirmReceiptVoucherAsync(Key(), r.Value, CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Each_step_of_a_cheque_at_the_bank_has_its_own_voucher_with_the_bank_detail()
    {
        await SetTemplate();
        var receipt = await ChequeReceipt();
        var cheque = (await _receipts.GetAsync(receipt, CancellationToken.None))!.Instruments![0].Id;
        Assert.Empty(await _vouchers.ChequeVouchersAsync(cheque, CancellationToken.None));

        Assert.True((await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RegisterSayad, "1405/08/24"), CancellationToken.None)).IsSuccess); // rule N3
        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), cheque, LedgerBank.Id, "1405/08/25", "SLIP-1"), CancellationToken.None)).IsSuccess);
        Assert.True((await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RecordCleared, "1405/10/02"), CancellationToken.None)).IsSuccess);
        var steps = await _vouchers.ChequeVouchersAsync(cheque, CancellationToken.None);
        Assert.Equal([3, 4], steps.Select(s => s.Step));
        Assert.Equal([(true, "1202", "600009"), (false, "1201", "200015")], steps[0].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal([(true, "1109", "600009"), (false, "1202", "600009")], steps[1].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal("1405/10/02", steps[1].Date);

        Assert.True((await _vouchers.ConfirmChequeVoucherAsync(Key(), cheque, 3, CancellationToken.None)).IsSuccess);
        Assert.Equal("treasurer", (await _vouchers.ChequeVouchersAsync(cheque, CancellationToken.None))[0].ConfirmedBy);
        Assert.Equal("Voucher.AlreadyConfirmed", (await _vouchers.ConfirmChequeVoucherAsync(Key(), cheque, 3, CancellationToken.None)).ErrorCode);
        Assert.Equal("Voucher.NoVoucherForStep", (await _vouchers.ConfirmChequeVoucherAsync(Key(), cheque, 1, CancellationToken.None)).ErrorCode);
        Assert.Equal("Voucher.NoVoucherForStep", (await _vouchers.ConfirmChequeVoucherAsync(Key(), cheque, 2, CancellationToken.None)).ErrorCode); // «ثبت صیادی»
        Assert.Equal("Instrument.NotFound", (await _vouchers.ConfirmChequeVoucherAsync(Key(), Guid.NewGuid(), 2, CancellationToken.None)).ErrorCode);
    }

    private async Task<Guid> ChequeOf(Guid receiptId) => (await _receipts.GetAsync(receiptId, CancellationToken.None))!.Instruments![0].Id;

    [Fact]
    public async Task A_cheque_returned_by_the_bank_is_replaced_by_two_cheques_on_a_receipt_of_its_own_item_14()
    {
        await SetTemplate();
        var cheque = await ChequeOf(await ChequeReceipt());
        Assert.True((await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RegisterSayad, "1405/08/24"), CancellationToken.None)).IsSuccess); // rule N3
        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), cheque, LedgerBank.Id, "1405/08/25", "SLIP-1"), CancellationToken.None)).IsSuccess);
        Assert.True((await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.ReturnFromBank, "1405/08/28", Note: "امضا ناقص"), CancellationToken.None)).IsSuccess);

        ReceivedInstrumentInput Line(string number, decimal amount) =>
            new(InstrumentKind.OrdinaryCheque, number, "1405/10/15", new MoneyInput(amount), FakeReceiptReference.Melli, SayadId: "6543210987654321");
        var header = new ReceiptHeaderInput("1405/08/29", "200015", FakeReceiptReference.SalesGroup, "تعویض چک 445566");
        var preview = await _receipts.PreviewReplacementAsync(new("preview", cheque, header, [Line("901", 6_000_000), Line("902", 5_000_000)]), CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.ErrorCode);
        Assert.Equal(("تعویض چک", 4), (preview.Value!.Operation, preview.Value.Lines.Count));

        var other = new ReceiptHeaderInput("1405/08/29", "200016", FakeReceiptReference.SalesGroup, "تعویض");
        Assert.Equal("Receipt.ReplacementPayerDiffers", (await _receipts.ReplaceAsync(new(Key(), cheque, other, [Line("903", 1)]), CancellationToken.None)).ErrorCode);

        var r = await _receipts.ReplaceAsync(new(Key(), cheque, header, [Line("901", 6_000_000), Line("902", 5_000_000)]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var receipt = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(((Guid?)cheque, 2, 2), (receipt.ReplacedInstrumentId, receipt.Instruments!.Count, receipt.Number));
        Assert.Equal("Replaced", (await _instruments.GetAsync(cheque, CancellationToken.None))!.State);

        var v = (await _vouchers.ReceiptVoucherAsync(r.Value, CancellationToken.None))!;
        Assert.Equal([(true, "1201"), (true, "1201"), (false, "1201"), (false, "4101")], v.Lines.Select(l => (l.IsDebit, l.AccountCode)));
        Assert.True(v.IsComplete);
        Assert.Equal([3, 4], (await _vouchers.ChequeVouchersAsync(cheque, CancellationToken.None)).Select(x => x.Step));
        Assert.Equal("Instrument.InvalidTransition", (await _receipts.ReplaceAsync(new(Key(), cheque, header, [Line("904", 1)]), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task An_endorsement_pays_an_approved_payment_request_and_its_return_takes_the_payment_back_Q36()
    {
        await SetTemplate();
        var cheque = await ChequeOf(await ChequeReceipt());
        Assert.Equal("Instrument.ActionNotSupportedHere",
            (await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.Endorse, "1405/08/24", "پیمانکار"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Instrument.PaymentRequestRequired", (await _receipts.EndorseAsync(new(Key(), cheque, "1405/08/24", Guid.Empty, "SY-1"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Instrument.PaymentRequestNotApproved",
            (await _receipts.EndorseAsync(new(Key(), cheque, "1405/08/24", FakePaymentRequests.Registered, "SY-1"), CancellationToken.None)).ErrorCode);
        _requests.Requests[FakePaymentRequests.Approved] = _requests.Requests[FakePaymentRequests.Approved] with { PaidAmount = 20_000_000 };
        Assert.Equal("Instrument.PaymentRequestOverpaid",
            (await _receipts.EndorseAsync(new(Key(), cheque, "1405/08/24", FakePaymentRequests.Approved, "SY-1"), CancellationToken.None)).ErrorCode);
        _requests.Requests[FakePaymentRequests.Approved] = _requests.Requests[FakePaymentRequests.Approved] with { PaidAmount = 15_000_000 };

        // The request's beneficiary receives the cheque; its payment group's account is debited.
        var r = await _receipts.EndorseAsync(new(Key(), cheque, "1405/08/24", FakePaymentRequests.Approved, "SY-1"), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Equal(("Paid", 25_000_000m), (_requests.Requests[FakePaymentRequests.Approved].State, _requests.Requests[FakePaymentRequests.Approved].PaidAmount));
        var view = (await _instruments.GetAsync(cheque, CancellationToken.None))!;
        Assert.Equal(("پیمانکار الف", (Guid?)FakePaymentRequests.Approved), (view.Destination, view.History[^1].Details!.PaymentRequestId));
        var steps = await _vouchers.ChequeVouchersAsync(cheque, CancellationToken.None);
        Assert.Equal([(true, "2101", "300001"), (false, "1201", "200015")], steps.Single().Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));

        Assert.Equal("Instrument.ActionNotSupportedHere",
            (await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RecordEndorsementReturned, "1405/10/02", Reference: "BN-9"), CancellationToken.None)).ErrorCode);
        Assert.True((await _receipts.EndorsementReturnedAsync(new(Key(), cheque, "1405/10/02", "BN-9"), CancellationToken.None)).IsSuccess);
        Assert.Equal(("Approved", 15_000_000m), (_requests.Requests[FakePaymentRequests.Approved].State, _requests.Requests[FakePaymentRequests.Approved].PaidAmount));
        steps = await _vouchers.ChequeVouchersAsync(cheque, CancellationToken.None);
        Assert.Equal([(true, "1203", "200015"), (false, "2101", "300001")], steps[1].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
    }

    private async Task<Guid> NoteOf(NoteReason reason = NoteReason.Debt, string number = "778899", decimal amount = 10_000_000)
    {
        var r = await _receipts.RecordNotesAsync(new(Key(), Header(), new NoteTermsInput(reason, NoteForm.Paper, NoteMaturity.OnDemand),
            [new(number, null, null, amount, new NoteChecks(true, true, true, true))]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return await ChequeOf(r.Value);
    }

    [Fact]
    public async Task A_received_note_goes_to_the_bank_and_is_collected_with_notes_in_collection_owner_16_Nov()
    {
        await SetTemplate();
        Assert.True((await _vouchers.SetTemplateAccountAsync(Key(), FixedAccount.NotesInCollection, "9101", CancellationToken.None)).IsSuccess);
        var note = await NoteOf();
        Assert.Contains("DepositForCollection", (await _instruments.GetAsync(note, CancellationToken.None))!.AllowedActions);

        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), note, LedgerBank.Id, "1405/08/25", "SLIP-7"), CancellationToken.None)).IsSuccess);
        // Payable on demand: collected the next day (owner's answer to Q38).
        Assert.True((await _instruments.ChangeStateAsync(new(Key(), note, InstrumentAction.RecordCleared, "1405/08/26", Reference: "ST-1"), CancellationToken.None)).IsSuccess);
        Assert.Equal("Cleared", (await _instruments.GetAsync(note, CancellationToken.None))!.State);

        var steps = await _vouchers.ChequeVouchersAsync(note, CancellationToken.None);
        Assert.Equal(["واگذاری سفته به بانک", "اعلام وصول سفته توسط بانک"], steps.Select(v => v.Operation));
        Assert.Equal([(true, "9101", "600009"), (false, "1201", "200015")], steps[0].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal([(true, "1109", "600009"), (false, "9101", "600009")], steps[1].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.True((await _vouchers.ConfirmChequeVoucherAsync(Key(), note, 2, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task A_note_is_endorsed_to_pay_an_approved_request_without_a_Sayad_transfer_and_a_guarantee_note_is_only_handed_back()
    {
        await SetTemplate();
        var note = await NoteOf();
        var r = await _receipts.EndorseAsync(new(Key(), note, "1405/08/24", FakePaymentRequests.Approved, string.Empty, "ظهرنویسی شد"), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Equal(10_000_000m, _requests.Requests[FakePaymentRequests.Approved].PaidAmount);
        var view = (await _instruments.GetAsync(note, CancellationToken.None))!;
        Assert.Equal(("Endorsed", "پیمانکار الف"), (view.State, view.Destination));
        var step = Assert.Single(await _vouchers.ChequeVouchersAsync(note, CancellationToken.None));
        Assert.Equal(("خرج سفته", true), (step.Operation, step.IsComplete));
        Assert.Equal([(true, "2101", "300001"), (false, "1201", "200015")], step.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));

        // A guarantee note is neither deposited nor endorsed; handing it back («استرداد») clears the off-balance accounts.
        // It may also get legal action (owner, 18 Nov 2026).
        var guarantee = await NoteOf(NoteReason.Guarantee, "G-1", 5_000_000);
        Assert.Equal(["ReturnToIssuer", "Cancel", "SendToLegal"], (await _instruments.GetAsync(guarantee, CancellationToken.None))!.AllowedActions);
        Assert.Equal("Instrument.KindNotAllowedForAction",
            (await _instruments.DepositForCollectionAsync(new(Key(), guarantee, LedgerBank.Id, "1405/08/25", null), CancellationToken.None)).ErrorCode);
        Assert.Equal("Instrument.KindNotAllowedForAction",
            (await _receipts.EndorseAsync(new(Key(), guarantee, "1405/08/24", FakePaymentRequests.Approved, string.Empty), CancellationToken.None)).ErrorCode);
        Assert.True((await _instruments.ChangeStateAsync(new(Key(), guarantee, InstrumentAction.ReturnToIssuer, "1405/09/30", Note: "پایان قرارداد"), CancellationToken.None)).IsSuccess);
        var back = Assert.Single(await _vouchers.ChequeVouchersAsync(guarantee, CancellationToken.None));
        Assert.Equal("استرداد سفته انتظامی دریافتی", back.Operation);
        Assert.Equal(["GuaranteeCounterpart", "GuaranteeNotesForCompany"], back.Lines.Select(l => l.Missing!));

        // An ordinary note handed back debits the account its receipt credited (the purpose group's, «بدهکاران»), answer to Q50.
        var ordinary = await NoteOf(number: "N-9", amount: 3_000_000);
        Assert.True((await _instruments.ChangeStateAsync(new(Key(), ordinary, InstrumentAction.ReturnToIssuer, "1405/08/24", Note: "توافق"), CancellationToken.None)).IsSuccess);
        var returned = Assert.Single(await _vouchers.ChequeVouchersAsync(ordinary, CancellationToken.None));
        Assert.Equal(("استرداد سفته به متعهد", true), (returned.Operation, returned.IsComplete));
        Assert.Equal([(true, "4101", "200015"), (false, "1201", "200015")], returned.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
    }

    [Fact]
    public async Task A_bounce_needs_its_notice_and_a_cancellation_managements_order_Q30()
    {
        await SetTemplate();
        var cheque = await ChequeOf(await ChequeReceipt());
        Assert.True((await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RegisterSayad, "1405/08/24"), CancellationToken.None)).IsSuccess); // rule N3
        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), cheque, LedgerBank.Id, "1405/08/25", "SLIP-1"), CancellationToken.None)).IsSuccess);
        Assert.Equal("Instrument.BounceNoticeRequired",
            (await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RecordBounced, "1405/10/02", Note: "کسری"), CancellationToken.None)).ErrorCode);
        Assert.True((await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RecordBounced, "1405/10/02", Note: "کسری", Reference: "BN-1"), CancellationToken.None)).IsSuccess);

        Assert.Equal("Instrument.CancelOrderRequired",
            (await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.Cancel, "1405/10/20", Note: "سوخت"), CancellationToken.None)).ErrorCode);
        Assert.True((await _instruments.ChangeStateAsync(
            new(Key(), cheque, InstrumentAction.Cancel, "1405/10/20", Note: "سوخت", Reference: "م-12", DocumentDate: "1405/10/18"), CancellationToken.None)).IsSuccess);
        var view = (await _instruments.GetAsync(cheque, CancellationToken.None))!;
        Assert.Equal(("Cancelled", "1405/10/18"), (view.State, view.History[^1].Details!.DocumentDate));

        var cancel = (await _vouchers.ChequeVouchersAsync(cheque, CancellationToken.None))[^1];
        Assert.Equal(("BadDebts", "1203"), (cancel.Lines[0].Missing, cancel.Lines[1].AccountCode));
        Assert.True((await _vouchers.SetTemplateAccountAsync(Key(), FixedAccount.BadDebts, "8101", CancellationToken.None)).IsSuccess);
        Assert.True((await _vouchers.ConfirmChequeVoucherAsync(Key(), cheque, 5, CancellationToken.None)).IsSuccess);
    }

    private sealed class LedgerBank : IBankAccountLookup
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
