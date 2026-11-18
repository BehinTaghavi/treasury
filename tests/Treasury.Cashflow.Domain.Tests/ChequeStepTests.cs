using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Vouchers;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// Steps of a received cheque after receipt (owner's item 14, 15 Nov 2026; answers Q30–Q35): return from the bank, bounce with its notice,
/// replacement, cancellation by management's order, endorsement and its return; each with its voucher (docs/domain/received-cheque-steps.md).
/// </summary>
public sealed class ChequeStepTests
{
    private static readonly Guid BankA = Guid.NewGuid();
    private static readonly Guid Request = Guid.NewGuid();
    private static readonly LedgerSide SideA = new("1102", "600001");
    private static readonly LedgerSide SideB = new("1109", "600009");

    private static VoucherTemplate Template() => new(new Dictionary<FixedAccount, string>
    {
        [FixedAccount.ChequesInCashDesk] = "1201",
        [FixedAccount.ChequesInCollection] = "1202",
        [FixedAccount.ProtestedCheques] = "1203",
        [FixedAccount.GuaranteeChequesForCompany] = "9101",
        [FixedAccount.GuaranteeCounterpart] = "9201",
        [FixedAccount.BadDebts] = "8101",
    });

    private static Instrument Cheque(InstrumentKind kind = InstrumentKind.OrdinaryCheque, decimal amount = 10_000_000, string? due = "1405/09/01") =>
        Instrument.Receive(Guid.NewGuid(), kind, "445566", "بانک ملت", Money.Rials(amount), D("1405/08/01"), due is null ? null : D(due),
            CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User, new ReceivedInstrumentDetails(SayadId: "1234567890123456"), Guid.NewGuid());

    private static Instrument AtBank(Guid? account = null)
    {
        var c = Cheque();
        c.RegisterSayad(D("1405/08/02"), User); // rule N3 (owner, 18 Nov 2026): registered in Sayad before it goes to the bank
        c.DepositForCollection(new CollectionAccount(account ?? BankA, "0101", "IRR", true), D("1405/08/05"), User, "F-1");
        return c;
    }

