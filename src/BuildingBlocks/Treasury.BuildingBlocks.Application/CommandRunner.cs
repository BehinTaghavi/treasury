using Treasury.BuildingBlocks.Domain;

namespace Treasury.BuildingBlocks.Application;

/// <summary>
/// Runs a write command: idempotency key, one transaction, rule violations turned into results.
/// A failed command saves nothing, its idempotency record included.
/// </summary>
public sealed class CommandRunner(IUnitOfWork unitOfWork, IIdempotencyStore idempotency)
{
    public async Task<Result<T>> RunAsync<T>(
        string idempotencyKey,
        Func<CancellationToken, Task<Result<T>>> work,
        Func<T, string> serialize,
        Func<string, T> deserialize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        ArgumentNullException.ThrowIfNull(serialize);
        ArgumentNullException.ThrowIfNull(deserialize);
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result<T>.Fail("Request.IdempotencyKeyRequired", "An idempotency key is required for every write.");
        }

        var previous = await idempotency.GetResultAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
        if (previous is not null)
        {
            return Result<T>.Ok(deserialize(previous));
        }

        try
        {
            return await unitOfWork.ExecuteAsync(async ct =>
            {
                var result = await work(ct).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    // Abort the transaction: nothing of a failed command is saved.
                    throw new CommandFailedException<T>(result);
                }

                await idempotency.SaveResultAsync(idempotencyKey, serialize(result.Value!), ct).ConfigureAwait(false);
                return result;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (CommandFailedException<T> failed)
        {
            return failed.Result;
        }
        catch (DomainException ex)
        {
            return Result<T>.From(ex);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Result<T>.Fail("Concurrency.Conflict", ex.Message);
        }
    }

    private sealed class CommandFailedException<T>(Result<T> result) : Exception(result.ErrorMessage)
    {
        public Result<T> Result { get; } = result;
    }
}
