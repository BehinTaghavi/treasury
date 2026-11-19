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

    private static readonly DateTimeOffset Stamp = new(2026, 11, 19, 9, 30, 0, TimeSpan.Zero);

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
        Assert.Equal([InstrumentAction.DepositForCollection, InstrumentAction.RecordBounced, InstrumentAction.ReturnToIssuer, InstrumentAction.Cancel],
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
        Assert.Equal([InstrumentAction.Cancel, InstrumentAction.SendToLegal], c.AllowedActions());
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

    /// <summary>A cheque in the cash desk whose latest status is «ثبت صیادی شد» (step 2 of its history).</summary>
    private static Instrument Registered()
    {
        var c = Cheque();
        c.RegisterSayad(D("1405/08/02"), User, "SY-REG");
        return c;
    }

    /// <summary>The endorsement of a request of the kind «ثبت انتقال (خرج چک)» written for <paramref name="c"/> (owner, 19 Nov 2026).</summary>
    private static Endorsement TransferOf(Instrument c, string tracking = "SY-99", Guid? group = null, Guid? request = null, string code = "300200", string name = "پیمانکار ب") =>
        new(code, name, group ?? Guid.NewGuid(), "خرید مواد", tracking, request ?? Request, c.Id, SheetNumber: 12);

    [Fact]
    public void Endorsement_pays_an_approved_request_and_needs_receiver_group_purpose_and_Sayad_tracking_g_Q32_Q36()
    {
        var group = Guid.NewGuid();
        Instrument c;
        Assert.Equal("Instrument.SayadTrackingRequired", Assert.Throws<DomainException>(() =>
            (c = Registered()).Endorse(TransferOf(c, " "), D("1405/08/02"), User)).Code);
        Assert.Equal("Instrument.EndorseeRequired", Assert.Throws<DomainException>(() =>
            (c = Registered()).Endorse(TransferOf(c, code: "", name: ""), D("1405/08/02"), User)).Code);
        Assert.Equal("Instrument.PaymentRequestRequired", Assert.Throws<DomainException>(() =>
            (c = Registered()).Endorse(TransferOf(c, request: Guid.Empty), D("1405/08/02"), User)).Code);
        Assert.Equal("Instrument.EndorsementGroupRequired", Assert.Throws<DomainException>(() =>
            (c = Registered()).Endorse(TransferOf(c, group: Guid.Empty), D("1405/08/02"), User)).Code);

        c = Registered();
        c.Endorse(TransferOf(c, group: group), D("1405/08/02"), User);
        Assert.Equal((InstrumentState.Endorsed, "پیمانکار ب", "SY-99", (Guid?)Request), (c.State, c.Destination, c.History[^1].Details!.SayadTracking, c.History[^1].Details!.PaymentRequestId));
        Assert.Equal($"endorse:{c.Id}:3", c.EndorsementPaymentKey(3));
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => c.EndorsementPaymentKey(2)).Code);
        var endorsementSide = new LedgerSide("2101", "300200");
        var voucher = ReceiptVouchers.ForStep(c, 3, SideA, endorsementSide, Template())!;
        Assert.Equal([(true, "2101", "300200"), (false, "1201", "200015")], Lines(voucher));
        Assert.Equal("ثبت انتقال چک (خرج چک)", voucher.Operation); // the owner's name of the step, 19 Nov 2026
        Assert.Equal([InstrumentAction.RecordEndorsementReturned], c.AllowedActions());
    }

    [Fact]
    public void A_cheque_is_transferred_only_when_its_status_is_Sayad_registered_owner_19_nov()
    {
        // «چک دریافتی … میباید ابتدا ثبت صیادی شده باشد و … به وضعیت ثبت صیادی شد در آمده باشد». The old program's D_Check_Kh_m
        // endorsed a cheque straight from the cash desk (11 -> 16).
        var inDesk = Cheque();
        Assert.DoesNotContain(InstrumentAction.Endorse, inDesk.AllowedActions());
        Assert.Equal("Instrument.TransferNeedsSayadRegistration", Assert.Throws<DomainException>(() => inDesk.Endorse(TransferOf(inDesk), D("1405/08/02"), User)).Code);
        Assert.Equal(InstrumentState.InCashDesk, inDesk.State);

        inDesk.RegisterSayad(D("1405/08/02"), User);
        Assert.Contains(InstrumentAction.Endorse, inDesk.AllowedActions());
        inDesk.Endorse(TransferOf(inDesk), D("1405/08/03"), User);
        Assert.Equal(InstrumentState.Endorsed, inDesk.State);

        // A guarantee cheque is held, never transferred (rule R3), registered in Sayad or not.
        var guarantee = Cheque(InstrumentKind.GuaranteeCheque, due: null);
        guarantee.RegisterSayad(D("1405/08/02"), User);
        Assert.Equal("Instrument.KindNotAllowedForAction", Assert.Throws<DomainException>(() => guarantee.Endorse(TransferOf(guarantee), D("1405/08/03"), User)).Code);
    }

    [Fact]
    public void A_cheque_is_transferred_only_on_the_payment_request_written_for_it_owner_19_nov()
    {
        // «درخواست وجه لازم برای ثبت انتقال چک مزبور به شخص یا شرکت مورد نظر صادر شده باشد»: a request of another kind (no cheque named) or
        // one that names another cheque does not transfer this cheque.
        var c = Registered();
        var anyRequest = new Endorsement("300200", "پیمانکار ب", Guid.NewGuid(), "خرید", "SY-1", Request);
        Assert.Equal("Instrument.PaymentRequestNotForThisCheque", Assert.Throws<DomainException>(() => c.Endorse(anyRequest, D("1405/08/03"), User)).Code);
        Assert.Equal("Instrument.PaymentRequestNotForThisCheque",
            Assert.Throws<DomainException>(() => c.Endorse(anyRequest with { RequestChequeId = Guid.NewGuid() }, D("1405/08/03"), User)).Code);
        Assert.Equal(InstrumentState.SayadRegistered, c.State);
    }

    [Fact]
    public void The_transfer_of_a_cheque_has_a_payment_sheet_with_its_own_number_and_its_authorised_signatories_owner_answer_Q57()
    {
        // «برگ پرداخت» (19 Nov 2026): a serial number of its own, and the signatories of the rule «برگ پرداخت چک» besides the two parties.
        var c = Registered();
        Assert.Equal("Instrument.PaymentSheetNumberRequired",
            Assert.Throws<DomainException>(() => c.Endorse(TransferOf(c) with { SheetNumber = null }, D("1405/08/03"), User)).Code);
        var ceo = new ReceiptSigner(Guid.NewGuid(), "علی رضایی", "مدیر عامل");
        c.Endorse(TransferOf(c) with { SheetNumber = 7, Signers = [ceo] }, D("1405/08/03"), User);
        Assert.Equal((7, ceo), (c.History[^1].Details!.SheetNumber, Assert.Single(c.History[^1].Details!.Signers!)));

        // «خرج سفته» has no Sayad transfer and, so far, no payment sheet.
        var note = Instrument.Receive(Guid.NewGuid(), InstrumentKind.PromissoryNote, "N-1", null, Money.Rials(1_000), D("1405/08/01"), D("1405/09/01"),
            CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User, ReceivedInstrumentDetails.None, Guid.NewGuid());
        note.Endorse(new Endorsement("300200", "پیمانکار ب", Guid.NewGuid(), "خرید", " ", Request, SheetNumber: 9, Signers: [ceo]), D("1405/08/03"), User);
        Assert.Equal((null, null), (note.History[^1].Details!.SheetNumber, note.History[^1].Details!.Signers));
    }

    [Fact]
    public void An_endorsed_cheque_comes_back_bounced_h()
    {
        var c = Registered();
        c.Endorse(TransferOf(c, "SY-1"), D("1405/08/02"), User);
        Assert.Equal("Instrument.BounceNoticeRequired", Assert.Throws<DomainException>(() => c.RecordEndorsementReturned(D("1405/09/05"), User, null)).Code);
        Assert.Equal("Instrument.BouncedBeforeDueDate", Assert.Throws<DomainException>(() => c.RecordEndorsementReturned(D("1405/08/20"), User, "BN-5")).Code);
        c.RecordEndorsementReturned(D("1405/09/05"), User, "BN-5");
        Assert.Equal((InstrumentState.Bounced, (string?)null), (c.State, c.Destination));
        Assert.Equal([(true, "1203", "200015"), (false, "2101", "300200")], Lines(ReceiptVouchers.ForStep(c, 4, SideA, new LedgerSide("2101", "300200"), Template())));
        Assert.Contains(InstrumentAction.Cancel, c.AllowedActions()); // cancelled first, then replaced (Q62)
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
        // Owner, 19 Nov 2026 (answer to Q62): a cheque is replaced only after it was cancelled.
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => old.Replace(receipt.Id, receipt.Date, User)).Code);
        old.CancelReceived(D("1405/09/09"), User, "م-12", D("1405/09/08"), "تعویض با دو چک", originalStamped: true, recordedAt: Stamp);
        Assert.Equal([InstrumentAction.Replace], old.AllowedActions());
        old.Replace(receipt.Id, receipt.Date, User);
        Assert.Equal((InstrumentState.Replaced, (Guid?)old.Id, (Guid?)receiptId), (old.State, receipt.ReplacedInstrumentId, old.History[^1].Details!.ReceiptId));
        Assert.Equal((InstrumentState?)InstrumentState.Cancelled, old.History[^1].From);

        // The cancellation wrote the cheque off (Dr «مطالبات سوخت شده» / Cr «اسناد واخواست شده»); the replacement takes it out of there:
        // Dr «اسناد نزد صندوق» for each new cheque / Cr «مطالبات سوخت شده», the difference to the purpose group (to confirm: Q64).
        Assert.Equal([(true, "8101", "200015"), (false, "1203", "200015")], Lines(ReceiptVouchers.ForStep(old, old.History.Count - 1, SideA, null, Template())));
        Assert.Null(ReceiptVouchers.ForStep(old, old.History.Count, SideA, null, Template()));
        var v = ReceiptVouchers.ForReceipt(receipt, created, null, "4101", Template(), new Replacement(old, InstrumentState.Cancelled));
        Assert.Equal([(true, "1201", "200015"), (true, "1201", "200015"), (false, "8101", "200015"), (false, "4101", "200015")], Lines(v));
        Assert.Equal(1_000_000m, v.Lines[^1].AmountRial);
        Assert.True(v.IsComplete);

        var smaller = Cheque();
        smaller.CancelReceived(D("1405/09/09"), User, "م-13", D("1405/09/08"), "تعویض");
        var less = Receipt.RecordInstruments(receiptId, 9, header, [New(8_000_000, "3")], User, smaller);
        var w = ReceiptVouchers.ForReceipt(less, [], null, "4101", Template(), new Replacement(smaller, InstrumentState.Cancelled));
        Assert.Equal((true, "4101", 10_000_000m), (w.Lines[^1].IsDebit, w.Lines[^1].AccountCode, w.Lines[^1].AmountRial));
    }

    [Fact]
    public void A_cancellation_records_the_stamp_on_the_original_with_the_user_and_the_time_owner_answer_Q62()
    {
        // «اصل چک ممهور به مهر ابطال شد», with the cancelling user and the computer's date and time beside it.
        var c = Cheque();
        c.CancelReceived(D("1405/08/10"), User, "م-1", D("1405/08/09"), "دستور مدیریت", originalStamped: true, recordedAt: Stamp);
        var step = c.History[^1];
        Assert.Equal((InstrumentAction.Cancel, User, true, (DateTimeOffset?)Stamp), (step.Action, step.UserId, step.Details!.OriginalStamped, step.Details.RecordedAt));
        Assert.Equal([(true, "8101", "200015"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(c, 2, SideA, null, Template())));

        var unstamped = Cheque();
        unstamped.CancelReceived(D("1405/08/10"), User, "م-2", D("1405/08/09"), "دستور مدیریت");
        Assert.False(unstamped.History[^1].Details!.OriginalStamped);
    }

    [Fact]
    public void No_state_but_cancelled_leads_to_a_replacement_owner_answer_Q62()
    {
        Assert.DoesNotContain(InstrumentAction.Replace, Cheque().AllowedActions());
        Assert.DoesNotContain(InstrumentAction.Replace, Registered().AllowedActions());
        Assert.DoesNotContain(InstrumentAction.Replace, AtBank().AllowedActions());
        Assert.All(InstrumentLifecycle.All.Where(t => t.Action == InstrumentAction.Replace),
            t => Assert.Equal((InstrumentDirection.Received, InstrumentState.Cancelled, InstrumentState.Replaced), (t.Direction, t.From, t.To)));
        // A guarantee cheque is cancelled but not replaced.
        var guarantee = Cheque(InstrumentKind.GuaranteeCheque, due: null);
        guarantee.CancelReceived(D("1405/08/10"), User, "م-3", D("1405/08/09"), "پایان قرارداد");
        Assert.Empty(guarantee.AllowedActions());
    }

    [Fact]
    public void Replacement_comes_from_the_same_payer_and_not_for_guarantee_cheques()
    {
        var other = new PayerFact("200099", "دیگری", true);
        var header = new ReceiptHeader(D("1405/09/10"), other, new PurposeGroupFact(Guid.NewGuid(), "فروش", true, true), "تعویض");
        Assert.Equal("Receipt.ReplacementPayerDiffers", Assert.Throws<DomainException>(() =>
            Receipt.RecordInstruments(Guid.NewGuid(), 1, header, [], User, Cheque())).Code);
    }
}
