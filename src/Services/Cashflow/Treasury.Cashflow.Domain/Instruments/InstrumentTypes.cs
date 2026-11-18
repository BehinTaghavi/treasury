using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.Instruments;

/// <summary>Whether we received the instrument or issued it. Old system: Check.Dp (1 = received, 2 = issued).</summary>
public enum InstrumentDirection
{
    Received = 1,
    Issued = 2,
}

/// <summary>
/// Kind of instrument. Old system: Check.Checktype, whose meaning depends on Dp (see docs/domain/instrument-lifecycle.md).
/// Old Checktype 9 ("cash payment") is not an instrument; it becomes a cash payment in the Disbursements context.
/// </summary>
public enum InstrumentKind
{
    OrdinaryCheque,
    GuaranteeCheque,
    BankCheque,
    PostDatedCheque,
    Instalment,
    PromissoryNote,
    GuaranteeNote,
    Other,
}

/// <summary>Lifecycle states. Old status codes (Check.Stcode) in brackets.</summary>
public enum InstrumentState
{
    // Received side
    InCashDesk,              // [11] نزد صندوق
    DepositedForCollection,  // [12] واگذار جهت وصول
    Cleared,                 // [13] وصول شده
    Bounced,                 // [14] برگشت شده
    ReturnedToIssuer,        // [15] عودت شده
    Endorsed,                // [16] واگذار به سایرین (خرج شده)
    ReturnedFromBank,        // عودت شده از بانک: back in the cash desk, not collected (owner's item 14 (c))
    Replaced,                // تعویض شده: replaced by new cheques of the same payer (owner's item 14 (d))
    SayadRegistered,         // ثبت صیادی شد: still in the cash desk, registered in the banks' Sayad system (owner, 18 Nov 2026)
    InLegal,                 // حقوقی: a bounced cheque handed to the company's lawyer (owner, 18 Nov 2026; old flag GoInlaw)

    // Issued side
    BeingIssued,             // [20] در جریان صدور
    Issued,                  // [21] چک صادره
    Delivered,               // [22] تحویل شده
    Cancelled,               // [26] ابطال شده
    PaidByBank,              // [21/22 + Vos, RegVos] وصول شده (چک پرداختنی)
    BouncedAtBank,           // [21/22 + Backed, RegVos] برگشت شده (چک پرداختنی)
}

/// <summary>Things a user can do to an instrument. Which ones are allowed is decided by <see cref="InstrumentLifecycle"/>.</summary>
public enum InstrumentAction
{
    Receive,
    PrepareIssue,
    DepositForCollection,
    RecordCleared,
    RecordBounced,
    ReturnToIssuer,
    Endorse,
    Issue,
    Deliver,
    Cancel,
    RecordMatured,
    RecordPaidByBank,
    RecordBouncedAtBank,
    ReturnFromBank,
    Replace,
    RecordEndorsementReturned,
    RegisterSayad,
    SendToLegal,
}

/// <summary>
/// Where a received cheque is, read from its latest state (owner, 18 Nov 2026: «آخرین وضعیت»). The accounts of the next step's voucher
/// follow from it.
/// </summary>
public static class ReceivedChequeStatus
{
    /// <summary>«نزد صندوق» or «ثبت صیادی شد»: the cheque is in the cash desk and was never sent to a bank.</summary>
    public static bool IsInCashDesk(this InstrumentState state) => state is InstrumentState.InCashDesk or InstrumentState.SayadRegistered;

    /// <summary>«برگشت چک» or «حقوقی»: the bank issued its bounce notice; the cheque is in «اسناد واخواست شده».</summary>
    public static bool IsProtested(this InstrumentState state) => state is InstrumentState.Bounced or InstrumentState.InLegal;

    /// <summary>
    /// «سررسید چک» (owner, 18 Nov 2026): a post-dated cheque (مدت‌دار, due after the day it was received) that is still in the cash desk
    /// or at the bank has reached its due date. In the cash desk it waits for the deposit at the bank or a replacement; at the bank for
    /// collected, returned by the bank or bounced. Not a step: it follows from the dates and the latest state.
    /// </summary>
    public static bool HasReachedDueDate(InstrumentState state, FiscalDate receivedOn, FiscalDate? dueDate, FiscalDate today) =>
        dueDate is { } due && due > receivedOn && due <= today && (state.IsInCashDesk() || state == InstrumentState.DepositedForCollection);
}

public static class InstrumentKindRules
{
    /// <summary>Old rule R3: only ordinary and bank cheques may be sent to a bank for collection or endorsed to others.</summary>
    public static bool IsCollectable(this InstrumentKind kind) =>
        kind is InstrumentKind.OrdinaryCheque or InstrumentKind.BankCheque;

    /// <summary>Guarantee items are held as security; they have no due date of their own.</summary>
    public static bool IsGuarantee(this InstrumentKind kind) =>
        kind is InstrumentKind.GuaranteeCheque or InstrumentKind.GuaranteeNote;

    /// <summary>Old rule R6: for these kinds the due date may not be earlier than the issue date.</summary>
    public static bool RequiresDueNotBeforeIssue(this InstrumentKind kind) =>
        kind is InstrumentKind.OrdinaryCheque or InstrumentKind.PostDatedCheque;

    /// <summary>Cheques carry a bank, branch, account number and Sayad ID.</summary>
    public static bool IsCheque(this InstrumentKind kind) =>
        kind is InstrumentKind.OrdinaryCheque or InstrumentKind.GuaranteeCheque or InstrumentKind.BankCheque or InstrumentKind.PostDatedCheque;

    /// <summary>Promissory notes (سفته) carry guarantors instead of bank details.</summary>
    public static bool IsNote(this InstrumentKind kind) =>
        kind is InstrumentKind.PromissoryNote or InstrumentKind.GuaranteeNote;

    public static bool IsValidFor(this InstrumentKind kind, InstrumentDirection direction) => direction switch
    {
        InstrumentDirection.Received => kind != InstrumentKind.PostDatedCheque,
        InstrumentDirection.Issued => kind != InstrumentKind.BankCheque,
        _ => false,
    };
}
