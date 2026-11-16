using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Vouchers;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// Receipts of promissory notes: the owner's form «فرم دریافت سفته» (16 Nov 2026) and his report no. 4, item 13 ("promissory notes get
/// forms of their own"); vouchers from his template table («دریافت سفته عادی», «دریافت سفته انتظامی»).
/// </summary>
public sealed class NoteReceiptTests
{
    private static readonly PayerFact Issuer = new("200015", "شرکت نمونه بازرگانی", true);
    private static readonly PurposeGroupFact Sales = new(Guid.NewGuid(), "وجوه حاصل فروش", true, true, "4101");

    private static ReceiptHeader Header(string date = "1405/08/25") => new(D(date), Issuer, Sales, "بابت بدهی فاکتور ۱۲۳");

    private static NoteReceiptTerms Terms(NoteReason reason = NoteReason.Debt, NoteMaturity maturity = NoteMaturity.Fixed) =>
        new(reason, NoteForm.Paper, maturity);

    private static Instrument Note(Guid receiptId, NoteReceiptTerms terms, string number = "123456", string? due = "1405/11/30", string? written = "1405/08/20",
        decimal amount = 500_000_000, string date = "1405/08/25", NoteChecks? checks = null, IReadOnlyList<NoteGuarantor>? guarantors = null) =>
        Instrument.Receive(Guid.NewGuid(), terms.InstrumentKind, number, null, Money.Rials(amount), D(date), due is null ? null : D(due),
            CounterpartyRef.ForDetail(Issuer.DetailCode, Issuer.Name), "بابت بدهی فاکتور ۱۲۳", User,
            new ReceivedInstrumentDetails(Guarantors: guarantors, WrittenOn: written is null ? null : D(written).Value,
                OnDemand: terms.Maturity == NoteMaturity.OnDemand, Checks: checks),
            receiptId);

    [Fact]
    public void Notes_of_one_issuer_come_on_one_receipt_as_instruments_in_the_cash_desk_owner_form()
    {
        var id = Guid.NewGuid();
        var terms = Terms();
        var a = Note(id, terms, "123456", checks: new NoteChecks(Signature: true, Date: true, AmountInWords: true, Payee: true, PaymentDate: true));
        var b = Note(id, terms, "123457", due: "1405/12/25", amount: 250_000_000);

        var r = Receipt.RecordNotes(id, 4, Header(), terms, [a, b], User);

        Assert.Equal(ReceiptKind.Notes, r.Kind);
        Assert.Equal(4, r.Number);
        Assert.Equal(750_000_000m, r.TotalRial);
        Assert.Equal([a.Id, b.Id], r.InstrumentIds);
        Assert.Equal(NoteReason.Debt, r.NoteTerms!.Reason);
        Assert.Null(r.AccountId);
        Assert.Equal(InstrumentKind.PromissoryNote, a.Kind);
        Assert.Equal(InstrumentState.InCashDesk, a.State);
        Assert.Equal(D("1405/08/20").Value, a.Details.WrittenOn);
        Assert.True(a.Details.Checks!.AmountInWords);
        Assert.IsType<ReceiptRecorded>(Assert.Single(r.PendingEvents));
    }

    [Theory]
    [InlineData(NoteReason.Debt, InstrumentKind.PromissoryNote)]
    [InlineData(NoteReason.Other, InstrumentKind.PromissoryNote)]
    [InlineData(NoteReason.Guarantee, InstrumentKind.GuaranteeNote)]
    [InlineData(NoteReason.Trust, InstrumentKind.GuaranteeNote)]
    public void Guarantee_and_trust_notes_are_guarantee_notes_the_others_ordinary_notes(NoteReason reason, InstrumentKind kind)
    {
        var terms = Terms(reason);
        Assert.Equal(kind, terms.InstrumentKind);
        var id = Guid.NewGuid();
        var wrong = Instrument.Receive(Guid.NewGuid(), kind == InstrumentKind.PromissoryNote ? InstrumentKind.GuaranteeNote : InstrumentKind.PromissoryNote, "9", null,
            Money.Rials(1), D("1405/08/25"), D("1405/09/01"), CounterpartyRef.ForDetail(Issuer.DetailCode, Issuer.Name), "x", User, ReceivedInstrumentDetails.None, id);
        Assert.Equal("Receipt.NoteKindMismatch", Assert.Throws<DomainException>(() => Receipt.RecordNotes(id, 1, Header(), terms, [wrong], User)).Code);
    }

