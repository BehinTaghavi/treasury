using System.Text.Json;
using System.Text.Json.Serialization;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Infrastructure.InMemory;

/// <summary>A domain event waiting in the outbox. Saved in the same transaction as the aggregate; published afterwards.</summary>
public sealed record OutboxMessage(Guid Id, string Type, string Payload, DateTimeOffset OccurredAt)
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    public static OutboxMessage From(IDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new(e.EventId, e.GetType().Name, JsonSerializer.Serialize(e, e.GetType(), Json), e.OccurredAt);
    }
}

/// <summary>Stored form of a bank account (immutable: a transaction's copy never leaks into committed state).</summary>
public sealed record BankAccountSnapshot(Guid Id, BankAccountData Data, Guid? MainAccountId, bool IsActive, long Version)
{
    public static BankAccountSnapshot From(BankAccount a, long version) => new(a.Id, a.Data, a.MainAccountId, a.IsActive, version);

    public BankAccount ToDomain() => BankAccount.Restore(Id, Data, MainAccountId, IsActive, Version);
}

/// <summary>In-memory storage for local runs and tests: one transaction at a time, all-or-nothing commit, version checks, outbox.</summary>
public sealed class InMemoryAccountsDatabase : IDisposable
{
    internal readonly object Gate = new();
    internal readonly Dictionary<Guid, BankAccountSnapshot> BankAccounts = [];
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
    public Dictionary<Guid, BankAccountSnapshot> BankAccounts { get; } = [];

    public Dictionary<string, string> Idempotency { get; } = [];

    public List<OutboxMessage> Outbox { get; } = [];
}

public sealed class InMemoryUnitOfWork(InMemoryAccountsDatabase db) : IUnitOfWork
{
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
                foreach (var (id, s) in pending.BankAccounts)
                {
                    db.BankAccounts[id] = s;
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

public sealed class InMemoryBankAccountRepository(InMemoryAccountsDatabase db) : IBankAccountRepository
{
    public Task<BankAccount?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Visible().FirstOrDefault(s => s.Id == id)?.ToDomain());

    public Task<bool> NumberTakenAsync(string numberKey, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Visible().Any(s => s.Id != exceptId && BankAccount.NumberKeyOf(s.Data.AccountNumber) == numberKey));

    public Task<bool> HasBackupAsync(Guid mainAccountId, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Visible().Any(s => s.Id != exceptId && s.MainAccountId == mainAccountId));

    public Task AddAsync(BankAccount account, CancellationToken cancellationToken) => Write(account, 1, null);

    public Task SaveAsync(BankAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        return Write(account, account.Version + 1, account.Version);
    }

    public Task<IReadOnlyList<BankAccount>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<BankAccount>>(Visible().Where(s => includeInactive || s.IsActive).Select(s => s.ToDomain()).ToList());

    private List<BankAccountSnapshot> Visible()
    {
        var pending = InMemoryUnitOfWork.Pending;
        lock (db.Gate)
        {
            var all = new Dictionary<Guid, BankAccountSnapshot>(db.BankAccounts);
            foreach (var (id, s) in pending.BankAccounts)
            {
                all[id] = s;
            }

            return [.. all.Values];
        }
    }

    private Task Write(BankAccount account, long newVersion, long? expected)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (expected is { } v && Visible().FirstOrDefault(s => s.Id == account.Id)?.Version != v)
        {
            throw new ConcurrencyConflictException($"Bank account {account.Id} was changed by someone else.");
        }

        var pending = InMemoryUnitOfWork.Pending;
        pending.BankAccounts[account.Id] = BankAccountSnapshot.From(account, newVersion);
        pending.Outbox.AddRange(account.PendingEvents.Select(OutboxMessage.From));
        account.MarkSaved(newVersion);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryIdempotencyStore(InMemoryAccountsDatabase db) : IIdempotencyStore
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
