using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Vouchers;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// The steps of a received promissory note after receipt (owner, 16 Nov 2026: «مراحل سفته پس از دریافت شامل واگذاری به بانک، وصول،
/// استرداد، خرج سفته (ظهرنویسی و انتقال به غیر) و …»), with the vouchers of his template table («واگذاری سفته به بانک», «اعلام وصول
/// چک / سفته واگذاری توسط بانک», «ابطال چک / سفته انتظامی دریافتی»). The states and steps are those of a received cheque.
/// </summary>
public sealed class NoteStepTests
{
    private static readonly Guid BankA = Guid.NewGuid();
    private static readonly LedgerSide SideA = new("1102", "600001");

    private static VoucherTemplate Template() => new(new Dictionary<FixedAccount, string>
    {
        [FixedAccount.ChequesInCashDesk] = "1201",
        [FixedAccount.ChequesInCollection] = "1202",
        [FixedAccount.ProtestedCheques] = "1203",
        [FixedAccount.GuaranteeCounterpart] = "9201",
        [FixedAccount.BadDebts] = "8101",
        [FixedAccount.GuaranteeNotesForCompany] = "9102",
        [FixedAccount.NotesInCollection] = "1204",
        [FixedAccount.ProtestedChequesInLegal] = "1205",
        [FixedAccount.GuaranteeChequesInLegal] = "9103",
    });

    private static Instrument Note(InstrumentKind kind = InstrumentKind.PromissoryNote, string? due = "1405/09/01", bool onDemand = false) =>
        Instrument.Receive(Guid.NewGuid(), kind, "778899", null, Money.Rials(500_000_000), D("1405/08/01"), due is null ? null : D(due),
            CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "بابت بدهی", User, new ReceivedInstrumentDetails(OnDemand: onDemand), Guid.NewGuid());

    private static Instrument AtBank(Instrument? note = null)
    {
        var n = note ?? Note();
        n.DepositForCollection(new CollectionAccount(BankA, "0101", "IRR", true), D("1405/08/05"), User, "F-1");
        return n;
    }

