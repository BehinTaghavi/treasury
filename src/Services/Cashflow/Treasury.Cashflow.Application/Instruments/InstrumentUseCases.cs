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
    string? Reference = null);


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

public sealed record InstrumentHistoryView(string Action, string? From, string To, string Date, string UserId, string? Reference, string? Note);

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
    long Version)
{
    public static InstrumentView From(Instrument i) => new(
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
        i.History.Select(h => new InstrumentHistoryView(h.Action.ToString(), h.From?.ToString(), h.To.ToString(), h.Date.ToJalaliString(), h.UserId, h.Reference, h.Note)).ToList(),
        i.Version);
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
    ICurrentUser currentUser)
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
                    instrument.RecordBounced(date, user, command.Note, command.Reference);
                    break;
                case InstrumentAction.ReturnToIssuer:
                    instrument.ReturnToIssuer(date, user, command.Note);
                    break;
                case InstrumentAction.Endorse:
                    instrument.Endorse(command.Destination ?? string.Empty, date, user, command.Note);
                    break;
                case InstrumentAction.Deliver:
                    instrument.Deliver(date, user, command.Note);
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
            instrument.Issue(book.Id, sheet.ToString(CultureInfo.InvariantCulture), issueDate, currentUser.UserId);

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
    public Task<InstrumentView?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var instrument = await instruments.GetAsync(id, ct).ConfigureAwait(false);
            return instrument is null ? null : InstrumentView.From(instrument);
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
