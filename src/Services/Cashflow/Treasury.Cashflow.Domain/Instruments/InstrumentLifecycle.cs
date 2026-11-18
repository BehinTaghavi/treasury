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
        // Received. The old program's D_Check_Vag_m sent a cheque from the cash desk straight to the bank (11 -> 12); since the owner's
        // rule of 18 Nov 2026 (N3) a cheque is deposited only after «ثبت صیادی», so that step starts at SayadRegistered (below). Old
        // history is migrated as recorded (tools/migration: LEGACY_TRANSITIONS).
        new(InstrumentDirection.Received, InstrumentState.DepositedForCollection, InstrumentAction.RecordCleared, InstrumentState.Cleared, AnyKind, "D_Check_vos_m: 12 -> 13"),
        new(InstrumentDirection.Received, InstrumentState.DepositedForCollection, InstrumentAction.RecordBounced, InstrumentState.Bounced, AnyKind, "D_Check_vos_m / Checkbacked: 12 -> 14"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.ReturnToIssuer, InstrumentState.ReturnedToIssuer, AnyKind, "D_Check_Od_m: 11 -> 15"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.Endorse, InstrumentState.Endorsed, k => k.IsCollectable(), "D_Check_Kh_m: 11 -> 16, Checktype 0/2 only"),

        // Received: the owner's item 14 (15 Nov 2026, answers Q30–Q35; docs/domain/received-cheque-steps.md). New-version manual
        // «ثبت وصول / برگشت / عودت چک‌های واگذاری» (return to the cash desk), «تعویض چک‌ها», «ابطال»; owner's voucher table.
        new(InstrumentDirection.Received, InstrumentState.DepositedForCollection, InstrumentAction.ReturnFromBank, InstrumentState.ReturnedFromBank, AnyKind, "Owner item 14 (c); manual: عودت به صندوق"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.DepositForCollection, InstrumentState.DepositedForCollection, k => k.IsCollectable(), "Owner item 14, answer Q33: deposited again"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.RecordBounced, InstrumentState.Bounced, AnyKind, "Owner item 14, answer Q33: bounce notice after the return"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.ReturnToIssuer, InstrumentState.ReturnedToIssuer, AnyKind, "Manual: عودت شده از نزد بانک → عودت"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.Replace, InstrumentState.Replaced, k => !k.IsGuarantee(), "Owner item 14 (d); manual: تعویض چک‌های نزد صندوق"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.Replace, InstrumentState.Replaced, k => !k.IsGuarantee(), "Owner item 14 (d): replacement of a returned cheque"),
        new(InstrumentDirection.Received, InstrumentState.Bounced, InstrumentAction.Replace, InstrumentState.Replaced, k => !k.IsGuarantee(), "Owner item 14 (d)"),
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Owner item 14 (f), answer Q30; manual: ابطال چک‌های نزد صندوق"),
        new(InstrumentDirection.Received, InstrumentState.ReturnedFromBank, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Owner item 14 (f)"),
        new(InstrumentDirection.Received, InstrumentState.Bounced, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Owner item 14 (f)"),
        new(InstrumentDirection.Received, InstrumentState.Endorsed, InstrumentAction.RecordEndorsementReturned, InstrumentState.Bounced, AnyKind, "Owner item 14 (h); voucher table: عودت چک‌های خرج شده (پس از برگشت)"),

        // Received: the owner's description of the latest status («آخرین وضعیت») of a received cheque, 18 Nov 2026: «ثبت صیادی شد» (the
        // cheque is registered in the banks' Sayad system; it stays in the cash desk, so every step of «نزد صندوق» stays open) and «اقدام حقوقی»
        // (a bounced cheque handed to the company's lawyer; it can still be replaced). Cancellation "in any state" is item 14 (f).
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.RegisterSayad, InstrumentState.SayadRegistered, k => k.IsCheque(), "Owner, 18 Nov 2026: ثبت صیادی; cheques only (notes have no Sayad ID)"),
        new(InstrumentDirection.Received, InstrumentState.SayadRegistered, InstrumentAction.DepositForCollection, InstrumentState.DepositedForCollection, k => k.IsCollectable(), "Owner, 18 Nov 2026: ثبت صیادی → واگذاری به بانک; D_Check_Vag_m, Checktype 0/2 only"),
        new(InstrumentDirection.Received, InstrumentState.SayadRegistered, InstrumentAction.ReturnToIssuer, InstrumentState.ReturnedToIssuer, AnyKind, "Owner, 18 Nov 2026: عودت از صندوق; D_Check_Od_m"),
        new(InstrumentDirection.Received, InstrumentState.SayadRegistered, InstrumentAction.Endorse, InstrumentState.Endorsed, k => k.IsCollectable(), "Owner item 14 (g) for a cheque in the cash desk; D_Check_Kh_m, Checktype 0/2 only"),
        new(InstrumentDirection.Received, InstrumentState.SayadRegistered, InstrumentAction.Replace, InstrumentState.Replaced, k => !k.IsGuarantee(), "Owner, 18 Nov 2026: تعویض چک نزد صندوق"),
        new(InstrumentDirection.Received, InstrumentState.SayadRegistered, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Owner item 14 (f) for a cheque in the cash desk"),
        new(InstrumentDirection.Received, InstrumentState.Bounced, InstrumentAction.SendToLegal, InstrumentState.InLegal, AnyKind, "Owner, 18 Nov 2026: اقدام حقوقی (تحویل چک برگشتی به وکیل شرکت); old flag GoInlaw"),
        new(InstrumentDirection.Received, InstrumentState.InLegal, InstrumentAction.Replace, InstrumentState.Replaced, k => !k.IsGuarantee(), "Owner, 18 Nov 2026: تعویض چک در وضعیت اقدام حقوقی"),
        new(InstrumentDirection.Received, InstrumentState.InLegal, InstrumentAction.Cancel, InstrumentState.Cancelled, AnyKind, "Owner item 14 (f): cancellation in any state by management's order"),

        // Owner's answer to Q38 (18 Nov 2026): "if, unlikely as it is, legal action is taken on a cheque that is «نزد صندوق»" — then
        // «اسناد واخواست شده حقوقی شده» is debited against «اسناد نزد صندوق». A cheque registered in Sayad is still in the cash desk.
        // Owner, 18 Nov 2026: guarantee cheques and guarantee notes may get legal action too (they are always in the cash desk), and
        // bounced notes (from Bounced, above). An ordinary note that has not bounced gets none.
        new(InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentAction.SendToLegal, InstrumentState.InLegal, k => k.IsCheque() || k.IsGuarantee(), "Owner, answer Q38 and 18 Nov 2026: اقدام حقوقی روی چک نزد صندوق، چک تضمینی و سفته تضمینی"),
        new(InstrumentDirection.Received, InstrumentState.SayadRegistered, InstrumentAction.SendToLegal, InstrumentState.InLegal, k => k.IsCheque() || k.IsGuarantee(), "Owner, answer Q38: اقدام حقوقی روی چک نزد صندوق (ثبت صیادی شده)"),

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
