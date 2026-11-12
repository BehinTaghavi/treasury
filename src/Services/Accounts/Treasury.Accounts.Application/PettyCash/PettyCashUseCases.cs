using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Domain.PettyCash;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Application.PettyCash;

/// <summary>The petty cash form as sent by the screen.</summary>
public sealed record PettyCashForm(
    string HolderDetailCode, string LedgerAccountCode, decimal Amount, decimal SettleLimit, decimal PayCeiling,
    IReadOnlyList<Guid>? AllowedPurposeGroupIds, PettyCashRecovery Recovery);

public sealed record PettyCashView(
    Guid Id, string HolderDetailCode, string? HolderName, string LedgerAccountCode, decimal Amount, decimal SettleLimit, decimal PayCeiling,
    IReadOnlyList<Guid> AllowedPurposeGroupIds, PettyCashRecovery Recovery, bool IsActive, long Version);

/// <summary>Petty cash holders (manual item 11). Each write is one command in one transaction on one petty cash.</summary>
public sealed class PettyCashUseCases(IUnitOfWork unitOfWork, IPettyCashRepository funds, IReferenceLookup reference, IIdempotencyStore idempotency)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> RegisterAsync(string key, PettyCashForm form, CancellationToken cancellationToken) =>
        Run(key, async ct =>
        {
            var (data, ledger, groups) = await PrepareAsync(form, ct).ConfigureAwait(false);
            var fund = PettyCashFund.Register(Guid.NewGuid(), data, ledger, groups);
            if (await funds.HolderTakenAsync(fund.Data.HolderDetailCode, null, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("PettyCash.HolderTaken", "This holder already has a petty cash.");
            }

            await MarkGroupsAsync(fund.Data.AllowedPurposeGroupIds, ct).ConfigureAwait(false);
            await funds.AddAsync(fund, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(fund.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeAsync(string key, Guid id, PettyCashForm form, long expectedVersion, CancellationToken cancellationToken) =>
        Run(key, async ct =>
        {
            var fund = await LoadAsync(id, expectedVersion, ct).ConfigureAwait(false);
            var (data, ledger, groups) = await PrepareAsync(form, ct).ConfigureAwait(false);
            fund.Change(data, ledger, groups);
            if (await funds.HolderTakenAsync(fund.Data.HolderDetailCode, fund.Id, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("PettyCash.HolderTaken", "This holder already has a petty cash.");
            }

            await MarkGroupsAsync(fund.Data.AllowedPurposeGroupIds, ct).ConfigureAwait(false);
            await funds.SaveAsync(fund, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(fund.Id);
        }, cancellationToken);

    public Task<Result<Guid>> SetActiveAsync(string key, Guid id, bool active, long expectedVersion, CancellationToken cancellationToken) =>
        Run(key, async ct =>
        {
            var fund = await LoadAsync(id, expectedVersion, ct).ConfigureAwait(false);
            if (active)
            {
                fund.Activate();
            }
            else
            {
                fund.Deactivate();
            }

            await funds.SaveAsync(fund, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(fund.Id);
        }, cancellationToken);

    /// <summary>Petty cashes with the name of each holder's تفصیل (asked from reference-svc; shown without it when it cannot answer).</summary>
    public async Task<IReadOnlyList<PettyCashView>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var list = await unitOfWork.ExecuteAsync(ct => funds.ListAsync(includeInactive, ct), cancellationToken).ConfigureAwait(false);
        var views = new List<PettyCashView>();
        foreach (var f in list.OrderBy(f => f.Data.HolderDetailCode, StringComparer.Ordinal))
        {
            string? name = null;
            try
            {
                name = await reference.DetailNameAsync(f.Data.HolderDetailCode, cancellationToken).ConfigureAwait(false);
            }
            catch (DomainException)
            {
                // reference-svc cannot answer now.
            }

            var d = f.Data;
            views.Add(new(f.Id, d.HolderDetailCode, name, d.LedgerAccountCode, d.Amount, d.SettleLimit, d.PayCeiling, d.AllowedPurposeGroupIds, d.Recovery, f.IsActive, f.Version));
        }

        return views;
    }

    private async Task<PettyCashFund> LoadAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var fund = await funds.GetAsync(id, ct).ConfigureAwait(false) ?? throw new DomainException("PettyCash.NotFound", $"Petty cash {id} was not found.");
        return fund.Version == expectedVersion ? fund : throw new ConcurrencyConflictException($"Petty cash {id} was changed by someone else.");
    }

    /// <summary>The allowed purpose groups become "in use" in reference-svc, just before the save (as banks for bank accounts).</summary>
    private async Task MarkGroupsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        foreach (var id in ids)
        {
            if (!await reference.MarkInUseAsync("purpose-groups", id, ct).ConfigureAwait(false))
            {
                throw new DomainException("PettyCash.PurposeGroupNotFound", "A purpose group is not in the list.");
            }
        }
    }

    private async Task<(PettyCashData Data, LedgerFacts Ledger, Dictionary<Guid, PurposeGroupFact> Groups)> PrepareAsync(PettyCashForm form, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(form);
        var accounts = new Dictionary<string, LedgerAccountFact>();
        if (PersianText.Clean(form.LedgerAccountCode) is { Length: > 0 } code && await reference.LedgerAccountAsync(code, ct).ConfigureAwait(false) is { } account)
        {
            accounts[code] = account;
        }

        var details = new Dictionary<string, DetailFact>();
        if (PersianText.Clean(form.HolderDetailCode) is { Length: > 0 } holder && await reference.DetailAsync(holder, ct).ConfigureAwait(false) is { } detail)
        {
            details[holder] = detail;
        }

        var groups = new Dictionary<Guid, PurposeGroupFact>();
        foreach (var id in (form.AllowedPurposeGroupIds ?? []).Distinct())
        {
            if (await reference.PurposeGroupAsync(id, ct).ConfigureAwait(false) is { } g)
            {
                groups[id] = g;
            }
        }

        var data = new PettyCashData(form.HolderDetailCode, form.LedgerAccountCode, form.Amount, form.SettleLimit, form.PayCeiling, form.AllowedPurposeGroupIds ?? [], form.Recovery);
        return (data, new LedgerFacts(accounts, details), groups);
    }

    private Task<Result<Guid>> Run(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
