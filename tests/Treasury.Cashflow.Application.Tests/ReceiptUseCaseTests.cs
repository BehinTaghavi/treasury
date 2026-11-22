using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Application.Receipts;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Cashflow.Application.Tests;

/// <summary>accounts-svc stand-in for receipts: one Rial cash desk, one Rial and one dollar bank account.</summary>
public sealed class FakeReceiptAccounts : IReceiptAccountLookup
{
    public static readonly Guid Desk = Guid.NewGuid();
    public static readonly Guid ClosedDesk = Guid.NewGuid();
    public static readonly Guid RialBank = Guid.NewGuid();
    public static readonly Guid DollarBank = Guid.NewGuid();

    public Task<ReceiptAccountFact?> CashDeskAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(
        id == Desk || id == ClosedDesk ? new ReceiptAccountFact(ReceiptAccountKind.CashDesk, id, "صندوق", "IRR", id == Desk, "1101", "500001") : null);

    public Task<ReceiptAccountFact?> BankAccountAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(
        id == RialBank ? new ReceiptAccountFact(ReceiptAccountKind.BankAccount, id, "0202-3572", "IRR", true, "1102", "600001", HasCards: true)
        : id == DollarBank ? new ReceiptAccountFact(ReceiptAccountKind.BankAccount, id, "USD-1", "USD", true, "1103", "600002") : null);

    /// <summary>A petty cash kept on its holder's bank card, one kept in cash only (no card), a company card without a bank account, and one switched off.</summary>
    public static readonly Guid PettyCashOnCard = Guid.NewGuid();
    public static readonly Guid PettyCashInCash = Guid.NewGuid();
    public static readonly Guid CompanyCard = Guid.NewGuid();
    public static readonly Guid ClosedCompanyCard = Guid.NewGuid();

    public Task<ReceiptAccountFact?> CardAsync(ReceiptAccountKind kind, Guid id, CancellationToken cancellationToken) => Task.FromResult(
        kind == ReceiptAccountKind.PettyCashCard && id == PettyCashOnCard ? new ReceiptAccountFact(kind, id, "مریم کریمی – 6219861034529007", "IRR", true, "1104", "700002")
        : kind == ReceiptAccountKind.CompanyCard && (id == CompanyCard || id == ClosedCompanyCard)
            ? new ReceiptAccountFact(kind, id, "شرکت نمونه – 6037991234567893", "IRR", id == CompanyCard, "1105", "600009")
        : null);
}

/// <summary>reference-svc stand-in: two payers (one switched off), a receipt and a payment purpose group, two banks.</summary>
public sealed class FakeReceiptReference : IReceiptReferenceLookup
{
    public static readonly Guid SalesGroup = Guid.NewGuid();
    public static readonly Guid PaymentGroup = Guid.NewGuid();
    public static readonly Guid Mellat = Guid.NewGuid();
    public static readonly Guid Melli = Guid.NewGuid();

    public bool Down { get; set; }

    /// <summary>The city of the company's address («مشخصات شرکت»); null: not entered yet.</summary>
    public string? CompanyCity { get; set; }

    public Task<string?> CompanyCityAsync(CancellationToken cancellationToken) => Check(CompanyCity);

    public List<(string List, Guid Id)> Marked { get; } = [];

    public Task<PayerFact?> PayerAsync(string detailCode, CancellationToken cancellationToken) => Check(detailCode switch
    {
        "200015" => new PayerFact("200015", "شرکت نمونه بازرگانی", true),
        "200099" => new PayerFact("200099", "مشتری قدیمی", false),
        "200016" => new PayerFact("200016", "مشتری دیگر", true),
        "300001" => new PayerFact("300001", "کارمند", true, MayPay: false),
        _ => null,
    });

