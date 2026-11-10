using Treasury.BuildingBlocks.Domain;
using System.Collections.Concurrent;
using Treasury.BuildingBlocks.Application;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Infrastructure.InMemory;

/// <summary>
/// In-memory storage for local runs and tests. Behaves like the database where it matters:
/// one transaction at a time (like a row lock), all-or-nothing commit, optimistic version checks, outbox.
/// Not for production.
/// </summary>
public sealed class InMemoryDatabase : IDisposable
{
    internal readonly object Gate = new();
    internal readonly Dictionary<Guid, InstrumentSnapshot> Instruments = [];
    internal readonly Dictionary<Guid, ChequeBookSnapshot> ChequeBooks = [];
    internal readonly Dictionary<string, string> IdempotencyResults = [];
    internal readonly List<OutboxMessage> Outbox = [];
    internal readonly SemaphoreSlim TransactionLock = new(1, 1);

    public void Dispose() => TransactionLock.Dispose();

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
}

internal sealed class PendingChanges
{
    public Dictionary<Guid, InstrumentSnapshot> Instruments { get; } = [];

    public Dictionary<Guid, ChequeBookSnapshot> ChequeBooks { get; } = [];

    public Dictionary<string, string> Idempotency { get; } = [];

    public List<OutboxMessage> Outbox { get; } = [];
}

public sealed class InMemoryUnitOfWork(InMemoryDatabase db) : IUnitOfWork
{
    private static readonly AsyncLocal<PendingChanges?> Current = new();

    internal static PendingChanges? Pending => Current.Value;

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
                foreach (var (id, s) in pending.Instruments)
                {
                    db.Instruments[id] = s;
                }

                foreach (var (id, s) in pending.ChequeBooks)
                {
                    db.ChequeBooks[id] = s;
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

public sealed class InMemoryInstrumentRepository(InMemoryDatabase db) : IInstrumentRepository
{
    public Task<Instrument?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var pending = InMemoryUnitOfWork.Pending;
        if (pending is not null && pending.Instruments.TryGetValue(id, out var staged))
        {
            return Task.FromResult<Instrument?>(staged.ToDomain());
        }

        lock (db.Gate)
        {
            return Task.FromResult(db.Instruments.TryGetValue(id, out var s) ? s.ToDomain() : null);
        }
    }

    public Task<IReadOnlyList<Guid>> FindReceivedAsync(string bankName, string number, CancellationToken cancellationToken)
    {
        lock (db.Gate)
        {
            IReadOnlyList<Guid> ids = db.Instruments.Values
                .Where(s => s.Direction == InstrumentDirection.Received
                         && string.Equals(s.BankName, bankName, StringComparison.OrdinalIgnoreCase)
                         && s.Number == number)
                .Select(s => s.Id)
                .ToList();
            return Task.FromResult(ids);
        }
    }

    public Task AddAsync(Instrument instrument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        lock (db.Gate)
        {
            if (db.Instruments.ContainsKey(instrument.Id))
            {
                throw new ConcurrencyConflictException($"Instrument {instrument.Id} already exists.");
            }
        }

        Stage(instrument, 1);
        return Task.CompletedTask;
    }

    public Task SaveAsync(Instrument instrument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        long stored;
        lock (db.Gate)
        {
            stored = db.Instruments.TryGetValue(instrument.Id, out var s) ? s.Version : 0;
        }

        if (stored != instrument.Version)
        {
            throw new ConcurrencyConflictException($"Instrument {instrument.Id} was changed by someone else (version {stored}, expected {instrument.Version}).");
        }

        Stage(instrument, instrument.Version + 1);
        return Task.CompletedTask;
    }

    private static void Stage(Instrument instrument, long newVersion)
    {
        var pending = InMemoryUnitOfWork.Pending ?? throw new InvalidOperationException("Writes must run inside IUnitOfWork.ExecuteAsync.");
        pending.Instruments[instrument.Id] = InstrumentSnapshot.From(instrument, newVersion);
        pending.Outbox.AddRange(instrument.PendingEvents.Select(OutboxMessage.From));
        instrument.MarkSaved(newVersion);
    }
}

public sealed class InMemoryChequeBookRepository(InMemoryDatabase db) : IChequeBookRepository
{
    public Task<ChequeBook?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        // The single transaction lock of InMemoryUnitOfWork plays the role of SELECT ... FOR UPDATE.
        var pending = InMemoryUnitOfWork.Pending;
        if (pending is not null && pending.ChequeBooks.TryGetValue(id, out var staged))
        {
            return Task.FromResult<ChequeBook?>(staged.ToDomain());
        }