    [Fact]
    public void A_note_payable_on_demand_has_no_due_date_and_a_dated_note_needs_one()
    {
        var onDemand = Terms(maturity: NoteMaturity.OnDemand);
        var id = Guid.NewGuid();
        var note = Note(id, onDemand, due: null);
        Assert.Null(note.DueDate);
        Assert.True(note.Details.OnDemand);
        Assert.False(note.IsPostDated);
        Assert.Equal(NoteMaturity.OnDemand, Receipt.RecordNotes(id, 1, Header(), onDemand, [note], User).NoteTerms!.Maturity);

        Assert.Equal("Instrument.OnDemandHasDueDate", Assert.Throws<DomainException>(() => Note(id, onDemand, due: "1405/11/30")).Code);
        Assert.Equal("Instrument.DueDateRequired", Assert.Throws<DomainException>(() => Note(id, Terms(), due: null)).Code);

        // A guarantee note may have no due date of its own, but on a receipt with «معین» every note has one.
        var guarantee = Terms(NoteReason.Guarantee);
        var undated = Note(id, guarantee, due: null);
        Assert.Equal("Instrument.DueDateRequired", Assert.Throws<DomainException>(() => Receipt.RecordNotes(id, 1, Header(), guarantee, [undated], User)).Code);

        // «معین» and «عندالمطالبه» are chosen once for the receipt.
        Assert.Equal("Receipt.NoteKindMismatch", Assert.Throws<DomainException>(() => Receipt.RecordNotes(id, 1, Header(), Terms(), [note], User)).Code);
    }

    [Fact]
    public void A_note_is_not_due_before_the_day_it_was_written_rule_R6() =>
        Assert.Equal("Instrument.DueBeforeIssue", Assert.Throws<DomainException>(() => Note(Guid.NewGuid(), Terms(), due: "1405/08/01", written: "1405/08/20")).Code);

    [Fact]
    public void Only_notes_carry_an_issue_date_on_demand_and_checks()
    {
        var cheque = Instrument.Receive(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, "1", "بانک ملت", Money.Rials(1), D("1405/08/25"), D("1405/09/01"),
            CounterpartyRef.ForDetail(Issuer.DetailCode, Issuer.Name), "x", User,
            new ReceivedInstrumentDetails(SayadId: "1234567890123456", WrittenOn: D("1405/08/20").Value, OnDemand: true, Checks: new NoteChecks(true)), null);
        Assert.Null(cheque.Details.WrittenOn);
        Assert.False(cheque.Details.OnDemand);
        Assert.Null(cheque.Details.Checks);
    }

    [Fact]
    public void A_guarantor_keeps_its_detail_code_and_phone_owner_form()
    {
        var note = Note(Guid.NewGuid(), Terms(), guarantors: [new("رضا احمدی", "0123456789", " 300045 ", " 021-88990011 ")]);
        var g = Assert.Single(note.Details.GuarantorList);
        Assert.Equal(("300045", "021-88990011"), (g.DetailCode, g.Phone));
        Assert.Equal("Instrument.GuarantorPhoneTooLong", Assert.Throws<DomainException>(() =>
            Note(Guid.NewGuid(), Terms(), guarantors: [new("رضا احمدی", null, null, new string('1', 31))])).Code);
    }

    [Fact]
    public void The_receipt_needs_at_least_one_note_of_this_issuer_on_this_date()
    {
        var id = Guid.NewGuid();
        Assert.Equal("Receipt.NoInstruments", Assert.Throws<DomainException>(() => Receipt.RecordNotes(id, 1, Header(), Terms(), [], User)).Code);
        var otherDay = Note(id, Terms(), date: "1405/08/24");
        Assert.Equal("Receipt.InstrumentMismatch", Assert.Throws<DomainException>(() => Receipt.RecordNotes(id, 1, Header(), Terms(), [otherDay], User)).Code);
        var otherReceipt = Note(Guid.NewGuid(), Terms());
        Assert.Equal("Receipt.InstrumentMismatch", Assert.Throws<DomainException>(() => Receipt.RecordNotes(id, 1, Header(), Terms(), [otherReceipt], User)).Code);
    }

    [Fact]
    public void The_terms_keep_what_applies_to_the_reason_and_the_date_of_the_original()
    {
        var id = Guid.NewGuid();
        var full = new NoteReceiptTerms(NoteReason.Guarantee, NoteForm.Electronic, NoteMaturity.Fixed, "0123456789", " 0912 ", " حسن انجام قرارداد ۴۵ ", "پس از تأیید واحد فنی",
            "صندوق شرکت", true, D("1405/08/26"), " ", new NoteAttachments(NoteImage: true, Contract: true), "علی رضایی", "مدیر مالی");
        var t = Receipt.RecordNotes(id, 1, Header(), full, [Note(id, full)], User).NoteTerms!;
        Assert.Equal(("حسن انجام قرارداد ۴۵", "پس از تأیید واحد فنی", "0912", D("1405/08/26")), (t.CommitmentSubject, t.ReturnTerms, t.IssuerPhone, t.OriginalReceivedOn));
        Assert.Null(t.Discrepancy);
        Assert.True(t.Attachments!.Contract);

        // «موضوع تعهد» belongs to a guarantee; a note that pays a debt has no return terms; no date without the original.
        var debt = full with { Reason = NoteReason.Debt, OriginalReceived = false };
        var d = Receipt.RecordNotes(id, 1, Header(), debt, [Note(id, debt)], User).NoteTerms!;
        Assert.Null(d.CommitmentSubject);
        Assert.Null(d.ReturnTerms);
        Assert.Null(d.OriginalReceivedOn);
    }

