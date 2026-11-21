using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.ChequeBooks;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Application.Transfers;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Transfers;
using Treasury.Cashflow.Domain.Vouchers;
using Treasury.Cashflow.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Cashflow.Application.Tests;

/// <summary>
/// Transfers between the company's own bank accounts (owner's report no. 7 of 20 Nov 2026): electronic within the limits kept on the
/// Basic Information page, and by Sayad cheque — payment side, the button to the receipts side, and the cheque's steps to collection.
/// </summary>
public sealed class TransferUseCaseTests : IDisposable
{
    private const decimal Milliard = 1_000_000_000m;
    private const string Today = "1405/08/29";

    private readonly InMemoryDatabase _db = new();
    private readonly InMemoryVoucherStore _store = new();
    private readonly Banks _banks = new();
    private readonly FakeReceiptReference _reference = new();
    private readonly FakePaymentRequests _requests = new();
    private readonly TransferUseCases _transfers;
    private readonly InstrumentUseCases _instruments;
    private readonly ChequeBookUseCases _books;

    public TransferUseCaseTests()
    {
        var policies = new DevelopmentPolicies().OpenYear(1405);
        var uow = new InMemoryUnitOfWork(_db);
        var transfers = new InMemoryBankTransferRepository(_db);
        _transfers = new TransferUseCases(uow, transfers, new InMemoryTransferLimitRepository(_db), new InMemoryInstrumentRepository(_db),
            new InMemoryChequeBookRepository(_db), _banks, _reference, policies, new InMemoryVoucherTemplateRepository(_store),
            new InMemoryConfirmedVoucherRepository(_store), new InMemoryIdempotencyStore(_db), new Treasurer(), _requests);
        _instruments = new InstrumentUseCases(uow, new InMemoryInstrumentRepository(_db), new InMemoryChequeBookRepository(_db), new InMemoryIdempotencyStore(_db),
            policies, policies, policies, new Treasurer(), _banks, transfers: transfers);
        _books = new ChequeBookUseCases(uow, new InMemoryChequeBookRepository(_db), _banks, new InMemoryIdempotencyStore(_db), new InMemoryChequeFormatRepository(_db));
        new InMemoryVoucherTemplateRepository(_store).SaveAsync(new VoucherTemplate(new Dictionary<FixedAccount, string>
        {
            [FixedAccount.ChequesInCashDesk] = "1201", [FixedAccount.ChequesInCollection] = "1202", [FixedAccount.FundsInTransit] = "1109",
            [FixedAccount.BankFees] = "8205", [FixedAccount.NotesPayable] = "2105",
        }), CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// An approved payment request «نقل و انتقال بین بانک‌های شرکت» in requests-svc for these two accounts (owner's answer to Q82), large
    /// enough for every transfer of a test unless an amount is given.
    /// </summary>
    private Guid RequestFor(Guid source, Guid destination, decimal amount = 1_000_000 * Milliard, string state = "Approved", string method = "OwnAccountTransfer")
    {
        var id = Guid.NewGuid();
        _requests.Requests[id] = new PaymentRequestFact(id, 40 + _requests.Requests.Count, state, string.Empty, "حساب مقصد", Guid.Empty, "تأمین موجودی", amount, "IRR", 0,
            null, method, source, destination);
        return id;
    }

    private readonly Dictionary<(Guid, Guid), Guid> _standing = [];

    private Guid StandingRequest(Guid source, Guid destination) =>
        _standing.TryGetValue((source, destination), out var id) ? id : _standing[(source, destination)] = RequestFor(source, destination);

    /// <summary>The form with the standing request of its two accounts, unless the test chose a request itself.</summary>
    private ElectronicTransferInput R(ElectronicTransferInput form) =>
        form.PaymentRequestId is null ? form with { PaymentRequestId = StandingRequest(form.SourceAccountId, form.DestinationAccountId) } : form;

    private TransferChequeInput C(TransferChequeInput form) =>
        form.PaymentRequestId is null ? form with { PaymentRequestId = StandingRequest(form.SourceAccountId, form.DestinationAccountId) } : form;

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static ElectronicTransferInput Paya(decimal amount, TransferChannel channel = TransferChannel.Remote, Guid? source = null, string date = Today) =>
        new(date, source ?? Banks.MellatCurrent, Banks.MelliCurrent, TransferMethod.Paya, channel, amount, null, "TRK-9", "تأمین موجودی");

    private async Task<Guid> Record(ElectronicTransferInput form)
    {
        var r = await _transfers.RecordElectronicAsync(new(Key(), R(form)), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<string?> Refused(ElectronicTransferInput form) => (await _transfers.RecordElectronicAsync(new(Key(), R(form)), CancellationToken.None)).ErrorCode;

    private async Task<TransferView> View(Guid id) => (await _transfers.GetAsync(id, CancellationToken.None))!;

    // ----- electronic -------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task An_electronic_transfer_is_numbered_per_year_and_marks_both_accounts_in_use()
    {
        var first = await Record(Paya(Milliard));
        var second = await Record(new(Today, Banks.MellatCurrent, Banks.MellatSaving, TransferMethod.Internal, TransferChannel.InPerson, 250_000_000));

        var list = await _transfers.ListAsync(1405, cheque: false, CancellationToken.None);
        Assert.Equal([2, 1], list.Select(t => t.Number));
        var view = await View(first);
        Assert.Equal(("Paya", "Remote", "Recorded", "بانک ملت", "0101-1", "بانک ملی", "0202-1", Milliard, "treasurer"),
            (view.Method, view.Channel, view.State, view.SourceBankName, view.SourceAccountNumber, view.DestinationBankName, view.DestinationAccountNumber, view.AmountRial, view.RecordedBy));
        Assert.Equal((null, null), (view.WhyNotCorrectable, view.WhyNotDeletable));
        Assert.Equal("Internal", (await View(second)).Method);
        Assert.True(_banks.InUse.IsSupersetOf([Banks.MellatCurrent, Banks.MelliCurrent, Banks.MellatSaving]));
        Assert.Empty(await _transfers.ListAsync(1405, cheque: true, CancellationToken.None));
    }

    [Fact]
    public async Task A_repeated_command_records_the_transfer_once()
    {
        var command = new RecordElectronicTransferCommand(Key(), R(Paya(Milliard)));
        var a = await _transfers.RecordElectronicAsync(command, CancellationToken.None);
        var b = await _transfers.RecordElectronicAsync(command, CancellationToken.None);
        Assert.Equal(a.Value, b.Value);
        Assert.Single(await _transfers.ListAsync(1405, null, CancellationToken.None));
    }

    [Fact]
    public async Task The_rules_of_the_way_the_accounts_and_the_fiscal_year_are_checked()
    {
        Assert.Equal("Transfer.MethodNeedsOtherBank", await Refused(Paya(Milliard) with { DestinationAccountId = Banks.MellatSaving }));
        Assert.Equal("Transfer.InternalNeedsSameBank", await Refused(Paya(Milliard) with { Method = TransferMethod.Internal }));
        Assert.Equal("Transfer.AccountNotFound", await Refused(Paya(Milliard) with { DestinationAccountId = Guid.NewGuid() }));
        Assert.Equal("Transfer.AccountRequired", await Refused(Paya(Milliard) with { SourceAccountId = Guid.Empty }));
        Assert.Equal("Transfer.AccountInactive", await Refused(Paya(Milliard) with { DestinationAccountId = Banks.MelliClosed }));
        Assert.Equal("Transfer.ChannelRequired", await Refused(Paya(Milliard) with { Channel = null }));
        Assert.Equal("Transfer.DateRequired", await Refused(Paya(Milliard) with { Date = " " }));
        Assert.Equal("FiscalPeriod.Closed", await Refused(Paya(Milliard, date: "1404/08/29")));
        Assert.Equal("Money.NotPositive", await Refused(Paya(0)));
        Assert.Empty(await _transfers.ListAsync(1405, null, CancellationToken.None));
    }

    [Fact]
    public async Task The_daily_limits_count_what_the_same_account_already_sent_that_day_by_the_same_way()
    {
        // Paya (owner's figures): each remote order at most 3 milliard, at most 5 orders a day.
        Assert.Equal("Transfer.OverOrderLimit", await Refused(Paya(3 * Milliard + 1)));
        for (var i = 0; i < 5; i++)
        {
            await Record(Paya(3 * Milliard));
        }

        Assert.Equal("Transfer.OverDailyCount", await Refused(Paya(1_000_000)));
        await Record(Paya(1_000_000, source: Banks.SaderatCurrent));  // another source account has its own day
        await Record(Paya(1_000_000, date: "1405/08/30"));            // and so has another day

        // Pol: the remote orders of a day carry at most 2 milliard together.
        var pol = new ElectronicTransferInput(Today, Banks.MellatCurrent, Banks.MelliCurrent, TransferMethod.Pol, TransferChannel.Remote, 1_500_000_000);
        var sent = await Record(pol);
        Assert.Equal("Transfer.OverDailyLimit", await Refused(pol with { Amount = 600_000_000 }));
        await Record(pol with { Amount = 600_000_000, Channel = TransferChannel.InPerson }); // the branch has its own total (3 milliard)

        // A deleted transfer no longer counts.
        var deleted = await _transfers.DeleteAsync(new(Key(), sent, (await View(sent)).Version), CancellationToken.None);
        Assert.True(deleted.IsSuccess, deleted.ErrorCode);
        await Record(pol with { Amount = 2 * Milliard });
    }

    [Fact]
    public async Task The_limits_are_read_from_the_basic_information_page_and_changed_there()
    {
        var rows = await _transfers.GetLimitsAsync(CancellationToken.None);
        Assert.Equal(["Internal", "Pol", "Paya", "Satna"], rows.Select(r => r.Method));
        Assert.Equal((3 * Milliard, 2 * Milliard, 5), (rows[1].MaxPerDayInPerson, rows[1].MaxPerDayRemote, rows[1].MaxCountPerDay));
        Assert.Equal((5 * Milliard, 3 * Milliard, 5), (rows[2].MaxPerOrderInPerson, rows[2].MaxPerOrderRemote, rows[2].MaxCountPerDay));
        Assert.Equal(2 * Milliard, rows[3].MinPerOrder);

        // The banks raise Paya: the company types the new figure, no new version of the program is needed.
        var set = await _transfers.SetLimitAsync(new(Key(), TransferMethod.Paya, null, null, 10 * Milliard, 10 * Milliard, null, null, null), CancellationToken.None);
        Assert.True(set.IsSuccess, set.ErrorCode);
        await Record(Paya(8 * Milliard));

        // Mellat's own limit for transfers out of its accounts; Saderat keeps the general one.
        Assert.True((await _transfers.SetLimitAsync(new(Key(), TransferMethod.Paya, FakeReceiptReference.Mellat, null, null, Milliard, null, null, null), CancellationToken.None)).IsSuccess);
        Assert.Equal("Transfer.OverOrderLimit", await Refused(Paya(2 * Milliard)));
        await Record(Paya(2 * Milliard, source: Banks.SaderatCurrent));
        Assert.Equal(5, (await _transfers.GetLimitsAsync(CancellationToken.None)).Count);
        Assert.True((await _transfers.RemoveBankLimitAsync(Key(), TransferMethod.Paya, FakeReceiptReference.Mellat, CancellationToken.None)).IsSuccess);
        await Record(Paya(2 * Milliard));

        Assert.Equal("TransferLimit.BankNotFound",
            (await _transfers.SetLimitAsync(new(Key(), TransferMethod.Pol, Guid.NewGuid(), null, null, null, null, null, 3), CancellationToken.None)).ErrorCode);
        Assert.Equal("TransferLimit.AmountInvalid",
            (await _transfers.SetLimitAsync(new(Key(), TransferMethod.Pol, null, -1, null, null, null, null, null), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_voucher_of_an_electronic_transfer_is_previewed_confirmed_and_then_closes_correction_and_deletion()
    {
        var preview = await _transfers.PreviewElectronicAsync(R(Paya(Milliard)), null, CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.ErrorCode);
        Assert.Equal("نقل و انتقال بانکی (پایا)", preview.Value!.Operation);
        Assert.Equal("Transfer.OverOrderLimit", (await _transfers.PreviewElectronicAsync(R(Paya(4 * Milliard)), null, CancellationToken.None)).ErrorCode);
        Assert.Empty(await _transfers.ListAsync(1405, null, CancellationToken.None)); // a preview saves nothing and takes no number

        var id = await Record(Paya(Milliard));
        var voucher = Assert.Single(await _transfers.VouchersAsync(id, CancellationToken.None));
        Assert.Equal([(true, "1103", "600003"), (false, "1102", "600001")], voucher.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal(("Transfer", 0, true, null), (voucher.Source, voucher.Step, voucher.IsComplete, voucher.ConfirmedBy));

        // Correction before «تایید سند»: the transfer itself is not counted against the day's limits.
        var corrected = await _transfers.CorrectElectronicAsync(new(Key(), id, (await View(id)).Version, R(Paya(3 * Milliard, TransferChannel.InPerson))), CancellationToken.None);
        Assert.True(corrected.IsSuccess, corrected.ErrorCode);
        Assert.Equal((3 * Milliard, "InPerson", 1), ((await View(id)).Amount, (await View(id)).Channel, (await View(id)).Number));

        Assert.True((await _transfers.ConfirmVoucherAsync(Key(), id, 0, CancellationToken.None)).IsSuccess);
        Assert.Equal("Voucher.AlreadyConfirmed", (await _transfers.ConfirmVoucherAsync(Key(), id, 0, CancellationToken.None)).ErrorCode);
        Assert.Equal("Voucher.NoVoucherForStep", (await _transfers.ConfirmVoucherAsync(Key(), id, 1, CancellationToken.None)).ErrorCode);
        var view = await View(id);
        Assert.Equal((true, "Transfer.VoucherConfirmed", "Transfer.VoucherConfirmed"), (view.PayerVoucherConfirmed, view.WhyNotCorrectable, view.WhyNotDeletable));
        Assert.Equal("treasurer", (await _transfers.VouchersAsync(id, CancellationToken.None))[0].ConfirmedBy);
        Assert.Equal("Transfer.VoucherConfirmed", (await _transfers.CorrectElectronicAsync(new(Key(), id, view.Version, R(Paya(Milliard))), CancellationToken.None)).ErrorCode);
        Assert.Equal("Transfer.VoucherConfirmed", (await _transfers.DeleteAsync(new(Key(), id, view.Version), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_deleted_transfer_stays_in_the_list_marked_and_its_number_is_not_given_again()
    {
        var id = await Record(Paya(Milliard));
        Assert.Equal("Concurrency.Conflict", (await _transfers.DeleteAsync(new(Key(), id, 99), CancellationToken.None)).ErrorCode);
        Assert.True((await _transfers.DeleteAsync(new(Key(), id, 1), CancellationToken.None)).IsSuccess);

        var view = await View(id);
        Assert.Equal(("Deleted", "treasurer", "Transfer.Deleted"), (view.State, view.DeletedBy, view.WhyNotDeletable));
        Assert.NotNull(view.DeletedAt);
        Assert.Empty(await _transfers.VouchersAsync(id, CancellationToken.None));
        Assert.Equal("Transfer.Deleted", (await _transfers.ConfirmVoucherAsync(Key(), id, 0, CancellationToken.None)).ErrorCode);
        Assert.Equal(2, (await View(await Record(Paya(Milliard)))).Number);
    }

    // ----- by Sayad cheque --------------------------------------------------------------------------------------------------------

    private async Task<Guid> Book(Guid? account = null)
    {
        var r = await _books.RegisterAsync(new(Key(), account ?? Banks.MellatCurrent, "1234", 100500, 100524, null, "12345678", "7"), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private static TransferChequeInput Cheque(Guid book, decimal amount = 7_000_000_000, string due = "1405/10/15", long? sheet = null) =>
        new(Today, Banks.MellatCurrent, Banks.MelliCurrent, book, sheet, "4821", amount, due, "تأمین موجودی حساب ملی");

    private async Task<Guid> Issue(TransferChequeInput form)
    {
        var r = await _transfers.IssueChequeAsync(new(Key(), C(form)), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<ChequeBookView> BookView(Guid id) => (await _books.ListAsync(null, true, CancellationToken.None)).Single(b => b.Id == id);

    private async Task Step(Guid cheque, InstrumentAction action, string date)
    {
        var r = await _instruments.ChangeStateAsync(new(Key(), cheque, action, date), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
    }

    [Fact]
    public async Task The_payment_side_takes_the_next_sheet_of_the_source_accounts_book_and_builds_its_sayad_id()
    {
        var book = await Book();
        var id = await Issue(Cheque(book));

        var view = await View(id);
        Assert.Equal(("Cheque", null, "ChequeIssued", 7_000_000_000m), (view.Method, view.Channel, view.State, view.AmountRial));
        Assert.Equal(("100500", "4821" + "12345678" + "0007", "1405/10/15", null), (view.Cheque!.Number, view.Cheque.SayadId, view.Cheque.DueDate, view.Cheque.ReceivedChequeId));
        Assert.Equal((1L, 100501L), ((await BookView(book)).UsedSheets, (await BookView(book)).NextSheet!.Value));

        var issued = (await _instruments.GetAsync(view.Cheque.IssuedChequeId, CancellationToken.None))!;
        Assert.Equal(("Issued", "PostDatedCheque", "Issued", "100500"), (issued.Direction, issued.Kind, issued.State, issued.Number));

        // The issued cheque follows its transfer: it is not cancelled, delivered or marked paid on its own.
        foreach (var action in new[] { InstrumentAction.Cancel, InstrumentAction.Deliver, InstrumentAction.RecordPaidByBank })
        {
            Assert.Equal("Transfer.IssuedChequeFollowsTransfer",
                (await _instruments.ChangeStateAsync(new(Key(), issued.Id, action, Today, Note: "x"), CancellationToken.None)).ErrorCode);
        }

        // The payer's voucher: Dr «وجوه در راه» / Cr «اسناد پرداختنی» with the source bank's تفصیل, because the cheque is post-dated (Q81).
        var voucher = Assert.Single(await _transfers.VouchersAsync(id, CancellationToken.None));
        Assert.Equal([(true, "1109", null), (false, "2105", "600001")], voucher.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal("پرداخت چک – نقل و انتقال (انتقال دهنده) – شماره 1", voucher.Operation);
        Assert.Single(await _transfers.ListAsync(1405, cheque: true, CancellationToken.None));
    }

    [Fact]
    public async Task A_cheque_is_issued_only_from_a_book_of_the_source_account_and_a_preview_takes_no_sheet()
    {
        var book = await Book();
        var other = await Book(Banks.SaderatCurrent);
        async Task<string?> Fails(TransferChequeInput form) => (await _transfers.IssueChequeAsync(new(Key(), C(form)), CancellationToken.None)).ErrorCode;

        Assert.Equal("ChequeBook.WrongAccount", await Fails(Cheque(other)));
        Assert.Equal("ChequeBook.NotFound", await Fails(Cheque(Guid.NewGuid())));
        Assert.Equal("ChequeBook.SheetOutOfRange", await Fails(Cheque(book, sheet: 100999)));
        Assert.Equal("ChequeBook.SayadPrefixInvalid", await Fails(Cheque(book) with { SayadPrefix = "48" }));
        Assert.Equal("Instrument.DueBeforeIssue", await Fails(Cheque(book, due: "1405/08/28")));
        Assert.Equal("Instrument.DueDateRequired", await Fails(Cheque(book) with { DueDate = "" }));
        Assert.Equal("Transfer.SameAccount", await Fails(Cheque(book) with { DestinationAccountId = Banks.MellatCurrent }));

        var preview = await _transfers.PreviewChequeAsync(C(Cheque(book)), CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.ErrorCode);
        Assert.Equal([(true, "1109"), (false, "2105")], preview.Value!.Lines.Select(l => (l.IsDebit, l.AccountCode))); // post-dated: «اسناد پرداختنی»
        Assert.Equal(0, (await BookView(book)).UsedSheets); // nothing above took a sheet
        Assert.Empty(await _transfers.ListAsync(1405, null, CancellationToken.None));
    }

    [Fact]
    public async Task The_button_puts_the_cheque_in_the_cash_desk_of_the_receipts_side_and_it_goes_on_to_collection()
    {
        var id = await Issue(Cheque(await Book(), due: Today)); // a cheque of the day
        var received = await _transfers.ReceiveChequeAsync(new(Key(), id, 1), CancellationToken.None);
        Assert.True(received.IsSuccess, received.ErrorCode);
        Assert.Equal("Transfer.ChequeAlreadyReceived", (await _transfers.ReceiveChequeAsync(new(Key(), id, 2), CancellationToken.None)).ErrorCode);

        var view = await View(id);
        Assert.Equal(("ChequeReceived", Today, "InCashDesk", true), (view.State, view.Cheque!.ReceivedOn, view.Cheque.ReceivedState, view.CanUndoReceive));
        var cheque = Assert.Single(await _instruments.ListReceivedAsync(InstrumentState.InCashDesk, CancellationToken.None));
        Assert.Equal((view.Cheque.ReceivedChequeId, "100500", "بانک ملت", 7_000_000_000m, "600001"), (cheque.Id, cheque.Number, cheque.BankName, cheque.Amount, cheque.CounterpartyDetailCode));
        Assert.Equal((id, Banks.MelliCurrent, "0101-1", "مرکزی", view.Cheque.SayadId),
            (cheque.Details!.TransferId!.Value, cheque.Details.TransferAccountId!.Value, cheque.Details.AccountNumber, cheque.Details.BranchName, cheque.Details.SayadId));
        Assert.Equal(["RegisterSayad"], cheque.AllowedActions);

        // Two vouchers now: the payer's and the receiver's (Dr «اسناد نزد صندوق» with the source account's تفصیل / Cr «وجوه در راه»).
        var vouchers = await _transfers.VouchersAsync(id, CancellationToken.None);
        Assert.Equal([0, 1], vouchers.Select(v => v.Step));
        Assert.Equal([(true, "1201", "600001"), (false, "1109", null)], vouchers[1].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.True((await _transfers.ConfirmVoucherAsync(Key(), id, 1, CancellationToken.None)).IsSuccess);
        Assert.Equal((false, true, false), ((await View(id)).PayerVoucherConfirmed, (await View(id)).ReceiverVoucherConfirmed, (await View(id)).CanUndoReceive));

        // «دریافت ثبت صیاد شد», then to the destination bank only, then collected.
        await Step(cheque.Id, InstrumentAction.RegisterSayad, Today);
        Assert.Equal("Instrument.TransferChequeOtherAccount",
            (await _instruments.DepositForCollectionAsync(new(Key(), cheque.Id, Banks.SaderatCurrent, Today), CancellationToken.None)).ErrorCode);
        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), cheque.Id, Banks.MelliCurrent, Today, "F-1"), CancellationToken.None)).IsSuccess);
        Assert.Equal("Instrument.NotForTransferCheque",
            (await _instruments.ChangeStateAsync(new(Key(), cheque.Id, InstrumentAction.RecordBounced, Today, Reference: "BN-1"), CancellationToken.None)).ErrorCode);
        Assert.Equal("DepositedForCollection", (await View(id)).Cheque!.ReceivedState);
        await Step(cheque.Id, InstrumentAction.RecordCleared, "1405/08/30");

        // The collection finishes the transfer and the same cheque on the payment side is paid by its bank, in the same command.
        var done = await View(id);
        Assert.Equal(("Collected", "1405/08/30", "Cleared"), (done.State, done.Cheque!.CollectedOn, done.Cheque.ReceivedState));
        var issued = (await _instruments.GetAsync(done.Cheque.IssuedChequeId, CancellationToken.None))!;
        Assert.Equal(("PaidByBank", "RecordPaidByBank"), (issued.State, issued.History[^1].Action));
        Assert.Equal("Transfer.ChequeAlreadyReceived", (await _transfers.DeleteAsync(new(Key(), id, done.Version), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_cheque_may_be_received_on_a_later_day_but_not_before_it_was_issued()
    {
        var id = await Issue(Cheque(await Book()));
        Assert.Equal("Transfer.ReceivedBeforeIssue", (await _transfers.ReceiveChequeAsync(new(Key(), id, 1, "1405/08/28"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Concurrency.Conflict", (await _transfers.ReceiveChequeAsync(new(Key(), id, 7, Today), CancellationToken.None)).ErrorCode);
        Assert.True((await _transfers.ReceiveChequeAsync(new(Key(), id, 1, "1405/09/02"), CancellationToken.None)).IsSuccess);
        Assert.Equal("1405/09/02", (await _transfers.VouchersAsync(id, CancellationToken.None))[1].Date);

        var electronic = await Record(Paya(Milliard));
        Assert.Equal("Transfer.NotACheque", (await _transfers.ReceiveChequeAsync(new(Key(), electronic, 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_receipts_side_record_is_taken_back_until_a_step_is_recorded_on_the_cheque()
    {
        var id = await Issue(Cheque(await Book()));
        Assert.Equal("Transfer.ChequeNotReceived", (await _transfers.UndoReceiveAsync(new(Key(), id, 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _transfers.ReceiveChequeAsync(new(Key(), id, 1), CancellationToken.None)).IsSuccess);
        Assert.True((await _transfers.UndoReceiveAsync(new(Key(), id, 2), CancellationToken.None)).IsSuccess);
        Assert.Equal(("ChequeIssued", null), ((await View(id)).State, (await View(id)).Cheque!.ReceivedChequeId));
        Assert.Empty(await _instruments.ListReceivedAsync(InstrumentState.InCashDesk, CancellationToken.None));

        Assert.True((await _transfers.ReceiveChequeAsync(new(Key(), id, 3), CancellationToken.None)).IsSuccess);
        var cheque = (await View(id)).Cheque!.ReceivedChequeId!.Value;
        await Step(cheque, InstrumentAction.RegisterSayad, Today);
        Assert.False((await View(id)).CanUndoReceive);
        Assert.Equal("Transfer.ChequeMovedOn", (await _transfers.UndoReceiveAsync(new(Key(), id, 4), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Deleting_a_cheque_transfer_before_it_is_received_removes_the_issued_cheque_and_frees_its_sheet()
    {
        var book = await Book();
        var id = await Issue(Cheque(book));
        var issuedId = (await View(id)).Cheque!.IssuedChequeId;

        Assert.True((await _transfers.ReceiveChequeAsync(new(Key(), id, 1), CancellationToken.None)).IsSuccess);
        Assert.Equal("Transfer.ChequeAlreadyReceived", (await _transfers.DeleteAsync(new(Key(), id, 2), CancellationToken.None)).ErrorCode);
        Assert.True((await _transfers.UndoReceiveAsync(new(Key(), id, 2), CancellationToken.None)).IsSuccess);

        var deleted = await _transfers.DeleteAsync(new(Key(), id, 3), CancellationToken.None);
        Assert.True(deleted.IsSuccess, deleted.ErrorCode);
        Assert.Equal("Deleted", (await View(id)).State);
        Assert.Null(await _instruments.GetAsync(issuedId, CancellationToken.None));
        Assert.Equal((0L, 100500L), ((await BookView(book)).UsedSheets, (await BookView(book)).NextSheet!.Value));

        Assert.Equal("Transfer.Deleted", (await _transfers.ReceiveChequeAsync(new(Key(), id, 4), CancellationToken.None)).ErrorCode);

        // The same sheet is typed again, on the next number.
        var again = await View(await Issue(Cheque(book, amount: 6_500_000_000, sheet: 100500)));
        Assert.Equal((2, "100500", 6_500_000_000m), (again.Number, again.Cheque!.Number, again.Amount));
    }

    // ----- the owner's answers of 21 Nov 2026 --------------------------------------------------------------------------------------

    [Fact]
    public async Task A_transfer_needs_an_approved_payment_request_and_pays_it_Q82()
    {
        async Task<string?> Fails(Guid? request) => (await _transfers.RecordElectronicAsync(
            new(Key(), Paya(Milliard) with { PaymentRequestId = request ?? Guid.Empty }), CancellationToken.None)).ErrorCode;
        Assert.Equal("Transfer.RequestRequired", await Fails(null));
        Assert.Equal("Transfer.RequestNotFound", await Fails(Guid.NewGuid()));
        Assert.Equal("Transfer.RequestNotApproved", await Fails(RequestFor(Banks.MellatCurrent, Banks.MelliCurrent, state: "Registered")));
        Assert.Equal("Transfer.RequestNotForTransfer", await Fails(RequestFor(Banks.MellatCurrent, Banks.MelliCurrent, method: "Transfer")));
        Assert.Equal("Transfer.RequestOtherAccounts", await Fails(RequestFor(Banks.MelliCurrent, Banks.MellatCurrent)));
        Assert.Equal("Transfer.RequestOverpaid", await Fails(RequestFor(Banks.MellatCurrent, Banks.MelliCurrent, amount: Milliard - 1)));
        Assert.Empty(_requests.Payments);

        // Paid in two parts (Q29); the second transfer finishes the request.
        var request = RequestFor(Banks.MellatCurrent, Banks.MelliCurrent, amount: 3 * Milliard);
        var first = await Record(Paya(Milliard) with { PaymentRequestId = request });
        var view = await View(first);
        Assert.Equal((request, _requests.Requests[request].Number), (view.PaymentRequestId!.Value, view.PaymentRequestNumber!.Value));
        Assert.Equal((Milliard, "Approved"), (_requests.Requests[request].PaidAmount, _requests.Requests[request].State));
        var second = await Record(Paya(2 * Milliard) with { PaymentRequestId = request });
        Assert.Equal((3 * Milliard, "Paid"), (_requests.Requests[request].PaidAmount, _requests.Requests[request].State));
        Assert.Equal("Transfer.RequestNotApproved", await Fails(request)); // nothing is left to pay on it

        // A correction takes its own payment back first: 2 milliard → 1.5 milliard on the same, fully paid request.
        var corrected = await _transfers.CorrectElectronicAsync(
            new(Key(), second, 1, Paya(1_500_000_000) with { PaymentRequestId = request }), CancellationToken.None);
        Assert.True(corrected.IsSuccess, corrected.ErrorCode);
        Assert.Equal((2_500_000_000m, "Approved", 2), (_requests.Requests[request].PaidAmount, _requests.Requests[request].State, _requests.Payments.Count));
        Assert.Equal("Transfer.RequestOverpaid",
            (await _transfers.CorrectElectronicAsync(new(Key(), second, 2, Paya(2 * Milliard + 1) with { PaymentRequestId = request }), CancellationToken.None)).ErrorCode);

        // A deleted transfer gives its payment back to the request.
        Assert.True((await _transfers.DeleteAsync(new(Key(), first, 1), CancellationToken.None)).IsSuccess);
        Assert.Equal((1_500_000_000m, 1), (_requests.Requests[request].PaidAmount, _requests.Payments.Count));
    }

    [Fact]
    public async Task The_bank_fee_is_part_of_the_voucher_of_an_electronic_transfer_Q77()
    {
        var id = await Record(Paya(Milliard) with { Fee = 35_000 });
        Assert.Equal(35_000m, (await View(id)).Fee);
        var voucher = Assert.Single(await _transfers.VouchersAsync(id, CancellationToken.None));
        Assert.Equal([(true, "1103", Milliard), (false, "1102", Milliard), (true, "8205", 35_000m), (false, "1102", 35_000m)],
            voucher.Lines.Select(l => (l.IsDebit, l.AccountCode, l.AmountRial)));
        Assert.Equal("Transfer.FeeInvalid", await Refused(Paya(Milliard) with { Fee = -5 }));
        Assert.Null((await View(await Record(Paya(Milliard)))).Fee);
    }

    [Fact]
    public async Task A_post_dated_cheque_leaves_notes_payable_at_its_maturity_or_at_the_latest_when_it_is_collected_Q81()
    {
        var clock = new FixedClock(new DateTimeOffset(2027, 1, 6, 8, 0, 0, TimeSpan.Zero)); // 1405/10/16 in Iran
        var transfers = new TransferUseCases(new InMemoryUnitOfWork(_db), new InMemoryBankTransferRepository(_db), new InMemoryTransferLimitRepository(_db),
            new InMemoryInstrumentRepository(_db), new InMemoryChequeBookRepository(_db), _banks, _reference, new DevelopmentPolicies().OpenYear(1405),
            new InMemoryVoucherTemplateRepository(_store), new InMemoryConfirmedVoucherRepository(_store), new InMemoryIdempotencyStore(_db), new Treasurer(), _requests, clock);
        var book = await Book();

        // Recorded on the payment side («سررسید»), from the due date on.
        var id = await Issue(Cheque(book)); // due 1405/10/15
        var before = (await transfers.GetAsync(id, CancellationToken.None))!;
        Assert.Equal((true, null, true), (before.Cheque!.IsPostDated, before.Cheque.MaturedOn, before.Cheque.CanMature));
        Assert.Equal("Transfer.MaturedBeforeDueDate", (await transfers.MatureChequeAsync(new(Key(), id, 1, "1405/10/14"), CancellationToken.None)).ErrorCode);
        Assert.True((await transfers.MatureChequeAsync(new(Key(), id, 1, "1405/10/15"), CancellationToken.None)).IsSuccess);
        var after = (await transfers.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(("1405/10/15", false), (after.Cheque!.MaturedOn, after.Cheque.CanMature));
        var vouchers = await transfers.VouchersAsync(id, CancellationToken.None);
        Assert.Equal([0, 2], vouchers.Select(v => v.Step));
        Assert.Equal([(true, "2105", "600001"), (false, "1102", "600001")], vouchers[1].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal("سررسید چک پرداختنی – نقل و انتقال – شماره 1", vouchers[1].Operation);
        Assert.True((await transfers.ConfirmVoucherAsync(Key(), id, 2, CancellationToken.None)).IsSuccess);
        Assert.Equal([2], (await transfers.GetAsync(id, CancellationToken.None))!.ConfirmedSteps!);
        var issued = (await _instruments.GetAsync(after.Cheque.IssuedChequeId, CancellationToken.None))!;
        Assert.Equal("RecordMatured", issued.History[^1].Action);
        Assert.Equal("Transfer.AlreadyMatured", (await transfers.MatureChequeAsync(new(Key(), id, 2, "1405/10/16"), CancellationToken.None)).ErrorCode);

        // Not recorded on the payment side: the collection on the receipts side records it, in the same command.
        var late = await Issue(Cheque(book));
        Assert.True((await _transfers.ReceiveChequeAsync(new(Key(), late, 1), CancellationToken.None)).IsSuccess);
        var cheque = (await View(late)).Cheque!.ReceivedChequeId!.Value;
        await Step(cheque, InstrumentAction.RegisterSayad, Today);
        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), cheque, Banks.MelliCurrent, Today), CancellationToken.None)).IsSuccess);
        await Step(cheque, InstrumentAction.RecordCleared, "1405/10/17");
        var done = await View(late);
        Assert.Equal(("Collected", "1405/10/17", "1405/10/17"), (done.State, done.Cheque!.MaturedOn, done.Cheque.CollectedOn));
        Assert.Equal(["PrepareIssue", "Issue", "RecordMatured", "RecordPaidByBank"],
            (await _instruments.GetAsync(done.Cheque.IssuedChequeId, CancellationToken.None))!.History.Select(h => h.Action));
        Assert.Equal([0, 1, 2], (await _transfers.VouchersAsync(late, CancellationToken.None)).Select(v => v.Step));
    }

    [Fact]
    public async Task The_receipts_side_waits_for_the_payers_sayad_registration_Q79()
    {
        var id = await Issue(Cheque(await Book()) with { SayadRegistered = false });
        Assert.Null((await View(id)).Cheque!.PayerSayadOn);
        Assert.True((await _transfers.ReceiveChequeAsync(new(Key(), id, 1), CancellationToken.None)).IsSuccess); // «نزد صندوق» at once
        var cheque = (await View(id)).Cheque!.ReceivedChequeId!.Value;
        Assert.Equal("Transfer.PayerSayadRequired",
            (await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RegisterSayad, Today), CancellationToken.None)).ErrorCode);

        Assert.True((await _transfers.RegisterPayerSayadAsync(new(Key(), id, 2, Today), CancellationToken.None)).IsSuccess);
        Assert.Equal((Today, "Transfer.SayadRegisteredNotCancelled"), ((await View(id)).Cheque!.PayerSayadOn, (await View(id)).Cheque!.WhyNotCancellable));
        Assert.Equal("Transfer.PayerSayadAlreadyRegistered", (await _transfers.RegisterPayerSayadAsync(new(Key(), id, 3, Today), CancellationToken.None)).ErrorCode);
        await Step(cheque, InstrumentAction.RegisterSayad, Today);
        Assert.Equal("SayadRegistered", (await View(id)).Cheque!.ReceivedState);
        Assert.Equal("Transfer.SayadRegisteredNotCancelled", (await _transfers.CancelChequeAsync(new(Key(), id, 3, Today, "انصراف"), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Cancelling_a_cheque_not_registered_in_sayad_voids_its_sheet_and_takes_back_both_sides_and_the_payment_Q79()
    {
        var book = await Book();
        var request = RequestFor(Banks.MellatCurrent, Banks.MelliCurrent, amount: 7_000_000_000);
        var id = await Issue(Cheque(book) with { SayadRegistered = false, PaymentRequestId = request });
        Assert.Equal("Paid", _requests.Requests[request].State);
        Assert.True((await _transfers.ReceiveChequeAsync(new(Key(), id, 1), CancellationToken.None)).IsSuccess);
        var open = await View(id);
        Assert.Null(open.Cheque!.WhyNotCancellable);

        Assert.Equal("Transfer.CancelReasonRequired", (await _transfers.CancelChequeAsync(new(Key(), id, 2, Today, ""), CancellationToken.None)).ErrorCode);
        var cancelled = await _transfers.CancelChequeAsync(new(Key(), id, 2, Today, "انصراف از انتقال"), CancellationToken.None);
        Assert.True(cancelled.IsSuccess, cancelled.ErrorCode);

        var view = await View(id);
        Assert.Equal(("Cancelled", Today, "انصراف از انتقال", "Transfer.ChequeCancelled"), (view.State, view.Cheque!.CancelledOn, view.Cheque.CancelReason, view.WhyNotDeletable));
        Assert.Equal("Cancelled", (await _instruments.GetAsync(view.Cheque.IssuedChequeId, CancellationToken.None))!.State);
        var received = (await _instruments.GetAsync(view.Cheque.ReceivedChequeId!.Value, CancellationToken.None))!;
        Assert.Equal(("Cancelled", 0), (received.State, received.AllowedActions.Count)); // no «تعویض»
        Assert.Empty(await _instruments.ListReceivedAsync(InstrumentState.InCashDesk, CancellationToken.None));

        // The sheet is void — never offered again — and the request is to be paid again.
        Assert.Equal((1L, 100501L), ((await BookView(book)).UsedSheets, (await BookView(book)).NextSheet!.Value));
        Assert.Equal("ChequeBook.SheetAlreadyUsed",
            (await _transfers.IssueChequeAsync(new(Key(), C(Cheque(book, sheet: 100500))), CancellationToken.None)).ErrorCode);
        Assert.Equal(("Approved", 0m), (_requests.Requests[request].State, _requests.Requests[request].PaidAmount));

        // Four vouchers: the two of the transfer and the two that take them back.
        var vouchers = await _transfers.VouchersAsync(id, CancellationToken.None);
        Assert.Equal([0, 1, 3, 4], vouchers.Select(v => v.Step));
        Assert.Equal([(true, "2105", "600001"), (false, "1109", null)], vouchers[2].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal([(true, "1109", null), (false, "1201", "600001")], vouchers[3].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.True((await _transfers.ConfirmVoucherAsync(Key(), id, 3, CancellationToken.None)).IsSuccess);

        // A new transfer by cheque is a new, independent cheque on the next sheet.
        var again = await View(await Issue(Cheque(book) with { PaymentRequestId = request }));
        Assert.Equal(("100501", "ChequeIssued"), (again.Cheque!.Number, again.State));
    }

    [Fact]
    public async Task The_fee_asks_for_a_detail_when_the_companys_fee_account_takes_one_Q83()
    {
        // The account has no تفصیل: nothing is asked.
        Assert.Equal(("8205", 0), ((await _transfers.FeeAccountAsync(CancellationToken.None)).AccountCode, (await _transfers.FeeAccountAsync(CancellationToken.None)).DetailTypes.Count));
        Assert.Null((await View(await Record(Paya(Milliard) with { Fee = 35_000, FeeDetailCode = "810001" }))).FeeDetailCode);

        // The accounting system gives the account the attribute "with تفصیل" (a cost centre): now the fee line needs one.
        _reference.FeeAccountDetailTypes = ["مراکز هزینه"];
        Assert.Equal(["مراکز هزینه"], (await _transfers.FeeAccountAsync(CancellationToken.None)).DetailTypes);
        Assert.Equal("Transfer.FeeDetailRequired", await Refused(Paya(Milliard) with { Fee = 35_000 }));
        Assert.Equal("Transfer.FeeDetailNotFound", await Refused(Paya(Milliard) with { Fee = 35_000, FeeDetailCode = "999999" }));
        Assert.Equal("Transfer.FeeDetailInactive", await Refused(Paya(Milliard) with { Fee = 35_000, FeeDetailCode = "810002" }));
        Assert.Equal("Transfer.FeeDetailTypeNotAllowed", await Refused(Paya(Milliard) with { Fee = 35_000, FeeDetailCode = "200015" }));
        var id = await Record(Paya(Milliard) with { Fee = 35_000, FeeDetailCode = "810001" });
        Assert.Equal("810001", (await View(id)).FeeDetailCode);
        var voucher = Assert.Single(await _transfers.VouchersAsync(id, CancellationToken.None));
        Assert.Equal((true, "8205", "810001", 35_000m), voucher.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode, l.AmountRial)).ElementAt(2));
        await Record(Paya(Milliard)); // without a fee no تفصیل is asked
    }

    [Fact]
    public async Task The_orders_of_the_day_and_what_remains_on_the_request_are_counted_Q84()
    {
        // «اگر سقف مبلغ درخواستی طی چند مرحله تکمیل شود لازم است در طی روز تعدد پرداخت و مانده پرداخت نشده درخواست در سیستم کنترل شود»
        var request = RequestFor(Banks.MellatCurrent, Banks.MelliCurrent, amount: 14 * Milliard);
        Task<TransferDayUsageView> Usage(TransferChannel channel = TransferChannel.Remote) =>
            _transfers.DayUsageAsync(Banks.MellatCurrent, Today, TransferMethod.Paya, channel, null, CancellationToken.None);
        Assert.Equal(new TransferDayUsageView(0, 0), await Usage());

        for (var i = 1; i <= 4; i++) // 12 milliard in four Paya orders of 3 milliard, the most one remote order carries
        {
            await Record(Paya(3 * Milliard) with { PaymentRequestId = request });
            Assert.Equal((i * 3 * Milliard, i, (14 - (i * 3)) * Milliard),
                ((await Usage()).TotalRialSameChannel, (await Usage()).Count, _requests.Requests[request].Amount - _requests.Requests[request].PaidAmount));
        }

        Assert.Equal(new TransferDayUsageView(0, 4), await Usage(TransferChannel.InPerson)); // the count is over both channels
        Assert.Equal("Transfer.RequestOverpaid", await Refused(Paya(3 * Milliard) with { PaymentRequestId = request })); // only 2 milliard remain
        await Record(Paya(2 * Milliard) with { PaymentRequestId = request });                                              // the fifth order of the day
        Assert.Equal("Paid", _requests.Requests[request].State);
        Assert.Equal("Transfer.OverDailyCount", await Refused(Paya(Milliard)));                                           // a sixth is refused
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>accounts-svc stand-in: three banks, current accounts with cheque books, a savings account, a closed one.</summary>
    private sealed class Banks : IBankAccountLookup
    {
        public static readonly Guid Saderat = Guid.NewGuid();
        public static readonly Guid MellatCurrent = Guid.NewGuid();
        public static readonly Guid MellatSaving = Guid.NewGuid();
        public static readonly Guid MelliCurrent = Guid.NewGuid();
        public static readonly Guid MelliClosed = Guid.NewGuid();
        public static readonly Guid SaderatCurrent = Guid.NewGuid();

        private static readonly Dictionary<Guid, BankAccountFact> Accounts = new()
        {
            [MellatCurrent] = new(MellatCurrent, "0101-1", true, true, "IRR", "1102", "600001", FakeReceiptReference.Mellat, "مرکزی", "6501"),
            [MellatSaving] = new(MellatSaving, "0101-2", true, false, "IRR", "1102", "600002", FakeReceiptReference.Mellat),
            [MelliCurrent] = new(MelliCurrent, "0202-1", true, true, "IRR", "1103", "600003", FakeReceiptReference.Melli),
            [MelliClosed] = new(MelliClosed, "0202-9", false, true, "IRR", "1103", "600009", FakeReceiptReference.Melli),
            [SaderatCurrent] = new(SaderatCurrent, "0303-1", true, true, "IRR", "1105", "600005", Saderat),
        };

        public HashSet<Guid> InUse { get; } = [];

        public Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Accounts.GetValueOrDefault(id));

        public Task MarkInUseAsync(Guid id, CancellationToken cancellationToken)
        {
            InUse.Add(id);
            return Task.CompletedTask;
        }
    }

    private sealed class Treasurer : ICurrentUser
    {
        public string UserId => "treasurer";
    }
}