    public Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken) => Check(
        id == SalesGroup ? new PurposeGroupFact(id, "وجوه حاصل فروش", true, true, "4101") : id == PaymentGroup ? new PurposeGroupFact(id, "خرید مواد", false, true, "2101") : null);

    public Task<string?> BankNameAsync(Guid id, CancellationToken cancellationToken) =>
        Check(id == Mellat ? "بانک ملت" : id == Melli ? "بانک ملی" : null);

    /// <summary>The chart: 1201, 1202, 1203, 9101, 9201 take entries; 12 has a lower level; 1299 is inactive.</summary>
    public Task<Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact?> LedgerAccountAsync(string code, CancellationToken cancellationToken) => Check(code switch
    {
        "1201" or "1202" or "1203" or "9101" or "9201" or "8101" => new Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact(code, "حساب " + code, true, true),
        "8205" => new Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact(code, "هزینه کارمزد خدمات بانکی", true, true, FeeAccountDetailTypes),
        "12" => new Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact(code, "کل", false, true),
        "1299" => new Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact(code, "قدیمی", true, false),
        _ => null,
    });

    /// <summary>تفصیل by code with its type: 810001 a cost centre of the finance department, 810002 one that is switched off, 200015 a customer.</summary>
    public Task<Treasury.Cashflow.Domain.Transfers.FeeDetailFact?> DetailAsync(string detailCode, CancellationToken cancellationToken) => Check(detailCode switch
    {
        "810001" => new Treasury.Cashflow.Domain.Transfers.FeeDetailFact("810001", "مرکز هزینه امور مالی", "مراکز هزینه", true),
        "810002" => new Treasury.Cashflow.Domain.Transfers.FeeDetailFact("810002", "مرکز هزینه قدیمی", "مراکز هزینه", false),
        "200015" => new Treasury.Cashflow.Domain.Transfers.FeeDetailFact("200015", "شرکت نمونه بازرگانی", "مشتریان", true),
        _ => null,
    });

    /// <summary>The تفصیل types account 8205 («هزینه کارمزد خدمات بانکی») takes; empty: the account has no تفصیل.</summary>
    public IReadOnlyList<string> FeeAccountDetailTypes { get; set; } = [];

    /// <summary>The holders of a permission in the receipt of notes («دارندگان مجوزها», Basic Information); null: none.</summary>
    public FormSigners? NoteReceiptSigners { get; set; }

    /// <summary>The holders of a permission in «ثبت انتقال (خرج چک)», who may sign the cheque payment sheet; null: none.</summary>
    public FormSigners? ChequeTransferSigners { get; set; }

    public Task<FormSigners?> FormSignersAsync(string operation, CancellationToken cancellationToken) =>
        Check(operation == "NoteReceipt" ? NoteReceiptSigners : operation == "ChequeTransfer" ? ChequeTransferSigners : null);

    /// <summary>
    /// «ذینفعان / متعهدین / ضامنین» of Basic Information by تفصیل: 200015 an obligor, 200016 a guarantor, 300001 (staff) both, 200017 a
    /// guarantor that is switched off.
    /// </summary>
    public Dictionary<string, NotePartyFact> Parties { get; } = new()
    {
        ["200015"] = new("200015", "شرکت نمونه بازرگانی", true, IsObligor: true, IsGuarantor: false),
        ["200016"] = new("200016", "مشتری دیگر", true, IsObligor: false, IsGuarantor: true, NationalId: "0123456789", Phone: "09121112233"),
        ["300001"] = new("300001", "کارمند", true, IsObligor: true, IsGuarantor: true),
        ["200017"] = new("200017", "ضامن قدیمی", false, IsObligor: false, IsGuarantor: true),
    };

    public Task<NotePartyFact?> NotePartyAsync(string detailCode, CancellationToken cancellationToken) => Check(Parties.GetValueOrDefault(detailCode));

    public Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken)
    {
        _ = Check<object>(null);
        Marked.Add((list, id));
        return Task.CompletedTask;
    }

    private Task<T?> Check<T>(T? value) => Down ? throw new DomainException("Reference.Unavailable", "down") : Task.FromResult(value);
}

/// <summary>requests-svc stand-in (answer to Q36): approved requests that endorsed cheques pay, with the same rules as the real one.</summary>
public sealed class FakePaymentRequests : IPaymentRequestLookup
{
    public static readonly Guid Approved = Guid.NewGuid();
    public static readonly Guid Registered = Guid.NewGuid();

    public Dictionary<Guid, PaymentRequestFact> Requests { get; } = new()
    {
        [Approved] = new(Approved, 7, "Approved", "300001", "پیمانکار الف", FakeReceiptReference.PaymentGroup, "خرید مواد", 25_000_000, "IRR", 0),
        [Registered] = new(Registered, 8, "Registered", "300001", "پیمانکار الف", FakeReceiptReference.PaymentGroup, "خرید", 1_000_000, "IRR", 0),
    };

    public Dictionary<string, (Guid Request, decimal Amount)> Payments { get; } = [];

