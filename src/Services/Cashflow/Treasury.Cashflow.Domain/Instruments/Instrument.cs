using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;

namespace Treasury.Cashflow.Domain.Instruments;

/// <summary>A party on the other side of the instrument (drawer of a received cheque, payee of an issued one).</summary>
public sealed record CounterpartyRef(Guid Id, string DisplayName);

/// <summary>One step in the instrument's life. Replaces the old fixed columns D1–D5 / G1–G5 with a full history.</summary>
public sealed record InstrumentHistoryEntry(
    InstrumentAction Action,
    InstrumentState? From,
    InstrumentState To,
    FiscalDate Date,
    string UserId,
    string? Reference,
    string? Note);

/// <summary>
/// A received or issued cheque, promissory note or similar instrument, with its whole life across fiscal years.
/// Old system: one row of dbo.Checks (kept in the shared Cashnajoo86 database). MVC system: Receipt_Detail + Receipt_Detail_PlanState.
/// </summary>
public sealed class Instrument : AggregateRoot<Guid>
{
    private readonly List<InstrumentHistoryEntry> _history = [];

    private Instrument(
        Guid id,
        InstrumentDirection direction,
        InstrumentKind kind,
        Money amount,
        FiscalDate issueDate,
        FiscalDate? dueDate,
        CounterpartyRef counterparty,
        string purpose,
        string? bankName)
        : base(id)
    {
        Direction = direction;
        Kind = kind;
        Amount = amount;
        IssueDate = issueDate;
        DueDate = dueDate;
        Counterparty = counterparty;
        Purpose = purpose;
        BankName = bankName;
    }

    public InstrumentDirection Direction { get; }

    public InstrumentKind Kind { get; }

    /// <summary>Number printed on the instrument. Set at receipt, or when an issued cheque gets its sheet number.</summary>
    public string? Number { get; private set; }

    /// <summary>Drawer's bank for received cheques.</summary>
    public string? BankName { get; }

    /// <summary>Our bank account for issued cheques.</summary>
    public Guid? BankAccountId { get; private set; }

    public Guid? ChequeBookId { get; private set; }

    /// <summary>Payment (in the Disbursements context) this issued instrument settles.</summary>
    public Guid? PaymentId { get; private set; }

    public Money Amount { get; }

    /// <summary>Date written on the instrument (received side: date received).</summary>
    public FiscalDate IssueDate { get; }

    /// <summary>Maturity (سررسید). Null only for guarantee items.</summary>
    public FiscalDate? DueDate { get; }

    public CounterpartyRef Counterparty { get; }

    /// <summary>بابت — what the instrument is for.</summary>
    public string Purpose { get; }

    /// <summary>Collection account (received cheques sent to a bank) or endorsee (received cheques given to others).</summary>
    public string? Destination { get; private set; }

    public InstrumentState State { get; private set; }

    /// <summary>Issued post-dated items: maturity has been recorded (old: Ctocash).</summary>
    public bool IsMatured { get; private set; }

    /// <summary>Jalali fiscal year the instrument was created in. Later state changes belong to the year they happen in.</summary>
    public int OriginFiscalYear { get; private set; }

    public IReadOnlyList<InstrumentHistoryEntry> History => _history;

    public IReadOnlyList<InstrumentAction> AllowedActions() =>
        InstrumentLifecycle.AllowedActions(Direction, State, Kind, IsMatured);

    /// <summary>A cheque or note received into a cash desk (old Stcode 11, form D_Check; rule R1).</summary>
    public static Instrument Receive(
        Guid id,
        InstrumentKind kind,
        string number,
        string bankName,
        Money amount,
        FiscalDate receivedOn,
        FiscalDate? dueDate,
        CounterpartyRef drawer,
        string purpose,
        string userId)
    {
        Require(kind.IsValidFor(InstrumentDirection.Received), "Instrument.KindNotValidForDirection", $"{kind} cannot be received.");
        Require(!string.IsNullOrWhiteSpace(number), "Instrument.NumberRequired", "Instrument number is required.");
        Require(!string.IsNullOrWhiteSpace(bankName), "Instrument.BankRequired", "Bank is required.");
        ValidateCommon(kind, drawer, purpose, dueDate);

        var instrument = new Instrument(id, InstrumentDirection.Received, kind, amount, receivedOn, dueDate, drawer, purpose.Trim(), bankName.Trim())
        {
            Number = number.Trim(),
            State = InstrumentState.InCashDesk,
            OriginFiscalYear = receivedOn.JalaliYear,
        };
        instrument.Record(InstrumentAction.Receive, null, InstrumentState.InCashDesk, receivedOn, userId, null, null);
        return instrument;
    }

