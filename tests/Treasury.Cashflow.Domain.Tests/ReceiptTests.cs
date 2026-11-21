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

    private static readonly ReceiptAccountFact HolderCard = new(ReceiptAccountKind.PettyCashCard, Guid.NewGuid(), "مریم کریمی – 6219861034529007", "IRR", true, "1104", "700002");
    private static readonly ReceiptAccountFact CompanyCard = new(ReceiptAccountKind.CompanyCard, Guid.NewGuid(), "شرکت نمونه – 6037991234567893", "IRR", true, "1105", "600009");

    [Theory]
    [InlineData(DepositMethod.CardToCard)]
    [InlineData(DepositMethod.DepositSlip)]
    [InlineData(DepositMethod.PaymentGateway)]
    public void Money_is_paid_onto_the_card_of_a_petty_cash_holder_or_a_company_card_report_6_item_12(DepositMethod method)
    {
        // Owner's report no. 6, item 12 («واریز به کارت بانک»); answer to Q70: card to card, a deposit slip or the company's gateway.
        var r = Receipt.RecordCardDeposit(Guid.NewGuid(), 3, Header(), HolderCard, method, " 5544 ", Money.Rials(2_500_000), User);
        Assert.Equal((ReceiptKind.CardDeposit, ReceiptAccountKind.PettyCashCard, HolderCard.Id, method, "5544", 3, 2_500_000m),
            (r.Kind, r.AccountKind!.Value, r.AccountId!.Value, r.Method!.Value, r.TrackingNumber, r.Number, r.TotalRial));
        Assert.Null(r.PayerSheba);
        Assert.IsType<ReceiptRecorded>(Assert.Single(r.PendingEvents));
        Assert.Equal(ReceiptAccountKind.CompanyCard, Receipt.RecordCardDeposit(Guid.NewGuid(), 4, Header(), CompanyCard, method, null, Money.Rials(1), User).AccountKind);
    }

    [Fact]
    public void A_card_deposit_names_a_card_an_allowed_kind_of_deposit_and_an_active_card()
    {
        Assert.Equal("Receipt.AccountKindWrong", Assert.Throws<DomainException>(() =>
            Receipt.RecordCardDeposit(Guid.NewGuid(), 1, Header(), Bank, DepositMethod.CardToCard, null, Money.Rials(1), User)).Code);
        Assert.Equal("Receipt.AccountKindWrong", Assert.Throws<DomainException>(() =>
            Receipt.RecordCardDeposit(Guid.NewGuid(), 1, Header(), Desk, DepositMethod.CardToCard, null, Money.Rials(1), User)).Code);
        foreach (var method in new[] { DepositMethod.Paya, DepositMethod.Satna, DepositMethod.InternalTransfer, DepositMethod.Pos, DepositMethod.Transfer })
        {
            Assert.Equal("Receipt.CardDepositMethod", Assert.Throws<DomainException>(() =>
                Receipt.RecordCardDeposit(Guid.NewGuid(), 1, Header(), HolderCard, method, null, Money.Rials(1), User)).Code);
        }

        Assert.Equal("Receipt.DepositMethodRequired", Assert.Throws<DomainException>(() =>
            Receipt.RecordCardDeposit(Guid.NewGuid(), 1, Header(), HolderCard, (DepositMethod)99, null, Money.Rials(1), User)).Code);
        Assert.Equal("Receipt.AccountInactive", Assert.Throws<DomainException>(() =>
            Receipt.RecordCardDeposit(Guid.NewGuid(), 1, Header(), CompanyCard with { IsActive = false }, DepositMethod.CardToCard, null, Money.Rials(1), User)).Code);
        Assert.Equal("Receipt.CurrencyMismatch", Assert.Throws<DomainException>(() =>
            Receipt.RecordCardDeposit(Guid.NewGuid(), 1, Header(), HolderCard, DepositMethod.CardToCard, null, Money.Foreign(10, "USD", 600_000), User)).Code);

        // A bank deposit still goes into a bank account, and cash into a cash desk.
        Assert.Equal("Receipt.AccountKindWrong", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), CompanyCard, DepositMethod.CardToCard, null, Money.Rials(1), User)).Code);
    }

    [Fact]
    public void The_voucher_of_a_card_deposit_debits_the_card_and_credits_the_purpose_group_answers_Q68_Q69()
    {
        // Owner, 20 Nov 2026 (Q68): Dr the petty cash's account with the holder's تفصیل / Cr the purpose group with the payer's تفصیل;
        // (Q69) a company card without a bank account debits its own معین and تفصیل.
        var template = new Treasury.Cashflow.Domain.Vouchers.VoucherTemplate();
        var onHolder = Receipt.RecordCardDeposit(Guid.NewGuid(), 1, Header(), HolderCard, DepositMethod.CardToCard, null, Money.Rials(2_500_000), User);
        var v = Treasury.Cashflow.Domain.Vouchers.ReceiptVouchers.ForReceipt(onHolder, [], new(HolderCard.LedgerAccountCode, HolderCard.DetailCode), "4101", template);
        Assert.Equal([(true, "1104", "700002", 2_500_000m), (false, "4101", "200015", 2_500_000m)], v.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode, l.AmountRial)));
        Assert.StartsWith("واریز به کارت بانک", v.Operation, StringComparison.Ordinal);

        var onCompany = Receipt.RecordCardDeposit(Guid.NewGuid(), 2, Header(), CompanyCard, DepositMethod.PaymentGateway, null, Money.Rials(900_000), User);
        var w = Treasury.Cashflow.Domain.Vouchers.ReceiptVouchers.ForReceipt(onCompany, [], new(CompanyCard.LedgerAccountCode, CompanyCard.DetailCode), "4101", template);
        Assert.Equal([(true, "1105", "600009"), (false, "4101", "200015")], w.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
    }

    [Fact]
    public void A_card_deposit_is_corrected_until_its_voucher_is_confirmed_and_cancelled_like_a_bank_deposit()
    {
        var r = Receipt.RecordCardDeposit(Guid.NewGuid(), 1, Header(), HolderCard, DepositMethod.CardToCard, "T-1", Money.Rials(2_500_000), User);
        r.CorrectCardDeposit(Header(purpose: "علی‌الحساب"), CompanyCard, DepositMethod.DepositSlip, "F-9", Money.Rials(3_000_000), User, voucherConfirmed: false);
        Assert.Equal((ReceiptAccountKind.CompanyCard, CompanyCard.Id, DepositMethod.DepositSlip, "F-9", 3_000_000m, "علی‌الحساب", 1),
            (r.AccountKind!.Value, r.AccountId!.Value, r.Method!.Value, r.TrackingNumber, r.TotalRial, r.Purpose, r.Number));
        Assert.Equal("Receipt.CardDepositMethod", Assert.Throws<DomainException>(() =>
            r.CorrectCardDeposit(Header(), CompanyCard, DepositMethod.Paya, null, Money.Rials(1), User, voucherConfirmed: false)).Code);
        Assert.Equal("Receipt.VoucherConfirmed", Assert.Throws<DomainException>(() =>
            r.CorrectCardDeposit(Header(), CompanyCard, DepositMethod.CardToCard, null, Money.Rials(1), User, voucherConfirmed: true)).Code);
        Assert.Equal("Receipt.CorrectionKindDiffers", Assert.Throws<DomainException>(() =>
            r.CorrectBankDeposit(Header(), Bank, DepositMethod.Pos, null, Money.Rials(1), User, null, voucherConfirmed: false)).Code);
        r.Cancel(D("1405/08/24"), "ثبت اشتباه", User);
        Assert.Equal(ReceiptState.Cancelled, r.State);
    }

    [Fact]
    public void Cash_cannot_go_into_a_bank_account() =>
        Assert.Equal("Receipt.AccountKindWrong", Assert.Throws<DomainException>(() => Receipt.RecordCash(Guid.NewGuid(), 1, Header(), Bank, Money.Rials(1), User)).Code);

    [Theory]
    [InlineData(DepositMethod.DepositSlip)]
    [InlineData(DepositMethod.InternalTransfer)]
    [InlineData(DepositMethod.Pos)]
    [InlineData(DepositMethod.CardToCard)]
    [InlineData(DepositMethod.PaymentGateway)]
    public void Money_paid_into_a_bank_account_D_variz_POS_card_to_card(DepositMethod method)
    {
        var r = Receipt.RecordBankDeposit(Guid.NewGuid(), 7, Header(), Bank with { HasCards = true }, method, " 998877 ", Money.Rials(3_000_000), User);
        Assert.Equal(method, r.Method);
        Assert.Equal("998877", r.TrackingNumber);
        Assert.Equal(7, r.Number);
        Assert.Null(r.PayerSheba);
    }

    // Owner, 18 Nov 2026: the kinds of a direct deposit. A made-up Sheba with valid check digits.
    private const string Sheba = "IR062960000000100324200001";

    [Theory]
    [InlineData(DepositMethod.Paya)]
    [InlineData(DepositMethod.Satna)]
    public void A_Paya_or_Satna_transfer_is_recorded_with_its_Sheba(DepositMethod method)
    {
        var r = Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank, method, "T-1", Money.Rials(3_000_000), User, " ir06 2960 0000 0010 0324 2000 01 ");
        Assert.Equal((method, Sheba), (r.Method!.Value, r.PayerSheba));

        // The 24 digits alone are accepted too.
        Assert.Equal(Sheba, Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank, method, null, Money.Rials(1), User, Sheba[2..]).PayerSheba);
        Assert.Equal("Receipt.ShebaRequired", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank, method, null, Money.Rials(1), User, " ")).Code);
        // Owner's report no. 6, item 17: 24 digits with a wrong digit are refused for their check digits, not for their length.
        Assert.Equal("Receipt.ShebaCheckDigits", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank, method, null, Money.Rials(1), User, "IR062960000000100324200002")).Code);
        Assert.Equal("Receipt.ShebaCheckDigits", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank, method, null, Money.Rials(1), User, "123456789012345678901234")).Code);
        // Persian digits are read.
        Assert.Equal(Sheba, Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank, method, null, Money.Rials(1), User, "۰۶۲۹۶۰۰۰۰۰۰۰۱۰۰۳۲۴۲۰۰۰۰۱").PayerSheba);
        Assert.Equal("Receipt.ShebaInvalid", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank, method, null, Money.Rials(1), User, "IR0629600000001003242")).Code);
    }

    [Fact]
    public void Card_to_card_needs_a_bank_account_with_a_bank_card()
    {
        Assert.Equal("Receipt.AccountHasNoCard", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank with { HasCards = false }, DepositMethod.CardToCard, null, Money.Rials(1), User)).Code);
        Assert.Equal(DepositMethod.CardToCard,
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank with { HasCards = true }, DepositMethod.CardToCard, null, Money.Rials(1), User).Method);
    }

    [Fact]
    public void The_old_combined_transfer_kind_is_not_chosen_for_a_new_receipt_but_old_receipts_keep_it()
    {
        Assert.Equal("Receipt.DepositMethodRequired", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank, DepositMethod.Transfer, null, Money.Rials(1), User)).Code);
        var old = Receipt.Restore(Guid.NewGuid(), ReceiptKind.BankDeposit, 3, D("1405/08/20"), CounterpartyRef.ForDetail("200015", "شرکت نمونه"), Guid.NewGuid(), "فروش", "فروش",
            null, null, null, false, null, User, ReceiptAccountKind.BankAccount, Guid.NewGuid(), DepositMethod.Transfer, "T-9", Money.Rials(5), [], 5, ReceiptState.Recorded, null, null, 1);
        Assert.Equal(DepositMethod.Transfer, old.Method);
    }

    [Fact]
    public void A_foreign_currency_account_takes_its_own_currency_with_the_rial_equivalent()
    {
        var r = Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), DollarBank, DepositMethod.InternalTransfer, null, Money.Foreign(1_000.50m, "USD", 600_000), User);
        Assert.Equal(600_300_000m, r.TotalRial);
        Assert.Equal("Receipt.CurrencyMismatch", Assert.Throws<DomainException>(() =>
            Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), DollarBank, DepositMethod.InternalTransfer, null, Money.Rials(1_000), User)).Code);
    }

    [Theory]
    [InlineData(DepositMethod.Pos)]
    [InlineData(DepositMethod.CardToCard)]
    [InlineData(DepositMethod.PaymentGateway)]
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
    public void A_note_has_at_most_three_guarantors_with_valid_national_codes_owner_answer_Q39()
    {
        Assert.Equal("Instrument.TooManyGuarantors", Assert.Throws<DomainException>(() =>
            Receive(InstrumentKind.PromissoryNote, null, new(Guarantors: [new("الف", null), new("ب", null), new("ج", null), new("د", null)]))).Code);
        Assert.Equal(3, Receive(InstrumentKind.PromissoryNote, null, new(Guarantors: [new("الف", null), new("ب", null), new("ج", null)])).Details.GuarantorList.Count);
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

/// <summary>Owner's report no. 4, item 9: a cheque goes from the cash desk to one of our bank accounts for collection.</summary>
public sealed class DepositForCollectionTests
{
    private static readonly CollectionAccount Mellat = new(Guid.NewGuid(), "0202-3572", "IRR", true);

    [Fact]
    public void The_cheque_is_at_the_bank_account_with_its_slip_number()
    {
        var cheque = RegisteredCheque();
        cheque.DepositForCollection(Mellat, D("1405/08/05"), User, " 4455 ");
        Assert.Equal((InstrumentState.DepositedForCollection, (Guid?)Mellat.Id, "0202-3572"), (cheque.State, cheque.CollectionAccountId, cheque.Destination));
        Assert.Equal("4455", cheque.History[^1].Reference);
        Assert.Contains(InstrumentAction.RecordCleared, cheque.AllowedActions());
    }

    [Fact]
    public void Only_at_a_switched_on_account_in_the_cheque_currency()
    {
        Assert.Equal("Instrument.CollectionAccountInactive", Assert.Throws<DomainException>(() =>
            RegisteredCheque().DepositForCollection(Mellat with { IsActive = false }, D("1405/08/05"), User)).Code);
        Assert.Equal("Instrument.CollectionAccountCurrency", Assert.Throws<DomainException>(() =>
            RegisteredCheque().DepositForCollection(Mellat with { CurrencyCode = "USD" }, D("1405/08/05"), User)).Code);
    }

    [Fact]
    public void A_guarantee_cheque_is_not_deposited() =>
        Assert.Equal("Instrument.KindNotAllowedForAction", Assert.Throws<DomainException>(() =>
            ReceivedCheque(InstrumentKind.GuaranteeCheque, due: null).DepositForCollection(Mellat, D("1405/08/05"), User)).Code);
}
