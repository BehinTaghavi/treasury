using System.Text.Json;
using System.Text.Json.Serialization;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Requests.Application.Abstractions;
using Treasury.Requests.Domain.PaymentRequests;

namespace Treasury.Requests.Infrastructure.InMemory;

/// <summary>A domain event waiting in the outbox. Saved in the same transaction as the aggregate; published afterwards.</summary>
public sealed record OutboxMessage(Guid Id, string Type, string Payload, DateTimeOffset OccurredAt)
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter(), new FiscalDateJson() } };

    public static OutboxMessage From(IDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new(e.EventId, e.GetType().Name, JsonSerializer.Serialize(e, e.GetType(), Json), e.OccurredAt);
    }

    /// <summary>Dates in events are ISO dates, never Jalali text.</summary>
    private sealed class FiscalDateJson : JsonConverter<FiscalDate>
    {
        public override FiscalDate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(DateOnly.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture));

        public override void Write(Utf8JsonWriter writer, FiscalDate value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
    }
}

/// <summary>Stored form of a payment request (immutable, so a transaction's copy never leaks into committed state).</summary>
public sealed record PaymentRequestSnapshot(
    Guid Id, int Number, int FiscalYear, string RecordedBy, PaymentRequestSnapshotData Data, long Version, IReadOnlyList<RequestAttachment>? Files = null)
{
    public static PaymentRequestSnapshot From(PaymentRequest r, long version) => new(r.Id, r.Number, r.FiscalYear, r.RecordedBy, r.Data(), version, [.. r.Files]);

    public PaymentRequest ToDomain() => PaymentRequest.Restore(Id, Number, FiscalYear, RecordedBy, Data, Version, Files);
}

/// <summary>In-memory storage for local runs and tests: one transaction at a time, all-or-nothing commit, version checks, outbox.</summary>
public sealed class InMemoryRequestsDatabase : IDisposable
{
    internal readonly object Gate = new();
    internal readonly Dictionary<Guid, PaymentRequestSnapshot> Requests = [];
    internal readonly Dictionary<int, int> Numbers = [];
    internal readonly Dictionary<Guid, byte[]> FileContents = [];
    internal readonly Dictionary<string, string> IdempotencyResults = [];
    internal readonly List<OutboxMessage> Outbox = [];
    internal readonly SemaphoreSlim TransactionLock = new(1, 1);

    public IReadOnlyList<OutboxMessage> OutboxMessages
    {
        get
        {
            lock (Gate)
            {
                return Outbox.ToList();
            }
        }
    }

    public void Dispose() => TransactionLock.Dispose();
}

internal sealed class PendingChanges
{
    public Dictionary<Guid, PaymentRequestSnapshot> Requests { get; } = [];

    public Dictionary<int, int> Numbers { get; } = [];

    public Dictionary<Guid, byte[]?> FileContents { get; } = []; // null: removed

    public Dictionary<string, string> Idempotency { get; } = [];

    public List<OutboxMessage> Outbox { get; } = [];
}

public sealed class InMemoryUnitOfWork(InMemoryRequestsDatabase db) : IUnitOfWork
{
    private static readonly AsyncLocal<PendingChanges?> Current = new();

    internal static PendingChanges Pending => Current.Value ?? throw new InvalidOperationException("Database access must run inside IUnitOfWork.ExecuteAsync.");

    internal static PendingChanges? PendingOrNull => Current.Value;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        await db.TransactionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        var pending = new PendingChanges();
        Current.Value = pending;
        try
        {
            var result = await work(cancellationToken).ConfigureAwait(false);
            lock (db.Gate)
            {
                foreach (var (id, s) in pending.Requests)
                {
                    db.Requests[id] = s;
                }

                foreach (var (year, n) in pending.Numbers)
                {
                    db.Numbers[year] = n;
                }

                foreach (var (id, content) in pending.FileContents)
                {
                    if (content is null)
                    {
                        db.FileContents.Remove(id);
                    }
                    else
                    {
                        db.FileContents[id] = content;
                    }
                }

                foreach (var (key, value) in pending.Idempotency)
                {
                    db.IdempotencyResults[key] = value;
                }

                db.Outbox.AddRange(pending.Outbox);
            }

            return result;
        }
        finally
        {
            Current.Value = null;
            db.TransactionLock.Release();
        }
    }
}

