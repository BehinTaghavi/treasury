using System.Globalization;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Purposes;

namespace Treasury.Reference.Application.Purposes;

/// <summary>The purpose group form. Reference ids null: none.</summary>
public sealed record PurposeGroupForm(PurposeDirection Direction, string Name, string LedgerAccountCode, Guid? Reference1Id, Guid? Reference2Id);

public sealed record AddPurposeGroupCommand(string IdempotencyKey, PurposeGroupForm Form);

public sealed record EditPurposeGroupCommand(string IdempotencyKey, Guid Id, PurposeGroupForm Form, long ExpectedVersion);

public sealed record AddPurposeCommand(string IdempotencyKey, Guid GroupId, string Name);

public sealed record EditPurposeCommand(string IdempotencyKey, Guid Id, Guid GroupId, string Name, long ExpectedVersion);

/// <summary>Switch on/off or delete; <paramref name="Change"/> is "activate", "deactivate" or "delete".</summary>
public sealed record ChangePurposeStateCommand(string IdempotencyKey, Guid Id, string Change, long ExpectedVersion);

public sealed record PurposeGroupView(
    Guid Id, int Number, PurposeDirection Direction, string Name, string LedgerAccountCode, Guid? Reference1Id, Guid? Reference2Id,
    bool IsActive, bool IsInUse, long Version)
{
    public static PurposeGroupView From(PurposeGroup g) =>
        new(g.Id, g.Number, g.Direction, g.Name, g.LedgerAccountCode, g.Reference1Id, g.Reference2Id, g.IsActive, g.IsInUse, g.Version);
}

public sealed record PurposeView(Guid Id, Guid GroupId, string Name, bool IsActive, bool IsInUse, long Version)
{
    public static PurposeView From(Purpose p) => new(p.Id, p.GroupId, p.Name, p.IsActive, p.IsInUse, p.Version);
}

