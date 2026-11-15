using Treasury.BuildingBlocks.Domain;

namespace Treasury.BuildingBlocks.Application;

/// <summary>
/// Runs one use case in one database transaction: aggregates and their outbox events are saved together, or not at all.
/// Fixes the "several SaveChanges with manual undo" pattern of the MVC version.
/// </summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);
}

/// <summary>Remembers the result of a command by its idempotency key, so a double click or network retry does not repeat it.</summary>
public interface IIdempotencyStore
{
    Task<string?> GetResultAsync(string key, CancellationToken cancellationToken);

    Task SaveResultAsync(string key, string result, CancellationToken cancellationToken);
}

/// <summary>Someone else saved the same aggregate first (optimistic concurrency); the use case returns "Concurrency.Conflict".</summary>
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
