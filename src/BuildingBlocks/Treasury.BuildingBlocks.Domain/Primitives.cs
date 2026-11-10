namespace Treasury.BuildingBlocks.Domain;

/// <summary>Something that happened in the domain. Published through the transactional outbox.</summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

/// <summary>Base record for domain events.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Base class for aggregate roots: identity, optimistic-concurrency version and pending domain events.
/// One command changes one aggregate and saves it once (see docs/adr/0002).
/// </summary>
public abstract class AggregateRoot<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _pendingEvents = [];

    protected AggregateRoot(TId id) => Id = id;

    public TId Id { get; }

    /// <summary>Version loaded from storage. The repository rejects a save when the stored version has changed.</summary>
    public long Version { get; private set; }

    public IReadOnlyList<IDomainEvent> PendingEvents => _pendingEvents;

    protected void Raise(IDomainEvent domainEvent) => _pendingEvents.Add(domainEvent);

    /// <summary>Called by the repository after a successful save.</summary>
    public void MarkSaved(long newVersion)
    {
        Version = newVersion;
        _pendingEvents.Clear();
    }

    /// <summary>Called by the repository when the aggregate is loaded from storage.</summary>
    public void SetLoadedVersion(long version) => Version = version;
}

/// <summary>
/// A business rule was broken. <see cref="Code"/> is stable and is translated in the UI (fa/en);
/// the message is for logs and developers only — never show database or exception text to users.
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
