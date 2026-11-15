using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Application.Abstractions;

/// <summary>Runs one use case in one database transaction: the entry and its outbox event are saved together, or not at all.</summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);
}

public interface ICatalogueRepository
{
    Task<CatalogueItem?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Is <paramref name="nameKey"/> (see PersianText.Key) already used by another entry of the same list and parent?</summary>
    Task<bool> NameTakenAsync(CatalogueKind kind, Guid? parentId, string nameKey, Guid? exceptId, CancellationToken cancellationToken);

    Task<bool> CurrencyCodeTakenAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(CatalogueItem item, CancellationToken cancellationToken);

    /// <summary>Saves changes and pending events. Throws <see cref="ConcurrencyConflictException"/> if someone else saved first.</summary>
    Task SaveAsync(CatalogueItem item, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogueItem>> ListAsync(CatalogueKind kind, Guid? parentId, bool includeInactive, CancellationToken cancellationToken);
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
