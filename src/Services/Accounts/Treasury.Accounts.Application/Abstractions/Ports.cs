using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Application.Abstractions;

/// <summary>Runs one use case in one database transaction: the aggregate and its outbox event are saved together, or not at all.</summary>
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

public sealed class ConcurrencyConflictException(string message) : Exception(message);

/// <summary>Outcome of a use case: success with a value, or a stable error code that the UI translates.</summary>
public sealed record Result<T>
{
    private Result(T? value, string? errorCode, string? errorMessage)
    {
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public T? Value { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static Result<T> Ok(T value) => new(value, null, null);

    public static Result<T> Fail(string code, string message) => new(default, code, message);

    public static Result<T> From(DomainException exception) => Fail(exception.Code, exception.Message);
}
