using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// Correction («ویرایش») of a saved receipt, owner's report no. 6 (18 Nov 2026), items 2 and 6: "تا زمانیکه سند ارسال نشده است امکان
/// ویرایش فرم دریافت" and "تا زمانیکه چک نزد صندوق «تایید سند» حسابداری نشده است بتوان آنرا ویرایش نمود".
/// </summary>
public sealed class ReceiptCorrectionTests
{
    private static readonly PayerFact Payer = new("200015", "شرکت نمونه بازرگانی", true);
    private static readonly PayerFact OtherPayer = new("200016", "مشتری دیگر", true);
    private static readonly PurposeGroupFact Sales = new(Guid.NewGuid(), "وجوه حاصل فروش", IsForReceipts: true, IsActive: true);
    private static readonly PurposeGroupFact Advance = new(Guid.NewGuid(), "پیش دریافت", IsForReceipts: true, IsActive: true);
    private static readonly ReceiptAccountFact Desk = new(ReceiptAccountKind.CashDesk, Guid.NewGuid(), "صندوق مرکزی", "IRR", true);
    private static readonly ReceiptAccountFact OtherDesk = new(ReceiptAccountKind.CashDesk, Guid.NewGuid(), "صندوق کارخانه", "IRR", true);
    private static readonly ReceiptAccountFact Bank = new(ReceiptAccountKind.BankAccount, Guid.NewGuid(), "0202-3572", "IRR", true);

    private static ReceiptHeader Header(string date = "1405/08/23", PayerFact? payer = null, PurposeGroupFact? group = null, string purpose = "فروش محصولات") =>
        new(D(date), payer ?? Payer, group ?? Sales, purpose);

    private static Instrument Cheque(Guid receiptId, string number = "123456", string date = "1405/08/23", PayerFact? payer = null) =>
        Instrument.Receive(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, number, "بانک ملت", Money.Rials(10_000_000), D(date), D("1405/10/01"),
            CounterpartyRef.ForDetail((payer ?? Payer).DetailCode, (payer ?? Payer).Name), "فروش محصولات", User, new ReceivedInstrumentDetails(SayadId: "1234567890123456"), receiptId);

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    private static Receipt SavedCash()
    {
        var r = Receipt.RecordCash(Guid.NewGuid(), 4, Header(), Desk, Money.Rials(5_000_000), User);
        r.MarkSaved(1);
        return r;
    }

    [Fact]
    public void A_cash_receipt_is_corrected_and_keeps_its_number_and_who_recorded_it()
    {
        var r = SavedCash();
        r.CorrectCash(Header("1405/08/25", OtherPayer, Advance, "پیش دریافت سفارش ۷"), OtherDesk, Money.Rials(7_500_000), "clerk-2", voucherConfirmed: false);
        Assert.Equal((4, 1405, D("1405/08/25"), "200016", Advance.Id, "پیش دریافت", "پیش دریافت سفارش ۷", OtherDesk.Id, 7_500_000m, User),
            (r.Number, r.FiscalYear, r.Date, r.Payer.DetailCode, r.PurposeGroupId, r.PurposeGroupName, r.Purpose, r.AccountId!.Value, r.TotalRial, r.RecordedBy));
        var e = Assert.IsType<ReceiptCorrected>(Assert.Single(r.PendingEvents));
        Assert.Equal(("clerk-2", 7_500_000m, 4), (e.UserId, e.TotalRial, e.Number));
    }

    [Fact]
    public void A_confirmed_voucher_or_a_cancellation_closes_the_correction()
    {
        var r = SavedCash();
        Assert.Equal("Receipt.VoucherConfirmed", Code(() => r.CorrectCash(Header(), Desk, Money.Rials(1), User, voucherConfirmed: true)));
        Assert.Equal("Receipt.VoucherConfirmed", r.WhyNotCorrectable(voucherConfirmed: true, []));
        Assert.Null(r.WhyNotCorrectable(voucherConfirmed: false, []));
        Assert.Equal(5_000_000m, r.TotalRial);

        r.Cancel(D("1405/08/24"), "اشتباه", User);
        Assert.Equal("Receipt.AlreadyCancelled", Code(() => r.CorrectCash(Header(), Desk, Money.Rials(1), User, voucherConfirmed: false)));
        Assert.Equal("Receipt.AlreadyCancelled", r.WhyNotCorrectable(voucherConfirmed: false, []));
    }

