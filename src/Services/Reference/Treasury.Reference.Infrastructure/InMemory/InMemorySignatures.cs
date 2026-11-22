using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Infrastructure.InMemory;

public sealed record SignatorySnapshot(
    Guid Id, string FullName, string Position, string? DetailCode, string? NationalCode, string? Mobile, string? Phone, bool IsActive, long Version);

public sealed record SignatureRuleSnapshot(Guid Id, SignedDocument Document, int Required, IReadOnlyList<Guid> Compulsory, IReadOnlyList<Guid> Optional, long Version);

/// <summary>In-memory signatories and signature rules (local runs, tests): same version checks and outbox as the database.</summary>
public sealed class InMemorySignatureRepository(InMemoryReferenceDatabase db) : ISignatureRepository
{
    private const string Signatories = "signatories";
    private const string SignatureRules = "signature-rules";

    public Task<Signatory?> GetSignatoryAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows<SignatorySnapshot>(Signatories).GetValueOrDefault(id) is { } s ? ToDomain(s) : null);

    public Task<IReadOnlyList<Signatory>> ListSignatoriesAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Signatory>>(Rows<SignatorySnapshot>(Signatories).Values.Where(s => includeInactive || s.IsActive).Select(ToDomain).ToList());

    public Task AddSignatoryAsync(Signatory signatory, CancellationToken cancellationToken) => Write(Signatories, signatory, 1, null, Snapshot);

    public Task SaveSignatoryAsync(Signatory signatory, CancellationToken cancellationToken) =>
        Write(Signatories, signatory, signatory.Version + 1, signatory.Version, Snapshot);

    public Task DeleteSignatoryAsync(Signatory signatory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(signatory);
        if (Version(Signatories, signatory.Id) != signatory.Version)
        {
            throw new ConcurrencyConflictException($"Entry {signatory.Id} was changed by someone else.");
        }

        Pending(Signatories)[signatory.Id] = null;
        InMemoryUnitOfWork.Pending.Outbox.AddRange(signatory.PendingEvents.Select(OutboxMessage.From));
        signatory.MarkSaved(signatory.Version);
        return Task.CompletedTask;
    }

    public Task<SignatureRule?> GetRuleAsync(SignedDocument document, CancellationToken cancellationToken) =>
        Task.FromResult(Rows<SignatureRuleSnapshot>(SignatureRules).Values.FirstOrDefault(r => r.Document == document) is { } s ? ToDomain(s) : null);

    public Task<IReadOnlyList<SignatureRule>> ListRulesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SignatureRule>>(Rows<SignatureRuleSnapshot>(SignatureRules).Values.Select(ToDomain).ToList());

    public Task AddRuleAsync(SignatureRule rule, CancellationToken cancellationToken) => Write(SignatureRules, rule, 1, null, Snapshot);

    public Task SaveRuleAsync(SignatureRule rule, CancellationToken cancellationToken) => Write(SignatureRules, rule, rule.Version + 1, rule.Version, Snapshot);

    private static SignatorySnapshot Snapshot(Signatory s, long v) => new(s.Id, s.FullName, s.Position, s.DetailCode, s.NationalCode, s.Mobile, s.Phone, s.IsActive, v);

    private static SignatureRuleSnapshot Snapshot(SignatureRule r, long v) => new(r.Id, r.Document, r.Required, [.. r.Compulsory], [.. r.Optional], v);

    private static Signatory ToDomain(SignatorySnapshot s) =>
        Signatory.Restore(s.Id, s.FullName, s.Position, s.DetailCode, s.NationalCode, s.Mobile, s.Phone, s.IsActive, s.Version);

    private static SignatureRule ToDomain(SignatureRuleSnapshot s) => SignatureRule.Restore(s.Id, s.Document, s.Required, s.Compulsory, s.Optional, s.Version);

    /// <summary>Committed rows of a table overlaid with this transaction's own changes.</summary>
    private Dictionary<Guid, T> Rows<T>(string table)
        where T : class
    {
        var pending = InMemoryUnitOfWork.Pending;
        lock (db.Gate)
        {
            var all = db.Tables.TryGetValue(table, out var committed) ? committed.ToDictionary(kv => kv.Key, kv => (T)kv.Value) : [];
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
                        all[id] = (T)row;
                    }
                }
            }

            return all;
        }
    }

    private Task Write<TAggregate, TSnapshot>(string table, TAggregate aggregate, long newVersion, long? expected, Func<TAggregate, long, TSnapshot> snapshot)
        where TAggregate : AggregateRoot<Guid>
        where TSnapshot : class
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        if (expected is { } v && Version(table, aggregate.Id) != v)
        {
            throw new ConcurrencyConflictException($"Entry {aggregate.Id} was changed by someone else.");
        }

        Pending(table)[aggregate.Id] = snapshot(aggregate, newVersion);
        InMemoryUnitOfWork.Pending.Outbox.AddRange(aggregate.PendingEvents.Select(OutboxMessage.From));
        aggregate.MarkSaved(newVersion);
        return Task.CompletedTask;
    }

    private long? Version(string table, Guid id) => Rows<object>(table).GetValueOrDefault(id) switch
    {
        SignatorySnapshot s => s.Version,
        SignatureRuleSnapshot r => r.Version,
        _ => null,
    };

    private static Dictionary<Guid, object?> Pending(string table)
    {
        var tables = InMemoryUnitOfWork.Pending.Tables;
        if (!tables.TryGetValue(table, out var rows))
        {
            rows = [];
            tables[table] = rows;
        }

        return rows;
    }
}
