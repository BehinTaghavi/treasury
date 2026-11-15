using System.Text.Json;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Ledger;

namespace Treasury.Reference.Application.Ledger;

public sealed record CodeFormatView(int Group, int General, int Subsidiary, int SubSubsidiary, int Detail, bool Locked);

public sealed record SetCodeFormatCommand(string IdempotencyKey, int Group, int General, int Subsidiary, int SubSubsidiary, int Detail);

public sealed record SetDetailTypesCommand(string IdempotencyKey, string Code, IReadOnlyList<string> Detail1Types, IReadOnlyList<string> Detail2Types);

/// <summary>
/// The copy of the accounting system's accounts (owner, 10 Nov 2026): code lengths per company, chart of accounts and
/// تفصیل list imported from the accounting system's Excel exports, and the تفصیل types each account needs.
/// The import files are read by an adapter (Excel); these use cases receive plain rows.
/// </summary>
public sealed class LedgerUseCases(IUnitOfWork unitOfWork, ILedgerRepository ledger, ILedgerQueries queries, IIdempotencyStore idempotency)
{
    public const int MaxSearchResults = 1000;

    private static readonly JsonSerializerOptions Json = new();
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<CodeFormatView?> GetCodeFormatAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var chart = await ledger.GetChartAsync(ct).ConfigureAwait(false);
            if (chart is null)
            {
                return null;
            }

