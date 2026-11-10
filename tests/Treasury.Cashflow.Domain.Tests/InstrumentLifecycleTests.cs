using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Instruments;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>Characterization tests: each one states a behaviour of the old Access program (doc section 12.3 / 12.4).</summary>
public sealed class ReceivedInstrumentTests
{
    [Fact]
    public void A_received_cheque_starts_in_the_cash_desk()
    {
        var cheque = ReceivedCheque();
        Assert.Equal(InstrumentState.InCashDesk, cheque.State);
        Assert.Equal(1405, cheque.OriginFiscalYear);
        var entry = Assert.Single(cheque.History);
        Assert.Equal(InstrumentAction.Receive, entry.Action);
        Assert.Single(cheque.PendingEvents);
    }

    [Fact]
    public void Full_collection_path_11_12_13()
    {
        var cheque = ReceivedCheque();
        cheque.DepositForCollection("بانک ملت - حساب جاری ۱۲۳", D("1405/08/05"), User);
        Assert.Equal(InstrumentState.DepositedForCollection, cheque.State);
        cheque.RecordCleared(D("1405/09/01"), User);
        Assert.Equal(InstrumentState.Cleared, cheque.State);
        Assert.Equal(3, cheque.History.Count);
        Assert.Empty(cheque.AllowedActions());
    }

    [Fact]
    public void A_deposited_cheque_can_bounce_12_14()
    {
        var cheque = ReceivedCheque();
        cheque.DepositForCollection("حساب واگذاری", D("1405/08/05"), User);
        cheque.RecordBounced(D("1405/09/02"), User, "کسری موجودی");
        Assert.Equal(InstrumentState.Bounced, cheque.State);
        Assert.Equal("کسری موجودی", cheque.History[^1].Note);
    }

    [Fact]
    public void Clearing_a_cheque_still_in_the_cash_desk_is_refused()
    {
        // Old message: "ترتيب عمليات نادرست است"
        var cheque = ReceivedCheque();
        var ex = Assert.Throws<DomainException>(() => cheque.RecordCleared(D("1405/08/05"), User));
        Assert.Equal("Instrument.InvalidTransition", ex.Code);
        Assert.Equal(InstrumentState.InCashDesk, cheque.State);
    }

    [Theory]
    [InlineData(InstrumentKind.GuaranteeCheque)]
    [InlineData(InstrumentKind.PromissoryNote)]
    [InlineData(InstrumentKind.Instalment)]
    public void Only_ordinary_and_bank_cheques_can_be_sent_for_collection(InstrumentKind kind)
    {
        var item = ReceivedCheque(kind, due: kind == InstrumentKind.GuaranteeCheque ? null : "1405/09/01");
        Assert.DoesNotContain(InstrumentAction.DepositForCollection, item.AllowedActions());
        var ex = Assert.Throws<DomainException>(() => item.DepositForCollection("حساب", D("1405/08/05"), User));
        Assert.Equal("Instrument.KindNotAllowedForAction", ex.Code);
    }

    [Fact]
    public void A_bank_cheque_can_be_sent_for_collection() =>
        ReceivedCheque(InstrumentKind.BankCheque).DepositForCollection("حساب", D("1405/08/05"), User);

    [Fact]
    public void A_collection_account_is_required()
    {
        var ex = Assert.Throws<DomainException>(() => ReceivedCheque().DepositForCollection(" ", D("1405/08/05"), User));
        Assert.Equal("Instrument.CollectionAccountRequired", ex.Code);
    }

