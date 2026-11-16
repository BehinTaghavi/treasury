using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// «ثبت صیادی شد» (owner, 15 Nov 2026): every cheque is recorded in the banks' Sayad system before it goes to the bank (received) or
/// to its beneficiary (issued). The user ticks it; the computer's date and time are kept. Not in the old program (Lcash.mdb is
/// older than the Sayad system).
/// </summary>
public sealed class SayadRegistrationTests
{
    private static readonly CollectionAccount Mellat = new(Guid.NewGuid(), "0202-3572", "IRR", true);

    [Theory]
    [InlineData("1405/08/01")] // چک روز
    [InlineData("1405/11/20")] // چک مدت‌دار
    public void A_received_cheque_current_or_post_dated_is_not_deposited_before_the_tick(string due)
    {
        var cheque = ReceivedCheque(receivedOn: "1405/08/01", due: due);
        Assert.True(cheque.RequiresSayadRegistration);
        Assert.Null(cheque.SayadRegistration);
        Assert.Equal("Instrument.SayadNotRegistered", Assert.Throws<DomainException>(() => cheque.DepositForCollection(Mellat, D("1405/08/05"), User, "F-1")).Code);
        Assert.Equal("Instrument.SayadNotRegistered", Assert.Throws<DomainException>(() => cheque.DepositForCollection("حساب واگذاری", D("1405/08/05"), User)).Code);
        Assert.Equal(InstrumentState.InCashDesk, cheque.State);
        Assert.Single(cheque.History);

        cheque.ConfirmSayadRegistration(TickedAt, "treasurer-1");
        cheque.DepositForCollection(Mellat, D("1405/08/05"), User, "F-1");
        Assert.Equal(InstrumentState.DepositedForCollection, cheque.State);
    }

    [Fact]
    public void Until_the_tick_the_deposit_is_not_offered_but_the_other_steps_are()
    {
        var cheque = ReceivedCheque();
        Assert.Equal([InstrumentAction.ReturnToIssuer, InstrumentAction.Endorse, InstrumentAction.Replace, InstrumentAction.Cancel], cheque.AllowedActions());
        cheque.ConfirmSayadRegistration(TickedAt, User);
        Assert.Equal(InstrumentAction.DepositForCollection, cheque.AllowedActions()[0]);
        Assert.Equal(5, cheque.AllowedActions().Count);
    }

    [Fact]
    public void The_tick_keeps_the_date_and_time_and_the_user_and_is_not_a_step_of_the_cheque()
    {
        var cheque = ReceivedCheque();
        cheque.ConfirmSayadRegistration(TickedAt, "treasurer-1");
        Assert.Equal(new SayadRegistration(TickedAt, "treasurer-1"), cheque.SayadRegistration);
        Assert.Single(cheque.History); // no step, so no voucher
        var raised = Assert.IsType<InstrumentSayadRegistrationChanged>(cheque.PendingEvents[^1]);
        Assert.Equal((cheque.Id, true, TickedAt, "treasurer-1"), (raised.InstrumentId, raised.Registered, raised.At, raised.UserId));
        Assert.Equal("Instrument.SayadAlreadyRegistered", Assert.Throws<DomainException>(() => cheque.ConfirmSayadRegistration(TickedAt.AddMinutes(1), User)).Code);
        Assert.Equal("Instrument.UserRequired", Assert.Throws<DomainException>(() => ReceivedCheque().ConfirmSayadRegistration(TickedAt, " ")).Code);
    }

    [Fact]
    public void A_tick_set_by_mistake_is_removed_only_before_the_cheque_goes_to_the_bank()
    {
        var cheque = ReceivedCheque();
        Assert.Equal("Instrument.SayadNotRegistered", Assert.Throws<DomainException>(() => cheque.WithdrawSayadRegistration(TickedAt, User)).Code);
        cheque.ConfirmSayadRegistration(TickedAt, User);
        cheque.WithdrawSayadRegistration(TickedAt.AddMinutes(2), "treasurer-1");
        Assert.Null(cheque.SayadRegistration);
        var raised = Assert.IsType<InstrumentSayadRegistrationChanged>(cheque.PendingEvents[^1]);
        Assert.Equal((false, "treasurer-1"), (raised.Registered, raised.UserId));
        Assert.Equal("Instrument.SayadNotRegistered", Assert.Throws<DomainException>(() => cheque.DepositForCollection(Mellat, D("1405/08/05"), User)).Code);

        cheque.ConfirmSayadRegistration(TickedAt.AddMinutes(3), User);
        cheque.DepositForCollection(Mellat, D("1405/08/05"), User);
        Assert.False(cheque.CanChangeSayadRegistration);
        Assert.Equal("Instrument.SayadRegistrationClosed", Assert.Throws<DomainException>(() => cheque.WithdrawSayadRegistration(TickedAt.AddMinutes(4), User)).Code);
        Assert.NotNull(cheque.SayadRegistration);
    }

    [Fact]
    public void A_cheque_back_from_the_bank_keeps_its_tick_and_is_deposited_again()
    {
        var cheque = ReceivedCheque().Ticked();
        cheque.DepositForCollection(Mellat, D("1405/08/05"), User);
        cheque.ReturnFromBank(D("1405/08/08"), User, "امضا ناقص");
        Assert.NotNull(cheque.SayadRegistration);
        Assert.Contains(InstrumentAction.DepositForCollection, cheque.AllowedActions());
        cheque.DepositForCollection(Mellat, D("1405/08/09"), User);
        Assert.Equal(InstrumentState.DepositedForCollection, cheque.State);
    }

