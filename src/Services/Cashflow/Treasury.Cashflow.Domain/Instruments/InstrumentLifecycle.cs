namespace Treasury.Cashflow.Domain.Instruments;

/// <summary>
/// The single declaration of every allowed state change (doc section 12.3).
/// Each line was taken from the old Access program; the form that performs it is noted.
/// A change to this table is a business decision and must be approved by the domain expert.
/// </summary>
public static class InstrumentLifecycle
{
    public sealed record Transition(
        InstrumentDirection Direction,
        InstrumentState From,
        InstrumentAction Action,
        InstrumentState To,
        Func<InstrumentKind, bool> KindAllowed,
        string LegacySource);

    private static readonly Func<InstrumentKind, bool> AnyKind = _ => true;

    public static IReadOnlyList<Transition> All { get; } =
    [
        // Received
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.DepositForCollection, InstrumentState.DepositedForCollection, k => k.IsCollectable(), "D_Check_Vag_m: 11 -> 12, Checktype 0/2 only"),
        new(InstrumentDirection.Received, InstrumentState.DepositedForCollection, InstrumentAction.RecordCleared, InstrumentState.Cleared, AnyKind, "D_Check_vos_m: 12 -> 13"),
        new(InstrumentDirection.Received, InstrumentState.DepositedForCollection, InstrumentAction.RecordBounced, InstrumentState.Bounced, AnyKind, "D_Check_vos_m / Checkbacked: 12 -> 14"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.ReturnToIssuer, InstrumentState.ReturnedToIssuer, AnyKind, "D_Check_Od_m: 11 -> 15"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.Endorse, InstrumentState.Endorsed, k => k.IsCollectable(), "D_Check_Kh_m: 11 -> 16, Checktype 0/2 only"),

        // Issued
        new(InstrumentDirection.Issued, InstrumentState.BeingIssued, InstrumentAction.Issue, InstrumentState.Issued, AnyKind, "Check_Print_single: 20 -> 21"),
        new(InstrumentDirection.Issued, InstrumentState.Issued, InstrumentAction.Deliver, InstrumentState.Delivered, AnyKind, "Check_deliv_m: 21 -> 22"),
        new(InstrumentDirection.Issued, InstrumentState.BeingIssued, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Check_Print_single: 20 -> 26"),
        new(InstrumentDirection.Issued, InstrumentState.Issued, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Check_Print_single: 21 -> 26"),

        // Issued: a post-dated cheque, instalment or note reaching maturity is reclassified (no state change). Old: Ctocash, D3, D5.
        new(InstrumentDirection.Issued, InstrumentState.Issued, InstrumentAction.RecordMatured, InstrumentState.Issued, k => k != InstrumentKind.OrdinaryCheque, "Check_longtocash_m: Ctocash = True"),
        new(InstrumentDirection.Issued, InstrumentState.Delivered, InstrumentAction.RecordMatured, InstrumentState.Delivered, k => k != InstrumentKind.OrdinaryCheque, "Check_longtocash_m: Ctocash = True"),

        // Issued: the payee presented the cheque and our bank paid it or returned it. Old: Vos / Backed + RegVos, D4.
        new(InstrumentDirection.Issued, InstrumentState.Issued, InstrumentAction.RecordPaidByBank, InstrumentState.PaidByBank, AnyKind, "Check_p_vos_m: Vos + RegVos"),
        new(InstrumentDirection.Issued, InstrumentState.Delivered, InstrumentAction.RecordPaidByBank, InstrumentState.PaidByBank, AnyKind, "Check_p_vos_m: Vos + RegVos"),
        new(InstrumentDirection.Issued, InstrumentState.Issued, InstrumentAction.RecordBouncedAtBank, InstrumentState.BouncedAtBank, AnyKind, "Check_p_vos_m: Backed + RegVos"),
        new(InstrumentDirection.Issued, InstrumentState.Delivered, InstrumentAction.RecordBouncedAtBank, InstrumentState.BouncedAtBank, AnyKind, "Check_p_vos_m: Backed + RegVos"),
    ];

    public static Transition? Find(InstrumentDirection direction, InstrumentState from, InstrumentAction action) =>
        All.FirstOrDefault(t => t.Direction == direction && t.From == from && t.Action == action);

    public static IReadOnlyList<InstrumentAction> AllowedActions(InstrumentDirection direction, InstrumentState from, InstrumentKind kind, bool matured = false) =>
        All.Where(t => t.Direction == direction && t.From == from && t.KindAllowed(kind) && !(matured && t.Action == InstrumentAction.RecordMatured))
           .Select(t => t.Action)
           .ToList();

    public static bool IsFinal(InstrumentState state) =>
        state is InstrumentState.Cleared or InstrumentState.Bounced or InstrumentState.ReturnedToIssuer
              or InstrumentState.Endorsed or InstrumentState.Cancelled or InstrumentState.PaidByBank or InstrumentState.BouncedAtBank;
}
