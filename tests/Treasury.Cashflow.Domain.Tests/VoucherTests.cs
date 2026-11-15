using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Vouchers;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// Treasury vouchers from the owner's template table (docs/domain/posting-rules.md, resent 15 Nov 2026) and his answers of 15 Nov 2026:
/// bank receipts debit the bank account's معین with its تفصیل; «چک های در جریان وصول» is one account with the bank account's تفصیل;
/// one voucher per receipt or operation, confirmed by the treasurer.
/// </summary>
public sealed class VoucherTests
{
    private static readonly PayerFact Payer = new("200015", "شرکت نمونه بازرگانی", true);
    private static readonly PurposeGroupFact Sales = new(Guid.NewGuid(), "وجوه حاصل فروش", true, true, "4101");
    private static readonly ReceiptAccountFact Desk = new(ReceiptAccountKind.CashDesk, Guid.NewGuid(), "صندوق", "IRR", true, "1101", "500001");
    private static readonly ReceiptAccountFact Bank = new(ReceiptAccountKind.BankAccount, Guid.NewGuid(), "0202-3572", "IRR", true, "1102", "600001");
    private static readonly ReceiptAccountFact DollarBank = new(ReceiptAccountKind.BankAccount, Guid.NewGuid(), "USD-1", "USD", true, "1103", "600002");
    private static readonly LedgerSide BankSide = new("1102", "600001");

    private static VoucherTemplate FullTemplate() => new(new Dictionary<FixedAccount, string>
    {
        [FixedAccount.ChequesInCashDesk] = "1201",
        [FixedAccount.ChequesInCollection] = "1202",
        [FixedAccount.ProtestedCheques] = "1203",
        [FixedAccount.GuaranteeChequesForCompany] = "9101",
        [FixedAccount.GuaranteeCounterpart] = "9201",
    });

    private static ReceiptHeader Header() => new(D("1405/08/23"), Payer, Sales, "فروش محصولات");

    private static Instrument Cheque(Guid receiptId, InstrumentKind kind = InstrumentKind.OrdinaryCheque, Money? amount = null) =>
        Instrument.Receive(Guid.NewGuid(), kind, "123456", "بانک ملت", amount ?? Money.Rials(10_000_000), D("1405/08/23"), D("1405/10/01"),
            CounterpartyRef.ForDetail(Payer.DetailCode, Payer.Name), "فروش محصولات", User, new ReceivedInstrumentDetails(SayadId: "1234567890123456"), receiptId);