    [Fact]
    public void Return_to_issuer_and_endorsement_only_from_the_cash_desk()
    {
        var returned = ReceivedCheque();
        returned.ReturnToIssuer(D("1405/08/02"), User);
        Assert.Equal(InstrumentState.ReturnedToIssuer, returned.State);

        var endorsed = ReceivedCheque();
        endorsed.Endorse("شرکت پیمانکار ب", D("1405/08/02"), User);
        Assert.Equal(InstrumentState.Endorsed, endorsed.State);
        Assert.Equal("شرکت پیمانکار ب", endorsed.Destination);

        var deposited = ReceivedCheque();
        deposited.DepositForCollection("حساب", D("1405/08/05"), User);
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => deposited.ReturnToIssuer(D("1405/08/06"), User)).Code);
    }

    [Fact]
    public void A_guarantee_note_cannot_be_endorsed_but_can_be_returned()
    {
        var note = ReceivedCheque(InstrumentKind.GuaranteeNote, due: null);
        Assert.Equal([InstrumentAction.ReturnToIssuer], note.AllowedActions());
    }

    [Fact]
    public void A_step_cannot_be_dated_before_the_previous_step()
    {
        var cheque = ReceivedCheque(receivedOn: "1405/08/10");
        Assert.Equal("Instrument.DateBeforePreviousStep", Assert.Throws<DomainException>(() => cheque.DepositForCollection("حساب", D("1405/08/09"), User)).Code);
    }

    [Fact]
    public void A_cheque_received_in_one_year_keeps_its_origin_year_when_cleared_in_the_next()
    {
        var cheque = ReceivedCheque(receivedOn: "1404/12/20", due: "1405/01/15");
        cheque.DepositForCollection("حساب", D("1404/12/25"), User);
        cheque.RecordCleared(D("1405/01/16"), User);
        Assert.Equal(1404, cheque.OriginFiscalYear);
        Assert.Equal(1405, cheque.History[^1].Date.JalaliYear);
    }

    [Fact]
    public void Due_date_is_required_except_for_guarantee_items()
    {
        Assert.Equal("Instrument.DueDateRequired", Assert.Throws<DomainException>(() => ReceivedCheque(due: null)).Code);
        Assert.Null(ReceivedCheque(InstrumentKind.GuaranteeCheque, due: null).DueDate);
    }

    [Fact]
    public void A_post_dated_cheque_kind_is_only_for_issued_cheques() =>
        Assert.Equal("Instrument.KindNotValidForDirection", Assert.Throws<DomainException>(() => ReceivedCheque(InstrumentKind.PostDatedCheque)).Code);

    [Fact]
    public void Every_step_raises_an_event_with_the_destination()
    {
        var cheque = ReceivedCheque();
        cheque.DepositForCollection("حساب واگذاری ۱", D("1405/08/05"), User);
        var last = Assert.IsType<InstrumentStateChanged>(cheque.PendingEvents[^1]);
        Assert.Equal(InstrumentAction.DepositForCollection, last.Action);
        Assert.Equal("حساب واگذاری ۱", last.Destination);
        Assert.Equal(InstrumentState.InCashDesk, last.From);
    }
}

public sealed class IssuedInstrumentTests
{
    [Fact]
    public void Issue_then_deliver_20_21_22()
    {
        var cheque = ChequeBeingIssued();
        Assert.Equal(InstrumentState.BeingIssued, cheque.State);
        var book = Guid.NewGuid();
        cheque.Issue(book, "765432", D("1405/08/10"), User);
        Assert.Equal(InstrumentState.Issued, cheque.State);
        Assert.Equal("765432", cheque.Number);
        Assert.Equal(book, cheque.ChequeBookId);
        cheque.Deliver(D("1405/08/11"), User, "آقای رضایی");
        Assert.Equal(InstrumentState.Delivered, cheque.State);
    }

    [Fact]
    public void An_issued_cheque_can_be_cancelled_with_a_reason_but_not_after_delivery()
    {
        var cancelled = ChequeBeingIssued();
        cancelled.Issue(Guid.NewGuid(), "1", D("1405/08/10"), User);
        Assert.Equal("Instrument.CancelReasonRequired", Assert.Throws<DomainException>(() => cancelled.Cancel(D("1405/08/10"), User, "")).Code);
        cancelled.Cancel(D("1405/08/10"), User, "اشتباه در مبلغ");
        Assert.Equal(InstrumentState.Cancelled, cancelled.State);

        var delivered = ChequeBeingIssued();
        delivered.Issue(Guid.NewGuid(), "2", D("1405/08/10"), User);
        delivered.Deliver(D("1405/08/10"), User);
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => delivered.Cancel(D("1405/08/11"), User, "x")).Code);
    }

    [Theory]
    [InlineData(InstrumentKind.OrdinaryCheque)]
    [InlineData(InstrumentKind.PostDatedCheque)]
    public void Due_date_before_issue_date_is_refused_for_cheques(InstrumentKind kind)
    {
        // Old message: "سررسيدازتاريخ صدوركوچكتراست"
        var ex = Assert.Throws<DomainException>(() => ChequeBeingIssued(kind, issue: "1405/08/10", due: "1405/08/09"));
        Assert.Equal("Instrument.DueBeforeIssue", ex.Code);
    }

    [Fact]
    public void An_instalment_may_have_an_earlier_due_date_as_in_the_old_program() =>
        Assert.Equal(InstrumentState.BeingIssued, ChequeBeingIssued(InstrumentKind.Instalment, issue: "1405/08/10", due: "1405/08/01").State);

    [Fact]
    public void Received_only_actions_are_not_offered_for_issued_cheques()
    {
        var cheque = ChequeBeingIssued();
        Assert.Equal([InstrumentAction.Issue, InstrumentAction.Cancel], cheque.AllowedActions());
        Assert.Equal("Instrument.InvalidTransition", Assert.Throws<DomainException>(() => cheque.RecordCleared(D("1405/08/11"), User)).Code);
    }
}

