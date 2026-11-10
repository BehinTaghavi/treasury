using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Application.Abstractions;

/// <summary>
/// Runs one use case in one database transaction: aggregates and their outbox events are saved together, or not at all.
/// Fixes the "several SaveChanges with manual undo" pattern of the MVC version.
/// </summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);
}

public interface IInstrumentRepository
{
    Task<Instrument?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Received instruments with the same bank and number (old duplicate check, rule R1).</summary>
    Task<IReadOnlyList<Guid>> FindReceivedAsync(string bankName, string number, CancellationToken cancellationToken);

    Task AddAsync(Instrument instrument, CancellationToken cancellationToken);

    /// <summary>Saves changes and pending events. Throws <see cref="ConcurrencyConflictException"/> if someone else saved first.</summary>
    Task SaveAsync(Instrument instrument, CancellationToken cancellationToken);
}

public interface IChequeBookRepository
{
    /// <summary>Loads the book locked for update until the transaction ends (SELECT … FOR UPDATE).</summary>
    Task<ChequeBook?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ChequeBook book, CancellationToken cancellationToken);

    Task SaveAsync(ChequeBook book, CancellationToken cancellationToken);
}

/// <summary>Remembers the result of a command by its idempotency key, so a double click or network retry does not repeat it.</summary>
public interface IIdempotencyStore
{
    Task<string?> GetResultAsync(string key, CancellationToken cancellationToken);

    Task SaveResultAsync(string key, string result, CancellationToken cancellationToken);
}

/// <summary>Answered by the Fiscal Periods context: may operations be recorded on this date? (rule R2)</summary>
public interface IFiscalPeriodPolicy
{
    Task<bool> IsOpenAsync(FiscalDate operationDate, CancellationToken cancellationToken);
}

/// <summary>Current balance of one of our bank accounts, in Rial (old: Sum(Bed) - Sum(Bes) over Subs).</summary>
public interface IBankBalanceQuery
{
    Task<decimal> GetBalanceAsync(Guid bankAccountId, CancellationToken cancellationToken);
}

/// <summary>Answered by the Payment Requests context (rule R7).</summary>
public interface IPaymentApprovalQuery
{
    Task<bool> IsApprovedAsync(Guid paymentId, CancellationToken cancellationToken);
}

public interface ICurrentUser
{
    string UserId { get; }
}

public sealed class ConcurrencyConflictException(string message) : Exception(message);

/// <summary>Outcome of a use case: success with a value, or a stable error code that the UI translates.</summary>
public sealed record Result<T>
{
    private Result(T? value, string? errorCode, string? errorMessage, IReadOnlyList<string> warnings)
    {
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Warnings = warnings;
    }

    public T? Value { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    /// <summary>Non-blocking remarks for the user, for example a possible duplicate cheque.</summary>
    public IReadOnlyList<string> Warnings { get; }

    public bool IsSuccess => ErrorCode is null;

    public static Result<T> Ok(T value, params string[] warnings) => new(value, null, null, warnings);

    public static Result<T> Fail(string code, string message) => new(default, code, message, []);

    public static Result<T> From(DomainException exception) => Fail(exception.Code, exception.Message);
}