    /// <summary>
    /// An instrument we are about to issue for an approved payment (old Stcode 20). It gets its sheet number in <see cref="Issue"/>.
    /// The caller must have checked that the payment is approved (rule R7).
    /// </summary>
    public static Instrument PrepareIssue(
        Guid id,
        InstrumentKind kind,
        Money amount,
        FiscalDate issueDate,
        FiscalDate? dueDate,
        CounterpartyRef payee,
        string purpose,
        Guid bankAccountId,
        Guid paymentId,
        string userId)
    {
        Require(kind.IsValidFor(InstrumentDirection.Issued), "Instrument.KindNotValidForDirection", $"{kind} cannot be issued.");
        ValidateCommon(kind, payee, purpose, dueDate);
        if (kind.RequiresDueNotBeforeIssue() && dueDate < issueDate)
        {
            throw new DomainException("Instrument.DueBeforeIssue", "Due date cannot be earlier than the issue date."); // rule R6
        }

        var instrument = new Instrument(id, InstrumentDirection.Issued, kind, amount, issueDate, dueDate, payee, purpose.Trim(), null)
        {
            BankAccountId = bankAccountId,
            PaymentId = paymentId,
            State = InstrumentState.BeingIssued,
            OriginFiscalYear = issueDate.JalaliYear,
        };
        instrument.Record(InstrumentAction.PrepareIssue, null, InstrumentState.BeingIssued, issueDate, userId, paymentId.ToString(), null);
        return instrument;
    }

    public void DepositForCollection(string collectionAccount, FiscalDate date, string userId, string? reference = null)
    {
        Require(!string.IsNullOrWhiteSpace(collectionAccount), "Instrument.CollectionAccountRequired", "Collection account is required."); // R3
        Apply(InstrumentAction.DepositForCollection, date, userId, reference, null, () => Destination = collectionAccount.Trim());
    }

    public void RecordCleared(FiscalDate date, string userId, string? reference = null) =>
        Apply(InstrumentAction.RecordCleared, date, userId, reference, null);

    public void RecordBounced(FiscalDate date, string userId, string? reason, string? reference = null) =>
        Apply(InstrumentAction.RecordBounced, date, userId, reference, reason);

    public void ReturnToIssuer(FiscalDate date, string userId, string? note = null) =>
        Apply(InstrumentAction.ReturnToIssuer, date, userId, null, note);

    public void Endorse(string endorsee, FiscalDate date, string userId, string? note = null)
    {
        Require(!string.IsNullOrWhiteSpace(endorsee), "Instrument.EndorseeRequired", "The party receiving the endorsed cheque is required.");
        Apply(InstrumentAction.Endorse, date, userId, null, note, () => Destination = endorsee.Trim());
    }

    /// <summary>Gives the instrument the sheet number reserved from <paramref name="chequeBookId"/> (old Stcode 21).</summary>
    public void Issue(Guid chequeBookId, string sheetNumber, FiscalDate date, string userId)
    {
        Require(!string.IsNullOrWhiteSpace(sheetNumber), "Instrument.NumberRequired", "Cheque number is required.");
        Apply(InstrumentAction.Issue, date, userId, sheetNumber, null, () =>
        {
            ChequeBookId = chequeBookId;
            Number = sheetNumber;
        });
    }

    public void Deliver(FiscalDate date, string userId, string? receivedBy = null) =>
        Apply(InstrumentAction.Deliver, date, userId, null, receivedBy);

    public void Cancel(FiscalDate date, string userId, string reason)
    {
        Require(!string.IsNullOrWhiteSpace(reason), "Instrument.CancelReasonRequired", "A reason is required to cancel.");
        Apply(InstrumentAction.Cancel, date, userId, null, reason);
    }

    /// <summary>A post-dated cheque, instalment or note we issued has reached maturity (old: Ctocash).</summary>
    public void RecordMatured(FiscalDate date, string userId)
    {
        if (IsMatured)
        {
            throw new DomainException("Instrument.AlreadyMatured", "Maturity has already been recorded.");
        }

        Apply(InstrumentAction.RecordMatured, date, userId, null, null, () => IsMatured = true);
    }

    /// <summary>Our bank paid the cheque we issued (old: Vos + RegVos on an issued cheque).</summary>
    public void RecordPaidByBank(FiscalDate date, string userId, string? reference = null) =>
        Apply(InstrumentAction.RecordPaidByBank, date, userId, reference, null);