    [Fact]
    public void A_promissory_note_is_not_in_the_sayad_system()
    {
        var note = ReceivedCheque(InstrumentKind.PromissoryNote);
        Assert.False(note.RequiresSayadRegistration);
        Assert.False(note.CanChangeSayadRegistration);
        Assert.Equal("Instrument.SayadRegistrationOnlyForCheques", Assert.Throws<DomainException>(() => note.ConfirmSayadRegistration(TickedAt, User)).Code);
    }

    [Fact]
    public void An_issued_cheque_is_not_handed_to_its_beneficiary_before_the_tick()
    {
        var cheque = ChequeBeingIssued();
        // No sheet number yet: nothing to record in Sayad.
        Assert.Equal("Instrument.SayadRegistrationClosed", Assert.Throws<DomainException>(() => cheque.ConfirmSayadRegistration(TickedAt, User)).Code);
        cheque.Issue(Guid.NewGuid(), "765432", D("1405/08/10"), User);
        Assert.DoesNotContain(InstrumentAction.Deliver, cheque.AllowedActions());
        Assert.Contains(InstrumentAction.Cancel, cheque.AllowedActions());
        Assert.Equal("Instrument.SayadNotRegistered", Assert.Throws<DomainException>(() => cheque.Deliver(D("1405/08/11"), User, "آقای رضایی")).Code);
        Assert.Equal(InstrumentState.Issued, cheque.State);

        cheque.ConfirmSayadRegistration(TickedAt, User);
        Assert.Contains(InstrumentAction.Deliver, cheque.AllowedActions());
        cheque.Deliver(D("1405/08/11"), User, "آقای رضایی");
        Assert.Equal(InstrumentState.Delivered, cheque.State);
        Assert.Equal("Instrument.SayadRegistrationClosed", Assert.Throws<DomainException>(() => cheque.WithdrawSayadRegistration(TickedAt, User)).Code);
    }

    [Fact]
    public void An_issued_instalment_or_note_is_delivered_without_the_tick()
    {
        var instalment = ChequeBeingIssued(InstrumentKind.Instalment, issue: "1405/08/10", due: "1405/09/10");
        instalment.Issue(Guid.NewGuid(), "12", D("1405/08/10"), User);
        Assert.Contains(InstrumentAction.Deliver, instalment.AllowedActions());
        instalment.Deliver(D("1405/08/11"), User);
        Assert.Equal(InstrumentState.Delivered, instalment.State);
    }

    [Fact]
    public void A_cheque_that_was_in_the_system_before_the_tick_counts_as_recorded_in_sayad_answer_Q39()
    {
        // Schema script 033 and the migration tool write the tick with SayadRegistration.AssumedBy in place of a user.
        var cheque = ReceivedCheque();
        var assumed = new SayadRegistration(TickedAt, SayadRegistration.AssumedBy);
        var restored = Instrument.Restore(cheque.Id, cheque.Direction, cheque.Kind, cheque.Number, cheque.BankName, null, null, null, Money.Rials(50_000_000),
            cheque.IssueDate, cheque.DueDate, cheque.Counterparty, cheque.Purpose, null, cheque.State, cheque.OriginFiscalYear, cheque.History, 1,
            sayadRegistration: assumed);
        Assert.True(restored.SayadRegistration!.IsAssumed);
        Assert.False(new SayadRegistration(TickedAt, User).IsAssumed);
        Assert.Contains(InstrumentAction.DepositForCollection, restored.AllowedActions());
        restored.DepositForCollection(Mellat, D("1405/08/05"), User);
        Assert.Equal(InstrumentState.DepositedForCollection, restored.State);
    }

    [Fact]
    public void Endorsing_does_not_wait_for_the_tick_answer_Q37()
    {
        // Owner, 16 Nov 2026: the tick is required only before the deposit at the bank.
        var cheque = ReceivedCheque();
        cheque.Endorse(new Endorsement("300200", "شرکت پیمانکار ب", Guid.NewGuid(), "خرید مواد", "SY-1", Guid.NewGuid()), D("1405/08/02"), User);
        Assert.Equal(InstrumentState.Endorsed, cheque.State);
        Assert.Null(cheque.SayadRegistration);
    }

    [Fact]
    public void The_tick_is_kept_when_the_cheque_is_read_back_from_storage()
    {
        var cheque = ReceivedCheque().Ticked();
        var restored = Instrument.Restore(cheque.Id, cheque.Direction, cheque.Kind, cheque.Number, cheque.BankName, null, null, null, Money.Rials(50_000_000),
            cheque.IssueDate, cheque.DueDate, cheque.Counterparty, cheque.Purpose, null, cheque.State, cheque.OriginFiscalYear, cheque.History, 2,
            sayadRegistration: cheque.SayadRegistration);
        Assert.Equal(new SayadRegistration(TickedAt, User), restored.SayadRegistration);
        Assert.Empty(restored.PendingEvents);
        restored.DepositForCollection(Mellat, D("1405/08/05"), User);
    }
}
