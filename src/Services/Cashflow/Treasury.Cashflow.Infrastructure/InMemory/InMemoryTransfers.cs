using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Transfers;

namespace Treasury.Cashflow.Infrastructure.InMemory;

/// <summary>In-memory transfers between the company's bank accounts (local runs and tests); behaves like the PostgreSQL adapter.</summary>
public sealed class InMemoryBankTransferRepository(InMemoryDatabase db) : IBankTransferRepository
{
    public Task<BankTransfer?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (InMemoryUnitOfWork.Pending?.Transfers.TryGetValue(id, out var staged) == true)
        {
            return Task.FromResult<BankTransfer?>(staged.ToDomain());
        }

        lock (db.Gate)
        {
            return Task.FromResult(db.Transfers.TryGetValue(id, out var s) ? s.ToDomain() : null);
        }
    }

    public Task<int> NextNumberAsync(int fiscalYear, CancellationToken cancellationToken)
    {
        var pending = Pending();
        if (!pending.TransferNumbers.TryGetValue(fiscalYear, out var last))
        {
            lock (db.Gate)
            {
                last = db.TransferNumbers.TryGetValue(fiscalYear, out var n) ? n : 0;
            }
        }

        pending.TransferNumbers[fiscalYear] = last + 1;
        return Task.FromResult(last + 1);
    }

    public Task AddAsync(BankTransfer transfer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        Stage(transfer, 1);
        return Task.CompletedTask;
    }

    public Task SaveAsync(BankTransfer transfer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        long stored;
        lock (db.Gate)
        {
            stored = db.Transfers.TryGetValue(transfer.Id, out var s) ? s.Version : 0;
        }

        if (stored != transfer.Version)
        {
            throw new ConcurrencyConflictException($"Transfer {transfer.Id} was changed by someone else.");
        }

        Stage(transfer, transfer.Version + 1);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BankTransfer>> ListAsync(int fiscalYear, CancellationToken cancellationToken)
    {
        lock (db.Gate)
        {
            IReadOnlyList<BankTransfer> list = db.Transfers.Values
                .Where(s => s.FiscalYear == fiscalYear)
                .OrderByDescending(s => s.Date).ThenByDescending(s => s.Number)
                .Select(s => s.ToDomain()).ToList();
            return Task.FromResult(list);
        }
    }

    public Task<TransferDayUsage> DayUsageAsync(Guid sourceAccountId, FiscalDate day, TransferMethod method, TransferChannel channel, Guid? exceptId,
        CancellationToken cancellationToken)
    {
        lock (db.Gate)
        {
            var sent = db.Transfers.Values
                .Where(s => s.SourceAccountId == sourceAccountId && s.Date == day.Value && s.Method == method && s.State != TransferState.Deleted && s.Id != exceptId)
                .ToList();
            return Task.FromResult(new TransferDayUsage(sent.Where(s => s.Channel == channel).Sum(s => s.AmountRial), sent.Count));
        }
    }

    private static PendingChanges Pending() => InMemoryUnitOfWork.Pending ?? throw new InvalidOperationException("Writes must run inside IUnitOfWork.ExecuteAsync.");

    private static void Stage(BankTransfer transfer, long newVersion)
    {
        var pending = Pending();
        pending.Transfers[transfer.Id] = BankTransferSnapshot.From(transfer, newVersion);
        pending.Outbox.AddRange(transfer.PendingEvents.Select(OutboxMessage.From));
        transfer.MarkSaved(newVersion);
    }
}

public sealed class InMemoryTransferLimitRepository(InMemoryDatabase db) : ITransferLimitRepository
{
    public Task<TransferLimits> GetAsync(CancellationToken cancellationToken)
    {
        if (InMemoryUnitOfWork.Pending?.TransferLimits is { } staged)
        {
            return Task.FromResult(new TransferLimits(staged));
        }

        lock (db.Gate)
        {
            return Task.FromResult(new TransferLimits(db.TransferLimits));
        }
    }

    public Task SaveAsync(TransferLimits limits, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(limits);
        var pending = InMemoryUnitOfWork.Pending ?? throw new InvalidOperationException("Writes must run inside IUnitOfWork.ExecuteAsync.");
        pending.TransferLimits = [.. limits.Rows];
        return Task.CompletedTask;
    }
}
