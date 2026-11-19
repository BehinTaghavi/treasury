using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Vouchers;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// The latest status («آخرین وضعیت») of a received cheque as the owner described it on 18 Nov 2026: «ثبت صیادی شد», «اقدام حقوقی», «سررسید چک»,
/// replacement from the cash desk, bounced or legal, and a voucher for the return to the payer (docs/domain/received-cheque-steps.md,
/// section 5). The old program kept legal action as the flag GoInlaw on dbo.Checks.
/// </summary>
public sealed class ChequeStatusTests
{
    private static readonly LedgerSide Bank = new("1102", "600001");

    private static VoucherTemplate Template() => new(new Dictionary<FixedAccount, string>
    {
        [FixedAccount.ChequesInCashDesk] = "1201",
        [FixedAccount.ChequesInCollection] = "1202",
        [FixedAccount.ProtestedCheques] = "1203",
        [FixedAccount.GuaranteeChequesForCompany] = "9101",
        [FixedAccount.GuaranteeCounterpart] = "9201",
        [FixedAccount.BadDebts] = "8101",
        [FixedAccount.ProtestedChequesInLegal] = "1204",
        [FixedAccount.GuaranteeChequesInLegal] = "9102",
        [FixedAccount.GuaranteeNotesForCompany] = "9103",
    });

    private static Instrument Cheque(InstrumentKind kind = InstrumentKind.OrdinaryCheque, string received = "1405/08/01", string? due = "1405/09/01") =>
        Instrument.Receive(Guid.NewGuid(), kind, "445566", kind.IsCheque() ? "بانک ملت" : null, Money.Rials(10_000_000), D(received), due is null ? null : D(due),
            CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User,
            kind.IsCheque() ? new ReceivedInstrumentDetails(SayadId: "1234567890123456") : ReceivedInstrumentDetails.None, Guid.NewGuid());

