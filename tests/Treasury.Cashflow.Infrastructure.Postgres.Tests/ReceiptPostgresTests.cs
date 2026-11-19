using Npgsql;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Application.Receipts;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Infrastructure.InMemory;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Cashflow.Infrastructure.Postgres.Tests;

/// <summary>Receipts through the real PostgreSQL adapters (db/postgres/025): numbering, cheques with their details, cancellation.</summary>
public sealed class ReceiptPostgresTests : IDisposable
{
    private static readonly Guid Group = Guid.NewGuid();
    private static readonly Guid Bank = Guid.NewGuid();
    private readonly NpgsqlDataSource _dataSource;
    private readonly ReceiptUseCases _receipts;
    private readonly Guid _desk = Guid.NewGuid();

    public ReceiptPostgresTests(PostgresDatabase db)
    {
        db.Require();
        _dataSource = NpgsqlDataSource.Create(db.Service);
        _receipts = new ReceiptUseCases(
            new PostgresUnitOfWork(_dataSource), new PostgresReceiptRepository(), new PostgresInstrumentRepository(), new Accounts(), new BankAccounts(),
            new Reference(), new DevelopmentPolicies().OpenYear(1405), new PostgresIdempotencyStore(_dataSource), new User(),
            new PostgresVoucherTemplateRepository(), new PostgresConfirmedVoucherRepository(), _requests);
    }

    private readonly Requests _requests = new();

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static readonly NoteChecks AllChecked = new(true, true, true, true, true);

    private static ReceiptHeaderInput Header() => new("1405/08/23", "200015", Group, "فروش محصولات", "ف-۱", null, null, true, "یادداشت");

    [Fact]
    public async Task Cash_receipts_get_consecutive_numbers_and_round_trip()
    {
        var a = await _receipts.RecordCashAsync(new(Key(), Header(), _desk, 5_000_000), CancellationToken.None);
        var b = await _receipts.RecordCashAsync(new(Key(), Header(), _desk, 6_000_000), CancellationToken.None);
        Assert.True(a.IsSuccess, a.ErrorCode);
        var va = (await _receipts.GetAsync(a.Value, CancellationToken.None))!;
        var vb = (await _receipts.GetAsync(b.Value, CancellationToken.None))!;
        Assert.Equal(va.Number + 1, vb.Number);
        Assert.Equal(("Cash", "1405/08/23", "200015", "ف-۱", true, "یادداشت", 5_000_000m, "IRR", "CashDesk", (Guid?)_desk),
            (va.Kind, va.Date, va.PayerDetailCode, va.Reference1, va.SalesProceeds, va.Note, va.TotalRial, va.Currency, va.AccountKind, va.AccountId));
        Assert.Contains(await _receipts.ListAsync(1405, ReceiptKind.Cash, CancellationToken.None), r => r.Id == a.Value);

        Assert.True((await _receipts.CancelAsync(new(Key(), a.Value, "1405/08/24", "اشتباه", 1), CancellationToken.None)).IsSuccess);
        va = (await _receipts.GetAsync(a.Value, CancellationToken.None))!;
        Assert.Equal(("Cancelled", "1405/08/24", "اشتباه", 2L), (va.State, va.CancelledOn, va.CancelReason, va.Version));
    }