            var f = chart.Format;
            return new CodeFormatView(f.GroupLength, f.GeneralLength, f.SubsidiaryLength, f.SubSubsidiaryLength, f.DetailLength, chart.Accounts.Count > 0);
        }, cancellationToken);

    /// <summary>Set once when the system is set up for a company; locked after the first import.</summary>
    public Task<Result<bool>> SetCodeFormatAsync(SetCodeFormatCommand command, CancellationToken cancellationToken) =>
        _runner.RunAsync(command.IdempotencyKey, async ct =>
        {
            var format = AccountCodeFormat.Create(command.Group, command.General, command.Subsidiary, command.SubSubsidiary, command.Detail);
            var chart = await ledger.GetChartAsync(ct).ConfigureAwait(false);
            if (chart is null)
            {
                chart = ChartOfAccounts.Create(Guid.NewGuid(), format);
            }
            else
            {
                chart.ChangeFormat(format);
            }

            await ledger.SaveChartAsync(chart, ct).ConfigureAwait(false);
            return Result<bool>.Ok(true);
        }, v => v.ToString(), bool.Parse, cancellationToken);

    /// <summary>What importing these rows would do; nothing is saved (shown to the user before they confirm).</summary>
    public Task<Result<ImportReport>> PreviewChartAsync(IReadOnlyList<ChartRow> rows, CancellationToken cancellationToken) =>
        ReadAsync(async ct => (await RequireChartAsync(ct).ConfigureAwait(false)).Preview(rows), cancellationToken);

    public Task<Result<ImportReport>> ImportChartAsync(string idempotencyKey, IReadOnlyList<ChartRow> rows, CancellationToken cancellationToken) =>
        _runner.RunAsync(idempotencyKey, async ct =>
        {
            var chart = await RequireChartAsync(ct).ConfigureAwait(false);
            var report = chart.Import(rows);
            await ledger.SaveChartAsync(chart, ct).ConfigureAwait(false);
            return Result<ImportReport>.Ok(report);
        }, Serialize, Deserialize, cancellationToken);

    public Task<Result<ImportReport>> PreviewDetailsAsync(IReadOnlyList<DetailRow> rows, CancellationToken cancellationToken) =>
        ReadAsync(async ct =>
        {
            var chart = await RequireChartAsync(ct).ConfigureAwait(false);
            var directory = await ledger.GetDetailsAsync(ct).ConfigureAwait(false) ?? DetailAccountDirectory.Create(Guid.NewGuid());
            return directory.Preview(rows, chart.Format);
        }, cancellationToken);

    public Task<Result<ImportReport>> ImportDetailsAsync(string idempotencyKey, IReadOnlyList<DetailRow> rows, CancellationToken cancellationToken) =>
        _runner.RunAsync(idempotencyKey, async ct =>
        {
            var chart = await RequireChartAsync(ct).ConfigureAwait(false);
            var directory = await ledger.GetDetailsAsync(ct).ConfigureAwait(false) ?? DetailAccountDirectory.Create(Guid.NewGuid());
            var report = directory.Import(rows, chart.Format);
            await ledger.SaveDetailsAsync(directory, ct).ConfigureAwait(false);
            return Result<ImportReport>.Ok(report);
        }, Serialize, Deserialize, cancellationToken);

    public Task<Result<bool>> SetDetailTypesAsync(SetDetailTypesCommand command, CancellationToken cancellationToken) =>
        _runner.RunAsync(command.IdempotencyKey, async ct =>
        {
            var chart = await RequireChartAsync(ct).ConfigureAwait(false);
            var directory = await ledger.GetDetailsAsync(ct).ConfigureAwait(false);
            chart.SetDetailTypes(command.Code, command.Detail1Types, command.Detail2Types, directory?.ActiveTypes ?? new HashSet<string>());
            await ledger.SaveChartAsync(chart, ct).ConfigureAwait(false);
            return Result<bool>.Ok(true);
        }, v => v.ToString(), bool.Parse, cancellationToken);

    public Task<IReadOnlyList<LedgerAccountView>> AccountsAsync(bool postableOnly, bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => queries.AccountsAsync(postableOnly, includeInactive, ct), cancellationToken);

    /// <summary>Search by code or name (Arabic/Persian letters alike), optionally of one type; at most <see cref="MaxSearchResults"/> rows.</summary>
    /// <param name="roles">Only تفصیل of these roles (staff, cost centres, organisational units: owner, 13 Nov 2026); null or empty: any.</param>
    /// <param name="costCentersOnly">Same as roles = cost centres: marked as cost centres or of a cost-centre type.</param>
    public Task<IReadOnlyList<DetailAccountView>> SearchDetailsAsync(
        string? search, string? type, bool includeInactive, int take, CancellationToken cancellationToken, bool costCentersOnly = false, int skip = 0,
        IReadOnlyCollection<DetailRole>? roles = null) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var (types, centers) = await RoleFilterAsync(roles, costCentersOnly, ct).ConfigureAwait(false);
            return await queries.SearchDetailsAsync(PersianText.Key(search), TypeOrNull(type), includeInactive, Math.Clamp(take, 1, MaxSearchResults), ct, centers, Math.Max(0, skip), types)
                .ConfigureAwait(false);
        }, cancellationToken);

    /// <summary>The number of تفصیل the same search finds, for "1–100 of 9,628" and the next/previous page buttons.</summary>
    public Task<int> CountDetailsAsync(
        string? search, string? type, bool includeInactive, bool costCentersOnly, CancellationToken cancellationToken, IReadOnlyCollection<DetailRole>? roles = null) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var (types, centers) = await RoleFilterAsync(roles, costCentersOnly, ct).ConfigureAwait(false);
            return await queries.CountDetailsAsync(PersianText.Key(search), TypeOrNull(type), includeInactive, centers, ct, types).ConfigureAwait(false);
        }, cancellationToken);

    /// <summary>The types that have the asked roles; marked cost centres count when cost centres are asked.</summary>
    private async Task<(IReadOnlyList<string>? Types, bool Centers)> RoleFilterAsync(IReadOnlyCollection<DetailRole>? roles, bool costCentersOnly, CancellationToken ct)
    {
        var asked = (roles ?? []).ToHashSet();
        if (costCentersOnly)
        {
            asked.Add(DetailRole.CostCentre);
        }

        if (asked.Count == 0)
        {
            return (null, false);
        }

        var types = (await queries.DetailTypesAsync(ct).ConfigureAwait(false))
            .Where(t => DetailRoles.Of(t.Type) is { } r && asked.Contains(r))
            .Select(t => t.Type)
            .ToList();
        return (types, asked.Contains(DetailRole.CostCentre));
    }

    private static string? TypeOrNull(string? type) => string.IsNullOrWhiteSpace(type) ? null : PersianText.Clean(type);

    /// <summary>One account by code (asked by accounts-svc when a bank account names it).</summary>
    public Task<LedgerAccountView?> AccountAsync(string code, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => queries.AccountAsync(code.Trim(), ct), cancellationToken);

    public Task<DetailAccountView?> DetailAsync(string code, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => queries.DetailAsync(code.Trim(), ct), cancellationToken);

    public Task<IReadOnlyList<DetailTypeView>> DetailTypesAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => queries.DetailTypesAsync(ct), cancellationToken);

    private async Task<ChartOfAccounts> RequireChartAsync(CancellationToken ct) =>
        await ledger.GetChartAsync(ct).ConfigureAwait(false)
        ?? throw new DomainException("Ledger.FormatNotSet", "Set the code lengths of the accounts first.");

    /// <summary>A preview reads only, but still turns rule violations into results.</summary>
    private async Task<Result<ImportReport>> ReadAsync(Func<CancellationToken, Task<ImportReport>> work, CancellationToken cancellationToken)
    {
        try
        {
            return Result<ImportReport>.Ok(await unitOfWork.ExecuteAsync(work, cancellationToken).ConfigureAwait(false));
        }
        catch (DomainException ex)
        {
            return Result<ImportReport>.From(ex);
        }
    }

    private static string Serialize(ImportReport report) => JsonSerializer.Serialize(report, Json);

    private static ImportReport Deserialize(string text) => JsonSerializer.Deserialize<ImportReport>(text, Json)!;
}