    [Fact]
    public void A_corrected_receipt_keeps_its_kind_and_fiscal_year_and_the_rules_of_recording()
    {
        var r = SavedCash();
        Assert.Equal("Receipt.CorrectionKindDiffers", Code(() => r.CorrectBankDeposit(Header(), Bank, DepositMethod.Pos, null, Money.Rials(1), User, null, voucherConfirmed: false)));
        Assert.Equal("Receipt.CorrectionOtherYear", Code(() => r.CorrectCash(Header("1406/01/05"), Desk, Money.Rials(1), User, voucherConfirmed: false)));
        Assert.Equal("Receipt.AccountKindWrong", Code(() => r.CorrectCash(Header(), Bank, Money.Rials(1), User, voucherConfirmed: false)));
        Assert.Equal("Receipt.PurposeRequired", Code(() => r.CorrectCash(Header(purpose: " "), Desk, Money.Rials(1), User, voucherConfirmed: false)));
        Assert.Equal("Receipt.AccountInactive", Code(() => r.CorrectCash(Header(), Desk with { IsActive = false }, Money.Rials(1), User, voucherConfirmed: false)));
        Assert.Empty(r.PendingEvents);
    }

    [Fact]
    public void A_bank_deposit_is_corrected_with_the_rules_of_its_kind()
    {
        var r = Receipt.RecordBankDeposit(Guid.NewGuid(), 2, Header(), Bank, DepositMethod.Pos, "T-1", Money.Rials(3_000_000), User);
        r.MarkSaved(1);
        Assert.Equal("Receipt.ShebaRequired", Code(() => r.CorrectBankDeposit(Header(), Bank, DepositMethod.Satna, "T-1", Money.Rials(1), User, null, voucherConfirmed: false)));
        Assert.Equal("Receipt.AccountHasNoCard", Code(() => r.CorrectBankDeposit(Header(), Bank, DepositMethod.CardToCard, "T-1", Money.Rials(1), User, null, voucherConfirmed: false)));
        r.CorrectBankDeposit(Header(), Bank, DepositMethod.DepositSlip, " F-9 ", Money.Rials(4_000_000), User, null, voucherConfirmed: false);
        Assert.Equal((DepositMethod.DepositSlip, "F-9", 4_000_000m), (r.Method!.Value, r.TrackingNumber, r.TotalRial));
    }

    [Fact]
    public void A_cheque_is_corrected_only_while_it_is_as_it_was_received()
    {
        var c = Cheque(Guid.NewGuid());
        c.MarkSaved(1);
        Assert.True(c.IsAsReceived);
        c.CorrectReceived(InstrumentKind.BankCheque, " 654321 ", "بانک ملی", Money.Rials(20_000_000), D("1405/08/25"), D("1405/12/01"),
            CounterpartyRef.ForDetail(OtherPayer.DetailCode, OtherPayer.Name), "پیش دریافت", "clerk-2", new ReceivedInstrumentDetails(SayadId: "6543210987654321"));
        Assert.Equal((InstrumentKind.BankCheque, "654321", "بانک ملی", 20_000_000m, D("1405/08/25"), D("1405/12/01"), "200016", "پیش دریافت", "6543210987654321"),
            (c.Kind, c.Number, c.BankName, c.Amount.Amount, c.IssueDate, c.DueDate!.Value, c.Counterparty.DetailCode, c.Purpose, c.Details.SayadId));
        var step = Assert.Single(c.History);
        Assert.Equal((InstrumentAction.Receive, D("1405/08/25"), User), (step.Action, step.Date, step.UserId)); // who received it stays
        Assert.IsType<ReceivedInstrumentCorrected>(Assert.Single(c.PendingEvents));

        // The rules of receiving still hold.
        Assert.Equal("Instrument.SayadIdInvalid", Code(() => c.CorrectReceived(InstrumentKind.OrdinaryCheque, "1", "بانک ملت", Money.Rials(1), D("1405/08/25"), D("1405/09/01"),
            c.Counterparty, "x", User, new ReceivedInstrumentDetails(SayadId: "12"))));
        Assert.Equal("Receipt.CorrectionOtherYear", Code(() => c.CorrectReceived(InstrumentKind.OrdinaryCheque, "1", "بانک ملت", Money.Rials(1), D("1406/01/05"), D("1406/02/01"),
            c.Counterparty, "x", User, new ReceivedInstrumentDetails(SayadId: "1234567890123456"))));

        // After «ثبت صیادی» (or any other step) what is written on the cheque stands.
        c.RegisterSayad(D("1405/08/26"), User, null);
        Assert.False(c.IsAsReceived);
        Assert.Equal("Instrument.CorrectionAfterStep", Code(() => c.CorrectReceived(InstrumentKind.OrdinaryCheque, "1", "بانک ملت", Money.Rials(1), D("1405/08/25"), D("1405/09/01"),
            c.Counterparty, "x", User, new ReceivedInstrumentDetails(SayadId: "1234567890123456"))));
        Assert.Equal("Instrument.CorrectionAfterStep", Code(() => c.RemoveFromReceipt(User)));
    }

