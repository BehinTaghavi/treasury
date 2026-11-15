using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>Receipts (owner, 14 Nov 2026: "different types of receipt forms"); old Access forms D_Naghd, D_variz, D_Check.</summary>
public sealed class ReceiptTests
{
    private static readonly PayerFact Payer = new("200015", "شرکت نمونه بازرگانی", true);
    private static readonly PurposeGroupFact Sales = new(Guid.NewGuid(), "وجوه حاصل فروش", IsForReceipts: true, IsActive: true);
    private static readonly ReceiptAccountFact Desk = new(ReceiptAccountKind.CashDesk, Guid.NewGuid(), "صندوق مرکزی", "IRR", true);
    private static readonly ReceiptAccountFact Bank = new(ReceiptAccountKind.BankAccount, Guid.NewGuid(), "0202-3572", "IRR", true);
    private static readonly ReceiptAccountFact DollarBank = new(ReceiptAccountKind.BankAccount, Guid.NewGuid(), "USD-1", "USD", true);

    private static ReceiptHeader Header(string date = "1405/08/23", PayerFact? payer = null, PurposeGroupFact? group = null, string purpose = "فروش محصولات") =>
        new(D(date), payer ?? Payer, group ?? Sales, purpose, Reference1: "فاکتور ۱۲۳");

    private static Instrument Cheque(Guid receiptId, string due, InstrumentKind kind = InstrumentKind.OrdinaryCheque, Money? amount = null, ReceivedInstrumentDetails? details = null, string date = "1405/08/23") =>
        Instrument.Receive(Guid.NewGuid(), kind, "123456", kind.IsCheque() ? "بانک ملت" : null, amount ?? Money.Rials(10_000_000), D(date), D(due),
            CounterpartyRef.ForDetail(Payer.DetailCode, Payer.Name), "فروش محصولات", User,
            details ?? (kind.IsCheque() ? new ReceivedInstrumentDetails(SayadId: "1234567890123456") : ReceivedInstrumentDetails.None), receiptId);

    [Fact]
    public void Cash_is_received_into_a_cash_desk_D_Naghd()
    {
        var r = Receipt.RecordCash(Guid.NewGuid(), 1, Header(), Desk, Money.Rials(5_000_000), User);
        Assert.Equal(ReceiptKind.Cash, r.Kind);
        Assert.Equal(1405, r.FiscalYear);
        Assert.Equal(5_000_000m, r.TotalRial);
        Assert.Equal(Desk.Id, r.AccountId);
        Assert.Equal("200015", r.Payer.DetailCode);
        Assert.Equal("فاکتور ۱۲۳", r.Reference1);
        Assert.IsType<ReceiptRecorded>(Assert.Single(r.PendingEvents));
    }

    [Fact]
    public void Cash_cannot_go_into_a_bank_account() =>
        Assert.Equal("Receipt.AccountKindWrong", Assert.Throws<DomainException>(() => Receipt.RecordCash(Guid.NewGuid(), 1, Header(), Bank, Money.Rials(1), User)).Code);

    [Theory]
    [InlineData(DepositMethod.DepositSlip)]
    [InlineData(DepositMethod.Transfer)]
    [InlineData(DepositMethod.Pos)]
    [InlineData(DepositMethod.CardToCard)]
    public void Money_paid_into_a_bank_account_D_variz_POS_card_to_card(DepositMethod method)
    {
        var r = Receipt.RecordBankDeposit(Guid.NewGuid(), 7, Header(), Bank, method, " 998877 ", Money.Rials(3_000_000), User);
        Assert.Equal(method, r.Method);
        Assert.Equal("998877", r.TrackingNumber);
        Assert.Equal(7, r.Number);
    }

