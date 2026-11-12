using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Ledger;

namespace Treasury.Reference.Infrastructure.InMemory;

public sealed record LedgerAccountRecord(string Code, string Name, LedgerLevel Level, string? ParentCode, bool IsActive, bool IsPostable, IReadOnlyList<string> Detail1Types, IReadOnlyList<string> Detail2Types)
{
    public static LedgerAccountRecord From(LedgerAccount a) =>
        new(a.Code, a.Name, a.Level, a.ParentCode, a.IsActive, a.IsPostable, [.. a.Detail1Types.Order(StringComparer.Ordinal)], [.. a.Detail2Types.Order(StringComparer.Ordinal)]);

    public LedgerAccount ToDomain() => LedgerAccount.Restore(Code, Name, Level, ParentCode, IsActive, IsPostable, Detail1Types, Detail2Types);
}

public sealed record DetailAccountRecord(string Code, string Name, string? Type, bool IsCostCenter, bool IsActive)
{
    public static DetailAccountRecord From(DetailAccount d) => new(d.Code, d.Name, d.Type, d.IsCostCenter, d.IsActive);

    public DetailAccount ToDomain() => DetailAccount.Restore(Code, Name, Type, IsCostCenter, IsActive);
}

/// <summary>Stored form of the chart (immutable, so a transaction's copy never leaks into committed state).</summary>
public sealed record ChartSnapshot(Guid Id, AccountCodeFormat Format, IReadOnlyList<LedgerAccountRecord> Accounts, long Version);

public sealed record DetailsSnapshot(Guid Id, IReadOnlyList<DetailAccountRecord> Details, long Version);

public sealed class InMemoryLedgerRepository(InMemoryReferenceDatabase db) : ILedgerRepository, ILedgerQueries
{
    public Task<ChartOfAccounts?> GetChartAsync(CancellationToken cancellationToken)
    {
        var s = Chart();
        return Task.FromResult(s is null ? null : ChartOfAccounts.Restore(s.Id, s.Format, s.Accounts.Select(a => a.ToDomain()), s.Version));
    }

    public Task<DetailAccountDirectory?> GetDetailsAsync(CancellationToken cancellationToken)
    {
        var s = Details();
        return Task.FromResult(s is null ? null : DetailAccountDirectory.Restore(s.Id, s.Details.Select(d => d.ToDomain()), s.Version));
    }

    public Task SaveChartAsync(ChartOfAccounts chart, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chart);
        CheckVersion(Chart()?.Version, chart.Version, "chart of accounts");
        var pending = InMemoryUnitOfWork.Pending;
        pending.Chart = new ChartSnapshot(chart.Id, chart.Format, chart.Accounts.Select(LedgerAccountRecord.From).ToList(), chart.Version + 1);
        pending.Outbox.AddRange(chart.PendingEvents.Select(OutboxMessage.From));
        chart.MarkSaved(chart.Version + 1);
        chart.ClearChanges();
        return Task.CompletedTask;
    }

    public Task SaveDetailsAsync(DetailAccountDirectory directory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(directory);
        CheckVersion(Details()?.Version, directory.Version, "detail account list");
        var pending = InMemoryUnitOfWork.Pending;
        pending.Details = new DetailsSnapshot(directory.Id, directory.Details.Select(DetailAccountRecord.From).ToList(), directory.Version + 1);
        pending.Outbox.AddRange(directory.PendingEvents.Select(OutboxMessage.From));
        directory.MarkSaved(directory.Version + 1);
        directory.ClearChanges();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LedgerAccountView>> AccountsAsync(bool postableOnly, bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LedgerAccountView>>((Chart()?.Accounts ?? [])
            .Where(a => (!postableOnly || a.IsPostable) && (includeInactive || a.IsActive))
            .OrderBy(a => a.Code, StringComparer.Ordinal)
            .Select(a => new LedgerAccountView(a.Code, a.Name, a.Level.ToString(), a.ParentCode, a.IsPostable, a.IsActive, a.Detail1Types, a.Detail2Types))
            .ToList());

    public Task<IReadOnlyList<DetailAccountView>> SearchDetailsAsync(string searchKey, string? type, bool includeInactive, int take, CancellationToken cancellationToken, bool costCentersOnly = false) =>
        Task.FromResult<IReadOnlyList<DetailAccountView>>((Details()?.Details ?? [])
            .Where(d => (includeInactive || d.IsActive) && (type is null || d.Type == type) && (!costCentersOnly || d.IsCostCenter)
                && (searchKey.Length == 0 || d.Code.Contains(searchKey, StringComparison.Ordinal) || PersianText.Key(d.Name).Contains(searchKey, StringComparison.Ordinal)))
            .OrderBy(d => d.Code, StringComparer.Ordinal)
            .Take(take)
            .Select(d => new DetailAccountView(d.Code, d.Name, d.Type, d.IsCostCenter, d.IsActive))
            .ToList());

    public Task<IReadOnlyList<DetailTypeView>> DetailTypesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DetailTypeView>>((Details()?.Details ?? [])
            .Where(d => d.IsActive && d.Type is not null)
            .GroupBy(d => d.Type!)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new DetailTypeView(g.Key, g.Count()))
            .ToList());

    public Task<LedgerAccountView?> AccountAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult((Chart()?.Accounts ?? []).Where(a => a.Code == code)
            .Select(a => new LedgerAccountView(a.Code, a.Name, a.Level.ToString(), a.ParentCode, a.IsPostable, a.IsActive, a.Detail1Types, a.Detail2Types))
            .FirstOrDefault());

    public Task<DetailAccountView?> DetailAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult((Details()?.Details ?? []).Where(d => d.Code == code)
            .Select(d => new DetailAccountView(d.Code, d.Name, d.Type, d.IsCostCenter, d.IsActive))
            .FirstOrDefault());

    private ChartSnapshot? Chart()
    {
        var pending = InMemoryUnitOfWork.Pending;
        lock (db.Gate)
        {
            return pending.Chart ?? db.Chart;
        }
    }

    private DetailsSnapshot? Details()
    {
        var pending = InMemoryUnitOfWork.Pending;
        lock (db.Gate)
        {
            return pending.Details ?? db.Details;
        }
    }

    private static void CheckVersion(long? stored, long loaded, string what)
    {
        if ((stored ?? 0) != loaded)
        {
            throw new ConcurrencyConflictException($"The {what} was changed by someone else.");
        }
    }
}
