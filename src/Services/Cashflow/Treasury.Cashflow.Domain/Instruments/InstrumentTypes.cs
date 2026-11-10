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
    Endorsed,                // [16] واگذار به سایرین

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

    public static bool IsValidFor(this InstrumentKind kind, InstrumentDirection direction) => direction switch
    {
        InstrumentDirection.Received => kind != InstrumentKind.PostDatedCheque,
        InstrumentDirection.Issued => kind != InstrumentKind.BankCheque,
        _ => false,
    };
}