    [Fact]
    public void A_foreign_currency_account_takes_its_own_currency_with_the_rial_equivalent()
    {
        var r = Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), DollarBank, DepositMethod.Transfer, null, Money.Foreign(1_000.50m, "USD", 600_000), User);
        Assert.Equal(600_300_000m, r.TotalRial);
        Assert.Equal("Receipt.CurrencyMismatch", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), DollarBank, DepositMethod.Transfer, null, Money.Rials(1_000), User)).Code);
    }

    [Theory]
    [InlineData(DepositMethod.Pos)]
    [InlineData(DepositMethod.CardToCard)]
    public void A_foreign_currency_account_takes_cash_deposits_and_transfers_only_report_4_item_7(DepositMethod method) =>
        Assert.Equal("Receipt.MethodNotForForeignCurrency", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), DollarBank, method, null, Money.Foreign(10, "USD", 600_000), User)).Code);

    [Fact]
    public void Old_required_fields_payer_purpose_D_Naghd()
    {
        Assert.Equal("Receipt.PayerRequired", Assert.Throws<DomainException>(() =>
            Receipt.RecordCash(Guid.NewGuid(), 1, Header(payer: new PayerFact(" ", "x", true)), Desk, Money.Rials(1), User)).Code);
        Assert.Equal("Receipt.PurposeRequired", Assert.Throws<DomainException>(() =>
            Receipt.RecordCash(Guid.NewGuid(), 1, Header(purpose: " "), Desk, Money.Rials(1), User)).Code);
        Assert.Equal("Receipt.PayerInactive", Assert.Throws<DomainException>(() =>
            Receipt.RecordCash(Guid.NewGuid(), 1, Header(payer: Payer with { IsActive = false }), Desk, Money.Rials(1), User)).Code);
    }

    [Fact]
    public void Only_a_receipt_purpose_group_that_is_switched_on()
    {
        Assert.Equal("Receipt.PurposeGroupNotForReceipts", Assert.Throws<DomainException>(() =>
            Receipt.RecordCash(Guid.NewGuid(), 1, Header(group: Sales with { IsForReceipts = false }), Desk, Money.Rials(1), User)).Code);
        Assert.Equal("Receipt.PurposeGroupInactive", Assert.Throws<DomainException>(() =>
            Receipt.RecordCash(Guid.NewGuid(), 1, Header(group: Sales with { IsActive = false }), Desk, Money.Rials(1), User)).Code);
    }

    [Fact]
    public void A_switched_off_cash_desk_takes_no_receipts() =>
        Assert.Equal("Receipt.AccountInactive", Assert.Throws<DomainException>(() =>
            Receipt.RecordCash(Guid.NewGuid(), 1, Header(), Desk with { IsActive = false }, Money.Rials(1), User)).Code);

    [Fact]
    public void Several_cheques_of_one_payer_share_one_receipt_number()
    {
        var id = Guid.NewGuid();
        var current = Cheque(id, "1405/08/23");
        var postDated = Cheque(id, "1405/11/30", amount: Money.Foreign(500, "EUR", 700_000));
        var r = Receipt.RecordInstruments(id, 12, Header(), [current, postDated], User);
        Assert.Equal(2, r.InstrumentIds.Count);
        Assert.Equal(10_000_000m + 350_000_000m, r.TotalRial);
        Assert.False(current.IsPostDated);
        Assert.True(postDated.IsPostDated);
        Assert.Equal(current.Counterparty.Id, r.Payer.Id);
    }

    [Fact]
    public void Every_cheque_on_a_receipt_needs_its_Sayad_ID_owner_answer_Q21()
    {
        var id = Guid.NewGuid();
        var noSayad = Cheque(id, "1405/09/01", details: ReceivedInstrumentDetails.None);
        Assert.Equal("Receipt.SayadIdRequired", Assert.Throws<DomainException>(() => Receipt.RecordInstruments(id, 1, Header(), [noSayad], User)).Code);
        var note = Cheque(id, "1405/09/01", InstrumentKind.PromissoryNote);
        Assert.Single(Receipt.RecordInstruments(id, 1, Header(), [note], User).InstrumentIds);
    }

    [Fact]
    public void Only_customers_persons_and_companies_pay_owner_answer_Q22() =>
        Assert.Equal("Receipt.PayerTypeNotAllowed", Assert.Throws<DomainException>(() =>
            Receipt.RecordCash(Guid.NewGuid(), 1, Header(payer: Payer with { MayPay = false }), Desk, Money.Rials(1), User)).Code);

    [Fact]
    public void A_receipt_of_cheques_needs_at_least_one() =>
        Assert.Equal("Receipt.NoInstruments", Assert.Throws<DomainException>(() => Receipt.RecordInstruments(Guid.NewGuid(), 1, Header(), [], User)).Code);

    [Fact]
    public void Cheques_must_belong_to_the_receipt()
    {
        var other = Cheque(Guid.NewGuid(), "1405/09/01");
        Assert.Equal("Receipt.InstrumentMismatch", Assert.Throws<DomainException>(() => Receipt.RecordInstruments(Guid.NewGuid(), 1, Header(), [other], User)).Code);
        var id = Guid.NewGuid();
        var otherDay = Cheque(id, "1405/09/01", date: "1405/08/22");
        Assert.Equal("Receipt.InstrumentMismatch", Assert.Throws<DomainException>(() => Receipt.RecordInstruments(id, 1, Header(), [otherDay], User)).Code);
    }

    [Fact]
    public void A_cash_receipt_typed_by_mistake_is_cancelled_with_a_reason()
    {
        var r = Receipt.RecordCash(Guid.NewGuid(), 1, Header(), Desk, Money.Rials(5_000_000), User);
        Assert.Equal("Receipt.CancelReasonRequired", Assert.Throws<DomainException>(() => r.Cancel(D("1405/08/24"), " ", User)).Code);
        Assert.Equal("Receipt.CancelBeforeReceipt", Assert.Throws<DomainException>(() => r.Cancel(D("1405/08/22"), "اشتباه", User)).Code);
        r.Cancel(D("1405/08/24"), "مبلغ اشتباه", User);
        Assert.Equal(ReceiptState.Cancelled, r.State);
        Assert.Equal("Receipt.AlreadyCancelled", Assert.Throws<DomainException>(() => r.Cancel(D("1405/08/24"), "دوباره", User)).Code);
    }

    [Fact]
    public void A_receipt_of_cheques_is_not_cancelled_each_cheque_is_returned_instead()
    {
        var id = Guid.NewGuid();
        var r = Receipt.RecordInstruments(id, 1, Header(), [Cheque(id, "1405/09/01")], User);
        Assert.Equal("Receipt.CancelInstruments", Assert.Throws<DomainException>(() => r.Cancel(D("1405/08/24"), "اشتباه", User)).Code);
    }
}

