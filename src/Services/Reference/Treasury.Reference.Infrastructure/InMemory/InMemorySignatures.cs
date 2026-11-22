using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Infrastructure.InMemory;

public sealed record SignatorySnapshot(
    Guid Id, PersonParticulars Person, SignatoryPosition PositionKind, string Position, bool SignsCheques, bool SignsNotes, string? SignatureType, byte[]? Scan,
    bool IsActive, bool IsInUse, long Version, bool SignsContracts = false);

public sealed record PermissionHolderSnapshot(
    Guid Id, PersonParticulars Person, string JobTitle, IReadOnlyList<PermissionRole> Roles, string? SignatureType, byte[]? Scan, bool IsActive, long Version);

/// <summary>Rows of one in-memory table with the version checks and outbox of the database (local runs, tests).</summary>
internal sealed class InMemoryRows<TSnapshot>(InMemoryReferenceDatabase db, string table, Func<TSnapshot, long> versionOf)
    where TSnapshot : class
{
    /// <summary>Committed rows overlaid with this transaction's own changes.</summary>
    public Dictionary<Guid, TSnapshot> All()
    {
        var pending = InMemoryUnitOfWork.Pending;
        lock (db.Gate)
        {
            var all = db.Tables.TryGetValue(table, out var committed) ? committed.ToDictionary(kv => kv.Key, kv => (TSnapshot)kv.Value) : [];
            if (pending.Tables.TryGetValue(table, out var mine))
            {
                foreach (var (id, row) in mine)
                {
                    if (row is null)
                    {
                        all.Remove(id);
                    }
                    else
                    {
                        all[id] = (TSnapshot)row;
                    }
                }
            }

            return all;
        }
    }

    public TSnapshot? Find(Guid id) => All().GetValueOrDefault(id);

    /// <summary>Insert (expected null), version-checked update, or delete (snapshot null).</summary>
    public Task Write(AggregateRoot<Guid> aggregate, long? expected, long newVersion, TSnapshot? snapshot)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        if (expected is { } v && (Find(aggregate.Id) is not { } stored || versionOf(stored) != v))
        {
            throw new ConcurrencyConflictException($"Entry {aggregate.Id} was changed by someone else.");
        }

        var tables = InMemoryUnitOfWork.Pending.Tables;
        if (!tables.TryGetValue(table, out var rows))
        {
            rows = [];
            tables[table] = rows;
        }

        rows[aggregate.Id] = snapshot;
        InMemoryUnitOfWork.Pending.Outbox.AddRange(aggregate.PendingEvents.Select(OutboxMessage.From));
        aggregate.MarkSaved(newVersion);
        return Task.CompletedTask;
    }
}

/// <summary>In-memory authorised signatories: same version checks and outbox as the database.</summary>
public sealed class InMemorySignatureRepository(InMemoryReferenceDatabase db) : ISignatureRepository
{
    private readonly InMemoryRows<SignatorySnapshot> _rows = new(db, "signatories", s => s.Version);

    public Task<Signatory?> GetSignatoryAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_rows.Find(id) is { } s ? ToDomain(s) : null);

    public Task<IReadOnlyList<Signatory>> ListSignatoriesAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Signatory>>(_rows.All().Values.Where(s => includeInactive || s.IsActive).Select(ToDomain).ToList());

    public Task AddSignatoryAsync(Signatory signatory, CancellationToken cancellationToken) => _rows.Write(signatory, null, 1, Snapshot(signatory, 1, null));

    public Task SaveSignatoryAsync(Signatory signatory, CancellationToken cancellationToken, byte[]? scan = null)
    {
        ArgumentNullException.ThrowIfNull(signatory);
        var kept = signatory.HasSignature ? scan ?? _rows.Find(signatory.Id)?.Scan : null;
        return _rows.Write(signatory, signatory.Version, signatory.Version + 1, Snapshot(signatory, signatory.Version + 1, kept));
    }

    public Task DeleteSignatoryAsync(Signatory signatory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(signatory);
        return _rows.Write(signatory, signatory.Version, signatory.Version, null);
    }

    public Task<(string ContentType, byte[] Data)?> GetSignatureAsync(Guid signatoryId, CancellationToken cancellationToken) =>
        Task.FromResult<(string, byte[])?>(_rows.Find(signatoryId) is { SignatureType: { } type, Scan: { } data } ? (type, data) : null);

    private static SignatorySnapshot Snapshot(Signatory s, long version, byte[]? scan) =>
        new(s.Id, s.Person, s.PositionKind, s.Position, s.SignsCheques, s.SignsNotes, s.SignatureType, scan, s.IsActive, s.IsInUse, version, s.SignsContracts);

    private static Signatory ToDomain(SignatorySnapshot s) =>
        Signatory.Restore(s.Id, s.Person, s.PositionKind, s.Position, s.SignsCheques, s.SignsNotes, s.SignatureType, s.IsActive, s.IsInUse, s.Version, s.SignsContracts);
}

/// <summary>In-memory permission holders: same version checks and outbox as the database.</summary>
public sealed class InMemoryPermissionHolderRepository(InMemoryReferenceDatabase db) : IPermissionHolderRepository
{
    private readonly InMemoryRows<PermissionHolderSnapshot> _rows = new(db, "permission-holders", h => h.Version);

    public Task<PermissionHolder?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_rows.Find(id) is { } h ? ToDomain(h) : null);

    public Task<IReadOnlyList<PermissionHolder>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PermissionHolder>>(_rows.All().Values.Where(h => includeInactive || h.IsActive).Select(ToDomain).ToList());

    public Task AddAsync(PermissionHolder holder, CancellationToken cancellationToken) => _rows.Write(holder, null, 1, Snapshot(holder, 1, null));

    public Task SaveAsync(PermissionHolder holder, CancellationToken cancellationToken, byte[]? scan = null)
    {
        ArgumentNullException.ThrowIfNull(holder);
        var kept = holder.HasSignature ? scan ?? _rows.Find(holder.Id)?.Scan : null;
        return _rows.Write(holder, holder.Version, holder.Version + 1, Snapshot(holder, holder.Version + 1, kept));
    }

    public Task DeleteAsync(PermissionHolder holder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(holder);
        return _rows.Write(holder, holder.Version, holder.Version, null);
    }

    public Task<(string ContentType, byte[] Data)?> GetSignatureAsync(Guid holderId, CancellationToken cancellationToken) =>
        Task.FromResult<(string, byte[])?>(_rows.Find(holderId) is { SignatureType: { } type, Scan: { } data } ? (type, data) : null);

    private static PermissionHolderSnapshot Snapshot(PermissionHolder h, long version, byte[]? scan) =>
        new(h.Id, h.Person, h.JobTitle, [.. h.Roles], h.SignatureType, scan, h.IsActive, version);

    private static PermissionHolder ToDomain(PermissionHolderSnapshot h) => PermissionHolder.Restore(h.Id, h.Person, h.JobTitle, h.Roles, h.SignatureType, h.IsActive, h.Version);
}