    public Task<PaymentRequestFact?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Requests.GetValueOrDefault(id));

    public Task RecordPaymentAsync(Guid requestId, RequestPaymentInput payment, CancellationToken cancellationToken)
    {
        if (!Payments.ContainsKey(payment.Key))
        {
            var r = Requests[requestId];
            var paid = r.PaidAmount + payment.Amount;
            Requests[requestId] = r with { PaidAmount = paid, State = paid == r.Amount ? "Paid" : r.State };
            Payments[payment.Key] = (requestId, payment.Amount);
        }

        return Task.CompletedTask;
    }

    public Task ReversePaymentAsync(Guid requestId, string paymentKey, CancellationToken cancellationToken)
    {
        var (id, amount) = Payments[paymentKey];
        Payments.Remove(paymentKey);
        var r = Requests[id];
        Requests[id] = r with { PaidAmount = r.PaidAmount - amount, State = "Approved" };
        return Task.CompletedTask;
    }
}

/// <summary>Receipts (owner, 14 Nov 2026): cash, bank deposits (slip, transfer, POS, card-to-card), cheques and notes.</summary>
public sealed class ReceiptUseCaseTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private readonly FakeBankAccounts _bankAccounts = new();
    private readonly FakeReceiptReference _reference = new();
    private readonly InMemoryVoucherStore _vouchers = new();
    private readonly ReceiptUseCases _receipts;

    public ReceiptUseCaseTests()
    {
        var uow = new InMemoryUnitOfWork(_db);
        _receipts = new ReceiptUseCases(
            uow, new InMemoryReceiptRepository(_db), new InMemoryInstrumentRepository(_db), new FakeReceiptAccounts(), _bankAccounts, _reference,
            new DevelopmentPolicies().OpenYear(1405), new InMemoryIdempotencyStore(_db), new User(),
            new InMemoryVoucherTemplateRepository(_vouchers), new InMemoryConfirmedVoucherRepository(_vouchers), new FakePaymentRequests());
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static ReceiptHeaderInput Header(string payer = "200015", Guid? group = null, string date = "1405/08/23") =>
        new(date, payer, group ?? FakeReceiptReference.SalesGroup, "فروش محصولات", Reference1: "فاکتور ۱۲۳", SalesProceeds: true);

    private static ReceivedInstrumentInput Cheque(string number, string due, Guid? bank = null, MoneyInput? amount = null) =>
        new(InstrumentKind.OrdinaryCheque, number, due, amount ?? new MoneyInput(10_000_000), bank ?? FakeReceiptReference.Mellat,
            BranchName: "کاشان", AccountNumber: "0202-3572", SayadId: "1234567890123456");

    [Fact]
    public async Task Cash_receipts_are_numbered_from_one_in_each_year()
    {
        var first = await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 5_000_000), CancellationToken.None);
        var second = await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 7_000_000), CancellationToken.None);
        Assert.True(first.IsSuccess, first.ErrorCode);
        var list = await _receipts.ListAsync(1405, ReceiptKind.Cash, CancellationToken.None);
        Assert.Equal([2, 1], list.Select(r => r.Number));
        var view = (await _receipts.GetAsync(first.Value, CancellationToken.None))!;
        Assert.Equal(("Cash", "200015", "شرکت نمونه بازرگانی", 5_000_000m, "IRR"), (view.Kind, view.PayerDetailCode, view.PayerName, view.TotalRial, view.Currency));
        Assert.True(view.SalesProceeds);
        Assert.Contains(("purpose-groups", FakeReceiptReference.SalesGroup), _reference.Marked);
        Assert.True(second.IsSuccess);
    }

    [Fact]
    public async Task A_cheque_on_a_bank_in_another_city_than_the_companys_is_out_of_town()
    {
        // Owner, 18 Nov 2026: the company is in Tehran; a cheque on a Shiraz bank is «خارج از مرکز», one on a Tehran bank is not, whatever
        // the user ticked. Arabic and Persian letters count as the same city name.
        _reference.CompanyCity = "تهران";
        var r = await _receipts.RecordInstrumentsAsync(new(Key(), Header(),
        [
            Cheque("1", "1405/10/01") with { City = "شیراز", OutOfTown = false },
            Cheque("2", "1405/10/01") with { City = "تهران", OutOfTown = true },
            Cheque("3", "1405/10/01") with { City = null, OutOfTown = true },
        ]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var cheques = (await _receipts.GetAsync(r.Value, CancellationToken.None))!.Instruments!;
        Assert.Equal([("1", true), ("2", false), ("3", true)], cheques.OrderBy(c => c.Number).Select(c => (c.Number!, c.Details!.OutOfTown)));

        _reference.CompanyCity = "شيراز"; // typed with the Arabic yeh
        var same = await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("4", "1405/10/01") with { City = "شیراز", OutOfTown = true }]), CancellationToken.None);
        Assert.False((await _receipts.GetAsync(same.Value, CancellationToken.None))!.Instruments![0].Details!.OutOfTown);

        // Until the company's city is entered in Basic Information, what the user ticked stays.
        _reference.CompanyCity = null;
        var manual = await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("5", "1405/10/01") with { City = "اصفهان", OutOfTown = true }]), CancellationToken.None);
        Assert.True((await _receipts.GetAsync(manual.Value, CancellationToken.None))!.Instruments![0].Details!.OutOfTown);
    }

    [Fact]
    public async Task Each_kind_has_its_own_numbers_as_in_the_old_program()
    {
        await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 1_000), CancellationToken.None);
        var bank = await _receipts.RecordBankDepositAsync(new(Key(), Header(), FakeReceiptAccounts.RialBank, DepositMethod.Pos, "778899", 2_000), CancellationToken.None);
        Assert.Equal(1, (await _receipts.GetAsync(bank.Value, CancellationToken.None))!.Number);
    }

    [Fact]
    public async Task A_deposit_onto_a_bank_card_has_its_own_numbers_and_names_its_card_report_6_item_12()
    {
        // Owner's report no. 6, item 12 («واریز به کارت بانک»), answers to Q68–Q70 (20 Nov 2026).
        await _receipts.RecordBankDepositAsync(new(Key(), Header(), FakeReceiptAccounts.RialBank, DepositMethod.CardToCard, null, 2_000), CancellationToken.None);
        var first = await _receipts.RecordCardDepositAsync(
            new(Key(), Header(), ReceiptAccountKind.PettyCashCard, FakeReceiptAccounts.PettyCashOnCard, DepositMethod.CardToCard, "T-77", 2_500_000), CancellationToken.None);
        Assert.True(first.IsSuccess, first.ErrorCode);
        var second = await _receipts.RecordCardDepositAsync(
            new(Key(), Header(), ReceiptAccountKind.CompanyCard, FakeReceiptAccounts.CompanyCard, DepositMethod.PaymentGateway, null, 900_000), CancellationToken.None);
        Assert.True(second.IsSuccess, second.ErrorCode);

        var v = (await _receipts.GetAsync(first.Value, CancellationToken.None))!;
        Assert.Equal(("CardDeposit", 1, "PettyCashCard", FakeReceiptAccounts.PettyCashOnCard, "CardToCard", "T-77", 2_500_000m, "IRR"),
            (v.Kind, v.Number, v.AccountKind, v.AccountId!.Value, v.Method, v.TrackingNumber, v.Amount!.Value, v.Currency));
        Assert.Equal(2, (await _receipts.GetAsync(second.Value, CancellationToken.None))!.Number);
        Assert.Equal(2, (await _receipts.ListAsync(1405, ReceiptKind.CardDeposit, CancellationToken.None)).Count);
        Assert.Single(await _receipts.ListAsync(1405, ReceiptKind.BankDeposit, CancellationToken.None));
        Assert.Contains(("purpose-groups", FakeReceiptReference.SalesGroup), _reference.Marked);
    }

    [Fact]
    public async Task A_card_deposit_is_refused_without_a_card_or_with_another_kind_of_account_or_deposit()
    {
        Task<Treasury.BuildingBlocks.Application.Result<Guid>> Record(ReceiptAccountKind kind, Guid id, DepositMethod method = DepositMethod.CardToCard) =>
            _receipts.RecordCardDepositAsync(new(Key(), Header(), kind, id, method, null, 1_000), CancellationToken.None);

        Assert.Equal("Receipt.AccountNotFound", (await Record(ReceiptAccountKind.PettyCashCard, Guid.NewGuid())).ErrorCode);
        // A petty cash whose holder keeps the money in cash has no card to pay onto.
        Assert.Equal("Receipt.AccountNotFound", (await Record(ReceiptAccountKind.PettyCashCard, FakeReceiptAccounts.PettyCashInCash)).ErrorCode);
        Assert.Equal("Receipt.AccountNotFound", (await Record(ReceiptAccountKind.CompanyCard, FakeReceiptAccounts.PettyCashOnCard)).ErrorCode);
        Assert.Equal("Receipt.AccountKindWrong", (await Record(ReceiptAccountKind.BankAccount, FakeReceiptAccounts.RialBank)).ErrorCode);
        Assert.Equal("Receipt.AccountInactive", (await Record(ReceiptAccountKind.CompanyCard, FakeReceiptAccounts.ClosedCompanyCard)).ErrorCode);
        Assert.Equal("Receipt.CardDepositMethod", (await Record(ReceiptAccountKind.CompanyCard, FakeReceiptAccounts.CompanyCard, DepositMethod.Satna)).ErrorCode);
        Assert.Equal("Receipt.CardDepositMethod",
            (await _receipts.PreviewCardDepositAsync(new("preview", Header(), ReceiptAccountKind.CompanyCard, FakeReceiptAccounts.CompanyCard, DepositMethod.Pos, null, 1_000), CancellationToken.None)).ErrorCode);
        Assert.Empty(await _receipts.ListAsync(1405, ReceiptKind.CardDeposit, CancellationToken.None));
    }

    [Fact]
    public async Task The_voucher_of_a_card_deposit_is_previewed_and_the_receipt_is_corrected_under_its_number()
    {
        // Answer to Q68: Dr the petty cash's account · holder / Cr the purpose group · payer.
        var preview = await _receipts.PreviewCardDepositAsync(
            new("preview", Header(), ReceiptAccountKind.PettyCashCard, FakeReceiptAccounts.PettyCashOnCard, DepositMethod.DepositSlip, "F-1", 2_500_000), CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.ErrorCode);
        Assert.Equal([(true, "1104", "700002", 2_500_000m), (false, "4101", "200015", 2_500_000m)],
            preview.Value!.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode, l.AmountRial)));
        Assert.Empty(await _receipts.ListAsync(1405, ReceiptKind.CardDeposit, CancellationToken.None)); // a preview saves nothing

        var id = (await _receipts.RecordCardDepositAsync(
            new(Key(), Header(), ReceiptAccountKind.PettyCashCard, FakeReceiptAccounts.PettyCashOnCard, DepositMethod.DepositSlip, "F-1", 2_500_000), CancellationToken.None)).Value;
        var corrected = await _receipts.CorrectCardDepositAsync(
            new(Key(), id, 1, Header(), ReceiptAccountKind.CompanyCard, FakeReceiptAccounts.CompanyCard, DepositMethod.CardToCard, null, 3_000_000), CancellationToken.None);
        Assert.True(corrected.IsSuccess, corrected.ErrorCode);
        var v = (await _receipts.GetAsync(id, CancellationToken.None))!;
        Assert.Equal((1, "CompanyCard", FakeReceiptAccounts.CompanyCard, "CardToCard", (string?)null, 3_000_000m),
            (v.Number, v.AccountKind, v.AccountId!.Value, v.Method, v.TrackingNumber, v.TotalRial));
        Assert.Equal("Concurrency.Conflict", (await _receipts.CorrectCardDepositAsync(
            new(Key(), id, 1, Header(), ReceiptAccountKind.CompanyCard, FakeReceiptAccounts.CompanyCard, DepositMethod.CardToCard, null, 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.CorrectionKindDiffers", (await _receipts.CorrectBankDepositAsync(
            new(Key(), id, v.Version, Header(), FakeReceiptAccounts.RialBank, DepositMethod.Pos, null, 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_bank_deposit_marks_the_bank_account_in_use()
    {
        var r = await _receipts.RecordBankDepositAsync(new(Key(), Header(), FakeReceiptAccounts.RialBank, DepositMethod.CardToCard, null, 2_000_000), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Contains(FakeReceiptAccounts.RialBank, _bankAccounts.InUse);
        var v = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(("BankDeposit", "CardToCard"), (v.Kind, v.Method));
    }

    [Fact]
    public async Task A_Paya_transfer_keeps_its_Sheba_and_card_to_card_needs_an_account_with_a_card()
    {
        // Owner, 18 Nov 2026: the kinds of a direct deposit.
        var paya = await _receipts.RecordBankDepositAsync(
            new(Key(), Header(), FakeReceiptAccounts.RialBank, DepositMethod.Paya, "P-1", 2_000_000, PayerSheba: "IR06 2960 0000 0010 0324 2000 01"), CancellationToken.None);
        Assert.True(paya.IsSuccess, paya.ErrorCode);
        var v = (await _receipts.GetAsync(paya.Value, CancellationToken.None))!;
        Assert.Equal(("Paya", "IR062960000000100324200001"), (v.Method, v.PayerSheba));
        Assert.Equal("IR062960000000100324200001", (await _receipts.ListAsync(1405, ReceiptKind.BankDeposit, CancellationToken.None)).Single().PayerSheba);

        Assert.Equal("Receipt.ShebaRequired",
            (await _receipts.RecordBankDepositAsync(new(Key(), Header(), FakeReceiptAccounts.RialBank, DepositMethod.Satna, null, 1_000), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.ShebaRequired",
            (await _receipts.PreviewBankDepositAsync(new("preview", Header(), FakeReceiptAccounts.RialBank, DepositMethod.Satna, null, 1_000), CancellationToken.None)).ErrorCode);
        Assert.True((await _receipts.RecordBankDepositAsync(new(Key(), Header(), FakeReceiptAccounts.RialBank, DepositMethod.PaymentGateway, "PG-7", 1_000), CancellationToken.None)).IsSuccess);

        // The dollar account of this test has no bank card (and takes no card payments anyway): the Rial rule is checked with it switched.
        Assert.Equal("Receipt.MethodNotForForeignCurrency",
            (await _receipts.RecordBankDepositAsync(new(Key(), Header(), FakeReceiptAccounts.DollarBank, DepositMethod.CardToCard, null, 10, 600_000), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_dollar_account_takes_dollars_at_a_rate()
    {
        var r = await _receipts.RecordBankDepositAsync(new(Key(), Header(), FakeReceiptAccounts.DollarBank, DepositMethod.InternalTransfer, "SW-1", 1_500.25m, 600_000), CancellationToken.None);
        var v = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(("USD", 1_500.25m, 900_150_000m), (v.Currency, v.Amount, v.TotalRial));
        Assert.Equal("Money.RateRequired", (await _receipts.RecordBankDepositAsync(new(Key(), Header(), FakeReceiptAccounts.DollarBank, DepositMethod.Transfer, null, 10), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Payer_purpose_group_and_account_are_checked_against_basic_information()
    {
        Assert.Equal("Receipt.PayerNotFound", (await _receipts.RecordCashAsync(new(Key(), Header(payer: "999999"), FakeReceiptAccounts.Desk, 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.PayerInactive", (await _receipts.RecordCashAsync(new(Key(), Header(payer: "200099"), FakeReceiptAccounts.Desk, 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.PayerTypeNotAllowed", (await _receipts.RecordCashAsync(new(Key(), Header(payer: "300001"), FakeReceiptAccounts.Desk, 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.PurposeGroupNotForReceipts", (await _receipts.RecordCashAsync(new(Key(), Header(group: FakeReceiptReference.PaymentGroup), FakeReceiptAccounts.Desk, 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.AccountNotFound", (await _receipts.RecordCashAsync(new(Key(), Header(), Guid.NewGuid(), 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.AccountInactive", (await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.ClosedDesk, 1), CancellationToken.None)).ErrorCode);
        Assert.Equal("FiscalPeriod.Closed", (await _receipts.RecordCashAsync(new(Key(), Header(date: "1404/12/01"), FakeReceiptAccounts.Desk, 1), CancellationToken.None)).ErrorCode);
        _reference.Down = true;
        Assert.Equal("Reference.Unavailable", (await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 1), CancellationToken.None)).ErrorCode);
        Assert.Empty(await _receipts.ListAsync(1405, null, CancellationToken.None));
    }

    [Fact]
    public async Task Several_cheques_and_a_note_from_one_payer_are_saved_with_their_receipt_in_one_go()
    {
        var r = await _receipts.RecordInstrumentsAsync(new(Key(), Header(),
        [
            Cheque("000123", "1405/08/23"),
            Cheque("000124", "1405/11/30", FakeReceiptReference.Melli, new MoneyInput(500, "EUR", 700_000)),
            new(InstrumentKind.PromissoryNote, "N-77", "1406/02/01", new MoneyInput(30_000_000), Guarantors: [new("رضا احمدی", "0123456789")]),
        ]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal((1, 3, 10_000_000m + 350_000_000m + 30_000_000m), (v.Number, v.InstrumentCount, v.TotalRial));
        var lines = v.Instruments!;
        Assert.Equal(["000123", "000124", "N-77"], lines.Select(i => i.Number!).Order());
        var current = lines.Single(i => i.Number == "000123");
        Assert.Equal(("InCashDesk", false, "بانک ملت", "1234567890123456"), (current.State, current.IsPostDated, current.BankName, current.Details!.SayadId));
        Assert.True(lines.Single(i => i.Number == "000124").IsPostDated);
        Assert.Equal("EUR", lines.Single(i => i.Number == "000124").Currency);
        Assert.Null(lines.Single(i => i.Number == "N-77").BankName);
        Assert.Contains(("banks", FakeReceiptReference.Mellat), _reference.Marked);
        Assert.Contains(("banks", FakeReceiptReference.Melli), _reference.Marked);
    }

    [Fact]
    public async Task One_bad_cheque_saves_nothing()
    {
        var r = await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("000123", "1405/09/01"), Cheque("000124", "1405/09/01") with { SayadId = "123" }]), CancellationToken.None);
        Assert.Equal("Instrument.SayadIdInvalid", r.ErrorCode);
        Assert.Empty(await _receipts.ListAsync(1405, null, CancellationToken.None));
        Assert.Empty(_db.OutboxMessages);
    }

    [Fact]
    public async Task The_same_cheque_twice_on_a_receipt_is_refused_a_cheque_seen_before_is_a_warning()
    {
        Assert.Equal("Receipt.SameInstrumentTwice",
            (await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("000123", "1405/09/01"), Cheque("000123", "1405/09/01")]), CancellationToken.None)).ErrorCode);
        Assert.Empty((await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("000123", "1405/09/01")]), CancellationToken.None)).Warnings);
        var again = await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("000123", "1405/09/01")]), CancellationToken.None);
        Assert.Equal([InstrumentUseCases.PossibleDuplicateWarning], again.Warnings);
        Assert.Equal(2, (await _receipts.GetAsync(again.Value, CancellationToken.None))!.Number);
    }

    [Fact]
    public async Task An_unknown_bank_or_no_cheques_or_a_cheque_without_its_Sayad_ID_is_refused()
    {
        Assert.Equal("Receipt.SayadIdRequired", (await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("1", "1405/09/01") with { SayadId = null }]), CancellationToken.None)).ErrorCode);
        Assert.Equal("Instrument.BankNotFound", (await _receipts.RecordInstrumentsAsync(new(Key(), Header(), [Cheque("1", "1405/09/01", Guid.NewGuid())]), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.NoInstruments", (await _receipts.RecordInstrumentsAsync(new(Key(), Header(), []), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_cash_receipt_is_cancelled_with_its_version_and_a_reason()
    {
        var id = (await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 5_000), CancellationToken.None)).Value;
        Assert.Equal("Concurrency.Conflict", (await _receipts.CancelAsync(new(Key(), id, "1405/08/24", "اشتباه", 9), CancellationToken.None)).ErrorCode);
        Assert.True((await _receipts.CancelAsync(new(Key(), id, "1405/08/24", "مبلغ اشتباه", 1), CancellationToken.None)).IsSuccess);
        var v = (await _receipts.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(("Cancelled", "1405/08/24", "مبلغ اشتباه", 2L), (v.State, v.CancelledOn, v.CancelReason, v.Version));
        Assert.Equal("Receipt.NotFound", (await _receipts.CancelAsync(new(Key(), Guid.NewGuid(), "1405/08/24", "x", 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_same_key_twice_records_one_receipt()
    {
        var key = Key();
        var a = await _receipts.RecordCashAsync(new(key, Header(), FakeReceiptAccounts.Desk, 5_000), CancellationToken.None);
        var b = await _receipts.RecordCashAsync(new(key, Header(), FakeReceiptAccounts.Desk, 5_000), CancellationToken.None);
        Assert.Equal(a.Value, b.Value);
        Assert.Single(await _receipts.ListAsync(1405, null, CancellationToken.None));
    }

    private sealed class User : ICurrentUser
    {
        public string UserId => "clerk-1";
    }
}