public sealed class InMemoryPaymentRequestRepository(InMemoryRequestsDatabase db) : IPaymentRequestRepository
{
    public Task<PaymentRequest?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (InMemoryUnitOfWork.PendingOrNull?.Requests.TryGetValue(id, out var staged) == true)
        {
            return Task.FromResult<PaymentRequest?>(staged.ToDomain());
        }

        lock (db.Gate)
        {
            return Task.FromResult(db.Requests.TryGetValue(id, out var s) ? s.ToDomain() : null);
        }
    }

    public Task<int> NextNumberAsync(int fiscalYear, CancellationToken cancellationToken)
    {
        var pending = InMemoryUnitOfWork.Pending;
        if (!pending.Numbers.TryGetValue(fiscalYear, out var last))
        {
            lock (db.Gate)
            {
                last = db.Numbers.TryGetValue(fiscalYear, out var n) ? n : 0;
            }
        }

        pending.Numbers[fiscalYear] = last + 1;
        return Task.FromResult(last + 1);
    }

    public Task AddAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Stage(request, 1);
        return Task.CompletedTask;
    }

    public Task SaveAsync(PaymentRequest request, CancellationToken cancellationToken, (Guid FileId, byte[] Content)? newFile = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        long stored;
        IReadOnlyList<RequestAttachment> before;
        lock (db.Gate)
        {
            stored = db.Requests.TryGetValue(request.Id, out var s) ? s.Version : 0;
            before = s?.Files ?? [];
        }

        var pending = InMemoryUnitOfWork.Pending;
        foreach (var gone in before.Where(f => request.Files.All(x => x.Id != f.Id)))
        {
            pending.FileContents[gone.Id] = null;
        }

        if (newFile is { } nf)
        {
            pending.FileContents[nf.FileId] = nf.Content;
        }

        if (stored != request.Version)
        {
            throw new ConcurrencyConflictException($"Payment request {request.Id} was changed by someone else.");
        }

        Stage(request, request.Version + 1);
        return Task.CompletedTask;
    }

    public Task<(RequestAttachment File, byte[] Content)?> GetFileAsync(Guid requestId, Guid fileId, CancellationToken cancellationToken)
    {
        lock (db.Gate)
        {
            if (db.Requests.TryGetValue(requestId, out var s) && s.Files?.FirstOrDefault(f => f.Id == fileId) is { } file
                && db.FileContents.TryGetValue(fileId, out var content))
            {
                return Task.FromResult<(RequestAttachment, byte[])?>((file, content));
            }

            return Task.FromResult<(RequestAttachment, byte[])?>(null);
        }
    }

    public Task<IReadOnlyList<PaymentRequest>> ListAsync(int fiscalYear, PaymentRequestState? state, CancellationToken cancellationToken)
    {
        lock (db.Gate)
        {
            IReadOnlyList<PaymentRequest> list = db.Requests.Values
                .Where(s => s.FiscalYear == fiscalYear && (state is null || s.Data.State == state))
                .Select(s => s.ToDomain()).ToList();
            return Task.FromResult(list);
        }
    }

    private static void Stage(PaymentRequest request, long newVersion)
    {
        var pending = InMemoryUnitOfWork.Pending;
        pending.Requests[request.Id] = PaymentRequestSnapshot.From(request, newVersion);
        pending.Outbox.AddRange(request.PendingEvents.Select(OutboxMessage.From));
        request.MarkSaved(newVersion);
    }
}

public sealed class InMemoryIdempotencyStore(InMemoryRequestsDatabase db) : IIdempotencyStore
{
    public Task<string?> GetResultAsync(string key, CancellationToken cancellationToken)
    {
        lock (db.Gate)
        {
            return Task.FromResult(db.IdempotencyResults.TryGetValue(key, out var r) ? r : null);
        }
    }

    public Task SaveResultAsync(string key, string result, CancellationToken cancellationToken)
    {
        InMemoryUnitOfWork.Pending.Idempotency[key] = result;
        return Task.CompletedTask;
    }
}