    [Fact]
    public void The_terms_are_checked()
    {
        var id = Guid.NewGuid();
        string Code(NoteReceiptTerms terms) => Assert.Throws<DomainException>(() => Receipt.RecordNotes(id, 1, Header(), terms, [Note(id, Terms())], User)).Code;
        Assert.Equal("Receipt.NoteTermsRequired", Code(Terms() with { Reason = (NoteReason)99 }));
        Assert.Equal("Receipt.IssuerNationalCodeInvalid", Code(Terms() with { IssuerNationalCode = "0123456788" }));
        Assert.Equal("Receipt.OriginalDateRequired", Code(Terms() with { OriginalReceived = true }));
        Assert.Equal("Receipt.NoteTextTooLong", Code(Terms() with { CustodyPlace = new string('ن', 501) }));
    }

    [Fact]
    public void A_note_receipt_is_not_cancelled_each_note_is_returned_instead()
    {
        var id = Guid.NewGuid();
        var r = Receipt.RecordNotes(id, 1, Header(), Terms(), [Note(id, Terms())], User);
        Assert.Equal("Receipt.CancelInstruments", Assert.Throws<DomainException>(() => r.Cancel(D("1405/08/26"), "اشتباه", User)).Code);
    }

    [Fact]
    public void An_ordinary_note_debits_notes_in_the_cash_desk_and_credits_the_purpose_group_owner_table()
    {
        var id = Guid.NewGuid();
        var terms = Terms(maturity: NoteMaturity.OnDemand);
        var r = Receipt.RecordNotes(id, 7, Header(), terms, [Note(id, terms, due: null)], User);
        var template = new VoucherTemplate(new Dictionary<FixedAccount, string> { [FixedAccount.ChequesInCashDesk] = "1201" });

        var v = ReceiptVouchers.ForReceipt(r, [Note(id, terms, due: null)], null, Sales.LedgerAccountCode, template);

        Assert.Equal([(true, "1201", "200015", 500_000_000m), (false, "4101", "200015", 500_000_000m)],
            v.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode, l.AmountRial)));
        Assert.Equal("دریافت سفته – رسید 7", v.Operation);
        Assert.Equal("سفته 123456 عندالمطالبه – شرکت نمونه بازرگانی", v.Lines[0].Description);
        Assert.True(v.IsComplete);
    }

    [Fact]
    public void A_guarantee_note_goes_to_guarantee_notes_for_the_company_against_the_guarantee_counterpart_owner_table()
    {
        var id = Guid.NewGuid();
        var terms = Terms(NoteReason.Guarantee);
        var note = Note(id, terms);
        var r = Receipt.RecordNotes(id, 1, Header(), terms, [note], User);
        var template = new VoucherTemplate(new Dictionary<FixedAccount, string>
        {
            [FixedAccount.GuaranteeNotesForCompany] = "9102",
            [FixedAccount.GuaranteeChequesForCompany] = "9101",
            [FixedAccount.GuaranteeCounterpart] = "9201",
        });

        var v = ReceiptVouchers.ForReceipt(r, [note], null, Sales.LedgerAccountCode, template);
        Assert.Equal([(true, "9102", "200015"), (false, "9201", "200015")], v.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode)));

        // Until the account is set in the template the line names what is missing.
        var missing = ReceiptVouchers.ForReceipt(r, [note], null, Sales.LedgerAccountCode, new VoucherTemplate());
        Assert.Equal(FixedAccount.GuaranteeNotesForCompany, missing.Lines[0].Missing);
        Assert.False(missing.IsComplete);

        // Cancelling it reverses the same accounts («ابطال چک / سفته انتظامی دریافتی نزد صندوق»).
        note.CancelReceived(D("1405/09/01"), User, "M-1", D("1405/09/01"), "پایان قرارداد");
        var cancel = ReceiptVouchers.ForStep(note, 2, new LedgerSide(null, null), null, template)!;
        Assert.Equal([(true, "9201"), (false, "9102")], cancel.Lines.Select(l => (l.IsDebit, l.AccountCode)));
        Assert.Equal("ابطال سفته انتظامی دریافتی", cancel.Operation);
    }
}
