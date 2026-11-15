using Treasury.BuildingBlocks.Application;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Vouchers;

namespace Treasury.Cashflow.Infrastructure.InMemory;

/// <summary>
/// In-memory voucher template and confirmed vouchers (local runs and tests). Writes take effect at once rather than at commit: they
/// are the last step of their command, so a failing command never leaves one behind.
/// </summary>
public sealed class InMemoryVoucherStore
{
    internal readonly object Gate = new();
    internal Dictionary<FixedAccount, string> Template { get; set; } = [];
    internal readonly List<ConfirmedVoucher> Confirmed = [];
}

public sealed class InMemoryVoucherTemplateRepository(InMemoryVoucherStore store) : IVoucherTemplateRepository
{
    public Task<VoucherTemplate> GetAsync(CancellationToken cancellationToken)
    {
        lock (store.Gate)
        {
            return Task.FromResult(new VoucherTemplate(new Dictionary<FixedAccount, string>(store.Template)));
        }
    }

    public Task SaveAsync(VoucherTemplate voucherTemplate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(voucherTemplate);
        lock (store.Gate)
        {
            store.Template = new Dictionary<FixedAccount, string>(voucherTemplate.Accounts);
        }

        return Task.CompletedTask;
    }
}

public sealed class InMemoryConfirmedVoucherRepository(InMemoryVoucherStore store) : IConfirmedVoucherRepository
{
    public Task<ConfirmedVoucher?> FindAsync(VoucherSource source, Guid sourceId, int stepNumber, CancellationToken cancellationToken)
    {
        lock (store.Gate)
        {
            return Task.FromResult(store.Confirmed.FirstOrDefault(v => v.Source == source && v.SourceId == sourceId && v.Step == stepNumber));
        }
    }

    public Task<IReadOnlyList<ConfirmedVoucher>> ListBySourceAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        lock (store.Gate)
        {
            return Task.FromResult<IReadOnlyList<ConfirmedVoucher>>(store.Confirmed.Where(v => v.SourceId == sourceId).ToList());
        }
    }

    public Task AddAsync(ConfirmedVoucher voucher, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        lock (store.Gate)
        {
            if (store.Confirmed.Any(v => v.Source == voucher.Source && v.SourceId == voucher.SourceId && v.Step == voucher.Step))
            {
                throw new ConcurrencyConflictException("This voucher was confirmed by someone else.");
            }

            store.Confirmed.Add(voucher);
        }

        voucher.MarkSaved(1);
        return Task.CompletedTask;
    }
}