    private static Instrument Bounced()
    {
        var c = Cheque();
        c.RegisterSayad(D("1405/08/02"), User);
        c.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User, "F-1");
        c.RecordBounced(D("1405/09/02"), User, "کسری موجودی", "BN-1");
        return c;
    }

    /// <summary>The certificate of non-payment of the bounce in <see cref="Bounced"/>: required for its legal action (owner, 18 Nov 2026).</summary>
    private static readonly NonPaymentCertificate Certificate = new("BN-1", D("1405/09/02"), "1234567890123456");

    private static (bool, string?, string?)[] Lines(Voucher? v) => [.. v!.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode))];

    [Fact]
    public void A_note_has_no_Sayad_registration_and_goes_to_the_bank_straight_from_the_cash_desk()
    {
        // Owner, 16 Nov 2026 (واگذاری سفته به بانک) and 18 Nov 2026 when the notes (#10) and rule N3 were joined: Sayad is for cheques
        // only, so an ordinary note is deposited from «نزد صندوق»; a cheque needs «ثبت صیادی» first.
        var note = Cheque(InstrumentKind.PromissoryNote);
        Assert.Contains(InstrumentAction.DepositForCollection, note.AllowedActions());
        Assert.DoesNotContain(InstrumentAction.RegisterSayad, note.AllowedActions());
        Assert.Equal("Instrument.KindNotAllowedForAction", Assert.Throws<DomainException>(() => note.RegisterSayad(D("1405/08/02"), User)).Code);
        note.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User);
        Assert.Equal(InstrumentState.DepositedForCollection, note.State);

        var cheque = Cheque();
        Assert.Equal("Instrument.SayadRegistrationRequired",
            Assert.Throws<DomainException>(() => cheque.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User)).Code);

        // A guarantee note is still not deposited.
        Assert.DoesNotContain(InstrumentAction.DepositForCollection, Cheque(InstrumentKind.GuaranteeNote, due: null).AllowedActions());
    }

    [Fact]
    public void A_cheque_in_the_cash_desk_is_registered_in_Sayad_and_stays_in_the_cash_desk()
    {
        var c = Cheque();
        var before = c.AllowedActions().Where(a => a != InstrumentAction.RegisterSayad).ToList();
        Assert.DoesNotContain(InstrumentAction.DepositForCollection, before); // rule N3: not before the registration
        Assert.DoesNotContain(InstrumentAction.Endorse, before);              // nor is it transferred («ثبت انتقال», owner, 19 Nov 2026)
        c.RegisterSayad(D("1405/08/02"), User, " SY-100 ", "تایید شد");
        Assert.Equal((InstrumentState.SayadRegistered, "SY-100", "SY-100"), (c.State, c.History[^1].Reference, c.History[^1].Details!.SayadTracking));
        Assert.True(c.State.IsInCashDesk());

        // Every step of «نزد صندوق» stays open, and the deposit at the bank (rule N3) and the transfer open; the registration itself is recorded once.
        Assert.Equal(before.Concat([InstrumentAction.DepositForCollection, InstrumentAction.Endorse]).Order(), c.AllowedActions().Order());
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => c.RegisterSayad(D("1405/08/03"), User)).Code);

        // No voucher: only the latest status changes (owner's answer to Q37, 18 Nov 2026).
        Assert.Null(ReceiptVouchers.ForStep(c, 2, Bank, null, Template()));
    }

    [Fact]
    public void The_Sayad_tracking_number_is_optional_and_only_cheques_are_registered()
    {
        var c = Cheque();
        c.RegisterSayad(D("1405/08/02"), User);
        Assert.Equal((InstrumentState.SayadRegistered, (string?)null, (StepDetails?)null), (c.State, c.History[^1].Reference, c.History[^1].Details));

        var note = Cheque(InstrumentKind.PromissoryNote);
        Assert.DoesNotContain(InstrumentAction.RegisterSayad, note.AllowedActions());
        Assert.Equal("Instrument.KindNotAllowedForAction", Assert.Throws<DomainException>(() => note.RegisterSayad(D("1405/08/02"), User)).Code);
    }

    [Fact]
    public void A_cheque_registered_in_Sayad_is_deposited_from_the_cash_desk_account()
    {
        var c = Cheque();
        c.RegisterSayad(D("1405/08/02"), User);
        c.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User, "F-1");
        Assert.Equal(InstrumentState.DepositedForCollection, c.State);
        Assert.Equal([(true, "1202", "600001"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(c, 3, Bank, null, Template())));
    }

    [Fact]
    public void A_bounced_cheque_is_handed_to_the_companys_lawyer()
    {
        var c = Bounced();
        Assert.Null(c.LegalActionStep);

        // The screen «اقدام حقوقی» records the date of the action and its description (owner, 18 Nov 2026).
        Assert.Equal("Instrument.LegalActionDescriptionRequired", Assert.Throws<DomainException>(() => c.SendToLegal(D("1405/09/10"), User, " ")).Code);
        c.SendToLegal(D("1405/09/10"), User, " تحویل به وکیل شرکت جهت طرح دعوی ", Certificate);
        Assert.Equal((InstrumentState.InLegal, D("1405/09/10"), "تحویل به وکیل شرکت جهت طرح دعوی"), (c.State, c.History[^1].Date, c.History[^1].Note));
        Assert.Equal(5, c.LegalActionStep);
        Assert.Equal([InstrumentAction.Replace, InstrumentAction.Cancel, InstrumentAction.ReturnFromLegal], c.AllowedActions());

        // Owner's answer to Q38 (18 Nov 2026): Dr «اسناد واخواست شده حقوقی شده» / Cr «اسناد واخواست شده», the payer on both sides.
        var v = ReceiptVouchers.ForStep(c, 5, Bank, null, Template());
        Assert.Equal([(true, "1204", "200015"), (false, "1203", "200015")], Lines(v));
        Assert.Equal(("اقدام حقوقی چک", D("1405/09/10"), true), (v!.Operation, v.Date, v.IsComplete));

        // Until the new account is set in the template the voucher names it and cannot be confirmed.
        var unset = ReceiptVouchers.ForStep(c, 5, Bank, null, new VoucherTemplate(new Dictionary<FixedAccount, string> { [FixedAccount.ProtestedCheques] = "1203" }));
        Assert.Equal((FixedAccount.ProtestedChequesInLegal, false), (unset!.Lines[0].Missing!.Value, unset.IsComplete));
    }

    [Fact]
    public void Legal_action_on_a_cheque_still_in_the_cash_desk_credits_the_cash_desk_account()
    {
        // Owner's answer to Q38: "if, unlikely as it is, legal action is taken on a cheque that is «نزد صندوق»".
        var c = Cheque();
        c.SendToLegal(D("1405/08/10"), User, "اقدام حقوقی پیش از واگذاری");
        Assert.Equal((InstrumentState.InLegal, 2), (c.State, c.LegalActionStep!.Value));
        Assert.Equal([(true, "1204", "200015"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(c, 2, Bank, null, Template())));

        var registered = Cheque();
        registered.RegisterSayad(D("1405/08/02"), User);
        registered.SendToLegal(D("1405/08/10"), User, "اقدام حقوقی");
        Assert.Equal([(true, "1204", "200015"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(registered, 3, Bank, null, Template())));

        // An ordinary note that has not bounced gets no legal action.
        Assert.DoesNotContain(InstrumentAction.SendToLegal, Cheque(InstrumentKind.PromissoryNote).AllowedActions());
    }

    [Fact]
    public void Guarantee_cheques_and_guarantee_notes_may_get_legal_action()
    {
        // Owner, 18 Nov 2026: "چک تضمینی و سفته تضمینی بعلاوه سفته های برگشتی ممکن است اقدام حقوقی شوند".
        var cheque = Cheque(InstrumentKind.GuaranteeCheque, due: null);
        cheque.SendToLegal(D("1405/08/10"), User, "اقدام حقوقی روی چک تضمینی");
        Assert.Equal((InstrumentState.InLegal, 2), (cheque.State, cheque.LegalActionStep!.Value));
        Assert.Equal([InstrumentAction.Cancel, InstrumentAction.ReturnFromLegal], cheque.AllowedActions());

        var note = Cheque(InstrumentKind.GuaranteeNote, due: null);
        note.SendToLegal(D("1405/08/10"), User, "اقدام حقوقی روی سفته تضمینی");
        Assert.Equal(InstrumentState.InLegal, note.State);

        // Owner's answer to Q42 (18 Nov 2026): «اسناد تضمینی به نفع شرکت» is credited and «اسناد تضمینی حقوقی شده به نفع شرکت» debited.
        var v = ReceiptVouchers.ForStep(cheque, 2, Bank, null, Template());
        Assert.Equal([(true, "9102", "200015"), (false, "9101", "200015")], Lines(v));
        Assert.Equal("اقدام حقوقی چک تضمینی", v!.Operation);
        // A guarantee note leaves its own account «سفته انتظامی به نفع شرکت» (#10, owner's answer to Q37 of the note questions).
        Assert.Equal([(true, "9102", "200015"), (false, "9103", "200015")], Lines(ReceiptVouchers.ForStep(note, 2, Bank, null, Template())));

        // Cancelled in legal action: out of the account it is in now (answer to the owner's point 3).
        cheque.CancelReceived(D("1405/09/01"), User, "م-3", D("1405/08/30"), "پایان دعوی");
        Assert.Equal([(true, "9201", "200015"), (false, "9102", "200015")], Lines(ReceiptVouchers.ForStep(cheque, 3, Bank, null, Template())));

        // Scans are attached to it like to any legal action.
        var file = InstrumentAttachment.ForLegalAction(Guid.NewGuid(), note, 0, "a.pdf", Pdf, User, Now, D("1405/08/11"));
        Assert.Equal(2, file.Step);
    }

    [Fact]
    public void A_bounced_note_may_get_legal_action()
    {
        // Notes are not deposited through this system yet; a bounced note comes from the old data (Stcode 14).
        var note = Instrument.Restore(Guid.NewGuid(), InstrumentDirection.Received, InstrumentKind.PromissoryNote, "S-17", null, null, null, null, Money.Rials(5_000_000),
            D("1386/10/01"), D("1386/11/01"), CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", null, InstrumentState.Bounced, 1386,
            [new InstrumentHistoryEntry(InstrumentAction.Receive, null, InstrumentState.InCashDesk, D("1386/10/01"), User, null, null),
             new InstrumentHistoryEntry(InstrumentAction.RecordBounced, InstrumentState.DepositedForCollection, InstrumentState.Bounced, D("1386/11/05"), User, null, null)], 1);
        Assert.Contains(InstrumentAction.SendToLegal, note.AllowedActions());
        note.SendToLegal(D("1405/08/10"), User, "اقدام حقوقی سفته برگشتی", Certificate);
        Assert.Equal(InstrumentState.InLegal, note.State);
        Assert.Equal([(true, "1204", "200015"), (false, "1203", "200015")], Lines(ReceiptVouchers.ForStep(note, 3, Bank, null, Template())));
    }

    [Fact]
    public void A_cheque_at_the_bank_or_back_from_it_gets_no_legal_action_and_the_action_is_not_dated_before_the_bounce()
    {
        var atBank = Cheque();
        atBank.RegisterSayad(D("1405/08/02"), User);
        atBank.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User);
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => atBank.SendToLegal(D("1405/09/10"), User, "اقدام")).Code);
        atBank.ReturnFromBank(D("1405/09/03"), User);
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => atBank.SendToLegal(D("1405/09/10"), User, "اقدام")).Code);
        var c = Bounced();
        Assert.Equal("Instrument.DateBeforePreviousStep", Assert.Throws<DomainException>(() => c.SendToLegal(D("1405/09/01"), User, "اقدام", Certificate)).Code);
        c.SendToLegal(D("1405/09/02"), User, "اقدام", Certificate);
        Assert.Equal(InstrumentState.InLegal, c.State);
    }

    [Fact]
    public void A_cheque_in_legal_action_is_replaced_or_cancelled_out_of_the_legal_account()
    {
        var old = Bounced();
        old.SendToLegal(D("1405/09/10"), User, "تحویل به وکیل", Certificate);
        var receiptId = Guid.NewGuid();
        var header = new ReceiptHeader(D("1405/10/01"), new PayerFact("200015", "شرکت نمونه", true), new PurposeGroupFact(Guid.NewGuid(), "فروش", true, true, "4101"), "تعویض چک حقوقی");
        var created = new[]
        {
            Instrument.Receive(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, "9", "بانک ملی", Money.Rials(10_000_000), D("1405/10/01"), D("1405/11/01"),
                CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User, new ReceivedInstrumentDetails(SayadId: "1234567890123456"), receiptId),
        };
        var receipt = Receipt.RecordInstruments(receiptId, 3, header, created, User, old);
        old.Replace(receipt.Id, receipt.Date, User);
        Assert.Equal((InstrumentState.Replaced, (InstrumentState?)InstrumentState.InLegal), (old.State, old.History[^1].From));
        // The legal action moved the cheque to «اسناد واخواست شده حقوقی شده» (answer to Q38), so that account is credited now.
        Assert.Equal([(true, "1201", "200015"), (false, "1204", "200015")],
            Lines(ReceiptVouchers.ForReceipt(receipt, created, null, "4101", Template(), new Replacement(old, InstrumentState.InLegal))));

        var cancelled = Bounced();
        cancelled.SendToLegal(D("1405/09/10"), User, "تحویل به وکیل", Certificate);
        cancelled.CancelReceived(D("1405/10/01"), User, "م-7", D("1405/09/30"), "لاوصول");
        Assert.Equal([(true, "8101", "200015"), (false, "1204", "200015")], Lines(ReceiptVouchers.ForStep(cancelled, 6, Bank, null, Template())));
    }

    [Fact]
    public void The_legal_action_keeps_the_certificate_of_non_payment_with_its_tracking_code()
    {
        // Owner, 18 Nov 2026: «گواهی عدم پرداخت» is the right name, and the legal-action screen takes its number, date and tracking code.
        var c = Bounced();
        c.SendToLegal(D("1405/09/10"), User, "تحویل به وکیل", new NonPaymentCertificate(" BN-1 ", D("1405/09/02"), " 1234567890123456 "));
        var step = c.History[^1];
        Assert.Equal(("BN-1", (FiscalDate?)D("1405/09/02"), "1234567890123456"), (step.Reference, step.Details!.DocumentDate, step.Details.TrackingCode));

        // Owner, 18 Nov 2026: for a bounced ordinary cheque or note the three fields are required.
        foreach (var missing in new NonPaymentCertificate?[] { null, new(null, D("1405/09/02"), "123"), new("BN-1", null, "123"), new("BN-1", D("1405/09/02"), " ") })
        {
            Assert.Equal("Instrument.NonPaymentCertificateRequired", Assert.Throws<DomainException>(() => Bounced().SendToLegal(D("1405/09/10"), User, "اقدام", missing)).Code);
        }

        // The bounce itself may already carry the tracking code (asked by the owner the same day).
        var withCode = Cheque();
        withCode.RegisterSayad(D("1405/08/02"), User);
        withCode.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User);
        withCode.RecordBounced(D("1405/09/02"), User, "کسری موجودی", "BN-2", " 5555444433332222 ");
        Assert.Equal(("BN-2", "5555444433332222"), (withCode.History[^1].Reference, withCode.History[^1].Details!.TrackingCode));
        Assert.NotNull(withCode.History[^1].Details!.AccountId);

        // A cheque without a certificate (a guarantee cheque, or one still in the cash desk) records none.
        var guarantee = Cheque(InstrumentKind.GuaranteeCheque, due: null);
        guarantee.SendToLegal(D("1405/08/10"), User, "اقدام", new NonPaymentCertificate(" ", null, null));
        Assert.Equal(((string?)null, (StepDetails?)null), (guarantee.History[^1].Reference, guarantee.History[^1].Details));
    }

    [Fact]
    public void A_cheque_that_becomes_collectable_in_legal_action_goes_back_to_the_cash_desk_like_a_new_cheque()
    {
        // Owner's answer to Q38 (2), 18 Nov 2026.
        var c = Bounced();
        c.SendToLegal(D("1405/09/10"), User, "تحویل به وکیل", Certificate);
        Assert.Equal("Instrument.DateBeforePreviousStep", Assert.Throws<DomainException>(() => c.ReturnFromLegal(D("1405/09/09"), User)).Code);
        c.ReturnFromLegal(D("1405/10/01"), User, "توافق با صادرکننده");
        Assert.Equal((InstrumentState.InCashDesk, (int?)null, "توافق با صادرکننده"), (c.State, c.LegalActionStep, c.History[^1].Note));

        // The cheque leaves the legal account: Dr «اسناد نزد صندوق» / Cr «اسناد واخواست شده حقوقی شده» (owner's answer to Q43).
        var v = ReceiptVouchers.ForStep(c, 6, Bank, null, Template());
        Assert.Equal([(true, "1201", "200015"), (false, "1204", "200015")], Lines(v));
        Assert.Equal("تحویل چک به صندوق پس از اقدام حقوقی", v!.Operation);

        // From there it goes on like a new cheque: registered in Sayad (rule N3), deposited, collected.
        Assert.Equal("Instrument.SayadRegistrationRequired",
            Assert.Throws<DomainException>(() => c.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/10/02"), User)).Code);
        c.RegisterSayad(D("1405/10/02"), User);
        c.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/10/03"), User);
        Assert.Equal([(true, "1202", "600001"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(c, 8, Bank, null, Template())));
        c.RecordCleared(D("1405/10/05"), User);
        Assert.Equal(InstrumentState.Cleared, c.State);

        // A guarantee cheque goes back between its two memorandum accounts; only a cheque in legal action is handed back.
        var guarantee = Cheque(InstrumentKind.GuaranteeCheque, due: null);
        guarantee.SendToLegal(D("1405/08/10"), User, "اقدام");
        guarantee.ReturnFromLegal(D("1405/09/01"), User);
        Assert.Equal([(true, "9101", "200015"), (false, "9102", "200015")], Lines(ReceiptVouchers.ForStep(guarantee, 3, Bank, null, Template())));
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => Bounced().ReturnFromLegal(D("1405/10/01"), User)).Code);
    }

    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37];
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Scans_are_attached_to_the_legal_action_of_a_cheque()
    {
        var c = Bounced();
        c.SendToLegal(D("1405/09/10"), User, "تحویل به وکیل", Certificate);
        var file = InstrumentAttachment.ForLegalAction(Guid.NewGuid(), c, 0, @"C:\scans\دادخواست.pdf", Pdf, User, Now, D("1405/09/11"));
        Assert.Equal((c.Id, 5, "دادخواست.pdf", "application/pdf", 8, 1405), (file.InstrumentId, file.Step, file.FileName, file.ContentType, file.Size, file.FiscalYear));
        Assert.IsType<InstrumentFileAttached>(Assert.Single(file.PendingEvents));

        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1];
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0];
        Assert.Equal("image/png", InstrumentAttachment.ForLegalAction(Guid.NewGuid(), c, 1, "a.png", png, User, Now, D("1405/09/11")).ContentType);
        Assert.Equal(("image/jpeg", "scan"), (InstrumentAttachment.ForLegalAction(Guid.NewGuid(), c, 2, " ", jpeg, User, Now, D("1405/09/11")) is var j ? (j.ContentType, j.FileName) : default));
    }

    [Fact]
    public void A_scan_is_a_pdf_or_picture_of_at_most_5_MB_and_a_cheque_has_at_most_20()
    {
        var c = Bounced();
        c.SendToLegal(D("1405/09/10"), User, "تحویل به وکیل", Certificate);
        string Code(byte[] content, int already = 0) =>
            Assert.Throws<DomainException>(() => InstrumentAttachment.ForLegalAction(Guid.NewGuid(), c, already, "x", content, User, Now, D("1405/09/11"))).Code;
        Assert.Equal("Instrument.AttachmentEmpty", Code([]));
        Assert.Equal("Instrument.AttachmentType", Code([0x4D, 0x5A, 0x90, 0x00]));
        Assert.Equal("Instrument.TooManyAttachments", Code(Pdf, InstrumentAttachment.MaxFilesPerCheque));
        var large = new byte[InstrumentAttachment.MaxBytes + 1];
        Pdf.CopyTo(large, 0);
        Assert.Equal("Instrument.AttachmentTooLarge", Code(large));
    }

    [Fact]
    public void Scans_are_added_and_removed_only_while_the_cheque_is_in_legal_action()
    {
        var bounced = Bounced();
        Assert.Equal("Instrument.NotInLegalAction",
            Assert.Throws<DomainException>(() => InstrumentAttachment.ForLegalAction(Guid.NewGuid(), bounced, 0, "a.pdf", Pdf, User, Now, D("1405/09/11"))).Code);

        bounced.SendToLegal(D("1405/09/10"), User, "تحویل به وکیل", Certificate);
        var file = InstrumentAttachment.ForLegalAction(Guid.NewGuid(), bounced, 0, "a.pdf", Pdf, User, Now, D("1405/09/11"));
        var kept = InstrumentAttachment.Restore(file.Id, file.InstrumentId, file.Step, file.FileName, file.ContentType, file.Size, file.UploadedBy, file.UploadedAt, file.FiscalYear);
        kept.Remove(bounced, User);
        Assert.IsType<InstrumentFileRemoved>(Assert.Single(kept.PendingEvents));

        // After the cheque leaves legal action (here: cancelled) its scans stay as they are.
        bounced.CancelReceived(D("1405/10/01"), User, "م-7", D("1405/09/30"), "لاوصول");
        Assert.Equal("Instrument.NotInLegalAction", Assert.Throws<DomainException>(() => kept.Remove(bounced, User)).Code);
        Assert.Equal("Instrument.NotInLegalAction",
            Assert.Throws<DomainException>(() => InstrumentAttachment.ForLegalAction(Guid.NewGuid(), bounced, 1, "b.pdf", Pdf, User, Now, D("1405/10/02"))).Code);
    }

    [Fact]
    public void A_cheque_registered_in_Sayad_is_replaced_out_of_the_cash_desk_account()
    {
        var old = Cheque();
        old.RegisterSayad(D("1405/08/02"), User);
        Assert.Contains(InstrumentAction.Replace, old.AllowedActions());
        var header = new ReceiptHeader(D("1405/08/10"), new PayerFact("200015", "شرکت نمونه", true), new PurposeGroupFact(Guid.NewGuid(), "فروش", true, true, "4101"), "تعویض");
        var receiptId = Guid.NewGuid();
        var created = new[]
        {
            Instrument.Receive(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, "10", "بانک ملی", Money.Rials(10_000_000), D("1405/08/10"), D("1405/10/10"),
                CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User, new ReceivedInstrumentDetails(SayadId: "1234567890123456"), receiptId),
        };
        var receipt = Receipt.RecordInstruments(receiptId, 4, header, created, User, old);
        old.Replace(receipt.Id, receipt.Date, User);
        var v = ReceiptVouchers.ForReceipt(receipt, created, null, "4101", Template(), new Replacement(old, InstrumentState.SayadRegistered));
        Assert.Equal([(true, "1201", "200015"), (false, "1201", "200015")], Lines(v));
    }

    [Fact]
    public void Returning_a_cheque_to_the_payer_takes_the_receipts_entry_back()
    {
        // Owner's table «عودت چک‌های نزد صندوق»: Dr «بدهکاران یا ...» / Cr «اسناد نزد صندوق»; old form D_Check_Od_m.
        var c = Cheque();
        c.RegisterSayad(D("1405/08/02"), User);
        c.ReturnToIssuer(D("1405/08/03"), User, "به درخواست مشتری");
        Assert.Equal(InstrumentState.ReturnedToIssuer, c.State);
        var v = ReceiptVouchers.ForStep(c, 3, Bank, null, Template(), "4101");
        Assert.Equal([(true, "4101", "200015"), (false, "1201", "200015")], Lines(v));
        Assert.True(v!.IsComplete);

        // Without the receipt's account the voucher cannot be confirmed.
        Assert.False(ReceiptVouchers.ForStep(c, 3, Bank, null, Template())!.IsComplete);

        // After the bank gave it back (the owner's case «الف» for cheques returned to the cash desk) it is the same voucher.
        var back = Cheque();
        back.RegisterSayad(D("1405/08/02"), User);
        back.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User);
        back.ReturnFromBank(D("1405/09/03"), User);
        back.ReturnToIssuer(D("1405/09/04"), User);
        Assert.Equal([(true, "4101", "200015"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(back, 5, Bank, null, Template(), "4101")));

        var guarantee = Cheque(InstrumentKind.GuaranteeCheque, due: null);
        guarantee.ReturnToIssuer(D("1405/08/03"), User);
        Assert.Equal([(true, "9201", "200015"), (false, "9101", "200015")], Lines(ReceiptVouchers.ForStep(guarantee, 2, Bank, null, Template(), "4101")));
    }

    [Fact]
    public void A_post_dated_cheque_in_the_cash_desk_or_at_the_bank_reaches_its_due_date()
    {
        var c = Cheque(received: "1405/08/01", due: "1405/09/01");
        Assert.False(c.HasReachedDueDate(D("1405/08/30")));
        Assert.True(c.HasReachedDueDate(D("1405/09/01")));
        c.RegisterSayad(D("1405/08/02"), User);
        Assert.True(c.HasReachedDueDate(D("1405/09/05")));
        c.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User);
        Assert.False(c.HasReachedDueDate(D("1405/08/30")));
        Assert.True(c.HasReachedDueDate(D("1405/09/05")));

        // Once the bank answers, the cheque is no longer waiting at its due date.
        c.RecordCleared(D("1405/09/05"), User);
        Assert.False(c.HasReachedDueDate(D("1405/09/06")));
    }

    [Fact]
    public void Current_cheques_guarantee_cheques_and_cheques_that_left_the_cash_desk_and_the_bank_are_not_marked_due()
    {
        // A cheque due on the day it is received is a current cheque (چک روز), not «مدت‌دار».
        Assert.False(Cheque(received: "1405/08/01", due: "1405/08/01").HasReachedDueDate(D("1405/08/10")));
        Assert.False(Cheque(InstrumentKind.GuaranteeCheque, due: null).HasReachedDueDate(D("1406/01/01")));

        var returned = Cheque();
        returned.ReturnToIssuer(D("1405/08/03"), User);
        Assert.False(returned.HasReachedDueDate(D("1405/09/05")));
        Assert.False(Bounced().HasReachedDueDate(D("1405/09/05")));

        var issued = ChequeBeingIssued(InstrumentKind.PostDatedCheque, "1405/08/10", "1405/09/10");
        Assert.False(issued.HasReachedDueDate(D("1405/09/20")));
    }
}