        lock (db.Gate)
        {
            return Task.FromResult(db.ChequeBooks.TryGetValue(id, out var s) ? s.ToDomain() : null);
        }
    }

    public Task AddAsync(ChequeBook book, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(book);
        Stage(book, 1);
        return Task.CompletedTask;
    }

    public Task SaveAsync(ChequeBook book, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(book);
        long stored;
        lock (db.Gate)
        {
            stored = db.ChequeBooks.TryGetValue(book.Id, out var s) ? s.Version : 0;
        }

        if (stored != book.Version)
        {
            throw new ConcurrencyConflictException($"Cheque book {book.Id} was changed by someone else.");
        }

        Stage(book, book.Version + 1);
        return Task.CompletedTask;
    }

    private static void Stage(ChequeBook book, long newVersion)
    {
        var pending = InMemoryUnitOfWork.Pending ?? throw new InvalidOperationException("Writes must run inside IUnitOfWork.ExecuteAsync.");
        pending.ChequeBooks[book.Id] = ChequeBookSnapshot.From(book, newVersion);
        pending.Outbox.AddRange(book.PendingEvents.Select(OutboxMessage.From));
        book.MarkSaved(newVersion);
    }
}

public sealed class InMemoryIdempotencyStore(InMemoryDatabase db) : IIdempotencyStore
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
        var pending = InMemoryUnitOfWork.Pending ?? throw new InvalidOperationException("Writes must run inside IUnitOfWork.ExecuteAsync.");
        pending.Idempotency[key] = result;
        return Task.CompletedTask;
    }
}

/// <summary>Development stand-ins for answers that will come from other contexts (Fiscal Periods, Payment Requests, balances).</summary>
public sealed class DevelopmentPolicies : IFiscalPeriodPolicy, IBankBalanceQuery, IPaymentApprovalQuery
{
    private readonly ConcurrentDictionary<int, bool> _openYears = new();
    private readonly ConcurrentDictionary<Guid, decimal> _balances = new();
    private readonly ConcurrentDictionary<Guid, bool> _approvedPayments = new();

    public DevelopmentPolicies OpenYear(int jalaliYear)
    {
        _openYears[jalaliYear] = true;
        return this;
    }

    public DevelopmentPolicies CloseYear(int jalaliYear)
    {
        _openYears[jalaliYear] = false;
        return this;
    }

    public DevelopmentPolicies SetBalance(Guid bankAccountId, decimal rial)
    {
        _balances[bankAccountId] = rial;
        return this;
    }

    public DevelopmentPolicies Approve(Guid paymentId)
    {
        _approvedPayments[paymentId] = true;
        return this;
    }

    public Task<bool> IsOpenAsync(FiscalDate operationDate, CancellationToken cancellationToken) =>
        Task.FromResult(_openYears.TryGetValue(operationDate.JalaliYear, out var open) && open);

    public Task<decimal> GetBalanceAsync(Guid bankAccountId, CancellationToken cancellationToken) =>
        Task.FromResult(_balances.TryGetValue(bankAccountId, out var b) ? b : 0m);

    public Task<bool> IsApprovedAsync(Guid paymentId, CancellationToken cancellationToken) =>
        Task.FromResult(_approvedPayments.ContainsKey(paymentId));
}