    [Fact]
    public async Task A_corrected_receipt_and_its_cheques_are_stored_and_a_removed_cheque_is_deleted_db_043()
    {
        // Owner's report no. 6, items 2 and 6: correction before the voucher is confirmed.
        var cash = (await _receipts.RecordCashAsync(new(Key(), Header(), _desk, 5_000_000), CancellationToken.None)).Value;
        var corrected = await _receipts.CorrectCashAsync(
            new(Key(), cash, 1, Header() with { Date = "1405/08/25", Purpose = "پیش دریافت", Note = null, SalesProceeds = false }, _desk, 7_500_000), CancellationToken.None);
        Assert.True(corrected.IsSuccess, corrected.ErrorCode);
        var c = (await _receipts.GetAsync(cash, CancellationToken.None))!;
        Assert.Equal(("1405/08/25", "پیش دریافت", (string?)null, false, 7_500_000m, 7_500_000m, 2L, (string?)null),
            (c.Date, c.Purpose, c.Note, c.SalesProceeds, c.Amount, c.TotalRial, c.Version, c.NotCorrectable));
        Assert.Equal("Concurrency.Conflict", (await _receipts.CorrectCashAsync(new(Key(), cash, 1, Header(), _desk, 1), CancellationToken.None)).ErrorCode);

        var number = Guid.NewGuid().ToString("N")[..8];
        ReceivedInstrumentInput Cheque(string no, decimal amount, Guid? id = null) =>
            new(InstrumentKind.OrdinaryCheque, no, "1405/12/01", new MoneyInput(amount), Bank, "کاشان", SayadId: "1234567890123456", Id: id);
        var receipt = (await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("A" + number, 1_000_000), Cheque("B" + number, 2_000_000)]), CancellationToken.None)).Value;
        var before = (await _receipts.GetAsync(receipt, CancellationToken.None))!.Instruments!;
        Assert.Equal(["A" + number, "B" + number], before.Select(i => i.Number!));

        var r = await _receipts.CorrectInstrumentsAsync(new(Key(), receipt, 1, Header() with { Date = "1405/08/26" },
            [Cheque("B" + number, 2_500_000, before[1].Id) with { BranchName = "اصفهان", DueDate = "1406/01/20" }, Cheque("C" + number, 4_000_000)]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _receipts.GetAsync(receipt, CancellationToken.None))!;
        Assert.Equal((6_500_000m, 2, "1405/08/26", 2L), (v.TotalRial, v.InstrumentCount, v.Date, v.Version));
        Assert.Equal(["B" + number, "C" + number], v.Instruments!.Select(i => i.Number!));
        var kept = v.Instruments![0];
        Assert.Equal((before[1].Id, 2_500_000m, "1406/01/20", "1405/08/26", "اصفهان", "1405/08/26"),
            (kept.Id, kept.Amount, kept.DueDate, kept.IssueDate, kept.Details!.BranchName, Assert.Single(kept.History).Date));

        await using var conn = await _dataSource.OpenConnectionAsync(CancellationToken.None);
        await using var cmd = new NpgsqlCommand(
            "SELECT (SELECT count(*) FROM cashflow.instrument WHERE id = @id) + (SELECT count(*) FROM cashflow.instrument_event WHERE instrument_id = @id)", conn);
        cmd.Parameters.AddWithValue("id", before[0].Id);
        Assert.Equal(0L, (long)(await cmd.ExecuteScalarAsync(CancellationToken.None))!);
    }

    [Fact]
    public async Task A_dollar_deposit_keeps_its_rate()
    {
        var r = await _receipts.RecordBankDepositAsync(new(Key(), Header(), Accounts.Dollar, DepositMethod.InternalTransfer, "SW-9", 100.5m, 600_000), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(("BankDeposit", "InternalTransfer", "SW-9", 100.5m, "USD", (decimal?)600_000m, 60_300_000m), (v.Kind, v.Method, v.TrackingNumber, v.Amount, v.Currency, v.RateToRial, v.TotalRial));
        Assert.Null(v.PayerSheba);

        // Owner, 18 Nov 2026 (db/postgres/036): a Satna transfer is stored with the Sheba it came from; a gateway deposit has none.
        var satna = await _receipts.RecordBankDepositAsync(
            new(Key(), Header(), Guid.NewGuid(), DepositMethod.Satna, "S-1", 7_000_000, PayerSheba: "IR062960000000100324200001"), CancellationToken.None);
        Assert.True(satna.IsSuccess, satna.ErrorCode);
        v = (await _receipts.GetAsync(satna.Value, CancellationToken.None))!;
        Assert.Equal(("Satna", "IR062960000000100324200001"), (v.Method, v.PayerSheba));
        Assert.True((await _receipts.RecordBankDepositAsync(new(Key(), Header(), Guid.NewGuid(), DepositMethod.PaymentGateway, "PG-1", 1_000), CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task Cheques_and_notes_are_saved_with_their_receipt_and_details()
    {
        var number = Guid.NewGuid().ToString("N")[..8];
        var r = await _receipts.RecordInstrumentsAsync(new(Key(), Header(),
        [
            new(InstrumentKind.OrdinaryCheque, number, "1405/12/01", new MoneyInput(500, "EUR", 700_000), Bank, "کاشان", "123", "0202-3572", "علی رضایی", "کاشان", true, "1234567890123456"),
            new(InstrumentKind.PromissoryNote, "N" + number, "1406/01/15", new MoneyInput(20_000_000), Guarantors: [new("رضا احمدی", "0123456789")]),
        ]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(2, v.Instruments!.Count);
        var cheque = v.Instruments.Single(i => i.Number == number);
        Assert.Equal((r.Value, true, "بانک ملت", "EUR", (decimal?)700_000m), (cheque.ReceiptId!.Value, cheque.IsPostDated, cheque.BankName, cheque.Currency, cheque.RateToRial));
        Assert.Equal(("کاشان", "علی رضایی", true, "1234567890123456"), (cheque.Details!.BranchName, cheque.Details.AccountHolder, cheque.Details.OutOfTown, cheque.Details.SayadId));
        var note = v.Instruments.Single(i => i.Number == "N" + number);
        Assert.Equal("0123456789", Assert.Single(note.Details!.GuarantorList).NationalCode);
        Assert.Equal(350_000_000m + 20_000_000m, v.TotalRial);

        // Report no. 4, item 9: deposited for collection at a company account, read back with the account.
        var useCases = new InstrumentUseCases(new PostgresUnitOfWork(_dataSource), new PostgresInstrumentRepository(), new PostgresChequeBookRepository(),
            new PostgresIdempotencyStore(_dataSource), new DevelopmentPolicies().OpenYear(1405), new DevelopmentPolicies(), new DevelopmentPolicies(), new User(), new EuroBank());
        Assert.True((await useCases.ChangeStateAsync(new(Key(), cheque.Id, InstrumentAction.RegisterSayad, "1405/08/24"), CancellationToken.None)).IsSuccess); // rule N3
        Assert.True((await useCases.DepositForCollectionAsync(new(Key(), cheque.Id, EuroBank.Id, "1405/08/25", "SLIP-9"), CancellationToken.None)).IsSuccess);
        var deposited = (await useCases.GetAsync(cheque.Id, CancellationToken.None))!;
        Assert.Equal(("DepositedForCollection", (Guid?)EuroBank.Id), (deposited.State, deposited.CollectionAccountId));
        Assert.Contains(await useCases.ListReceivedAsync(InstrumentState.DepositedForCollection, CancellationToken.None), i => i.Id == cheque.Id);
    }

    [Fact]
    public async Task A_note_receipt_round_trips_with_its_terms_notes_and_guarantors_db_033()
    {
        var number = Guid.NewGuid().ToString("N")[..8];
        var terms = new NoteTermsInput(NoteReason.Guarantee, NoteForm.Electronic, NoteMaturity.Fixed, "0123456789", "021-88990011", "حسن انجام قرارداد ۴۵",
            "پس از تأیید واحد فنی", "صندوق شرکت", true, "1405/08/24", "ندارد", new NoteAttachments(NoteImage: true, Contract: true), "علی رضایی");
        var r = await _receipts.RecordNotesAsync(new(Key(), Header(), terms,
            [new(number, "1405/08/20", "1405/11/30", 500_000_000, AllChecked), new("B" + number, null, "1405/12/25", 250_000_000, AllChecked)],
            [new("رضا احمدی", "0123456789", "300045", "0912", number)], [Reference.Cfo.Id, Reference.Ceo.Id]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);

        var v = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(("Notes", 2, 750_000_000m, (string?)null), (v.Kind, v.InstrumentCount, v.TotalRial, v.AccountKind));
        var t = v.NoteTerms!;
        Assert.Equal(("Guarantee", "Electronic", "Fixed", "0123456789", "021-88990011", "حسن انجام قرارداد ۴۵", "پس از تأیید واحد فنی", "صندوق شرکت", true, "1405/08/24", "ندارد"),
            (t.Reason, t.Form, t.Maturity, t.IssuerNationalCode, t.IssuerPhone, t.CommitmentSubject, t.ReturnTerms, t.CustodyPlace, t.OriginalReceived, t.OriginalReceivedOn, t.Discrepancy));
        Assert.Equal((true, true, false, "علی رضایی"), (t.Attachments.NoteImage, t.Attachments.Contract, t.Attachments.Other, t.DeliveredBy));
        Assert.Equal([("علی رضایی", "مدیر عامل"), ("بهرام کریمی", "مدیر مالی")], t.Signers.Select(s => (s.Name, s.Position!)));
        var note = v.Instruments!.Single(i => i.Number == number);
        Assert.Equal(("GuaranteeNote", "1405/08/20", "1405/11/30", true, true, false),
            (note.Kind, note.NoteIssueDate, note.DueDate, note.Details!.Checks!.Signature, note.Details.Checks.PaymentDate, note.Details.OnDemand));
        var guarantor = Assert.Single(note.Details.GuarantorList);
        Assert.Equal(("رضا احمدی", "0123456789", "300045", "0912"), (guarantor.Name, guarantor.NationalCode, guarantor.DetailCode, guarantor.Phone));
        var other = v.Instruments!.Single(i => i.Number == "B" + number);
        Assert.Empty(other.Details!.GuarantorList);
        Assert.Null(other.NoteIssueDate);

        // Its own series: the next note receipt gets the next number; a cheque receipt does not move it.
        var next = await _receipts.RecordNotesAsync(new(Key(), Header(), new NoteTermsInput(NoteReason.Debt, NoteForm.Paper, NoteMaturity.OnDemand),
            [new("C" + number, null, null, 1_000_000, AllChecked with { PaymentDate = false })], SignerIds: [Reference.Ceo.Id]), CancellationToken.None);
        Assert.True(next.IsSuccess, next.ErrorCode);
        var nv = (await _receipts.GetAsync(next.Value, CancellationToken.None))!;
        Assert.Equal(v.Number + 1, nv.Number);
        Assert.True(Assert.Single(nv.Instruments!).Details!.OnDemand);
        Assert.Contains(await _receipts.ListAsync(1405, ReceiptKind.Notes, CancellationToken.None), x => x.Id == r.Value && x.NoteTerms!.Reason == "Guarantee");
    }

    [Fact]
    public async Task A_confirmed_voucher_is_kept_once_with_its_lines_and_stops_cancellation()
    {
        var vouchers = new Treasury.Cashflow.Application.Vouchers.VoucherUseCases(new PostgresUnitOfWork(_dataSource), new PostgresVoucherTemplateRepository(),
            new PostgresConfirmedVoucherRepository(), new PostgresReceiptRepository(), new PostgresInstrumentRepository(), new Accounts(), new BankAccounts(), new Reference(),
            new PostgresIdempotencyStore(_dataSource), new User(), TimeProvider.System);
        Assert.True((await vouchers.SetTemplateAccountAsync(Key(), Treasury.Cashflow.Domain.Vouchers.FixedAccount.ChequesInCashDesk, "1201", CancellationToken.None)).IsSuccess);
        var template = await vouchers.GetTemplateAsync(CancellationToken.None);
        Assert.Equal("1201", template.Single(a => a.Account == "ChequesInCashDesk").Code);

        var number = Guid.NewGuid().ToString("N")[..8];
        var r = await _receipts.RecordInstrumentsAsync(new(Key(), Header(),
            [new(InstrumentKind.OrdinaryCheque, number, "1405/12/01", new MoneyInput(500, "EUR", 700_000), Bank, SayadId: "1234567890123456")]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var confirmed = await vouchers.ConfirmReceiptVoucherAsync(Key(), r.Value, CancellationToken.None);
        Assert.True(confirmed.IsSuccess, confirmed.ErrorCode);

        var v = (await vouchers.ReceiptVoucherAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(("clerk-1", "1405/08/23", 350_000_000m), (v.ConfirmedBy, v.Date, v.TotalDebit));
        Assert.Equal([(true, "1201", "200015", (decimal?)500m, "EUR"), (false, "4101", "200015", (decimal?)500m, "EUR")],
            v.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode, l.ForeignAmount, l.Currency)));
        Assert.Equal("Voucher.AlreadyConfirmed", (await vouchers.ConfirmReceiptVoucherAsync(Key(), r.Value, CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.VoucherConfirmed", (await _receipts.CancelAsync(new(Key(), r.Value, "1405/08/24", "اشتباه", 1), CancellationToken.None)).ErrorCode);

        Assert.True((await vouchers.SetTemplateAccountAsync(Key(), Treasury.Cashflow.Domain.Vouchers.FixedAccount.ChequesInCashDesk, "", CancellationToken.None)).IsSuccess);
        Assert.Null((await vouchers.GetTemplateAsync(CancellationToken.None)).Single(a => a.Account == "ChequesInCashDesk").Code);
    }

    [Fact]
    public async Task A_cheque_transfer_keeps_the_number_and_signatories_of_its_payment_sheet_db_042()
    {
        // Owner's answers to Q57 (19 Nov 2026): the payment sheet has a serial number of its own and is signed by the authorised signatories.
        var useCases = new InstrumentUseCases(new PostgresUnitOfWork(_dataSource), new PostgresInstrumentRepository(), new PostgresChequeBookRepository(),
            new PostgresIdempotencyStore(_dataSource), new DevelopmentPolicies().OpenYear(1405), new DevelopmentPolicies(), new DevelopmentPolicies(), new User(), new BankAccounts());
        async Task<(int Sheet, IReadOnlyList<ReceiptSigner> Signers)> Transfer(string number, params Guid[] signers)
        {
            var receipt = await _receipts.RecordInstrumentsAsync(new(Key(), Header(),
                [new(InstrumentKind.OrdinaryCheque, number, "1405/10/01", new MoneyInput(7_000_000), Guid.NewGuid(), SayadId: "1234567890123456")]), CancellationToken.None);
            Assert.True(receipt.IsSuccess, receipt.ErrorCode);
            var cheque = (await _receipts.GetAsync(receipt.Value, CancellationToken.None))!.Instruments![0].Id;
            Assert.True((await useCases.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RegisterSayad, "1405/08/24"), CancellationToken.None)).IsSuccess);
            var request = Guid.NewGuid();
            _requests.Known[request] = new(request, 3, "Approved", "300001", "پیمانکار الف", Reference.PaymentGroup, "بابت بدهی", 7_000_000, "IRR", 0, cheque);
            var done = await _receipts.EndorseAsync(new(Key(), cheque, "1405/08/25", request, "SY-" + number, SignerIds: signers), CancellationToken.None);
            Assert.True(done.IsSuccess, done.ErrorCode);
            var step = (await useCases.GetAsync(cheque, CancellationToken.None))!.History[^1];
            Assert.Equal(("Endorse", "Endorsed", "SY-" + number), (step.Action, step.To, step.Details!.SayadTracking));
            return (step.Details!.SheetNumber!.Value, step.Details.Signers ?? []);
        }

        var n = Guid.NewGuid().ToString("N")[..6];
        var first = await Transfer("T1" + n, Reference.Ceo.Id);
        var second = await Transfer("T2" + n, Reference.Cfo.Id, Reference.Ceo.Id);
        Assert.Equal(first.Sheet + 1, second.Sheet); // one series a year; other tests of this class take no sheet number
        Assert.Equal([("علی رضایی", "مدیر عامل")], first.Signers.Select(s => (s.Name, s.Position!)));
        Assert.Equal(2, second.Signers.Count);
    }

    [Fact]
    public async Task Steps_of_item_14_keep_their_details_and_a_replacement_names_the_old_cheque()
    {
        var useCases = new InstrumentUseCases(new PostgresUnitOfWork(_dataSource), new PostgresInstrumentRepository(), new PostgresChequeBookRepository(),
            new PostgresIdempotencyStore(_dataSource), new DevelopmentPolicies().OpenYear(1405), new DevelopmentPolicies(), new DevelopmentPolicies(), new User(), new BankAccounts());
        var number = Guid.NewGuid().ToString("N")[..8];
        var r = await _receipts.RecordInstrumentsAsync(new(Key(), Header(),
            [new(InstrumentKind.OrdinaryCheque, number, "1405/09/01", new MoneyInput(9_000_000), Bank, SayadId: "1234567890123456")]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var cheque = (await _receipts.GetAsync(r.Value, CancellationToken.None))!.Instruments![0].Id;
        var account = Guid.NewGuid();
        Assert.True((await useCases.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RegisterSayad, "1405/08/24"), CancellationToken.None)).IsSuccess); // rule N3
        Assert.True((await useCases.DepositForCollectionAsync(new(Key(), cheque, account, "1405/08/25", "F-1"), CancellationToken.None)).IsSuccess);
        Assert.True((await useCases.ChangeStateAsync(new(Key(), cheque, InstrumentAction.ReturnFromBank, "1405/08/28"), CancellationToken.None)).IsSuccess);
        var view = (await useCases.GetAsync(cheque, CancellationToken.None))!;
        Assert.Equal(("ReturnedFromBank", (Guid?)account, (Guid?)account), (view.State, view.History[2].Details!.AccountId, view.History[3].Details!.AccountId));

        var replace = await _receipts.ReplaceAsync(new(Key(), cheque, Header() with { Date = "1405/08/29" },
            [new(InstrumentKind.OrdinaryCheque, "N" + number, "1405/10/01", new MoneyInput(9_500_000), Bank, SayadId: "6543210987654321")]), CancellationToken.None);
        Assert.True(replace.IsSuccess, replace.ErrorCode);
        var receipt = (await _receipts.GetAsync(replace.Value, CancellationToken.None))!;
        Assert.Equal((Guid?)cheque, receipt.ReplacedInstrumentId);
        view = (await useCases.GetAsync(cheque, CancellationToken.None))!;
        Assert.Equal(("Replaced", (Guid?)replace.Value), (view.State, view.History[^1].Details!.ReceiptId));

        var fresh = receipt.Instruments![0].Id;
        Assert.True((await useCases.ChangeStateAsync(new(Key(), fresh, InstrumentAction.Cancel, "1405/08/30", Note: "سوخت", Reference: "م-1", DocumentDate: "1405/08/29"),
            CancellationToken.None)).IsSuccess);
        view = (await useCases.GetAsync(fresh, CancellationToken.None))!;
        Assert.Equal(("Cancelled", "م-1", "1405/08/29"), (view.State, view.History[^1].Reference, view.History[^1].Details!.DocumentDate));
    }

    [Fact]
    public async Task Sayad_registration_and_legal_action_are_stored_and_the_receipts_list_carries_each_cheques_latest_status()
    {
        // Owner, 18 Nov 2026 (db/postgres/033): «ثبت صیادی شد», «حقوقی», and the «وضعیت» column of the receipts list.
        var useCases = new InstrumentUseCases(new PostgresUnitOfWork(_dataSource), new PostgresInstrumentRepository(), new PostgresChequeBookRepository(),
            new PostgresIdempotencyStore(_dataSource), new DevelopmentPolicies().OpenYear(1405), new DevelopmentPolicies(), new DevelopmentPolicies(), new User(), new BankAccounts());
        var number = Guid.NewGuid().ToString("N")[..8];
        var r = await _receipts.RecordInstrumentsAsync(new(Key(), Header(),
        [
            new(InstrumentKind.OrdinaryCheque, number, "1405/08/25", new MoneyInput(9_000_000), Bank, SayadId: "1234567890123456"),
            new(InstrumentKind.OrdinaryCheque, "B" + number, "1405/08/24", new MoneyInput(1_000_000), Bank, SayadId: "1234567890123456"),
        ]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var cheques = (await _receipts.GetAsync(r.Value, CancellationToken.None))!.Instruments!;
        var first = cheques.Single(c => c.Number == number).Id;
        var second = cheques.Single(c => c.Number == "B" + number).Id;

        Assert.True((await useCases.ChangeStateAsync(new(Key(), first, InstrumentAction.RegisterSayad, "1405/08/23", Reference: "SY-7"), CancellationToken.None)).IsSuccess);
        var view = (await useCases.GetAsync(first, CancellationToken.None))!;
        Assert.Equal(("SayadRegistered", "SY-7", "SY-7"), (view.State, view.History[^1].Reference, view.History[^1].Details!.SayadTracking));
        Assert.Contains(await useCases.ListReceivedAsync(InstrumentState.SayadRegistered, CancellationToken.None), i => i.Id == first);

        Assert.Equal("Instrument.SayadRegistrationRequired", (await useCases.DepositForCollectionAsync(new(Key(), second, Guid.NewGuid(), "1405/08/23", "F-2"), CancellationToken.None)).ErrorCode);
        Assert.True((await useCases.ChangeStateAsync(new(Key(), second, InstrumentAction.RegisterSayad, "1405/08/23"), CancellationToken.None)).IsSuccess); // rule N3
        Assert.True((await useCases.DepositForCollectionAsync(new(Key(), second, Guid.NewGuid(), "1405/08/23", "F-2"), CancellationToken.None)).IsSuccess);
        Assert.True((await useCases.ChangeStateAsync(new(Key(), second, InstrumentAction.RecordBounced, "1405/08/24", Reference: "BN-3"), CancellationToken.None)).IsSuccess);
        Assert.True((await useCases.ChangeStateAsync(new(Key(), second, InstrumentAction.SendToLegal, "1405/08/25", Note: "تحویل به وکیل شرکت",
            Reference: "BN-3", DocumentDate: "1405/08/24", TrackingCode: "1122334455667788"), CancellationToken.None)).IsSuccess);
        view = (await useCases.GetAsync(second, CancellationToken.None))!;
        Assert.Equal(("InLegal", "1405/08/25", "تحویل به وکیل شرکت"), (view.State, view.History[^1].Date, view.History[^1].Note));

        // The certificate of non-payment of the legal action: number, date and tracking code.
        Assert.Equal(("BN-3", "1405/08/24", "1122334455667788"), (view.History[^1].Reference, view.History[^1].Details!.DocumentDate, view.History[^1].Details!.TrackingCode));

        // The scans of the legal action (db/postgres/034): stored with their content, listed, opened and removed.
        var legal = new LegalActionUseCases(new PostgresUnitOfWork(_dataSource), new PostgresInstrumentRepository(), new PostgresInstrumentAttachmentRepository(),
            new DevelopmentPolicies().OpenYear(1405), new PostgresIdempotencyStore(_dataSource), new User(), new Aban1405());
        byte[] pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37];
        var key = Key();
        var file = await legal.AttachAsync(key, second, "دادخواست.pdf", pdf, CancellationToken.None);
        Assert.True(file.IsSuccess, file.ErrorCode);
        Assert.Equal(file.Value, (await legal.AttachAsync(key, second, "دادخواست.pdf", pdf, CancellationToken.None)).Value);
        Assert.Equal("Instrument.NotInLegalAction", (await legal.AttachAsync(Key(), first, "a.pdf", pdf, CancellationToken.None)).ErrorCode);
        var stored = Assert.Single(await legal.ListAsync(second, CancellationToken.None));
        Assert.Equal(("دادخواست.pdf", "application/pdf", 8, 5, "clerk-1"), (stored.FileName, stored.ContentType, stored.Size, stored.Step, stored.UploadedBy));
        Assert.Equal(pdf, (await legal.GetAsync(second, file.Value, CancellationToken.None))!.Value.Content);
        Assert.True((await legal.RemoveAsync(Key(), second, file.Value, CancellationToken.None)).IsSuccess);
        Assert.Empty(await legal.ListAsync(second, CancellationToken.None));

        var row = (await _receipts.ListAsync(1405, ReceiptKind.Instruments, CancellationToken.None)).Single(x => x.Id == r.Value);
        Assert.Equal(["InLegal", "SayadRegistered"], row.ChequeStatuses!.Select(s => s.State).Order());

        // Both due dates are in the past by now: the cheque still in the cash desk is marked «سررسید چک», the one with the lawyer is not.
        Assert.Equal([("InLegal", false), ("SayadRegistered", true)], row.ChequeStatuses!.OrderBy(s => s.State).Select(s => (s.State, s.ReachedDueDate)));
    }

    /// <summary>A day in 1405, a year the test database has partitions for: a scan belongs to the year it is added in.</summary>
    private sealed class Aban1405 : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 18, 8, 0, 0, TimeSpan.Zero);
    }

    private sealed class EuroBank : IBankAccountLookup
    {
        public static readonly Guid Id = Guid.NewGuid();

        public Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<BankAccountFact?>(new(id, "EUR-1", true, true, "EUR"));

        public Task MarkInUseAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Accounts : IReceiptAccountLookup
    {
        public static readonly Guid Dollar = Guid.NewGuid();

        public Task<ReceiptAccountFact?> CashDeskAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ReceiptAccountFact?>(new(ReceiptAccountKind.CashDesk, id, "صندوق", "IRR", true));

        public Task<ReceiptAccountFact?> BankAccountAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ReceiptAccountFact?>(new(ReceiptAccountKind.BankAccount, id, "0101", id == Dollar ? "USD" : "IRR", true));
    }

    private sealed class BankAccounts : IBankAccountLookup
    {
        public Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<BankAccountFact?>(new(id, "0101", true));

        public Task MarkInUseAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Reference : IReceiptReferenceLookup
    {
        public Task<PayerFact?> PayerAsync(string detailCode, CancellationToken cancellationToken) => Task.FromResult<PayerFact?>(new(detailCode, "شرکت نمونه", true));

        /// <summary>Every تفصیل is an obligor and a guarantor of Basic Information here; 300045 is «رضا احمدی».</summary>
        public Task<NotePartyFact?> NotePartyAsync(string detailCode, CancellationToken cancellationToken) =>
            Task.FromResult<NotePartyFact?>(new(detailCode, detailCode == "300045" ? "رضا احمدی" : "شرکت نمونه", true, true, true));

        public static readonly Guid PaymentGroup = Guid.NewGuid();

        public Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<PurposeGroupFact?>(
            id == PaymentGroup ? new(id, "پرداخت به بستانکاران", false, true, "2101") : new(id, "وجوه حاصل فروش", true, true, "4101"));

        public Task<string?> BankNameAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<string?>("بانک ملت");

        public Task<string?> CompanyCityAsync(CancellationToken cancellationToken) => Task.FromResult<string?>("تهران");

        public Task<Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact?> LedgerAccountAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact?>(new(code, "حساب " + code, true, true));

        public Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public static readonly SignerFact Ceo = new(Guid.NewGuid(), "علی رضایی", "مدیر عامل");
        public static readonly SignerFact Cfo = new(Guid.NewGuid(), "بهرام کریمی", "مدیر مالی");

        public Task<SignatureRuleFact?> SignatureRuleAsync(string document, CancellationToken cancellationToken) =>
            Task.FromResult<SignatureRuleFact?>(new(1, [Ceo], [Cfo]));
    }

    /// <summary>requests-svc stand-in: the approved requests a test puts in.</summary>
    private sealed class Requests : IPaymentRequestLookup
    {
        public Dictionary<Guid, PaymentRequestFact> Known { get; } = [];

        public Task<PaymentRequestFact?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Known.GetValueOrDefault(id));

        public Task RecordPaymentAsync(Guid requestId, RequestPaymentInput payment, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ReversePaymentAsync(Guid requestId, string paymentKey, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class User : ICurrentUser
    {
        public string UserId => "clerk-1";
    }
}
