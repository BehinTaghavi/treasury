using Npgsql;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.ChequeBooks;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Application.Transfers;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Transfers;
using Treasury.Cashflow.Domain.Vouchers;
using Treasury.Cashflow.Infrastructure.InMemory;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Cashflow.Infrastructure.Postgres.Tests;

/// <summary>
/// Transfers between the company's own bank accounts through the real PostgreSQL adapters (db/postgres/046, owner's report no. 7):
/// numbering, the limits table, the day's usage, deletion that keeps the row, and a cheque transfer from its sheet to its collection.
/// </summary>
public sealed class TransferPostgresTests : IDisposable
{
    private const decimal Milliard = 1_000_000_000m;
    private const string Today = "1405/08/29";

    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly TransferUseCases _transfers;
    private readonly InstrumentUseCases _instruments;
    private readonly ChequeBookUseCases _books;

    // Fresh accounts and banks for every test: the day's usage and the bank's own limits of one test never meet another's.
    private readonly Guid _mellat = Guid.NewGuid();
    private readonly Guid _melli = Guid.NewGuid();
    private readonly Guid _source = Guid.NewGuid();
    private readonly Guid _destination = Guid.NewGuid();

    public TransferPostgresTests(PostgresDatabase db)
    {
        db.Require();
        _db = db;
        _dataSource = NpgsqlDataSource.Create(db.Service);
        var uow = new PostgresUnitOfWork(_dataSource);
        var policies = new DevelopmentPolicies().OpenYear(1405);
        var accounts = new Accounts(new()
        {
            [_source] = new(_source, "0101-1", true, true, "IRR", "1102", "600001", _mellat, "مرکزی", "6501"),
            [_destination] = new(_destination, "0202-1", true, true, "IRR", "1103", "600003", _melli),
        });
        _transfers = new TransferUseCases(uow, new PostgresBankTransferRepository(), new PostgresTransferLimitRepository(), new PostgresInstrumentRepository(),
            new PostgresChequeBookRepository(), accounts, new Reference(), policies, new PostgresVoucherTemplateRepository(), new PostgresConfirmedVoucherRepository(),
            new PostgresIdempotencyStore(_dataSource), new User());
        _instruments = new InstrumentUseCases(uow, new PostgresInstrumentRepository(), new PostgresChequeBookRepository(), new PostgresIdempotencyStore(_dataSource),
            policies, policies, policies, new User(), accounts, transfers: new PostgresBankTransferRepository());
        _books = new ChequeBookUseCases(uow, new PostgresChequeBookRepository(), accounts, new PostgresIdempotencyStore(_dataSource), new PostgresChequeFormatRepository());
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private ElectronicTransferInput Pol(decimal amount, TransferChannel channel = TransferChannel.Remote) =>
        new(Today, _source, _destination, TransferMethod.Pol, channel, amount, null, "TRK-1", "تأمین موجودی");

    private async Task<TransferView> View(Guid id) => (await _transfers.GetAsync(id, CancellationToken.None))!;

    [Fact]
    public async Task The_limits_table_starts_with_the_owners_figures_and_keeps_a_banks_own_row()
    {
        var general = (await _transfers.GetLimitsAsync(CancellationToken.None)).Where(r => r.BankId is null).ToList();
        Assert.Equal(["Internal", "Pol", "Paya", "Satna"], general.Select(r => r.Method));
        Assert.Equal((3 * Milliard, 2 * Milliard, 5), (general[1].MaxPerDayInPerson, general[1].MaxPerDayRemote, general[1].MaxCountPerDay));
        Assert.Equal((5 * Milliard, 3 * Milliard, 5), (general[2].MaxPerOrderInPerson, general[2].MaxPerOrderRemote, general[2].MaxCountPerDay));
        Assert.Equal(2 * Milliard, general[3].MinPerOrder);

        var set = await _transfers.SetLimitAsync(new(Key(), TransferMethod.Pol, _mellat, null, null, 500_000_000, null, 900_000_000, 3), CancellationToken.None);
        Assert.True(set.IsSuccess, set.ErrorCode);
        var own = Assert.Single(await _transfers.GetLimitsAsync(CancellationToken.None), r => r.BankId == _mellat);
        Assert.Equal(("Pol", 500_000_000m, 900_000_000m, 3, null), (own.Method, own.MaxPerOrderRemote, own.MaxPerDayRemote, own.MaxCountPerDay, own.MinPerOrder));
        Assert.Equal("Transfer.OverOrderLimit", (await _transfers.RecordElectronicAsync(new(Key(), Pol(600_000_000)), CancellationToken.None)).ErrorCode);

        Assert.True((await _transfers.RemoveBankLimitAsync(Key(), TransferMethod.Pol, _mellat, CancellationToken.None)).IsSuccess);
        Assert.DoesNotContain(await _transfers.GetLimitsAsync(CancellationToken.None), r => r.BankId == _mellat);
        Assert.Equal(4L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.transfer_limit WHERE bank_key = ''")); // the general rows always stay
    }

    [Fact]
    public async Task Electronic_transfers_get_consecutive_numbers_round_trip_and_count_against_the_day()
    {
        var a = await _transfers.RecordElectronicAsync(new(Key(), Pol(1_500_000_000)), CancellationToken.None);
        var b = await _transfers.RecordElectronicAsync(new(Key(), Pol(400_000_000, TransferChannel.InPerson)), CancellationToken.None);
        Assert.True(a.IsSuccess, a.ErrorCode);
        Assert.True(b.IsSuccess, b.ErrorCode);
        var va = await View(a.Value);
        Assert.Equal(va.Number + 1, (await View(b.Value)).Number);
        Assert.Equal(("Pol", "Remote", "Recorded", Today, _source, "0101-1", "بانک نمونه", _destination, 1_500_000_000m, "IRR", "TRK-1", "تأمین موجودی", "tester", 1L),
            (va.Method, va.Channel, va.State, va.Date, va.SourceAccountId, va.SourceAccountNumber, va.SourceBankName, va.DestinationAccountId, va.AmountRial, va.Currency,
                va.TrackingNumber, va.Description, va.RecordedBy, va.Version));
        Assert.Contains(await _transfers.ListAsync(1405, cheque: false, CancellationToken.None), t => t.Id == a.Value);

        // Pol: the remote orders of the day carry at most 2 milliard together (1.5 is already sent).
        Assert.Equal("Transfer.OverDailyLimit", (await _transfers.RecordElectronicAsync(new(Key(), Pol(600_000_000)), CancellationToken.None)).ErrorCode);

        // Correction (not counted against itself), then the voucher, then nothing more.
        var corrected = await _transfers.CorrectElectronicAsync(new(Key(), a.Value, 1, Pol(2 * Milliard)), CancellationToken.None);
        Assert.True(corrected.IsSuccess, corrected.ErrorCode);
        Assert.Equal((2 * Milliard, 2L), ((await View(a.Value)).Amount, (await View(a.Value)).Version));
        Assert.Equal("Concurrency.Conflict", (await _transfers.CorrectElectronicAsync(new(Key(), a.Value, 1, Pol(Milliard)), CancellationToken.None)).ErrorCode);
        var voucher = Assert.Single(await _transfers.VouchersAsync(a.Value, CancellationToken.None));
        Assert.Equal([(true, "1103", "600003"), (false, "1102", "600001")], voucher.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.True((await _transfers.ConfirmVoucherAsync(Key(), a.Value, 0, CancellationToken.None)).IsSuccess);
        Assert.Equal("Voucher.AlreadyConfirmed", (await _transfers.ConfirmVoucherAsync(Key(), a.Value, 0, CancellationToken.None)).ErrorCode);
        Assert.Equal("Transfer.VoucherConfirmed", (await _transfers.DeleteAsync(new(Key(), a.Value, 2), CancellationToken.None)).ErrorCode);

        // A deleted transfer keeps its row (state Deleted) and no longer counts against the day.
        Assert.True((await _transfers.DeleteAsync(new(Key(), b.Value, 1), CancellationToken.None)).IsSuccess);
        var deleted = await View(b.Value);
        Assert.Equal(("Deleted", "tester"), (deleted.State, deleted.DeletedBy));
        Assert.NotNull(deleted.DeletedAt);
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.bank_transfer WHERE id = @id AND state = 'Deleted'", ("id", b.Value)));
        Assert.True((await _transfers.RecordElectronicAsync(new(Key(), Pol(3 * Milliard, TransferChannel.InPerson)), CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task A_cheque_transfer_is_stored_from_its_sheet_to_its_collection()
    {
        // Only «وجوه در راه» is set here: other tests of this database set and clear the other template accounts.
        await _db.ExecuteAsync(
            "INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('FundsInTransit', '1109') ON CONFLICT (account_key) DO NOTHING");
        var book = await _books.RegisterAsync(new(Key(), _source, "1234", 100500, 100524, null, "12345678", "7"), CancellationToken.None);
        Assert.True(book.IsSuccess, book.ErrorCode);
        var issuedTransfer = await _transfers.IssueChequeAsync(
            new(Key(), new TransferChequeInput(Today, _source, _destination, book.Value, null, "4821", 7_000_000_000, Today, "تأمین موجودی")), CancellationToken.None);
        Assert.True(issuedTransfer.IsSuccess, issuedTransfer.ErrorCode);
        var id = issuedTransfer.Value;

        var view = await View(id);
        Assert.Equal(("Cheque", null, "ChequeIssued", "100500", "4821123456780007", Today), (view.Method, view.Channel, view.State, view.Cheque!.Number, view.Cheque.SayadId, view.Cheque.DueDate));
        Assert.Equal("Issued", await _db.ScalarAsync<string>("SELECT sheet_use FROM cashflow.cheque_sheet WHERE cheque_book_id = @b AND sheet = 100500", ("b", book.Value)));
        var payer = Assert.Single(await _transfers.VouchersAsync(id, CancellationToken.None));
        Assert.Equal([(true, "1109", null), (false, "1102", "600001")], payer.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.True((await _transfers.ConfirmVoucherAsync(Key(), id, 0, CancellationToken.None)).IsSuccess); // source 'Transfer' (schema 046)

        // The button: the same cheque on the receipts side, with its transfer written in its details.
        Assert.True((await _transfers.ReceiveChequeAsync(new(Key(), id, 1), CancellationToken.None)).IsSuccess);
        view = await View(id);
        var chequeId = view.Cheque!.ReceivedChequeId!.Value;
        var cheque = (await _instruments.GetAsync(chequeId, CancellationToken.None))!;
        Assert.Equal(("ChequeReceived", "InCashDesk", id, _destination, "600001"),
            (view.State, cheque.State, cheque.Details!.TransferId!.Value, cheque.Details.TransferAccountId!.Value, cheque.CounterpartyDetailCode));
        Assert.Equal(["RegisterSayad"], cheque.AllowedActions);
        var both = await _transfers.VouchersAsync(id, CancellationToken.None);
        Assert.Equal([(0, "tester"), (1, null)], both.Select(v => (v.Step, v.ConfirmedBy)));
        Assert.Equal([(true, "600001"), (false, null)], both[1].Lines.Select(l => (l.IsDebit, l.DetailCode))); // Dr «اسناد نزد صندوق» · source account / Cr «وجوه در راه»

        Assert.True((await _instruments.ChangeStateAsync(new(Key(), chequeId, InstrumentAction.RegisterSayad, Today), CancellationToken.None)).IsSuccess);
        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), chequeId, _destination, Today, "F-1"), CancellationToken.None)).IsSuccess);
        var cleared = await _instruments.ChangeStateAsync(new(Key(), chequeId, InstrumentAction.RecordCleared, "1405/08/30"), CancellationToken.None);
        Assert.True(cleared.IsSuccess, cleared.ErrorCode);

        view = await View(id);
        Assert.Equal(("Collected", "1405/08/30", "Cleared"), (view.State, view.Cheque!.CollectedOn, view.Cheque.ReceivedState));
        var issued = (await _instruments.GetAsync(view.Cheque.IssuedChequeId, CancellationToken.None))!;
        Assert.Equal(("PaidByBank", 3), (issued.State, issued.History.Count));
    }

    [Fact]
    public async Task Deleting_a_cheque_transfer_frees_its_sheet_and_removes_the_issued_cheque_but_never_the_transfers_row()
    {
        var book = (await _books.RegisterAsync(new(Key(), _source, "1234", 100600, 100624, null, "12345678", "7"), CancellationToken.None)).Value;
        var id = (await _transfers.IssueChequeAsync(
            new(Key(), new TransferChequeInput(Today, _source, _destination, book, 100603, "4821", 1_000_000, "1405/10/15")), CancellationToken.None)).Value;
        var issuedId = (await View(id)).Cheque!.IssuedChequeId;

        // Taken to the receipts side and back: the received cheque's row is gone again.
        Assert.True((await _transfers.ReceiveChequeAsync(new(Key(), id, 1), CancellationToken.None)).IsSuccess);
        var receivedId = (await View(id)).Cheque!.ReceivedChequeId!.Value;
        Assert.True((await _transfers.UndoReceiveAsync(new(Key(), id, 2), CancellationToken.None)).IsSuccess);
        Assert.Null(await _instruments.GetAsync(receivedId, CancellationToken.None));

        var deleted = await _transfers.DeleteAsync(new(Key(), id, 3), CancellationToken.None);
        Assert.True(deleted.IsSuccess, deleted.ErrorCode);
        Assert.Equal("Deleted", (await View(id)).State);
        Assert.Null(await _instruments.GetAsync(issuedId, CancellationToken.None));
        Assert.Equal(0L, await _db.ScalarAsync<long>("SELECT count(*) FROM cashflow.cheque_sheet WHERE cheque_book_id = @b", ("b", book)));
        var books = await _books.ListAsync(_source, true, CancellationToken.None);
        Assert.Equal(0L, books.Single(b => b.Id == book).UsedSheets);

        // The service role may not delete the row of a transfer.
        await using var connection = await _dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var cmd = new NpgsqlCommand("DELETE FROM cashflow.bank_transfer WHERE id = @id", connection);
        cmd.Parameters.AddWithValue("id", id);
        var refused = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync(CancellationToken.None));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, refused.SqlState);
    }

    private sealed class Accounts(Dictionary<Guid, BankAccountFact> accounts) : IBankAccountLookup
    {
        public Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(accounts.GetValueOrDefault(id));

        public Task MarkInUseAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>reference-svc stand-in: every bank is known by one name; nothing else is asked by transfers.</summary>
    private sealed class Reference : IReceiptReferenceLookup
    {
        public Task<string?> BankNameAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<string?>("بانک نمونه");

        public Task<PayerFact?> PayerAsync(string detailCode, CancellationToken cancellationToken) => Task.FromResult<PayerFact?>(null);

        public Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<PurposeGroupFact?>(null);

        public Task<LedgerAccountFact?> LedgerAccountAsync(string code, CancellationToken cancellationToken) => Task.FromResult<LedgerAccountFact?>(null);

        public Task<NotePartyFact?> NotePartyAsync(string detailCode, CancellationToken cancellationToken) => Task.FromResult<NotePartyFact?>(null);

        public Task<SignatureRuleFact?> SignatureRuleAsync(string document, CancellationToken cancellationToken) => Task.FromResult<SignatureRuleFact?>(null);

        public Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string?> CompanyCityAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class User : ICurrentUser
    {
        public string UserId => "tester";
    }
}
