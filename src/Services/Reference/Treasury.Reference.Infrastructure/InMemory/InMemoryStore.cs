using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Infrastructure.InMemory;

/// <summary>Stored form of an entry.</summary>
public sealed record CatalogueSnapshot(Guid Id, CatalogueKind Kind, Guid? ParentId, string Name, string NameKey, string? Code, bool IsActive, long Version, bool IsInUse)
{
    public static CatalogueSnapshot From(CatalogueItem i, long version) => new(i.Id, i.Kind, i.ParentId, i.Name, i.NameKey, i.Code, i.IsActive, version, i.IsInUse);

    public CatalogueItem ToDomain() => CatalogueItem.Restore(Id, Kind, ParentId, Name, Code, IsActive, Version, IsInUse);
}

/// <summary>
/// In-memory storage for local runs and tests. Like the database where it matters: one transaction at a time,
/// all-or-nothing commit, optimistic version checks, outbox. Not for production.
/// </summary>
public sealed class InMemoryReferenceDatabase : IDisposable
{
    internal readonly object Gate = new();
    internal readonly Dictionary<Guid, CatalogueSnapshot> Items = [];
    internal readonly Dictionary<Guid, PurposeGroupSnapshot> Groups = [];
    internal readonly Dictionary<Guid, PurposeSnapshot> Purposes = [];
    internal readonly Dictionary<string, string> IdempotencyResults = [];
    internal readonly List<OutboxMessage> Outbox = [];
    internal readonly SemaphoreSlim TransactionLock = new(1, 1);
    internal ChartSnapshot? Chart;
    internal DetailsSnapshot? Details;

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
    public Dictionary<Guid, CatalogueSnapshot> Items { get; } = [];

    public HashSet<Guid> DeletedItems { get; } = [];

    public Dictionary<Guid, PurposeGroupSnapshot?> Groups { get; } = []; // null: deleted

    public Dictionary<Guid, PurposeSnapshot?> Purposes { get; } = []; // null: deleted

    public Dictionary<string, string> Idempotency { get; } = [];

    public List<OutboxMessage> Outbox { get; } = [];

    public ChartSnapshot? Chart { get; set; }

    public DetailsSnapshot? Details { get; set; }
}

public sealed class InMemoryUnitOfWork(InMemoryReferenceDatabase db) : IUnitOfWork
{
    private static void Apply<T>(Dictionary<Guid, T> committed, Dictionary<Guid, T?> pending)
        where T : class
    {
        foreach (var (id, s) in pending)
        {
            if (s is null)
            {
                committed.Remove(id);
            }
            else
            {
                committed[id] = s;
            }
        }
    }

    private static readonly AsyncLocal<PendingChanges?> Current = new();

    internal static PendingChanges Pending => Current.Value ?? throw new InvalidOperationException("Database access must run inside IUnitOfWork.ExecuteAsync.");

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
                foreach (var (id, s) in pending.Items)
                {
                    db.Items[id] = s;
                }

                foreach (var id in pending.DeletedItems)
                {
                    db.Items.Remove(id);
                }

                Apply(db.Groups, pending.Groups);
                Apply(db.Purposes, pending.Purposes);

                foreach (var (key, value) in pending.Idempotency)
                {
                    db.IdempotencyResults[key] = value;
                }

                db.Outbox.AddRange(pending.Outbox);
                db.Chart = pending.Chart ?? db.Chart;
                db.Details = pending.Details ?? db.Details;
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

public sealed class InMemoryCatalogueRepository(InMemoryReferenceDatabase db) : ICatalogueRepository
{
    public Task<CatalogueItem?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Visible().FirstOrDefault(s => s.Id == id)?.ToDomain());

    public Task<bool> NameTakenAsync(CatalogueKind kind, Guid? parentId, string nameKey, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Visible().Any(s => s.Kind == kind && s.ParentId == parentId && s.NameKey == nameKey && s.Id != exceptId));

    public Task<bool> CurrencyCodeTakenAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Visible().Any(s => s.Kind == CatalogueKind.Currency && s.Code == code));

    public Task AddAsync(CatalogueItem item, CancellationToken cancellationToken) => Write(item, 1, expected: null);

    public Task SaveAsync(CatalogueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Write(item, item.Version + 1, expected: item.Version);
    }

    public Task DeleteAsync(CatalogueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        var pending = InMemoryUnitOfWork.Pending;
        var stored = Visible().FirstOrDefault(s => s.Id == item.Id);
        if (stored is null || stored.Version != item.Version)
        {
            throw new ConcurrencyConflictException($"Entry {item.Id} was changed by someone else.");
        }

        pending.Items.Remove(item.Id);
        pending.DeletedItems.Add(item.Id);
        pending.Outbox.AddRange(item.PendingEvents.Select(OutboxMessage.From));
        item.MarkSaved(item.Version);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CatalogueItem>> ListAsync(CatalogueKind kind, Guid? parentId, bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CatalogueItem>>(Visible()
            .Where(s => s.Kind == kind && (parentId is null || s.ParentId == parentId) && (includeInactive || s.IsActive))
            .Select(s => s.ToDomain())
            .ToList());

    /// <summary>Committed entries overlaid with this transaction's own changes.</summary>
    private List<CatalogueSnapshot> Visible()
    {
        var pending = InMemoryUnitOfWork.Pending;
        lock (db.Gate)
        {
            var all = new Dictionary<Guid, CatalogueSnapshot>(db.Items);
            foreach (var (id, s) in pending.Items)
            {
                all[id] = s;
            }

            foreach (var id in pending.DeletedItems)
            {
                all.Remove(id);
            }

            return [.. all.Values];
        }
    }

    private Task Write(CatalogueItem item, long newVersion, long? expected)
    {
        ArgumentNullException.ThrowIfNull(item);
        var pending = InMemoryUnitOfWork.Pending;
        if (expected is { } version)
        {
            var stored = Visible().FirstOrDefault(s => s.Id == item.Id);
            if (stored is null || stored.Version != version)
            {
                throw new ConcurrencyConflictException($"Entry {item.Id} was changed by someone else.");
            }
        }

        pending.Items[item.Id] = CatalogueSnapshot.From(item, newVersion);
        pending.Outbox.AddRange(item.PendingEvents.Select(OutboxMessage.From));
        item.MarkSaved(newVersion);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryIdempotencyStore(InMemoryReferenceDatabase db) : IIdempotencyStore
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
