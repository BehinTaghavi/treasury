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
            new Reference(), new DevelopmentPolicies().OpenYear(1405), new PostgresIdempotencyStore(_dataSource), new User());
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

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
        var r = await _receipts.RecordBankDepositAsync(new(Key(), Header(), Accounts.Dollar, DepositMethod.Pos, "POS-9", 100.5m, 600_000), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(("BankDeposit", "Pos", "POS-9", 100.5m, "USD", (decimal?)600_000m, 60_300_000m), (v.Kind, v.Method, v.TrackingNumber, v.Amount, v.Currency, v.RateToRial, v.TotalRial));
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

        public Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<PurposeGroupFact?>(new(id, "وجوه حاصل فروش", true, true));

        public Task<string?> BankNameAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<string?>("بانک ملت");

        public Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class User : ICurrentUser
    {
        public string UserId => "clerk-1";
    }
}
