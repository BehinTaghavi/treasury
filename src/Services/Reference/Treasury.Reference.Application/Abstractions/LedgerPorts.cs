using Treasury.Reference.Domain.Ledger;

namespace Treasury.Reference.Application.Abstractions;

/// <summary>Storage of the two ledger aggregates (one of each per company database).</summary>
public interface ILedgerRepository
{
    Task<ChartOfAccounts?> GetChartAsync(CancellationToken cancellationToken);

    Task<DetailAccountDirectory?> GetDetailsAsync(CancellationToken cancellationToken);

    /// <summary>Writes a new chart, or the changed accounts of a loaded one. Throws <see cref="ConcurrencyConflictException"/> if saved by someone else meanwhile.</summary>
    Task SaveChartAsync(ChartOfAccounts chart, CancellationToken cancellationToken);

    Task SaveDetailsAsync(DetailAccountDirectory directory, CancellationToken cancellationToken);
}

public sealed record LedgerAccountView(string Code, string Name, string Level, string? ParentCode, bool IsPostable, bool IsActive, IReadOnlyList<string> Detail1Types, IReadOnlyList<string> Detail2Types);

public sealed record DetailAccountView(string Code, string Name, string? Type, bool IsCostCenter, bool IsActive);

public sealed record DetailTypeView(string Type, int ActiveCount);

/// <summary>Read side of the ledger copy: lists and searches without loading the whole aggregate (9,600 تفصیل at Iran Nazho Co.).</summary>
public interface ILedgerQueries
{
    Task<IReadOnlyList<LedgerAccountView>> AccountsAsync(bool postableOnly, bool includeInactive, CancellationToken cancellationToken);

    /// <param name="searchKey">PersianText.Key of the search text; matched against code and name. Empty: no filter.</param>
    Task<IReadOnlyList<DetailAccountView>> SearchDetailsAsync(string searchKey, string? type, bool includeInactive, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<DetailTypeView>> DetailTypesAsync(CancellationToken cancellationToken);
}
