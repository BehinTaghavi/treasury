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
    private readonly ReceiptUseCases _receipts;
    private readonly InstrumentUseCases _instruments;
    private readonly VoucherUseCases _vouchers;

    public VoucherUseCaseTests()
    {
        var policies = new DevelopmentPolicies().OpenYear(1405);
        _receipts = new ReceiptUseCases(
            new InMemoryUnitOfWork(_db), new InMemoryReceiptRepository(_db), new InMemoryInstrumentRepository(_db), new FakeReceiptAccounts(), _bank, _reference,
            policies, new InMemoryIdempotencyStore(_db), new Treasurer(), new InMemoryVoucherTemplateRepository(_store), new InMemoryConfirmedVoucherRepository(_store));
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

        Assert.True((await _instruments.DepositForCollectionAsync(new(Key(), cheque, LedgerBank.Id, "1405/08/25", "SLIP-1"), CancellationToken.None)).IsSuccess);
        Assert.True((await _instruments.ChangeStateAsync(new(Key(), cheque, InstrumentAction.RecordCleared, "1405/10/02"), CancellationToken.None)).IsSuccess);
        var steps = await _vouchers.ChequeVouchersAsync(cheque, CancellationToken.None);
        Assert.Equal([2, 3], steps.Select(s => s.Step));
        Assert.Equal([(true, "1202", "600009"), (false, "1201", "200015")], steps[0].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal([(true, "1109", "600009"), (false, "1202", "600009")], steps[1].Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal("1405/10/02", steps[1].Date);

        Assert.True((await _vouchers.ConfirmChequeVoucherAsync(Key(), cheque, 2, CancellationToken.None)).IsSuccess);
        Assert.Equal("treasurer", (await _vouchers.ChequeVouchersAsync(cheque, CancellationToken.None))[0].ConfirmedBy);
        Assert.Equal("Voucher.AlreadyConfirmed", (await _vouchers.ConfirmChequeVoucherAsync(Key(), cheque, 2, CancellationToken.None)).ErrorCode);
        Assert.Equal("Voucher.NoVoucherForStep", (await _vouchers.ConfirmChequeVoucherAsync(Key(), cheque, 1, CancellationToken.None)).ErrorCode);
        Assert.Equal("Instrument.NotFound", (await _vouchers.ConfirmChequeVoucherAsync(Key(), Guid.NewGuid(), 2, CancellationToken.None)).ErrorCode);
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