    private static (bool, string?, string?)[] Lines(Voucher? v) => [.. v!.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode))];

    [Fact]
    public void An_ordinary_note_in_the_cash_desk_goes_to_the_bank_is_returned_endorsed_replaced_or_cancelled()
    {
        Assert.Equal([InstrumentAction.DepositForCollection, InstrumentAction.ReturnToIssuer, InstrumentAction.Endorse, InstrumentAction.Replace, InstrumentAction.Cancel],
            Note().AllowedActions());
    }

    [Fact]
    public void A_guarantee_note_is_returned_cancelled_or_taken_to_law_but_never_goes_to_the_bank()
    {
        // Owner, 16 Nov 2026: guarantee and trust notes are handed back or cancelled; 18 Nov 2026: they may also get a legal action.
        var note = Note(InstrumentKind.GuaranteeNote, due: null);
        Assert.Equal([InstrumentAction.ReturnToIssuer, InstrumentAction.Cancel, InstrumentAction.SendToLegal], note.AllowedActions());
        Assert.Equal("Instrument.KindNotAllowedForAction",
            Assert.Throws<DomainException>(() => note.DepositForCollection(new CollectionAccount(BankA, "0101", "IRR", true), D("1405/08/05"), User)).Code);
    }

    [Fact]
    public void A_note_is_deposited_at_the_bank_with_notes_in_collection_owner_table()
    {
        var note = AtBank();
        Assert.Equal(InstrumentState.DepositedForCollection, note.State);
        Assert.Equal((BankA, "F-1"), (note.CollectionAccountId!.Value, note.History[^1].Reference));
        var v = ReceiptVouchers.ForStep(note, 2, SideA, null, Template())!;
        Assert.Equal([(true, "1204", "600001"), (false, "1201", "200015")], Lines(v));
        Assert.Equal("واگذاری سفته به بانک", v.Operation);
        Assert.Equal("واگذاری سفته 778899 سررسید 1405/09/01 – شرکت نمونه", v.Lines[0].Description);

        // Without the note account set in the template the line names it, not the cheques' account.
        var missing = ReceiptVouchers.ForStep(note, 2, SideA, null, new VoucherTemplate())!;
        Assert.Equal(FixedAccount.NotesInCollection, missing.Lines[0].Missing);
    }

    [Fact]
    public void The_bank_collects_a_note_but_not_before_its_due_date_N2()
    {
        var note = AtBank();
        Assert.Equal("Instrument.ClearedBeforeDueDate", Assert.Throws<DomainException>(() => note.RecordCleared(D("1405/08/20"), User)).Code);
        note.RecordCleared(D("1405/09/01"), User, "ST-9");
        Assert.Equal(InstrumentState.Cleared, note.State);
        var v = ReceiptVouchers.ForStep(note, 3, SideA, null, Template())!;
        Assert.Equal([(true, "1102", "600001"), (false, "1204", "600001")], Lines(v));
        Assert.Equal("اعلام وصول سفته توسط بانک", v.Operation);
    }

    [Fact]
    public void A_note_payable_on_demand_is_collected_whenever_its_holder_wishes_owner_answer_QN2()
    {
        var note = AtBank(Note(due: null, onDemand: true));
        note.RecordCleared(D("1405/08/06"), User);
        Assert.Equal(InstrumentState.Cleared, note.State);
        Assert.Equal("واگذاری سفته 778899 عندالمطالبه – شرکت نمونه", ReceiptVouchers.ForStep(note, 2, SideA, null, Template())!.Lines[0].Description);
    }

    [Fact]
    public void A_note_the_bank_does_not_collect_is_protested_or_comes_back()
    {
        var protested = AtBank();
        Assert.Equal("Instrument.BounceNoticeRequired", Assert.Throws<DomainException>(() => protested.RecordBounced(D("1405/09/02"), User, null, " ")).Code);
        protested.RecordBounced(D("1405/09/02"), User, "عدم پرداخت", "W-12");
        Assert.Equal(InstrumentState.Bounced, protested.State);
        var v = ReceiptVouchers.ForStep(protested, 3, SideA, null, Template())!;
        Assert.Equal([(true, "1203", "200015"), (false, "1204", "600001")], Lines(v));
        Assert.Equal("واخواست سفته توسط بانک", v.Operation);
        // Owner, 18 Nov 2026: a bounced (protested) note may get a legal action, like a bounced cheque.
        Assert.Equal([InstrumentAction.Replace, InstrumentAction.Cancel, InstrumentAction.SendToLegal], protested.AllowedActions());

        var back = AtBank();
        back.ReturnFromBank(D("1405/09/03"), User, "مدارک ناقص");
        Assert.Equal([(true, "1201", "200015"), (false, "1204", "600001")], Lines(ReceiptVouchers.ForStep(back, 3, SideA, null, Template())));
        Assert.Contains(InstrumentAction.DepositForCollection, back.AllowedActions()); // deposited again
        Assert.Contains(InstrumentAction.ReturnToIssuer, back.AllowedActions());       // or handed back to the obligor
    }

    [Fact]
    public void A_note_is_endorsed_to_pay_an_approved_request_without_a_Sayad_transfer()
    {
        var note = Note();
        var request = Guid.NewGuid();
        note.Endorse(new Endorsement("300200", "شرکت پیمانکار ب", Guid.NewGuid(), "خرید مواد", " ", request), D("1405/08/10"), User, "ظهرنویسی شد");
        Assert.Equal((InstrumentState.Endorsed, "شرکت پیمانکار ب"), (note.State, note.Destination));
        Assert.Null(note.History[^1].Reference);
        Assert.Equal((request, (string?)null), (note.History[^1].Details!.PaymentRequestId!.Value, note.History[^1].Details!.SayadTracking));
        var v = ReceiptVouchers.ForStep(note, 2, SideA, new LedgerSide("2101", "300200"), Template())!;
        Assert.Equal([(true, "2101", "300200"), (false, "1201", "200015")], Lines(v));
        Assert.Equal("خرج سفته", v.Operation);

        // A cheque still needs its Sayad transfer (answer to Q32).
        var cheque = Instrument.Receive(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, "1", "بانک ملت", Money.Rials(1), D("1405/08/01"), D("1405/09/01"),
            CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User);
        Assert.Equal("Instrument.SayadTrackingRequired",
            Assert.Throws<DomainException>(() => cheque.Endorse(new Endorsement("300200", "ب", Guid.NewGuid(), "خرید", " ", request), D("1405/08/10"), User)).Code);

        // The receiver brings the endorsed note back protested.
        note.RecordEndorsementReturned(D("1405/09/05"), User, "W-3", "عدم پرداخت");
        Assert.Equal(InstrumentState.Bounced, note.State);
        Assert.Equal("برگشت سفته خرج شده", ReceiptVouchers.ForStep(note, 3, SideA, new LedgerSide("2101", "300200"), Template())!.Operation);
    }

    [Fact]
    public void Handing_a_guarantee_note_back_takes_it_out_of_the_off_balance_accounts()
    {
        var note = Note(InstrumentKind.GuaranteeNote, due: null);
        note.ReturnToIssuer(D("1405/10/01"), User, "پایان قرارداد");
        Assert.Equal(InstrumentState.ReturnedToIssuer, note.State);
        var v = ReceiptVouchers.ForStep(note, 2, new LedgerSide(null, null), null, Template())!;
        Assert.Equal([(true, "9201", "200015"), (false, "9102", "200015")], Lines(v));
        Assert.Equal("استرداد سفته انتظامی دریافتی", v.Operation);

    }

    [Fact]
    public void Handing_an_ordinary_note_back_debits_what_its_receipt_credited_owner_answer_QN14()
    {
        // In the cash desk: Dr «بدهکاران» (the receipt's purpose group, obligor) / Cr «اسناد نزد صندوق / اسناد دریافتنی».
        var note = Note();
        note.ReturnToIssuer(D("1405/08/02"), User, "توافق طرفین");
        var v = ReceiptVouchers.ForStep(note, 2, new LedgerSide(null, null), null, Template(), "4101")!;
        Assert.Equal([(true, "4101", "200015"), (false, "1201", "200015")], Lines(v));
        Assert.Equal("استرداد سفته به متعهد", v.Operation);
        Assert.True(v.IsComplete);

        // At the bank: it comes back first (Dr «اسناد دریافتنی» / Cr «سفته های در جریان وصول»), then it is handed back; together
        // «بدهکاران» is debited and «سفته های در جریان وصول» credited, as the owner said.
        var atBank = AtBank();
        atBank.ReturnFromBank(D("1405/08/20"), User);
        atBank.ReturnToIssuer(D("1405/08/21"), User);
        Assert.Equal([(true, "1201", "200015"), (false, "1204", "600001")], Lines(ReceiptVouchers.ForStep(atBank, 3, SideA, null, Template(), "4101")));
        Assert.Equal([(true, "4101", "200015"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(atBank, 4, SideA, null, Template(), "4101")));

        // A note without a known receipt: the debit account is missing and the voucher cannot be confirmed.
        Assert.False(ReceiptVouchers.ForStep(note, 2, new LedgerSide(null, null), null, Template())!.IsComplete);

        // A cheque returned to its payer has the same voucher since the owner's answer to Q41 (18 Nov 2026): Dr the account its
        // receipt credited / Cr «اسناد نزد صندوق».
        var cheque = Instrument.Receive(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, "1", "بانک ملت", Money.Rials(1), D("1405/08/01"), D("1405/09/01"),
            CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User);
        cheque.ReturnToIssuer(D("1405/08/02"), User);
        var returned = ReceiptVouchers.ForStep(cheque, 2, new LedgerSide(null, null), null, Template(), "4101")!;
        Assert.Equal([(true, "4101", "200015"), (false, "1201", "200015")], Lines(returned));
        Assert.Equal("عودت چک نزد صندوق", returned.Operation);
    }

    [Fact]
    public void A_note_is_not_in_Sayad_so_it_goes_to_the_bank_without_registration_while_a_cheque_does_not_N3()
    {
        // Rule N3 (owner, 18 Nov 2026) is about cheques: «ثبت صیادی» is a step of cheques only, and a cheque still «نزد صندوق» is refused
        // at the bank. An ordinary note keeps the step «واگذاری به بانک» from the cash desk (owner, 16 Nov 2026).
        var note = Note();
        Assert.DoesNotContain(InstrumentAction.RegisterSayad, note.AllowedActions());
        Assert.Equal("Instrument.KindNotAllowedForAction", Assert.Throws<DomainException>(() => note.RegisterSayad(D("1405/08/02"), User)).Code);
        AtBank(note);
        Assert.Equal(InstrumentState.DepositedForCollection, note.State);

        var cheque = Instrument.Receive(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, "1", "بانک ملت", Money.Rials(1), D("1405/08/01"), D("1405/09/01"),
            CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User);
        Assert.DoesNotContain(InstrumentAction.DepositForCollection, cheque.AllowedActions());
        Assert.Equal("Instrument.SayadRegistrationRequired",
            Assert.Throws<DomainException>(() => cheque.DepositForCollection(new CollectionAccount(BankA, "0101", "IRR", true), D("1405/08/05"), User)).Code);
    }

    [Fact]
    public void A_protested_note_taken_to_law_moves_to_the_legal_account_and_can_come_back_to_the_cash_desk()
    {
        // Owner, 18 Nov 2026: bounced notes may get a legal action; the voucher is the one of a bounced cheque (answer to Q38), and
        // the number, date and tracking code of the bank's paper are required.
        var note = AtBank();
        note.RecordBounced(D("1405/09/02"), User, "عدم پرداخت", "W-12");
        Assert.Equal("Instrument.NonPaymentCertificateRequired",
            Assert.Throws<DomainException>(() => note.SendToLegal(D("1405/09/10"), User, "تحویل به وکیل")).Code);
        note.SendToLegal(D("1405/09/10"), User, "تحویل به وکیل", new NonPaymentCertificate("W-12", D("1405/09/02"), "TR-1"));
        Assert.Equal(InstrumentState.InLegal, note.State);
        var legal = ReceiptVouchers.ForStep(note, 4, SideA, null, Template())!;
        Assert.Equal([(true, "1205", "200015"), (false, "1203", "200015")], Lines(legal));
        Assert.Equal("اقدام حقوقی سفته", legal.Operation);

        // It became collectable (answer to Q38 (2)): back in the cash desk, it goes to the bank again like a new note.
        note.ReturnFromLegal(D("1405/10/01"), User);
        var back = ReceiptVouchers.ForStep(note, 5, SideA, null, Template())!;
        Assert.Equal([(true, "1201", "200015"), (false, "1205", "200015")], Lines(back));
        Assert.Equal("تحویل سفته به صندوق پس از اقدام حقوقی", back.Operation);
        Assert.Contains(InstrumentAction.DepositForCollection, note.AllowedActions());
    }

    [Fact]
    public void A_cancelled_ordinary_note_goes_to_bad_debts_like_a_cheque()
    {
        var note = Note();
        note.CancelReceived(D("1405/08/10"), User, "M-7", D("1405/08/09"), "سوخت شده");
        var v = ReceiptVouchers.ForStep(note, 2, new LedgerSide(null, null), null, Template())!;
        Assert.Equal([(true, "8101", "200015"), (false, "1201", "200015")], Lines(v));
        Assert.Equal("ابطال سفته دریافتنی", v.Operation);
    }
}