    [Fact]
    public void Cash_debits_the_cash_desk_and_credits_the_purpose_group_with_the_payer()
    {
        var r = Receipt.RecordCash(Guid.NewGuid(), 3, Header(), Desk, Money.Rials(5_000_000), User);
        var v = ReceiptVouchers.ForReceipt(r, [], new LedgerSide(Desk.LedgerAccountCode, Desk.DetailCode), Sales.LedgerAccountCode, new VoucherTemplate());
        Assert.Equal([(true, "1101", "500001", 5_000_000m), (false, "4101", "200015", 5_000_000m)],
            v.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode, l.AmountRial)));
        Assert.True(v.IsComplete);
        Assert.Equal("دریافت نقدی – رسید 3", v.Operation);
        Assert.Equal(D("1405/08/23"), v.Date);
    }

    [Fact]
    public void Money_paid_into_a_bank_account_debits_its_ledger_account_with_its_detail_owner_answer_1()
    {
        var r = Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), Bank, DepositMethod.Pos, "778899", Money.Rials(2_000_000), User);
        var v = ReceiptVouchers.ForReceipt(r, [], BankSide, "4101", new VoucherTemplate());
        Assert.Equal((true, "1102", "600001"), (v.Lines[0].IsDebit, v.Lines[0].AccountCode, v.Lines[0].DetailCode));
        Assert.Equal((false, "4101", "200015"), (v.Lines[1].IsDebit, v.Lines[1].AccountCode, v.Lines[1].DetailCode));
    }

    [Fact]
    public void A_dollar_deposit_is_in_rial_and_keeps_the_dollar_amount()
    {
        var r = Receipt.RecordBankDeposit(Guid.NewGuid(), 1, Header(), DollarBank, DepositMethod.Transfer, null, Money.Foreign(100, "USD", 600_000), User);
        var v = ReceiptVouchers.ForReceipt(r, [], new LedgerSide("1103", "600002"), "4101", new VoucherTemplate());
        Assert.All(v.Lines, l => Assert.Equal((60_000_000m, (decimal?)100m, "USD"), (l.AmountRial, l.ForeignAmount, l.Currency)));
    }

    [Fact]
    public void Each_cheque_debits_cheques_in_cash_desk_with_the_payer()
    {
        var id = Guid.NewGuid();
        var cheques = new[] { Cheque(id), Cheque(id, amount: Money.Rials(7_000_000)) };
        var r = Receipt.RecordInstruments(id, 1, Header(), cheques, User);
        var v = ReceiptVouchers.ForReceipt(r, cheques, null, "4101", FullTemplate());
        Assert.Equal(4, v.Lines.Count);
        Assert.Equal([(true, "1201", "200015"), (false, "4101", "200015"), (true, "1201", "200015"), (false, "4101", "200015")],
            v.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal((17_000_000m, 17_000_000m), (v.TotalDebit, v.TotalCredit));
    }

    [Fact]
    public void A_guarantee_cheque_goes_to_the_off_balance_accounts()
    {
        var id = Guid.NewGuid();
        var cheque = Instrument.Receive(Guid.NewGuid(), InstrumentKind.GuaranteeCheque, "777", "بانک ملت", Money.Rials(1_000_000), D("1405/08/23"), null,
            CounterpartyRef.ForDetail("200015", Payer.Name), "ضمانت قرارداد", User, new ReceivedInstrumentDetails(SayadId: "1234567890123456"), id);
        var v = ReceiptVouchers.ForReceipt(Receipt.RecordInstruments(id, 1, Header(), [cheque], User), [cheque], null, "4101", FullTemplate());
        Assert.Equal([(true, "9101"), (false, "9201")], v.Lines.Select(l => (l.IsDebit, l.AccountCode)));
    }

    [Fact]
    public void A_fixed_account_not_set_leaves_the_line_without_account_and_names_it()
    {
        var id = Guid.NewGuid();
        var cheque = Cheque(id);
        var v = ReceiptVouchers.ForReceipt(Receipt.RecordInstruments(id, 1, Header(), [cheque], User), [cheque], null, "4101", new VoucherTemplate());
        Assert.Equal((null, (FixedAccount?)FixedAccount.ChequesInCashDesk), (v.Lines[0].AccountCode, v.Lines[0].Missing));
        Assert.False(v.IsComplete);
    }

    [Fact]
    public void Deposit_at_the_bank_moves_the_cheque_to_cheques_in_collection_with_the_bank_detail_owner_answer_2()
    {
        var cheque = ReceivedCheque();
        var v = ReceiptVouchers.ForDeposit(cheque, BankSide, D("1405/08/25"), FullTemplate());
        Assert.Equal([(true, "1202", "600001"), (false, "1201", null)], v.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.Equal("واگذاری چک به بانک", v.Operation);
        Assert.Equal(D("1405/08/25"), v.Date);
    }

    [Fact]
    public void Collected_debits_the_bank_account_and_credits_cheques_in_collection()
    {
        var v = ReceiptVouchers.ForCleared(ReceivedCheque(), BankSide, D("1405/09/01"), FullTemplate());
        Assert.Equal([(true, "1102", "600001"), (false, "1202", "600001")], v.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));
        Assert.True(v.IsComplete);
    }

    [Fact]
    public void Bounced_debits_protested_cheques_and_credits_cheques_in_collection()
    {
        var v = ReceiptVouchers.ForBounced(ReceivedCheque(), BankSide, D("1405/09/02"), FullTemplate());
        Assert.Equal([(true, "1203"), (false, "1202")], v.Lines.Select(l => (l.IsDebit, l.AccountCode)));
        Assert.Equal("600001", v.Lines[1].DetailCode);
    }

    [Fact]
    public void A_template_account_must_be_active_and_take_entries()
    {
        var t = new VoucherTemplate();
        t.Set(FixedAccount.ChequesInCashDesk, " 1201 ", new LedgerAccountFact("1201", "اسناد دریافتنی", true, true));
        Assert.Equal("1201", t.CodeOf(FixedAccount.ChequesInCashDesk));
        Assert.Equal("VoucherTemplate.AccountNotFound", Assert.Throws<DomainException>(() => t.Set(FixedAccount.ProtestedCheques, "9", null)).Code);
        Assert.Equal("VoucherTemplate.AccountInactive",
            Assert.Throws<DomainException>(() => t.Set(FixedAccount.ProtestedCheques, "1299", new LedgerAccountFact("1299", "x", true, false))).Code);
        Assert.Equal("VoucherTemplate.AccountNotPostable",
            Assert.Throws<DomainException>(() => t.Set(FixedAccount.ProtestedCheques, "12", new LedgerAccountFact("12", "x", false, true))).Code);
        Assert.Equal("VoucherTemplate.UnknownAccount", Assert.Throws<DomainException>(() => t.Set((FixedAccount)99, "1", null)).Code);
        t.Set(FixedAccount.ChequesInCashDesk, "", null);
        Assert.Null(t.CodeOf(FixedAccount.ChequesInCashDesk));
    }

    [Fact]
    public void Only_a_complete_voucher_is_confirmed_by_a_user_owner_answer_3()
    {
        var complete = ReceiptVouchers.ForCleared(ReceivedCheque(), BankSide, D("1405/09/01"), FullTemplate());
        var c = ConfirmedVoucher.Confirm(Guid.NewGuid(), VoucherSource.ChequeStep, Guid.NewGuid(), 3, complete, "treasurer", DateTimeOffset.UnixEpoch);
        Assert.Equal((1405, "treasurer", 3), (c.FiscalYear, c.ConfirmedBy, c.Step));
        Assert.IsType<VoucherConfirmed>(Assert.Single(c.PendingEvents));

        var incomplete = ReceiptVouchers.ForCleared(ReceivedCheque(), BankSide, D("1405/09/01"), new VoucherTemplate());
        Assert.Equal("Voucher.Incomplete", Assert.Throws<DomainException>(() =>
            ConfirmedVoucher.Confirm(Guid.NewGuid(), VoucherSource.ChequeStep, Guid.NewGuid(), 3, incomplete, "treasurer", DateTimeOffset.UnixEpoch)).Code);
        Assert.Equal("Voucher.UserRequired", Assert.Throws<DomainException>(() =>
            ConfirmedVoucher.Confirm(Guid.NewGuid(), VoucherSource.ChequeStep, Guid.NewGuid(), 3, complete, " ", DateTimeOffset.UnixEpoch)).Code);
    }
}
