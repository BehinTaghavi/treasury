using Treasury.BuildingBlocks.Application;
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
    /// <param name="costCentersOnly">Only تفصیل marked as cost centres (departments and requesters choose from them).</param>
    /// <param name="skip">Rows to leave out first (page by page browsing of the whole list, owner's report no. 2, item 2).</param>
    Task<IReadOnlyList<DetailAccountView>> SearchDetailsAsync(string searchKey, string? type, bool includeInactive, int take, CancellationToken cancellationToken, bool costCentersOnly = false, int skip = 0);

    /// <summary>How many تفصیل match the same filters as <see cref="SearchDetailsAsync"/>.</summary>
    Task<int> CountDetailsAsync(string searchKey, string? type, bool includeInactive, bool costCentersOnly, CancellationToken cancellationToken);

    Task<IReadOnlyList<DetailTypeView>> DetailTypesAsync(CancellationToken cancellationToken);

    Task<LedgerAccountView?> AccountAsync(string code, CancellationToken cancellationToken);

    Task<DetailAccountView?> DetailAsync(string code, CancellationToken cancellationToken);
}

/// <summary>Turns an export file of the accounting system into rows (Excel adapter; a stand-in in offline builds).</summary>
public interface ILedgerExportReader
{
    IReadOnlyList<Treasury.Reference.Domain.Ledger.ChartRow> ReadChart(Stream file);

    IReadOnlyList<Treasury.Reference.Domain.Ledger.DetailRow> ReadDetails(Stream file);
}
