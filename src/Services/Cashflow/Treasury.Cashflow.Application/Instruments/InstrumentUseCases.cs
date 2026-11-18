using System.Globalization;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Application.Instruments;

public sealed record MoneyInput(decimal Amount, string Currency = Money.Rial, decimal? RateToRial = null)
{
    public Money ToMoney() => string.Equals(Currency.Trim(), Money.Rial, StringComparison.OrdinalIgnoreCase)
        ? Money.Rials(Amount)
        : Money.Foreign(Amount, Currency, RateToRial ?? throw new DomainException("Money.RateRequired", "Exchange rate is required for foreign currency."));
}

/// <summary>Dates are entered as Jalali text ("1405/08/18"); they are converted to real dates at the boundary.</summary>
public sealed record ReceiveInstrumentCommand(
    string IdempotencyKey,
    InstrumentKind Kind,
    string Number,
    string BankName,
    MoneyInput Amount,
    string ReceivedOn,
    string? DueDate,
    Guid DrawerId,
    string DrawerName,
    string Purpose);

public sealed record ChangeInstrumentStateCommand(
    string IdempotencyKey,
    Guid InstrumentId,
    InstrumentAction Action,
    string Date,
    string? Destination = null,
    string? Note = null,
    string? Reference = null,
    string? DocumentDate = null,
    string? TrackingCode = null);


public sealed record IssueChequeCommand(
    string IdempotencyKey,
    Guid PaymentId,
    Guid BankAccountId,
    Guid ChequeBookId,
    long? SheetNumber,
    InstrumentKind Kind,
    MoneyInput Amount,
    string IssueDate,
    string? DueDate,
    Guid PayeeId,
    string PayeeName,
    string Purpose);

/// <param name="SlipNumber">Deposit slip number («شماره فیش واگذاری»).</param>
public sealed record DepositChequeCommand(string IdempotencyKey, Guid InstrumentId, Guid BankAccountId, string Date, string? SlipNumber = null);

public sealed record InstrumentHistoryView(string Action, string? From, string To, string Date, string UserId, string? Reference, string? Note, StepDetailsView? Details = null);

/// <summary>What a step recorded besides its date, reference and note (owner's item 14).</summary>
public sealed record StepDetailsView(
    Guid? AccountId, string? PartyDetailCode, string? PartyName, string? DocumentDate, Guid? PurposeGroupId, string? Purpose, string? SayadTracking, Guid? ReceiptId,
    Guid? PaymentRequestId = null, string? TrackingCode = null)
{
    public static StepDetailsView? From(StepDetails? d) => d is null ? null
        : new(d.AccountId, d.PartyDetailCode, d.PartyName, d.DocumentDate?.ToJalaliString(), d.PurposeGroupId, d.Purpose, d.SayadTracking, d.ReceiptId, d.PaymentRequestId,
            d.TrackingCode);
}

public sealed record InstrumentView(
    Guid Id,
    string Direction,
    string Kind,
    string? Number,
    string? BankName,
    decimal Amount,
    string Currency,
    decimal AmountInRial,
    string IssueDate,
    string? DueDate,
    string Counterparty,
    string Purpose,
    string? Destination,
    string State,
    int OriginFiscalYear,
    IReadOnlyList<string> AllowedActions,
    IReadOnlyList<InstrumentHistoryView> History,
    long Version,
    decimal? RateToRial = null,
    Guid? ReceiptId = null,
    bool IsPostDated = false,
    ReceivedInstrumentDetails? Details = null,
    Guid? CollectionAccountId = null,
    string? CounterpartyDetailCode = null,
    bool ReachedDueDate = false)
{
    /// <param name="today">Today's date, to mark «سررسید چک» (owner, 18 Nov 2026); without it the mark stays off.</param>
    public static InstrumentView From(Instrument i, FiscalDate? today = null) => new(
        i.Id,
        i.Direction.ToString(),
        i.Kind.ToString(),
        i.Number,
        i.BankName,
        i.Amount.Amount,
        i.Amount.Currency,
        i.Amount.ToRial(),
        i.IssueDate.ToJalaliString(),
        i.DueDate?.ToJalaliString(),
        i.Counterparty.DisplayName,
        i.Purpose,
        i.Destination,
        i.State.ToString(),
        i.OriginFiscalYear,
        i.AllowedActions().Select(a => a.ToString()).ToList(),
        i.History.Select(h => new InstrumentHistoryView(h.Action.ToString(), h.From?.ToString(), h.To.ToString(), h.Date.ToJalaliString(), h.UserId, h.Reference, h.Note,
            StepDetailsView.From(h.Details))).ToList(),
        i.Version,
        i.Amount.RateToRial,
        i.ReceiptId,
        i.IsPostDated,
        i.Direction == InstrumentDirection.Received ? i.Details : null,
        i.CollectionAccountId,
        i.Counterparty.DetailCode,
        today is { } t && i.HasReachedDueDate(t));
}