/// <summary>What is written on a received cheque or note (new-version manual, form «دریافت چک و اسناد دریافتنی»).</summary>
public sealed class ReceivedInstrumentDetailsTests
{
    private static Instrument Receive(InstrumentKind kind, string? bank, ReceivedInstrumentDetails details) =>
        Instrument.Receive(Guid.NewGuid(), kind, "000123", bank, Money.Rials(1_000_000), D("1405/08/23"), kind.IsGuarantee() ? null : D("1405/10/01"),
            CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User, details, Guid.NewGuid());

    [Fact]
    public void A_cheque_keeps_branch_account_holder_and_Sayad_ID()
    {
        var i = Receive(InstrumentKind.OrdinaryCheque, "بانک ملت",
            new(BranchName: " کاشان ", BranchCode: "123", AccountNumber: "0202-3572", AccountHolder: "علی رضایی", City: "کاشان", OutOfTown: true, SayadId: "1234567890123456"));
        Assert.Equal("کاشان", i.Details.BranchName);
        Assert.True(i.Details.OutOfTown);
        Assert.Equal("1234567890123456", i.Details.SayadId);
        Assert.Equal("000123", i.Number);
    }

    [Theory]
    [InlineData("123456789012345")]
    [InlineData("12345678901234567")]
    [InlineData("12345678901234ab")]
    public void The_Sayad_ID_has_16_digits(string sayad) =>
        Assert.Equal("Instrument.SayadIdInvalid", Assert.Throws<DomainException>(() => Receive(InstrumentKind.OrdinaryCheque, "بانک ملت", new(SayadId: sayad))).Code);

    [Fact]
    public void A_cheque_needs_its_bank_a_note_does_not()
    {
        Assert.Equal("Instrument.BankRequired", Assert.Throws<DomainException>(() => Receive(InstrumentKind.OrdinaryCheque, null, ReceivedInstrumentDetails.None)).Code);
        var note = Receive(InstrumentKind.PromissoryNote, null, new(Guarantors: [new("رضا احمدی", "0123456789"), new("شرکت ضامن", "10101010103")]));
        Assert.Null(note.BankName);
        Assert.Equal(2, note.Details.GuarantorList.Count);
    }

    [Fact]
    public void A_note_has_at_most_two_guarantors_with_valid_national_codes()
    {
        Assert.Equal("Instrument.TooManyGuarantors", Assert.Throws<DomainException>(() =>
            Receive(InstrumentKind.PromissoryNote, null, new(Guarantors: [new("الف", null), new("ب", null), new("ج", null)]))).Code);
        Assert.Equal("Instrument.GuarantorNationalCodeInvalid", Assert.Throws<DomainException>(() =>
            Receive(InstrumentKind.GuaranteeNote, null, new(Guarantors: [new("الف", "0123456788")]))).Code);
        Assert.Equal("Instrument.GuarantorNameRequired", Assert.Throws<DomainException>(() =>
            Receive(InstrumentKind.PromissoryNote, null, new(Guarantors: [new(" ", "0123456789")]))).Code);
    }

    [Fact]
    public void Guarantors_belong_to_notes_and_Sayad_IDs_to_cheques()
    {
        Assert.Equal("Instrument.GuarantorsOnlyForNotes", Assert.Throws<DomainException>(() =>
            Receive(InstrumentKind.OrdinaryCheque, "بانک ملت", new(Guarantors: [new("الف", null)]))).Code);
        Assert.Equal("Instrument.SayadIdOnlyForCheques", Assert.Throws<DomainException>(() =>
            Receive(InstrumentKind.PromissoryNote, null, new(SayadId: "1234567890123456"))).Code);
    }

    [Fact]
    public void The_same_tafsil_always_gives_the_same_party_id()
    {
        Assert.Equal(CounterpartyRef.ForDetail("200015", "الف").Id, CounterpartyRef.ForDetail(" 200015", "ب").Id);
        Assert.NotEqual(CounterpartyRef.ForDetail("200015", "الف").Id, CounterpartyRef.ForDetail("200016", "الف").Id);
    }
}