/// <summary>
/// Purpose groups (new-version manual item 9) and their purposes. Each write is one command in one transaction on one
/// aggregate. The references a group names are first marked "in use" by their own command (as accounts-svc does for banks),
/// so a reference a group uses can no longer be deleted.
/// </summary>
public sealed class PurposeUseCases(
    IUnitOfWork unitOfWork, IPurposeRepository purposes, ICatalogueRepository catalogue, ILedgerQueries ledger, IIdempotencyStore idempotency,
    CatalogueUseCases catalogueUseCases)
{
    private static readonly StringComparer PersianOrder = StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true);
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public async Task<Result<Guid>> AddGroupAsync(AddPurposeGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (await MarkReferencesAsync(command.Form, cancellationToken).ConfigureAwait(false) is { } refused)
        {
            return refused;
        }

        return await RunAsync(command.IdempotencyKey, async ct =>
        {
            var (fact, r1, r2) = await FactsAsync(command.Form, ct).ConfigureAwait(false);
            var number = await purposes.NextGroupNumberAsync(ct).ConfigureAwait(false);
            var group = PurposeGroup.Register(Guid.NewGuid(), number, command.Form.Direction, command.Form.Name, command.Form.LedgerAccountCode, fact, r1, r2);
            if (await purposes.GroupNameTakenAsync(group.Direction, group.NameKey, null, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("PurposeGroup.NameTaken", $"'{group.Name}' is already a group of this kind.");
            }

            await purposes.AddGroupAsync(group, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(group.Id);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<Guid>> EditGroupAsync(EditPurposeGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (await MarkReferencesAsync(command.Form, cancellationToken).ConfigureAwait(false) is { } refused)
        {
            return refused;
        }

        return await RunAsync(command.IdempotencyKey, async ct =>
        {
            var group = await LoadGroupAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var (fact, r1, r2) = await FactsAsync(command.Form, ct).ConfigureAwait(false);
            group.Edit(command.Form.Direction, command.Form.Name, command.Form.LedgerAccountCode, fact, r1, r2);
            if (await purposes.GroupNameTakenAsync(group.Direction, group.NameKey, group.Id, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("PurposeGroup.NameTaken", $"'{group.Name}' is already a group of this kind.");
            }

            await purposes.SaveGroupAsync(group, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(group.Id);
        }, cancellationToken).ConfigureAwait(false);
    }

    public Task<Result<Guid>> ChangeGroupStateAsync(ChangePurposeStateCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var group = await LoadGroupAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            switch (command.Change)
            {
                case "activate":
                    group.Activate();
                    break;
                case "deactivate":
                    group.Deactivate();
                    break;
                case "delete":
                    group.Delete(await purposes.GroupHasPurposesAsync(group.Id, ct).ConfigureAwait(false));
                    await purposes.DeleteGroupAsync(group, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(group.Id);
                default:
                    return Result<Guid>.Fail("Request.UnknownChange", $"Unknown change '{command.Change}'.");
            }

            await purposes.SaveGroupAsync(group, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(group.Id);
        }, cancellationToken);

    public Task<Result<Guid>> AddPurposeAsync(AddPurposeCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var group = await purposes.GetGroupAsync(command.GroupId, ct).ConfigureAwait(false);
            var purpose = Purpose.Add(Guid.NewGuid(), group, command.Name);
            if (await purposes.PurposeNameTakenAsync(purpose.GroupId, purpose.NameKey, null, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Purpose.NameTaken", $"'{purpose.Name}' is already in this group.");
            }

            await purposes.AddPurposeAsync(purpose, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(purpose.Id);
        }, cancellationToken);

    public Task<Result<Guid>> EditPurposeAsync(EditPurposeCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var purpose = await LoadPurposeAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            purpose.Edit(command.Name, await purposes.GetGroupAsync(command.GroupId, ct).ConfigureAwait(false));
            if (await purposes.PurposeNameTakenAsync(purpose.GroupId, purpose.NameKey, purpose.Id, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Purpose.NameTaken", $"'{purpose.Name}' is already in this group.");
            }

            await purposes.SavePurposeAsync(purpose, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(purpose.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangePurposeStateAsync(ChangePurposeStateCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var purpose = await LoadPurposeAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            switch (command.Change)
            {
                case "activate":
                    purpose.Activate();
                    break;
                case "deactivate":
                    purpose.Deactivate();
                    break;
                case "delete":
                    purpose.Delete();
                    await purposes.DeletePurposeAsync(purpose, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(purpose.Id);
                default:
                    return Result<Guid>.Fail("Request.UnknownChange", $"Unknown change '{command.Change}'.");
            }

            await purposes.SavePurposeAsync(purpose, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(purpose.Id);
        }, cancellationToken);

    /// <summary>Called by cashflow-svc before it saves a receipt or payment naming the group; afterwards it is never deleted.</summary>
    public Task<Result<Guid>> MarkGroupInUseAsync(string idempotencyKey, Guid id, CancellationToken cancellationToken) =>
        RunAsync(idempotencyKey, async ct =>
        {
            var group = await purposes.GetGroupAsync(id, ct).ConfigureAwait(false);
            if (group is null)
            {
                return Result<Guid>.Fail("PurposeGroup.NotFound", $"Purpose group {id} was not found.");
            }

            if (group.MarkInUse())
            {
                await purposes.SaveGroupAsync(group, ct).ConfigureAwait(false);
            }

            return Result<Guid>.Ok(group.Id);
        }, cancellationToken);

    /// <summary>Called by cashflow-svc before it saves a receipt or payment naming the purpose.</summary>
    public Task<Result<Guid>> MarkPurposeInUseAsync(string idempotencyKey, Guid id, CancellationToken cancellationToken) =>
        RunAsync(idempotencyKey, async ct =>
        {
            var purpose = await purposes.GetPurposeAsync(id, ct).ConfigureAwait(false);
            if (purpose is null)
            {
                return Result<Guid>.Fail("Purpose.NotFound", $"Purpose {id} was not found.");
            }

            if (purpose.MarkInUse())
            {
                await purposes.SavePurposeAsync(purpose, ct).ConfigureAwait(false);
            }

            return Result<Guid>.Ok(purpose.Id);
        }, cancellationToken);

    /// <summary>Groups by number; inactive ones only when asked (forms ask without).</summary>
    public Task<IReadOnlyList<PurposeGroupView>> ListGroupsAsync(PurposeDirection? direction, bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<PurposeGroupView>>(async ct =>
            (await purposes.ListGroupsAsync(direction, includeInactive, ct).ConfigureAwait(false))
                .OrderBy(g => g.Number).Select(PurposeGroupView.From).ToList(),
            cancellationToken);

    /// <summary>Purposes in Persian alphabetical order, of one group or all.</summary>
    public Task<IReadOnlyList<PurposeView>> ListPurposesAsync(Guid? groupId, bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<PurposeView>>(async ct =>
            (await purposes.ListPurposesAsync(groupId, includeInactive, ct).ConfigureAwait(false))
                .OrderBy(p => p.Name, PersianOrder).Select(PurposeView.From).ToList(),
            cancellationToken);

    /// <summary>Looks up the chosen account and references inside the command's transaction.</summary>
    private async Task<(PurposeLedgerFact? Ledger, CatalogueItem? Reference1, CatalogueItem? Reference2)> FactsAsync(PurposeGroupForm form, CancellationToken ct)
    {
        var code = PersianText.Clean(form.LedgerAccountCode);
        var account = code.Length == 0 ? null : await ledger.AccountAsync(code, ct).ConfigureAwait(false);
        var fact = account is null ? null : new PurposeLedgerFact(account.Code, account.IsActive, account.IsPostable);
        var r1 = form.Reference1Id is { } id1 ? await catalogue.GetAsync(id1, ct).ConfigureAwait(false) ?? throw NotFound() : null;
        var r2 = form.Reference2Id is { } id2 ? await catalogue.GetAsync(id2, ct).ConfigureAwait(false) ?? throw NotFound() : null;
        return (fact, r1, r2);

        static DomainException NotFound() => new("PurposeGroup.ReferenceNotFound", "The reference (عطف) is not in the list of references.");
    }

    /// <summary>Marks the chosen references in use (own commands, before the group's transaction). Only real references are marked.</summary>
    private async Task<Result<Guid>?> MarkReferencesAsync(PurposeGroupForm form, CancellationToken ct)
    {
        foreach (var id in new[] { form.Reference1Id, form.Reference2Id }.OfType<Guid>().Distinct())
        {
            var entry = await catalogueUseCases.GetAsync(id, ct).ConfigureAwait(false);
            if (entry is null || entry.Kind != nameof(CatalogueKind.Reference))
            {
                return Result<Guid>.Fail("PurposeGroup.ReferenceNotFound", "The reference (عطف) is not in the list of references.");
            }

            var marked = await catalogueUseCases.MarkInUseAsync(new MarkCatalogueItemInUseCommand($"in-use:{id}", id), ct).ConfigureAwait(false);
            if (!marked.IsSuccess)
            {
                return marked;
            }
        }

        return null;
    }

    private async Task<PurposeGroup> LoadGroupAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var group = await purposes.GetGroupAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("PurposeGroup.NotFound", $"Purpose group {id} was not found.");
        return group.Version == expectedVersion ? group : throw new ConcurrencyConflictException($"Purpose group {id} was changed by someone else.");
    }

    private async Task<Purpose> LoadPurposeAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var purpose = await purposes.GetPurposeAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("Purpose.NotFound", $"Purpose {id} was not found.");
        return purpose.Version == expectedVersion ? purpose : throw new ConcurrencyConflictException($"Purpose {id} was changed by someone else.");
    }

    private Task<Result<Guid>> RunAsync(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