    private static (bool, string?, string?)[] Lines(Voucher? v) => [.. v!.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode))];

    [Fact]
    public void The_bank_returns_a_cheque_to_the_company_c()
    {
        var c = AtBank();
        c.ReturnFromBank(D("1405/09/03"), User, "مدارک ناقص");
        Assert.Equal(InstrumentState.ReturnedFromBank, c.State);
        Assert.Equal(BankA, c.History[^1].Details!.AccountId);
        Assert.Equal([(true, "1201", "200015"), (false, "1202", "600001")], Lines(ReceiptVouchers.ForStep(c, 4, SideA, null, Template())));
        Assert.Equal([InstrumentAction.DepositForCollection, InstrumentAction.RecordBounced, InstrumentAction.ReturnToIssuer, InstrumentAction.Replace, InstrumentAction.Cancel],
            c.AllowedActions());
    }

    [Fact]
    public void A_returned_cheque_is_deposited_again_and_each_deposit_keeps_its_bank_Q33()
    {
        var c = AtBank();
        c.ReturnFromBank(D("1405/09/03"), User);
        var other = Guid.NewGuid();
        c.DepositForCollection(new CollectionAccount(other, "0909", "IRR", true), D("1405/09/04"), User, "F-2");
        Assert.Equal((BankA, other), (c.History[2].Details!.AccountId!.Value, c.History[4].Details!.AccountId!.Value));
        Assert.Equal([(true, "1202", "600009"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(c, 5, SideB, null, Template())));
    }

    [Fact]
    public void A_bounce_needs_the_banks_notice_number_e()
    {
        var c = AtBank();
        Assert.Equal("Instrument.BounceNoticeRequired", Assert.Throws<DomainException>(() => c.RecordBounced(D("1405/09/02"), User, "کسری موجودی", " ")).Code);
        c.RecordBounced(D("1405/09/02"), User, "کسری موجودی", "BN-77");
        var v = ReceiptVouchers.ForStep(c, 4, SideA, null, Template());
        Assert.Equal([(true, "1203", "200015"), (false, "1202", "600001")], Lines(v));
        Assert.Contains("BN-77", v!.Lines[0].Description, StringComparison.Ordinal);
        Assert.Equal([InstrumentAction.Replace, InstrumentAction.Cancel, InstrumentAction.SendToLegal], c.AllowedActions());
    }

    [Fact]
    public void A_cheque_back_from_the_bank_can_still_get_its_bounce_notice_Q33()
    {
        var c = AtBank();
        c.ReturnFromBank(D("1405/09/03"), User);
        c.RecordBounced(D("1405/09/10"), User, null, "BN-78");
        Assert.Equal(InstrumentState.Bounced, c.State);
        Assert.Equal([(true, "1203", "200015"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(c, 5, SideA, null, Template())));
    }

    [Fact]
    public void The_treasurer_cancels_with_the_number_and_date_of_managements_order_f_Q30()
    {
        var c = Cheque();
        Assert.Equal("Instrument.CancelOrderRequired", Assert.Throws<DomainException>(() => c.CancelReceived(D("1405/08/10"), User, "", D("1405/08/09"), "سوخت")).Code);
        Assert.Equal("Instrument.CancelOrderRequired", Assert.Throws<DomainException>(() => c.CancelReceived(D("1405/08/10"), User, "م-12", null, "سوخت")).Code);
        Assert.Equal("Instrument.CancelOrderAfterCancel", Assert.Throws<DomainException>(() => c.CancelReceived(D("1405/08/10"), User, "م-12", D("1405/08/11"), "سوخت")).Code);
        Assert.Equal("Instrument.CancelReasonRequired", Assert.Throws<DomainException>(() => c.CancelReceived(D("1405/08/10"), User, "م-12", D("1405/08/09"), " ")).Code);
        Assert.Equal("Instrument.CancelOrderRequired", Assert.Throws<DomainException>(() => c.Cancel(D("1405/08/10"), User, "سوخت")).Code);
        c.CancelReceived(D("1405/08/10"), User, "م-12", D("1405/08/09"), "سوخت شد");
        Assert.Equal((InstrumentState.Cancelled, "م-12", "سوخت شد"), (c.State, c.History[^1].Reference, c.History[^1].Note));
        Assert.Equal(D("1405/08/09"), c.History[^1].Details!.DocumentDate);
        Assert.Equal([(true, "8101", "200015"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(c, 2, SideA, null, Template())));
    }

    [Fact]
    public void A_bounced_cheque_is_cancelled_from_protested_cheques_and_a_guarantee_cheque_from_its_off_balance_accounts()
    {
        var bounced = AtBank();
        bounced.RecordBounced(D("1405/09/02"), User, null, "BN-1");
        bounced.CancelReceived(D("1405/09/20"), User, "م-1", D("1405/09/19"), "سوخت");
        Assert.Equal([(true, "8101", "200015"), (false, "1203", "200015")], Lines(ReceiptVouchers.ForStep(bounced, 5, SideA, null, Template())));

        var guarantee = Cheque(InstrumentKind.GuaranteeCheque, due: null);
        guarantee.CancelReceived(D("1405/08/10"), User, "م-2", D("1405/08/10"), "پایان قرارداد");
        Assert.Equal([(true, "9201", "200015"), (false, "9101", "200015")], Lines(ReceiptVouchers.ForStep(guarantee, 2, SideA, null, Template())));
    }

    [Fact]
    public void Collected_and_endorsed_cheques_cannot_be_cancelled()
    {
        var c = AtBank();
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => c.CancelReceived(D("1405/09/02"), User, "م", D("1405/09/02"), "x")).Code);
    }

    [Fact]
    public void Endorsement_pays_an_approved_request_and_needs_receiver_group_purpose_and_Sayad_tracking_g_Q32_Q36()
    {
        var group = Guid.NewGuid();
        Assert.Equal("Instrument.SayadTrackingRequired", Assert.Throws<DomainException>(() =>
            Cheque().Endorse(new Endorsement("300200", "پیمانکار ب", group, "خرید", " ", Request), D("1405/08/02"), User)).Code);
        Assert.Equal("Instrument.EndorseeRequired", Assert.Throws<DomainException>(() =>
            Cheque().Endorse(new Endorsement("", "", group, "خرید", "SY", Request), D("1405/08/02"), User)).Code);
        Assert.Equal("Instrument.PaymentRequestRequired", Assert.Throws<DomainException>(() =>
            Cheque().Endorse(new Endorsement("300200", "پیمانکار ب", group, "خرید", "SY", Guid.Empty), D("1405/08/02"), User)).Code);
        Assert.Equal("Instrument.EndorsementGroupRequired", Assert.Throws<DomainException>(() =>
            Cheque().Endorse(new Endorsement("300200", "پیمانکار ب", Guid.Empty, "خرید", "SY", Request), D("1405/08/02"), User)).Code);

        var c = Cheque();
        c.Endorse(new Endorsement("300200", "پیمانکار ب", group, "خرید مواد", "SY-99", Request), D("1405/08/02"), User);
        Assert.Equal((InstrumentState.Endorsed, "پیمانکار ب", "SY-99", (Guid?)Request), (c.State, c.Destination, c.History[^1].Details!.SayadTracking, c.History[^1].Details!.PaymentRequestId));
        Assert.Equal($"endorse:{c.Id}:2", c.EndorsementPaymentKey(2));
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => c.EndorsementPaymentKey(1)).Code);
        var endorsementSide = new LedgerSide("2101", "300200");
        Assert.Equal([(true, "2101", "300200"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(c, 2, SideA, endorsementSide, Template())));
        Assert.Equal([InstrumentAction.RecordEndorsementReturned], c.AllowedActions());
    }

    [Fact]
    public void An_endorsed_cheque_comes_back_bounced_h()
    {
        var c = Cheque();
        c.Endorse(new Endorsement("300200", "پیمانکار ب", Guid.NewGuid(), "خرید", "SY-1", Request), D("1405/08/02"), User);
        Assert.Equal("Instrument.BounceNoticeRequired", Assert.Throws<DomainException>(() => c.RecordEndorsementReturned(D("1405/09/05"), User, null)).Code);
        Assert.Equal("Instrument.BouncedBeforeDueDate", Assert.Throws<DomainException>(() => c.RecordEndorsementReturned(D("1405/08/20"), User, "BN-5")).Code);
        c.RecordEndorsementReturned(D("1405/09/05"), User, "BN-5");
        Assert.Equal((InstrumentState.Bounced, (string?)null), (c.State, c.Destination));
        Assert.Equal([(true, "1203", "200015"), (false, "2101", "300200")], Lines(ReceiptVouchers.ForStep(c, 3, SideA, new LedgerSide("2101", "300200"), Template())));
        Assert.Contains(InstrumentAction.Replace, c.AllowedActions());
    }

    [Fact]
    public void Replacement_new_cheques_may_differ_in_amount_and_number_d_Q31()
    {
        var old = AtBank();
        old.RecordBounced(D("1405/09/02"), User, null, "BN-1");
        var receiptId = Guid.NewGuid();
        var payer = new PayerFact("200015", "شرکت نمونه", true);
        var header = new ReceiptHeader(D("1405/09/10"), payer, new PurposeGroupFact(Guid.NewGuid(), "فروش", true, true, "4101"), "تعویض چک برگشتی");
        Instrument New(decimal amount, string number) => Instrument.Receive(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, number, "بانک ملی", Money.Rials(amount),
            D("1405/09/10"), D("1405/10/10"), CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User, new ReceivedInstrumentDetails(SayadId: "1234567890123456"), receiptId);
        var created = new[] { New(6_000_000, "1"), New(5_000_000, "2") };
        var receipt = Receipt.RecordInstruments(receiptId, 9, header, created, User, old);
        old.Replace(receipt.Id, receipt.Date, User);
        Assert.Equal((InstrumentState.Replaced, (Guid?)old.Id, (Guid?)receiptId), (old.State, receipt.ReplacedInstrumentId, old.History[^1].Details!.ReceiptId));

        var v = ReceiptVouchers.ForReceipt(receipt, created, null, "4101", Template(), new Replacement(old, InstrumentState.Bounced));
        Assert.Equal([(true, "1201", "200015"), (true, "1201", "200015"), (false, "1203", "200015"), (false, "4101", "200015")], Lines(v));
        Assert.Equal(1_000_000m, v.Lines[^1].AmountRial);
        Assert.True(v.IsComplete);

        var less = Receipt.RecordInstruments(receiptId, 9, header, [New(8_000_000, "3")], User, Cheque());
        var w = ReceiptVouchers.ForReceipt(less, [], null, "4101", Template(), new Replacement(Cheque(), InstrumentState.InCashDesk));
        Assert.Equal((true, "4101", 10_000_000m), (w.Lines[^1].IsDebit, w.Lines[^1].AccountCode, w.Lines[^1].AmountRial));
    }

    [Fact]
    public void Replacement_comes_from_the_same_payer_and_not_for_guarantee_cheques()
    {
        var other = new PayerFact("200099", "دیگری", true);
        var header = new ReceiptHeader(D("1405/09/10"), other, new PurposeGroupFact(Guid.NewGuid(), "فروش", true, true), "تعویض");
        Assert.Equal("Receipt.ReplacementPayerDiffers", Assert.Throws<DomainException>(() =>
            Receipt.RecordInstruments(Guid.NewGuid(), 1, header, [], User, Cheque())).Code);
        Assert.DoesNotContain(InstrumentAction.Replace, Cheque(InstrumentKind.GuaranteeCheque, due: null).AllowedActions());
    }
}