public sealed record IssuedChequeView(Guid InstrumentId, string SheetNumber, long RemainingSheets, bool ChequeBookStillActive);

/// <summary>Use cases of the Negotiable Instruments context. Each public method is one command in one transaction.</summary>
public sealed class InstrumentUseCases(
    IUnitOfWork unitOfWork,
    IInstrumentRepository instruments,
    IChequeBookRepository chequeBooks,
    IIdempotencyStore idempotency,
    IFiscalPeriodPolicy fiscalPeriods,
    IBankBalanceQuery bankBalances,
    IPaymentApprovalQuery paymentApprovals,
    ICurrentUser currentUser,
    IBankAccountLookup bankAccountLookup,
    TimeProvider? clock = null)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public const string PossibleDuplicateWarning = "Instrument.PossibleDuplicate";

    public Task<Result<Guid>> ReceiveAsync(ReceiveInstrumentCommand command, CancellationToken cancellationToken) =>
        _runner.RunAsync(command.IdempotencyKey, async ct =>
        {
            var receivedOn = FiscalDate.ParseJalali(command.ReceivedOn);
            await EnsureOpenAsync(receivedOn, ct).ConfigureAwait(false);

            // R1: the old program's duplicate check (bank + number) is commented out in Form_D_Check,
            // so a duplicate is only a warning here until the domain expert decides (open question Q1).
            var duplicates = await instruments.FindReceivedAsync(command.BankName.Trim(), command.Number.Trim(), ct).ConfigureAwait(false);

            var instrument = Instrument.Receive(
                Guid.NewGuid(),
                command.Kind,
                command.Number,
                command.BankName,
                command.Amount.ToMoney(),
                receivedOn,
                ParseOptionalDate(command.DueDate),
                new CounterpartyRef(command.DrawerId, command.DrawerName),
                command.Purpose,
                currentUser.UserId);
            await instruments.AddAsync(instrument, ct).ConfigureAwait(false);

            return duplicates.Count > 0
                ? Result<Guid>.Ok(instrument.Id, PossibleDuplicateWarning)
                : Result<Guid>.Ok(instrument.Id);
        }, id => id.ToString(), Guid.Parse, cancellationToken);

    public Task<Result<Guid>> ChangeStateAsync(ChangeInstrumentStateCommand command, CancellationToken cancellationToken) =>
        _runner.RunAsync(command.IdempotencyKey, async ct =>
        {
            var instrument = await instruments.GetAsync(command.InstrumentId, ct).ConfigureAwait(false);
            if (instrument is null)
            {
                return Result<Guid>.Fail("Instrument.NotFound", $"Instrument {command.InstrumentId} was not found.");
            }

            var date = FiscalDate.ParseJalali(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            var user = currentUser.UserId;

            switch (command.Action)
            {
                case InstrumentAction.DepositForCollection:
                    instrument.DepositForCollection(command.Destination ?? string.Empty, date, user, command.Reference);
                    break;
                case InstrumentAction.RecordCleared:
                    instrument.RecordCleared(date, user, command.Reference);
                    break;
                case InstrumentAction.RecordBounced:
                    instrument.RecordBounced(date, user, command.Note, command.Reference, command.TrackingCode);
                    break;
                case InstrumentAction.ReturnToIssuer:
                    instrument.ReturnToIssuer(date, user, command.Note);
                    break;
                case InstrumentAction.ReturnFromBank:
                    instrument.ReturnFromBank(date, user, command.Note);
                    break;
                case InstrumentAction.RegisterSayad:
                    // Owner, 18 Nov 2026: «ثبت صیادی شد»; the reference is the tracking number of the registration, when typed.
                    instrument.RegisterSayad(date, user, command.Reference, command.Note);
                    break;
                case InstrumentAction.SendToLegal:
                    // Owner, 18 Nov 2026: «اقدام حقوقی», recorded on its own screen; the note is the description of the action, the
                    // reference, document date and tracking code are those of the certificate of non-payment (گواهی عدم پرداخت).
                    instrument.SendToLegal(date, user, command.Note, new NonPaymentCertificate(
                        command.Reference, string.IsNullOrWhiteSpace(command.DocumentDate) ? null : FiscalDate.ParseJalali(command.DocumentDate), command.TrackingCode));
                    break;
                case InstrumentAction.ReturnFromLegal:
                    // Owner's answer to Q38 (2): the cheque became collectable and goes back to the cash desk.
                    instrument.ReturnFromLegal(date, user, command.Note);
                    break;
                case InstrumentAction.Deliver:
                    instrument.Deliver(date, user, command.Note);
                    break;
                case InstrumentAction.Cancel when instrument.Direction == InstrumentDirection.Received:
                    // Owner's item 14 (f), answer to Q30: the treasurer types the number and date of management's order.
                    instrument.CancelReceived(date, user, command.Reference, string.IsNullOrWhiteSpace(command.DocumentDate) ? null : FiscalDate.ParseJalali(command.DocumentDate),
                        command.Note);
                    break;
                case InstrumentAction.Cancel:
                    instrument.Cancel(date, user, command.Note ?? string.Empty);
                    break;
                case InstrumentAction.RecordMatured:
                    instrument.RecordMatured(date, user);
                    break;
                case InstrumentAction.RecordPaidByBank:
                    instrument.RecordPaidByBank(date, user, command.Reference);
                    break;
                case InstrumentAction.RecordBouncedAtBank:
                    instrument.RecordBouncedAtBank(date, user, command.Note);
                    break;
                case InstrumentAction.Receive:
                case InstrumentAction.PrepareIssue:
                case InstrumentAction.Issue:
                case InstrumentAction.Endorse:
                case InstrumentAction.Replace:
                case InstrumentAction.RecordEndorsementReturned:
                default:
                    return Result<Guid>.Fail("Instrument.ActionNotSupportedHere", $"Use the dedicated command for '{command.Action}'.");
            }

            await instruments.SaveAsync(instrument, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(instrument.Id);
        }, id => id.ToString(), Guid.Parse, cancellationToken);

    /// <summary>
    /// Issues a cheque for an approved payment: checks approval (R7), the posting window (R2), the bank balance (R8),
    /// reserves the sheet (R5) and records the instrument — all in one transaction, with the cheque book locked.
    /// </summary>
    public Task<Result<IssuedChequeView>> IssueChequeAsync(IssueChequeCommand command, CancellationToken cancellationToken) =>
        _runner.RunAsync(command.IdempotencyKey, async ct =>
        {
            if (!await paymentApprovals.IsApprovedAsync(command.PaymentId, ct).ConfigureAwait(false))
            {
                return Result<IssuedChequeView>.Fail("Payment.NotApproved", "The payment has not been approved."); // old: "پرداخت تایید نشده است"
            }

            var issueDate = FiscalDate.ParseJalali(command.IssueDate);
            await EnsureOpenAsync(issueDate, ct).ConfigureAwait(false);
            var amount = command.Amount.ToMoney();

            // R8: the old program blocks an ordinary (current) cheque that would make the account balance negative.
            if (command.Kind == InstrumentKind.OrdinaryCheque)
            {
                var balance = await bankBalances.GetBalanceAsync(command.BankAccountId, ct).ConfigureAwait(false);
                if (amount.ToRial() > balance)
                {
                    return Result<IssuedChequeView>.Fail("Payment.InsufficientBalance", "The bank account balance would become negative."); // "موجودي منفي ميشود"
                }
            }

            var book = await chequeBooks.GetForUpdateAsync(command.ChequeBookId, ct).ConfigureAwait(false);
            if (book is null)
            {
                return Result<IssuedChequeView>.Fail("ChequeBook.NotFound", "Cheque book was not found.");
            }

            if (book.BankAccountId != command.BankAccountId)
            {
                return Result<IssuedChequeView>.Fail("ChequeBook.WrongAccount", "The cheque book does not belong to this bank account.");
            }

            long sheet;
            if (command.SheetNumber is { } requested)
            {
                book.Reserve(requested);
                sheet = requested;
            }
            else
            {
                sheet = book.ReserveNext();
            }

            var instrument = Instrument.PrepareIssue(
                Guid.NewGuid(),
                command.Kind,
                amount,
                issueDate,
                ParseOptionalDate(command.DueDate),
                new CounterpartyRef(command.PayeeId, command.PayeeName),
                command.Purpose,
                command.BankAccountId,
                command.PaymentId,
                currentUser.UserId);
            // The cheque number keeps the leading zeros of the printed serial (owner, 12 Nov 2026).
            instrument.Issue(book.Id, book.SerialText(sheet), issueDate, currentUser.UserId);

            await chequeBooks.SaveAsync(book, ct).ConfigureAwait(false);
            await instruments.AddAsync(instrument, ct).ConfigureAwait(false);
            return Result<IssuedChequeView>.Ok(new IssuedChequeView(instrument.Id, instrument.Number!, book.RemainingSheets, book.IsActive));
        },
        view => string.Join('|', view.InstrumentId, view.SheetNumber, view.RemainingSheets, view.ChequeBookStillActive),
        text =>
        {
            var p = text.Split('|');
            return new IssuedChequeView(Guid.Parse(p[0]), p[1], long.Parse(p[2], CultureInfo.InvariantCulture), bool.Parse(p[3]));
        },
        cancellationToken);

    /// <summary>Reads run in a unit of work too: the PostgreSQL adapters need its connection (found by the adapter integration tests).</summary>
    /// <summary>
    /// Deposits a received cheque at one of our bank accounts for collection (owner's report no. 4, item 9). The account is checked
    /// with accounts-svc and marked in use; the deposit may be before the due date (rule N2 only stops clearing early).
    /// </summary>
    public Task<Result<Guid>> DepositForCollectionAsync(DepositChequeCommand command, CancellationToken cancellationToken) =>
        _runner.RunAsync(command.IdempotencyKey, async ct =>
        {
            var instrument = await instruments.GetAsync(command.InstrumentId, ct).ConfigureAwait(false);
            if (instrument is null)
            {
                return Result<Guid>.Fail("Instrument.NotFound", $"Instrument {command.InstrumentId} was not found.");
            }

            var date = FiscalDate.ParseJalali(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            var account = await bankAccountLookup.GetAsync(command.BankAccountId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Instrument.CollectionAccountNotFound", "The bank account is not one of the company's bank accounts.");
            instrument.DepositForCollection(new CollectionAccount(account.Id, account.AccountNumber, account.CurrencyCode, account.IsActive), date, currentUser.UserId, command.SlipNumber);
            await bankAccountLookup.MarkInUseAsync(account.Id, ct).ConfigureAwait(false);
            await instruments.SaveAsync(instrument, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(instrument.Id);
        }, id => id.ToString(), Guid.Parse, cancellationToken);

    /// <summary>Received cheques in a state, by due date: in the cash desk (to deposit) or at the bank (to record collected or bounced).</summary>
    public Task<IReadOnlyList<InstrumentView>> ListReceivedAsync(InstrumentState state, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<InstrumentView>>(async ct =>
            (await instruments.ListReceivedAsync(state, ct).ConfigureAwait(false)).Select(i => InstrumentView.From(i, IranClock.Today(clock))).ToList(), cancellationToken);

    public Task<InstrumentView?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var instrument = await instruments.GetAsync(id, ct).ConfigureAwait(false);
            return instrument is null ? null : InstrumentView.From(instrument, IranClock.Today(clock));
        }, cancellationToken);

    private async Task EnsureOpenAsync(FiscalDate date, CancellationToken ct)
    {
        if (!await fiscalPeriods.IsOpenAsync(date, ct).ConfigureAwait(false))
        {
            throw new DomainException("FiscalPeriod.Closed", $"The fiscal period of {date} is closed for new operations.");
        }
    }

    private static FiscalDate? ParseOptionalDate(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : FiscalDate.ParseJalali(text);
}
