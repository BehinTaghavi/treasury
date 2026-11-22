using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// «ثبت صیادی شد» on issued cheques (owner, 15 Nov 2026): every cheque is recorded in the banks' Sayad system before it goes to its
/// beneficiary. The user ticks it; the computer's date and time are kept. Not in the old program (Lcash.mdb is older than the Sayad
/// system). For a received cheque the same requirement is the step «ثبت صیادی» with its own status (owner, 18 Nov 2026, rule N3:
/// ChequeStatusTests), so the tick is not for received cheques.
/// </summary>
public sealed class SayadRegistrationTests
{
    private static readonly CollectionAccount Mellat = new(Guid.NewGuid(), "0202-3572", "IRR", true);

    private static Instrument IssuedCheque(InstrumentKind kind = InstrumentKind.OrdinaryCheque, string due = "1405/08/10")
    {
        var cheque = ChequeBeingIssued(kind, issue: "1405/08/10", due: due);
        cheque.Issue(Guid.NewGuid(), "765432", D("1405/08/10"), User);
        return cheque;
    }

    [Theory]
    [InlineData(InstrumentKind.OrdinaryCheque, "1405/08/10")] // چک روز
    [InlineData(InstrumentKind.PostDatedCheque, "1405/11/20")] // چک مدت‌دار
    public void An_issued_cheque_current_or_post_dated_is_not_handed_to_its_beneficiary_before_the_tick(InstrumentKind kind, string due)
    {
        var cheque = ChequeBeingIssued(kind, issue: "1405/08/10", due: due);
        // No sheet number yet: nothing to record in Sayad.
        Assert.Equal("Instrument.SayadRegistrationClosed", Assert.Throws<DomainException>(() => cheque.ConfirmSayadRegistration(TickedAt, User)).Code);
        cheque.Issue(Guid.NewGuid(), "765432", D("1405/08/10"), User);
        Assert.True(cheque.RequiresSayadRegistration);
        Assert.Null(cheque.SayadRegistration);
        Assert.DoesNotContain(InstrumentAction.Deliver, cheque.AllowedActions());
        Assert.Contains(InstrumentAction.Cancel, cheque.AllowedActions());
        Assert.Equal("Instrument.SayadNotRegistered", Assert.Throws<DomainException>(() => cheque.Deliver(D("1405/08/11"), User, "آقای رضایی")).Code);
        Assert.Equal(InstrumentState.Issued, cheque.State);

        cheque.ConfirmSayadRegistration(TickedAt, "treasurer-1");
        Assert.Contains(InstrumentAction.Deliver, cheque.AllowedActions());
        cheque.Deliver(D("1405/08/11"), User, "آقای رضایی");
        Assert.Equal(InstrumentState.Delivered, cheque.State);
    }

    [Fact]
    public void The_tick_keeps_the_date_and_time_and_the_user_and_is_not_a_step_of_the_cheque()
    {
        var cheque = IssuedCheque();
        var steps = cheque.History.Count;
        cheque.ConfirmSayadRegistration(TickedAt, "treasurer-1");
        Assert.Equal(new SayadRegistration(TickedAt, "treasurer-1"), cheque.SayadRegistration);
        Assert.Equal(steps, cheque.History.Count); // no step, so no voucher
        var raised = Assert.IsType<InstrumentSayadRegistrationChanged>(cheque.PendingEvents[^1]);
        Assert.Equal((cheque.Id, InstrumentDirection.Issued, true, TickedAt, "treasurer-1"), (raised.InstrumentId, raised.Direction, raised.Registered, raised.At, raised.UserId));
        Assert.Equal("Instrument.SayadAlreadyRegistered", Assert.Throws<DomainException>(() => cheque.ConfirmSayadRegistration(TickedAt.AddMinutes(1), User)).Code);
        Assert.Equal("Instrument.UserRequired", Assert.Throws<DomainException>(() => IssuedCheque().ConfirmSayadRegistration(TickedAt, " ")).Code);
    }