    [Fact]
    public void A_cheque_back_in_the_cash_desk_after_legal_action_is_not_as_it_was_received()
    {
        var c = ReceivedCheque();
        c.SendToLegal(D("1405/08/05"), User, "اقدام حقوقی");
        c.ReturnFromLegal(D("1405/08/20"), User, null);
        Assert.Equal(InstrumentState.InCashDesk, c.State);
        Assert.False(c.IsAsReceived);
    }

    [Fact]
    public void A_cheque_receipt_takes_its_corrected_added_and_remaining_cheques()
    {
        var id = Guid.NewGuid();
        var a = Cheque(id, "1");
        var b = Cheque(id, "2");
        var r = Receipt.RecordInstruments(id, 3, Header(), [a, b], User);
        r.MarkSaved(1);
        Assert.Null(r.WhyNotCorrectable(false, [a, b]));

        var header = Header("1405/08/24");
        a.CorrectReceived(a.Kind, "1", a.BankName, Money.Rials(30_000_000), header.Date, a.DueDate, a.Counterparty, a.Purpose, User, a.Details);
        var added = Cheque(id, "9", "1405/08/24");
        r.CorrectInstruments(header, [a, added], User, voucherConfirmed: false);
        Assert.Equal([a.Id, added.Id], r.InstrumentIds);
        Assert.Equal((40_000_000m, 3), (r.TotalRial, r.Number));

        // Every cheque carries the receipt's date and payer.
        Assert.Equal("Receipt.InstrumentMismatch", Code(() => r.CorrectInstruments(Header("1405/08/26"), [a, added], User, voucherConfirmed: false)));
        Assert.Equal("Receipt.NoInstruments", Code(() => r.CorrectInstruments(header, [], User, voucherConfirmed: false)));
        Assert.Equal("Receipt.VoucherConfirmed", Code(() => r.CorrectInstruments(header, [a, added], User, voucherConfirmed: true)));

        b.RegisterSayad(D("1405/08/25"), User, null);
        Assert.Equal("Instrument.CorrectionAfterStep", r.WhyNotCorrectable(false, [a, b]));
    }

    [Fact]
    public void A_replacement_receipt_keeps_the_payer_of_the_cheque_it_replaces()
    {
        var old = Cheque(Guid.NewGuid(), "777");
        var id = Guid.NewGuid();
        var fresh = Cheque(id, "888");
        var r = Receipt.RecordInstruments(id, 5, Header(), [fresh], User, old);
        r.MarkSaved(1);
        var other = Cheque(id, "889", payer: OtherPayer);
        Assert.Equal("Receipt.ReplacementPayerDiffers", Code(() => r.CorrectInstruments(Header(payer: OtherPayer), [other], User, voucherConfirmed: false)));
    }
}
