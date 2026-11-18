using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Vouchers;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// The latest status («آخرین وضعیت») of a received cheque as the owner described it on 18 Nov 2026: «ثبت صیادی شد», «حقوقی», «سررسید چک»,
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
    });

    private static Instrument Cheque(InstrumentKind kind = InstrumentKind.OrdinaryCheque, string received = "1405/08/01", string? due = "1405/09/01") =>
        Instrument.Receive(Guid.NewGuid(), kind, "445566", kind.IsCheque() ? "بانک ملت" : null, Money.Rials(10_000_000), D(received), due is null ? null : D(due),
            CounterpartyRef.ForDetail("200015", "شرکت نمونه"), "فروش", User,
            kind.IsCheque() ? new ReceivedInstrumentDetails(SayadId: "1234567890123456") : ReceivedInstrumentDetails.None, Guid.NewGuid());

    private static Instrument Bounced()
    {
        var c = Cheque();
        c.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User, "F-1");
        c.RecordBounced(D("1405/09/02"), User, "کسری موجودی", "BN-1");
        return c;
    }

    private static (bool, string?, string?)[] Lines(Voucher? v) => [.. v!.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode))];

    [Fact]
    public void A_cheque_in_the_cash_desk_is_registered_in_Sayad_and_stays_in_the_cash_desk()
    {
        var c = Cheque();
        var before = c.AllowedActions().Where(a => a != InstrumentAction.RegisterSayad).ToList();
        c.RegisterSayad(D("1405/08/02"), User, " SY-100 ", "تایید شد");
        Assert.Equal((InstrumentState.SayadRegistered, "SY-100", "SY-100"), (c.State, c.History[^1].Reference, c.History[^1].Details!.SayadTracking));
        Assert.True(c.State.IsInCashDesk());

        // Every step of «نزد صندوق» stays open; the registration itself is recorded once.
        Assert.Equal(before, c.AllowedActions());
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => c.RegisterSayad(D("1405/08/03"), User)).Code);

        // It moves no account of the voucher template (open question Q37).
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
        c.SendToLegal(D("1405/09/10"), User, " آقای وکیل ", "پرونده 12", "جهت اقدام");
        Assert.Equal((InstrumentState.InLegal, "آقای وکیل", "پرونده 12", "آقای وکیل"), (c.State, c.Destination, c.History[^1].Reference, c.History[^1].Details!.PartyName));
        Assert.True(c.State.IsProtested());
        Assert.Equal([InstrumentAction.Replace, InstrumentAction.Cancel], c.AllowedActions());

        // The cheque stays in «اسناد واخواست شده»: no voucher for the handover (open question Q38).
        Assert.Null(ReceiptVouchers.ForStep(c, 4, Bank, null, Template()));
    }

    [Fact]
    public void Only_a_bounced_cheque_goes_to_the_lawyer_and_not_before_the_bounce()
    {
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => Cheque().SendToLegal(D("1405/09/10"), User)).Code);
        var c = Bounced();
        Assert.Equal("Instrument.DateBeforePreviousStep", Assert.Throws<DomainException>(() => c.SendToLegal(D("1405/09/01"), User)).Code);
        c.SendToLegal(D("1405/09/02"), User);
        Assert.Equal((InstrumentState.InLegal, (string?)null), (c.State, c.Destination));
    }

    [Fact]
    public void A_cheque_with_the_lawyer_is_replaced_or_cancelled_out_of_protested_cheques()
    {
        var old = Bounced();
        old.SendToLegal(D("1405/09/10"), User, "وکیل");
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
        Assert.Equal([(true, "1201", "200015"), (false, "1203", "200015")],
            Lines(ReceiptVouchers.ForReceipt(receipt, created, null, "4101", Template(), new Replacement(old, InstrumentState.InLegal))));

        var cancelled = Bounced();
        cancelled.SendToLegal(D("1405/09/10"), User);
        cancelled.CancelReceived(D("1405/10/01"), User, "م-7", D("1405/09/30"), "لاوصول");
        Assert.Equal([(true, "8101", "200015"), (false, "1203", "200015")], Lines(ReceiptVouchers.ForStep(cancelled, 5, Bank, null, Template())));
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
        back.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0101", "IRR", true), D("1405/08/05"), User);
        back.ReturnFromBank(D("1405/09/03"), User);
        back.ReturnToIssuer(D("1405/09/04"), User);
        Assert.Equal([(true, "4101", "200015"), (false, "1201", "200015")], Lines(ReceiptVouchers.ForStep(back, 4, Bank, null, Template(), "4101")));

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