    [Fact]
    public void A_tick_set_by_mistake_is_removed_only_before_the_cheque_goes_to_its_beneficiary_answer_of_16_Nov_2026()
    {
        var cheque = IssuedCheque();
        Assert.Equal("Instrument.SayadNotRegistered", Assert.Throws<DomainException>(() => cheque.WithdrawSayadRegistration(TickedAt, User)).Code);
        cheque.ConfirmSayadRegistration(TickedAt, User);
        cheque.WithdrawSayadRegistration(TickedAt.AddMinutes(2), "treasurer-1");
        Assert.Null(cheque.SayadRegistration);
        var raised = Assert.IsType<InstrumentSayadRegistrationChanged>(cheque.PendingEvents[^1]);
        Assert.Equal((false, "treasurer-1"), (raised.Registered, raised.UserId));
        Assert.Equal("Instrument.SayadNotRegistered", Assert.Throws<DomainException>(() => cheque.Deliver(D("1405/08/11"), User)).Code);

        cheque.ConfirmSayadRegistration(TickedAt.AddMinutes(3), User);
        cheque.Deliver(D("1405/08/11"), User);
        Assert.False(cheque.CanChangeSayadRegistration);
        Assert.Equal("Instrument.SayadRegistrationClosed", Assert.Throws<DomainException>(() => cheque.WithdrawSayadRegistration(TickedAt.AddMinutes(4), User)).Code);
        Assert.NotNull(cheque.SayadRegistration);
    }

    [Fact]
    public void An_issued_instalment_or_note_is_delivered_without_the_tick()
    {
        // Only cheques are in the Sayad system.
        var instalment = IssuedCheque(InstrumentKind.Instalment, due: "1405/09/10");
        Assert.False(instalment.RequiresSayadRegistration);
        Assert.Contains(InstrumentAction.Deliver, instalment.AllowedActions());
        Assert.Equal("Instrument.SayadRegistrationOnlyForCheques", Assert.Throws<DomainException>(() => instalment.ConfirmSayadRegistration(TickedAt, User)).Code);
        instalment.Deliver(D("1405/08/11"), User);
        Assert.Equal(InstrumentState.Delivered, instalment.State);
    }

    [Fact]
    public void A_received_cheque_has_the_step_sabt_sayadi_not_the_tick_N3()
    {
        // Owner, 18 Nov 2026 (rule N3): for a received cheque «ثبت صیادی» is a step with the status «ثبت صیادی شد»; the deposit at the bank
        // waits for that step. The tick of 15 Nov is kept for issued cheques only, so one cheque is never asked for both.
        var cheque = ReceivedCheque();
        Assert.False(cheque.RequiresSayadRegistration);
        Assert.False(cheque.CanChangeSayadRegistration);
        Assert.Equal("Instrument.SayadRegistrationIsAStep", Assert.Throws<DomainException>(() => cheque.ConfirmSayadRegistration(TickedAt, User)).Code);
        Assert.Equal("Instrument.SayadRegistrationIsAStep", Assert.Throws<DomainException>(() => cheque.WithdrawSayadRegistration(TickedAt, User)).Code);
        Assert.Equal("Instrument.SayadRegistrationRequired", Assert.Throws<DomainException>(() => cheque.DepositForCollection(Mellat, D("1405/08/05"), User)).Code);

        cheque.RegisterSayad(D("1405/08/02"), User);
        Assert.Equal(InstrumentState.SayadRegistered, cheque.State);
        Assert.Null(cheque.SayadRegistration);
        cheque.DepositForCollection(Mellat, D("1405/08/05"), User, "F-1");
        Assert.Equal(InstrumentState.DepositedForCollection, cheque.State);

        // A received note is in neither.
        Assert.Equal("Instrument.SayadRegistrationIsAStep",
            Assert.Throws<DomainException>(() => ReceivedCheque(InstrumentKind.PromissoryNote).ConfirmSayadRegistration(TickedAt, User)).Code);
    }

    [Fact]
    public void The_tick_is_kept_when_the_cheque_is_read_back_from_storage()
    {
        var cheque = IssuedCheque().Ticked();
        var restored = Instrument.Restore(cheque.Id, cheque.Direction, cheque.Kind, cheque.Number, cheque.BankName, null, cheque.ChequeBookId, cheque.PaymentId,
            cheque.Amount, cheque.IssueDate, cheque.DueDate, cheque.Counterparty, cheque.Purpose, null, cheque.State, cheque.OriginFiscalYear, cheque.History, 2,
            sayadRegistration: cheque.SayadRegistration);
        Assert.Equal(new SayadRegistration(TickedAt, User), restored.SayadRegistration);
        Assert.Empty(restored.PendingEvents);
        restored.Deliver(D("1405/08/11"), User);
        Assert.Equal(InstrumentState.Delivered, restored.State);
    }
}
