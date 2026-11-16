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
            new PostgresVoucherTemplateRepository(), new PostgresConfirmedVoucherRepository(), new NoRequests());
    }

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
    public async Task A_dollar_deposit_keeps_its_rate()
    {
        var r = await _receipts.RecordBankDepositAsync(new(Key(), Header(), Accounts.Dollar, DepositMethod.Transfer, "SW-9", 100.5m, 600_000), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(("BankDeposit", "Transfer", "SW-9", 100.5m, "USD", (decimal?)600_000m, 60_300_000m), (v.Kind, v.Method, v.TrackingNumber, v.Amount, v.Currency, v.RateToRial, v.TotalRial));
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
        Assert.True((await useCases.DepositForCollectionAsync(new(Key(), cheque, account, "1405/08/25", "F-1"), CancellationToken.None)).IsSuccess);
        Assert.True((await useCases.ChangeStateAsync(new(Key(), cheque, InstrumentAction.ReturnFromBank, "1405/08/28"), CancellationToken.None)).IsSuccess);
        var view = (await useCases.GetAsync(cheque, CancellationToken.None))!;
        Assert.Equal(("ReturnedFromBank", (Guid?)account, (Guid?)account), (view.State, view.History[1].Details!.AccountId, view.History[2].Details!.AccountId));

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

        public Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<PurposeGroupFact?>(new(id, "وجوه حاصل فروش", true, true, "4101"));

        public Task<string?> BankNameAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<string?>("بانک ملت");

        public Task<Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact?> LedgerAccountAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact?>(new(code, "حساب " + code, true, true));

        public Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public static readonly SignerFact Ceo = new(Guid.NewGuid(), "علی رضایی", "مدیر عامل");
        public static readonly SignerFact Cfo = new(Guid.NewGuid(), "بهرام کریمی", "مدیر مالی");

        public Task<SignatureRuleFact?> SignatureRuleAsync(string document, CancellationToken cancellationToken) =>
            Task.FromResult<SignatureRuleFact?>(new(1, [Ceo], [Cfo]));
    }

    private sealed class NoRequests : IPaymentRequestLookup
    {
        public Task<PaymentRequestFact?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<PaymentRequestFact?>(null);

        public Task RecordPaymentAsync(Guid requestId, RequestPaymentInput payment, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ReversePaymentAsync(Guid requestId, string paymentKey, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class User : ICurrentUser
    {
        public string UserId => "clerk-1";
    }
}
