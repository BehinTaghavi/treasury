using System.Text.Json;
using System.Text.Json.Serialization;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Domain.CashDesks;
using Treasury.Accounts.Domain.PettyCash;
using Treasury.BuildingBlocks.Application;
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

/// <summary>Stored form of a cash desk.</summary>
public sealed record CashDeskSnapshot(Guid Id, CashDeskData Data, bool IsActive, long Version)
{
    public static CashDeskSnapshot From(CashDesk d, long version) => new(d.Id, d.Data, d.IsActive, version);

    public CashDesk ToDomain() => CashDesk.Restore(Id, Data, IsActive, Version);
}

/// <summary>Stored form of a petty cash.</summary>
public sealed record PettyCashSnapshot(Guid Id, PettyCashData Data, bool IsActive, long Version)
{
    public static PettyCashSnapshot From(PettyCashFund f, long version) => new(f.Id, f.Data, f.IsActive, version);

    public PettyCashFund ToDomain() => PettyCashFund.Restore(Id, Data, IsActive, Version);
}

/// <summary>In-memory storage for local runs and tests: one transaction at a time, all-or-nothing commit, version checks, outbox.</summary>
public sealed class InMemoryAccountsDatabase : IDisposable
{
    internal readonly object Gate = new();
    internal readonly Dictionary<Guid, BankAccountSnapshot> BankAccounts = [];
    internal readonly Dictionary<Guid, CashDeskSnapshot> CashDesks = [];
    internal readonly Dictionary<Guid, PettyCashSnapshot> PettyCash = [];
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

    public Dictionary<Guid, CashDeskSnapshot> CashDesks { get; } = [];

    public Dictionary<Guid, PettyCashSnapshot> PettyCash { get; } = [];

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

                foreach (var (id, s) in pending.CashDesks)
                {
                    db.CashDesks[id] = s;
                }

                foreach (var (id, s) in pending.PettyCash)
                {
                    db.PettyCash[id] = s;
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

public sealed class InMemoryCashDeskRepository(InMemoryAccountsDatabase db) : ICashDeskRepository
{
    public Task<CashDesk?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Visible().FirstOrDefault(s => s.Id == id)?.ToDomain());

    public Task<bool> NameTakenAsync(string nameKey, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Visible().Any(s => s.Id != exceptId && PersianText.Key(s.Data.Name) == nameKey));

    public Task AddAsync(CashDesk desk, CancellationToken cancellationToken) => Write(desk, 1, null);

    public Task SaveAsync(CashDesk desk, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(desk);
        return Write(desk, desk.Version + 1, desk.Version);
    }

    public Task<IReadOnlyList<CashDesk>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CashDesk>>(Visible().Where(s => includeInactive || s.IsActive).Select(s => s.ToDomain()).ToList());

    private List<CashDeskSnapshot> Visible()
    {
        var pending = InMemoryUnitOfWork.Pending;
        lock (db.Gate)
        {
            var all = new Dictionary<Guid, CashDeskSnapshot>(db.CashDesks);
            foreach (var (id, s) in pending.CashDesks)
            {
                all[id] = s;
            }

            return [.. all.Values];
        }
    }

    private Task Write(CashDesk desk, long newVersion, long? expected)
    {
        ArgumentNullException.ThrowIfNull(desk);
        if (expected is { } v && Visible().FirstOrDefault(s => s.Id == desk.Id)?.Version != v)
        {
            throw new ConcurrencyConflictException($"Cash desk {desk.Id} was changed by someone else.");
        }

        var pending = InMemoryUnitOfWork.Pending;
        pending.CashDesks[desk.Id] = CashDeskSnapshot.From(desk, newVersion);
        pending.Outbox.AddRange(desk.PendingEvents.Select(OutboxMessage.From));
        desk.MarkSaved(newVersion);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryPettyCashRepository(InMemoryAccountsDatabase db) : IPettyCashRepository
{
    public Task<PettyCashFund?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Visible().FirstOrDefault(s => s.Id == id)?.ToDomain());

    public Task<bool> HolderTakenAsync(string holderDetailCode, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Visible().Any(s => s.Id != exceptId && s.Data.HolderDetailCode == holderDetailCode));

    public Task AddAsync(PettyCashFund fund, CancellationToken cancellationToken) => Write(fund, 1, null);

    public Task SaveAsync(PettyCashFund fund, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fund);
        return Write(fund, fund.Version + 1, fund.Version);
    }

    public Task<IReadOnlyList<PettyCashFund>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PettyCashFund>>(Visible().Where(s => includeInactive || s.IsActive).Select(s => s.ToDomain()).ToList());

    private List<PettyCashSnapshot> Visible()
    {
        var pending = InMemoryUnitOfWork.Pending;
        lock (db.Gate)
        {
            var all = new Dictionary<Guid, PettyCashSnapshot>(db.PettyCash);
            foreach (var (id, s) in pending.PettyCash)
            {
                all[id] = s;
            }

            return [.. all.Values];
        }
    }

    private Task Write(PettyCashFund fund, long newVersion, long? expected)
    {
        ArgumentNullException.ThrowIfNull(fund);
        if (expected is { } v && Visible().FirstOrDefault(s => s.Id == fund.Id)?.Version != v)
        {
            throw new ConcurrencyConflictException($"Petty cash {fund.Id} was changed by someone else.");
        }

        var pending = InMemoryUnitOfWork.Pending;
        pending.PettyCash[fund.Id] = PettyCashSnapshot.From(fund, newVersion);
        pending.Outbox.AddRange(fund.PendingEvents.Select(OutboxMessage.From));
        fund.MarkSaved(newVersion);
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
