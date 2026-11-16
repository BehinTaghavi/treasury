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
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.DepositForCollection, InstrumentState.DepositedForCollection, k => k.IsNegotiable(), "D_Check_Vag_m: 11 -> 12, Checktype 0/2 only; ordinary notes too (owner, 16 Nov 2026)"),
        new(InstrumentDirection.Received, InstrumentState.DepositedForCollection, InstrumentAction.RecordCleared, InstrumentState.Cleared, AnyKind, "D_Check_vos_m: 12 -> 13"),
        new(InstrumentDirection.Received, InstrumentState.DepositedForCollection, InstrumentAction.RecordBounced, InstrumentState.Bounced, AnyKind, "D_Check_vos_m / Checkbacked: 12 -> 14"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.ReturnToIssuer, InstrumentState.ReturnedToIssuer, AnyKind, "D_Check_Od_m: 11 -> 15"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.Endorse, InstrumentState.Endorsed, k => k.IsNegotiable(), "D_Check_Kh_m: 11 -> 16, Checktype 0/2 only; ordinary notes too: خرج سفته (owner, 16 Nov 2026)"),

        // Received: the owner's item 14 (15 Nov 2026, answers Q30–Q35; docs/domain/received-cheque-steps.md). New-version manual
        // «ثبت وصول / برگشت / عودت چک‌های واگذاری» (return to the cash desk), «تعویض چک‌ها», «ابطال»; owner's voucher table.
        new(InstrumentDirection.Received, InstrumentState.DepositedForCollection, InstrumentAction.ReturnFromBank, InstrumentState.ReturnedFromBank, AnyKind, "Owner item 14 (c); manual: عودت به صندوق"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.DepositForCollection, InstrumentState.DepositedForCollection, k => k.IsNegotiable(), "Owner item 14, answer Q33: deposited again"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.RecordBounced, InstrumentState.Bounced, AnyKind, "Owner item 14, answer Q33: bounce notice after the return"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.ReturnToIssuer, InstrumentState.ReturnedToIssuer, AnyKind, "Manual: عودت شده از نزد بانک → عودت"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.Replace, InstrumentState.Replaced, k => !k.IsGuarantee(), "Owner item 14 (d); manual: تعویض چک‌های نزد صندوق"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.Replace, InstrumentState.Replaced, k => !k.IsGuarantee(), "Owner item 14 (d): replacement of a returned cheque"),
        new(InstrumentDirection.Received, InstrumentState.Bounced, InstrumentAction.Replace, InstrumentState.Replaced, k => !k.IsGuarantee(), "Owner item 14 (d)"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Owner item 14 (f), answer Q30; manual: ابطال چک‌های نزد صندوق"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Owner item 14 (f)"),
        new(InstrumentDirection.Received, InstrumentState.Bounced, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Owner item 14 (f)"),
        new(InstrumentDirection.Received, InstrumentState.Endorsed, InstrumentAction.RecordEndorsementReturned, InstrumentState.Bounced, AnyKind, "Owner item 14 (h); voucher table: عودت چک‌های خرج شده (پس از برگشت)"),

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
        state is InstrumentState.Cleared or InstrumentState.ReturnedToIssuer or InstrumentState.Replaced
              or InstrumentState.Cancelled or InstrumentState.PaidByBank or InstrumentState.BouncedAtBank;
}