    /// <summary>Our bank returned the cheque we issued (old: Backed + RegVos on an issued cheque).</summary>
    public void RecordBouncedAtBank(FiscalDate date, string userId, string? reason = null) =>
        Apply(InstrumentAction.RecordBouncedAtBank, date, userId, null, reason);

    /// <summary>Rebuilds an instrument from storage or from migrated data. Does not raise events.</summary>
    public static Instrument Restore(
        Guid id,
        InstrumentDirection direction,
        InstrumentKind kind,
        string? number,
        string? bankName,
        Guid? bankAccountId,
        Guid? chequeBookId,
        Guid? paymentId,
        Money amount,
        FiscalDate issueDate,
        FiscalDate? dueDate,
        CounterpartyRef counterparty,
        string purpose,
        string? destination,
        InstrumentState state,
        int originFiscalYear,
        IEnumerable<InstrumentHistoryEntry> history,
        long version)
    {
        var historyList = history.ToList();
        var instrument = new Instrument(id, direction, kind, amount, issueDate, dueDate, counterparty, purpose, bankName)
        {
            Number = number,
            BankAccountId = bankAccountId,
            ChequeBookId = chequeBookId,
            PaymentId = paymentId,
            Destination = destination,
            State = state,
            OriginFiscalYear = originFiscalYear,
            IsMatured = historyList.Any(h => h.Action == InstrumentAction.RecordMatured),
        };
        instrument._history.AddRange(historyList);
        instrument.SetLoadedVersion(version);
        return instrument;
    }

    private void Apply(InstrumentAction action, FiscalDate date, string userId, string? reference, string? note, Action? changeDetails = null)
    {
        Require(!string.IsNullOrWhiteSpace(userId), "Instrument.UserRequired", "User is required.");
        var transition = InstrumentLifecycle.Find(Direction, State, action)
            ?? throw new DomainException(
                "Instrument.InvalidTransition",
                $"'{action}' is not allowed for a {Direction.ToString().ToLowerInvariant()} instrument in state {State}."); // R4 "order of operations is wrong"

        if (!transition.KindAllowed(Kind))
        {
            throw new DomainException("Instrument.KindNotAllowedForAction", $"'{action}' is not allowed for {Kind}."); // R3
        }

        var lastDate = _history[^1].Date;
        if (date < lastDate)
        {
            throw new DomainException(
                "Instrument.DateBeforePreviousStep",
                $"Date {date} is earlier than the previous step ({lastDate})."); // N1 (new rule, to be confirmed)
        }

        var from = State;
        changeDetails?.Invoke();
        State = transition.To;
        Record(action, from, transition.To, date, userId, reference, note);
    }

    private void Record(InstrumentAction action, InstrumentState? from, InstrumentState to, FiscalDate date, string userId, string? reference, string? note)
    {
        Require(!string.IsNullOrWhiteSpace(userId), "Instrument.UserRequired", "User is required.");
        _history.Add(new InstrumentHistoryEntry(action, from, to, date, userId, reference, note));
        Raise(new InstrumentStateChanged(Id, Direction, Kind, action, from, to, date, Amount, Counterparty.Id, Number, Destination, userId));
    }

    private static void ValidateCommon(InstrumentKind kind, CounterpartyRef counterparty, string purpose, FiscalDate? dueDate)
    {
        ArgumentNullException.ThrowIfNull(counterparty);
        Require(!string.IsNullOrWhiteSpace(counterparty.DisplayName), "Instrument.CounterpartyRequired", "Drawer or payee is required.");
        Require(!string.IsNullOrWhiteSpace(purpose), "Instrument.PurposeRequired", "Purpose (بابت) is required.");
        Require(kind.IsGuarantee() || dueDate is not null, "Instrument.DueDateRequired", "Due date is required.");
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

/// <summary>
/// Raised for every step of an instrument's life. The Accounting Integration context turns these into vouchers using posting rules;
/// the instrument itself knows nothing about accounts.
/// </summary>
public sealed record InstrumentStateChanged(
    Guid InstrumentId,
    InstrumentDirection Direction,
    InstrumentKind Kind,
    InstrumentAction Action,
    InstrumentState? From,
    InstrumentState To,
    FiscalDate Date,
    Money Amount,
    Guid CounterpartyId,
    string? Number,
    string? Destination,
    string UserId) : DomainEvent;