public sealed class IssuedChequeAtTheBankTests
{
    [Fact]
    public void A_post_dated_cheque_matures_once_and_is_then_paid_by_the_bank()
    {
        var cheque = ChequeBeingIssued(InstrumentKind.PostDatedCheque, issue: "1405/08/10", due: "1405/10/01");
        cheque.Issue(Guid.NewGuid(), "300", D("1405/08/10"), User);
        cheque.Deliver(D("1405/08/11"), User);
        Assert.Contains(InstrumentAction.RecordMatured, cheque.AllowedActions());
        cheque.RecordMatured(D("1405/10/01"), User);
        Assert.True(cheque.IsMatured);
        Assert.Equal(InstrumentState.Delivered, cheque.State);
        Assert.DoesNotContain(InstrumentAction.RecordMatured, cheque.AllowedActions());
        Assert.Equal("Instrument.AlreadyMatured", Assert.Throws<DomainException>(() => cheque.RecordMatured(D("1405/10/02"), User)).Code);
        cheque.RecordPaidByBank(D("1405/10/03"), User);
        Assert.Equal(InstrumentState.PaidByBank, cheque.State);
        Assert.Empty(cheque.AllowedActions());
    }

    [Fact]
    public void An_ordinary_cheque_has_no_maturity_step()
    {
        var cheque = ChequeBeingIssued();
        cheque.Issue(Guid.NewGuid(), "301", D("1405/08/10"), User);
        Assert.DoesNotContain(InstrumentAction.RecordMatured, cheque.AllowedActions());
        Assert.Equal("Instrument.KindNotAllowedForAction", Assert.Throws<DomainException>(() => cheque.RecordMatured(D("1405/08/11"), User)).Code);
    }

    [Fact]
    public void Our_bank_can_return_an_issued_cheque()
    {
        var cheque = ChequeBeingIssued();
        cheque.Issue(Guid.NewGuid(), "302", D("1405/08/10"), User);
        cheque.RecordBouncedAtBank(D("1405/08/15"), User, "امضا مطابقت ندارد");
        Assert.Equal(InstrumentState.BouncedAtBank, cheque.State);
    }
}

public sealed class LifecycleTableTests
{
    [Fact]
    public void Every_transition_names_its_source_in_the_old_program() =>
        Assert.True(InstrumentLifecycle.All.All(t => !string.IsNullOrWhiteSpace(t.LegacySource)));

    [Fact]
    public void There_is_at_most_one_transition_per_state_and_action()
    {
        var keys = InstrumentLifecycle.All.Select(t => (t.Direction, t.From, t.Action)).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void Received_and_issued_states_never_mix()
    {
        InstrumentState[] received = [InstrumentState.InCashDesk, InstrumentState.DepositedForCollection, InstrumentState.Cleared, InstrumentState.Bounced, InstrumentState.ReturnedToIssuer, InstrumentState.Endorsed];
        foreach (var t in InstrumentLifecycle.All)
        {
            Assert.Equal(t.Direction == InstrumentDirection.Received, received.Contains(t.From));
            Assert.Equal(t.Direction == InstrumentDirection.Received, received.Contains(t.To));
        }
    }
}
