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
            new InMemoryConfirmedVoucherRepository(_store), new InMemoryIdempotencyStore(_db), new Treasurer());
        _instruments = new InstrumentUseCases(uow, new InMemoryInstrumentRepository(_db), new InMemoryChequeBookRepository(_db), new InMemoryIdempotencyStore(_db),
            policies, policies, policies, new Treasurer(), _banks, transfers: transfers);
        _books = new ChequeBookUseCases(uow, new InMemoryChequeBookRepository(_db), _banks, new InMemoryIdempotencyStore(_db), new InMemoryChequeFormatRepository(_db));
        new InMemoryVoucherTemplateRepository(_store).SaveAsync(new VoucherTemplate(new Dictionary<FixedAccount, string>
        {
            [FixedAccount.ChequesInCashDesk] = "1201", [FixedAccount.ChequesInCollection] = "1202", [FixedAccount.FundsInTransit] = "1109",
        }), CancellationToken.None).GetAwaiter().GetResult();
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static ElectronicTransferInput Paya(decimal amount, TransferChannel channel = TransferChannel.Remote, Guid? source = null, string date = Today) =>
        new(date, source ?? Banks.MellatCurrent, Banks.MelliCurrent, TransferMethod.Paya, channel, amount, null, "TRK-9", "تأمین موجودی");

    private async Task<Guid> Record(ElectronicTransferInput form)
    {
        var r = await _transfers.RecordElectronicAsync(new(Key(), form), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<string?> Refused(ElectronicTransferInput form) => (await _transfers.RecordElectronicAsync(new(Key(), form), CancellationToken.None)).ErrorCode;

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
        var command = new RecordElectronicTransferCommand(Key(), Paya(Milliard));
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
        var preview = await _transfers.PreviewElectronicAsync(Paya(Milliard), null, CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.ErrorCode);
        Assert.Equal("نقل و انتقال بانکی (پایا)", preview.Value!.Operation);
        Assert.Equal("Transfer.OverOrderLimit", (await _transfers.PreviewElectronicAsync(Paya(4 * Milliard), null, CancellationToken.None)).ErrorCode);
        Assert.Empty(await _transfers.ListAsync(1405, null, CancellationToken.None)); // a preview saves nothing and takes no number

        var id = await Record(Paya(Milliard));
        var voucher = Assert.Single(await _transfers.VouchersAsync(id, CancellationToken.None));
        Assert.Equal([(true, "1103", "600003"), (false, "1102", "600001")], voucher.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal(("Transfer", 0, true, null), (voucher.Source, voucher.Step, voucher.IsComplete, voucher.ConfirmedBy));

        // Correction before «تایید سند»: the transfer itself is not counted against the day's limits.
        var corrected = await _transfers.CorrectElectronicAsync(new(Key(), id, (await View(id)).Version, Paya(3 * Milliard, TransferChannel.InPerson)), CancellationToken.None);
        Assert.True(corrected.IsSuccess, corrected.ErrorCode);
        Assert.Equal((3 * Milliard, "InPerson", 1), ((await View(id)).Amount, (await View(id)).Channel, (await View(id)).Number));

        Assert.True((await _transfers.ConfirmVoucherAsync(Key(), id, 0, CancellationToken.None)).IsSuccess);
        Assert.Equal("Voucher.AlreadyConfirmed", (await _transfers.ConfirmVoucherAsync(Key(), id, 0, CancellationToken.None)).ErrorCode);
        Assert.Equal("Voucher.NoVoucherForStep", (await _transfers.ConfirmVoucherAsync(Key(), id, 1, CancellationToken.None)).ErrorCode);
        var view = await View(id);
        Assert.Equal((true, "Transfer.VoucherConfirmed", "Transfer.VoucherConfirmed"), (view.PayerVoucherConfirmed, view.WhyNotCorrectable, view.WhyNotDeletable));
        Assert.Equal("treasurer", (await _transfers.VouchersAsync(id, CancellationToken.None))[0].ConfirmedBy);
        Assert.Equal("Transfer.VoucherConfirmed", (await _transfers.CorrectElectronicAsync(new(Key(), id, view.Version, Paya(Milliard)), CancellationToken.None)).ErrorCode);
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
        var r = await _transfers.IssueChequeAsync(new(Key(), form), CancellationToken.None);
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

        // The payer's voucher, as the owner wrote it: Dr «وجوه در راه» / Cr the source bank.
        var voucher = Assert.Single(await _transfers.VouchersAsync(id, CancellationToken.None));
        Assert.Equal([(true, "1109", null), (false, "1102", "600001")], voucher.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal("پرداخت چک – نقل و انتقال (انتقال دهنده) – شماره 1", voucher.Operation);
        Assert.Single(await _transfers.ListAsync(1405, cheque: true, CancellationToken.None));
    }

    [Fact]
    public async Task A_cheque_is_issued_only_from_a_book_of_the_source_account_and_a_preview_takes_no_sheet()
    {
        var book = await Book();
        var other = await Book(Banks.SaderatCurrent);
        async Task<string?> Fails(TransferChequeInput form) => (await _transfers.IssueChequeAsync(new(Key(), form), CancellationToken.None)).ErrorCode;

        Assert.Equal("ChequeBook.WrongAccount", await Fails(Cheque(other)));
        Assert.Equal("ChequeBook.NotFound", await Fails(Cheque(Guid.NewGuid())));
        Assert.Equal("ChequeBook.SheetOutOfRange", await Fails(Cheque(book, sheet: 100999)));
        Assert.Equal("ChequeBook.SayadPrefixInvalid", await Fails(Cheque(book) with { SayadPrefix = "48" }));
        Assert.Equal("Instrument.DueBeforeIssue", await Fails(Cheque(book, due: "1405/08/28")));
        Assert.Equal("Instrument.DueDateRequired", await Fails(Cheque(book) with { DueDate = "" }));
        Assert.Equal("Transfer.SameAccount", await Fails(Cheque(book) with { DestinationAccountId = Banks.MellatCurrent }));

        var preview = await _transfers.PreviewChequeAsync(Cheque(book), CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.ErrorCode);
        Assert.Equal([(true, "1109"), (false, "1102")], preview.Value!.Lines.Select(l => (l.IsDebit, l.AccountCode)));
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
