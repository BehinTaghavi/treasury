using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Purposes;

namespace Treasury.Reference.Infrastructure.InMemory;

public sealed record PurposeGroupSnapshot(
    Guid Id, int Number, PurposeDirection Direction, string Name, string NameKey, string LedgerAccountCode, Guid? Reference1Id, Guid? Reference2Id,
    bool IsActive, bool IsInUse, long Version)
{
    public static PurposeGroupSnapshot From(PurposeGroup g, long version) =>
        new(g.Id, g.Number, g.Direction, g.Name, g.NameKey, g.LedgerAccountCode, g.Reference1Id, g.Reference2Id, g.IsActive, g.IsInUse, version);

    public PurposeGroup ToDomain() =>
        PurposeGroup.Restore(Id, Number, Direction, Name, LedgerAccountCode, Reference1Id, Reference2Id, IsActive, IsInUse, Version);
}

public sealed record PurposeSnapshot(Guid Id, Guid GroupId, string Name, string NameKey, bool IsActive, bool IsInUse, long Version)
{
    public static PurposeSnapshot From(Purpose p, long version) => new(p.Id, p.GroupId, p.Name, p.NameKey, p.IsActive, p.IsInUse, version);

    public Purpose ToDomain() => Purpose.Restore(Id, GroupId, Name, IsActive, IsInUse, Version);
}

/// <summary>In-memory purpose groups and purposes (local runs, tests). Same version checks and outbox as the database.</summary>
public sealed class InMemoryPurposeRepository(InMemoryReferenceDatabase db) : IPurposeRepository
{
    public Task<PurposeGroup?> GetGroupAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Groups().GetValueOrDefault(id)?.ToDomain());

    public Task<IReadOnlyList<PurposeGroup>> ListGroupsAsync(PurposeDirection? direction, bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PurposeGroup>>(Groups().Values
            .Where(g => (direction is null || g.Direction == direction) && (includeInactive || g.IsActive)).Select(g => g.ToDomain()).ToList());

    public Task<bool> GroupNameTakenAsync(PurposeDirection direction, string nameKey, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Groups().Values.Any(g => g.Direction == direction && g.NameKey == nameKey && g.Id != exceptId));

    public Task<int> NextGroupNumberAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Groups().Values.Select(g => g.Number).DefaultIfEmpty(0).Max() + 1);

    public Task<bool> GroupHasPurposesAsync(Guid groupId, CancellationToken cancellationToken) =>
        Task.FromResult(Purposes().Values.Any(p => p.GroupId == groupId));

    public Task AddGroupAsync(PurposeGroup group, CancellationToken cancellationToken) => WriteGroup(group, 1, null);

    public Task SaveGroupAsync(PurposeGroup group, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);
        return WriteGroup(group, group.Version + 1, group.Version);
    }

    public Task DeleteGroupAsync(PurposeGroup group, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);
        Check(Groups().GetValueOrDefault(group.Id)?.Version, group.Version, group.Id);
        InMemoryUnitOfWork.Pending.Groups[group.Id] = null;
        Outbox(group.PendingEvents);
        return Task.CompletedTask;
    }

    public Task<Purpose?> GetPurposeAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Purposes().GetValueOrDefault(id)?.ToDomain());

    public Task<IReadOnlyList<Purpose>> ListPurposesAsync(Guid? groupId, bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Purpose>>(Purposes().Values
            .Where(p => (groupId is null || p.GroupId == groupId) && (includeInactive || p.IsActive)).Select(p => p.ToDomain()).ToList());

    public Task<bool> PurposeNameTakenAsync(Guid groupId, string nameKey, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Purposes().Values.Any(p => p.GroupId == groupId && p.NameKey == nameKey && p.Id != exceptId));

    public Task AddPurposeAsync(Purpose purpose, CancellationToken cancellationToken) => WritePurpose(purpose, 1, null);

    public Task SavePurposeAsync(Purpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(purpose);
        return WritePurpose(purpose, purpose.Version + 1, purpose.Version);
    }

    public Task DeletePurposeAsync(Purpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(purpose);
        Check(Purposes().GetValueOrDefault(purpose.Id)?.Version, purpose.Version, purpose.Id);
        InMemoryUnitOfWork.Pending.Purposes[purpose.Id] = null;
        Outbox(purpose.PendingEvents);
        return Task.CompletedTask;
    }

    private Task WriteGroup(PurposeGroup group, long newVersion, long? expected)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (expected is { } v)
        {
            Check(Groups().GetValueOrDefault(group.Id)?.Version, v, group.Id);
        }

        InMemoryUnitOfWork.Pending.Groups[group.Id] = PurposeGroupSnapshot.From(group, newVersion);
        Outbox(group.PendingEvents);
        group.MarkSaved(newVersion);
        return Task.CompletedTask;
    }

    private Task WritePurpose(Purpose purpose, long newVersion, long? expected)
    {
        ArgumentNullException.ThrowIfNull(purpose);
        if (expected is { } v)
        {
            Check(Purposes().GetValueOrDefault(purpose.Id)?.Version, v, purpose.Id);
        }

        InMemoryUnitOfWork.Pending.Purposes[purpose.Id] = PurposeSnapshot.From(purpose, newVersion);
        Outbox(purpose.PendingEvents);
        purpose.MarkSaved(newVersion);
        return Task.CompletedTask;
    }

    private static void Check(long? stored, long expected, Guid id)
    {
        if (stored != expected)
        {
            throw new ConcurrencyConflictException($"Entry {id} was changed by someone else.");
        }
    }

    private static void Outbox(IEnumerable<Treasury.BuildingBlocks.Domain.IDomainEvent> events) =>
        InMemoryUnitOfWork.Pending.Outbox.AddRange(events.Select(OutboxMessage.From));

    private Dictionary<Guid, PurposeGroupSnapshot> Groups() => Visible(db.Groups, InMemoryUnitOfWork.Pending.Groups);

    private Dictionary<Guid, PurposeSnapshot> Purposes() => Visible(db.Purposes, InMemoryUnitOfWork.Pending.Purposes);

    /// <summary>Committed rows overlaid with this transaction's own changes.</summary>
    private Dictionary<Guid, T> Visible<T>(Dictionary<Guid, T> committed, Dictionary<Guid, T?> pending)
        where T : class
    {
        lock (db.Gate)
        {
            var all = new Dictionary<Guid, T>(committed);
            foreach (var (id, s) in pending)
            {
                if (s is null)
                {
                    all.Remove(id);
                }
                else
                {
                    all[id] = s;
                }
            }

            return all;
        }
    }
}
