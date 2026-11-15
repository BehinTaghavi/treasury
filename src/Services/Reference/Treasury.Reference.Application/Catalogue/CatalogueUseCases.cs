using System.Globalization;
using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Application.Catalogue;

public sealed record AddCatalogueItemCommand(string IdempotencyKey, CatalogueKind Kind, string Name, Guid? ParentId = null, string? Code = null);

/// <param name="ExpectedVersion">The version the user saw; a save over someone else's newer change is refused.</param>
public sealed record RenameCatalogueItemCommand(string IdempotencyKey, Guid Id, string Name, long ExpectedVersion);

/// <summary>Corrects an entry. <paramref name="ParentId"/> and <paramref name="Code"/> null: unchanged.</summary>
public sealed record EditCatalogueItemCommand(string IdempotencyKey, Guid Id, string Name, long ExpectedVersion, Guid? ParentId = null, string? Code = null);

public sealed record DeleteCatalogueItemCommand(string IdempotencyKey, Guid Id, long ExpectedVersion);

/// <summary>Sent by another service before it saves data that refers to the entry (see <see cref="CatalogueItem.IsInUse"/>).</summary>
public sealed record MarkCatalogueItemInUseCommand(string IdempotencyKey, Guid Id);

public sealed record SetCatalogueItemActiveCommand(string IdempotencyKey, Guid Id, bool Active, long ExpectedVersion);

public sealed record CatalogueItemView(Guid Id, string Kind, Guid? ParentId, string Name, string? Code, bool IsActive, long Version, bool IsInUse)
{
    public static CatalogueItemView From(CatalogueItem i) => new(i.Id, i.Kind.ToString(), i.ParentId, i.Name, i.Code, i.IsActive, i.Version, i.IsInUse);
}

/// <summary>Use cases of the Basic Information lists. Each write is one command in one transaction.</summary>
public sealed class CatalogueUseCases(IUnitOfWork unitOfWork, ICatalogueRepository items, IIdempotencyStore idempotency)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    private static readonly StringComparer PersianOrder = StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true);

    public Task<Result<Guid>> AddAsync(AddCatalogueItemCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            CatalogueItem? parent = null;
            if (command.ParentId is { } parentId)
            {
                parent = await items.GetAsync(parentId, ct).ConfigureAwait(false);
                if (parent is null)
                {
                    return Result<Guid>.Fail("Catalogue.ParentNotFound", $"Entry {parentId} was not found.");
                }
            }

            var item = CatalogueItem.Add(Guid.NewGuid(), command.Kind, command.Name, parent, command.Code);
            if (await DuplicateAsync(item, ct).ConfigureAwait(false) is { } duplicate)
            {
                return duplicate;
            }

            if (item.Code is { } code && await items.CurrencyCodeTakenAsync(code, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Catalogue.CodeTaken", $"Currency {code} already exists.");
            }

            await items.AddAsync(item, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(item.Id);
        }, cancellationToken);

    public Task<Result<Guid>> RenameAsync(RenameCatalogueItemCommand command, CancellationToken cancellationToken) =>
        EditAsync(new EditCatalogueItemCommand(command.IdempotencyKey, command.Id, command.Name, command.ExpectedVersion), cancellationToken);

    /// <summary>Corrects the name, the parent (province of a city, country of a province) or the currency code.</summary>
    public Task<Result<Guid>> EditAsync(EditCatalogueItemCommand command, CancellationToken cancellationToken) =>
        ChangeAsync(command.IdempotencyKey, command.Id, command.ExpectedVersion, async (item, ct) =>
        {
            CatalogueItem? parent = null;
            if (command.ParentId is { } parentId && parentId != item.ParentId)
            {
                parent = await items.GetAsync(parentId, ct).ConfigureAwait(false);
                if (parent is null)
                {
                    return Result<Guid>.Fail("Catalogue.ParentNotFound", $"Entry {parentId} was not found.");
                }
            }

            var oldCode = item.Code;
            item.Edit(command.Name, parent, command.Code);
            if (item.Code is { } code && code != oldCode && await items.CurrencyCodeTakenAsync(code, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Catalogue.CodeTaken", $"Currency {code} already exists.");
            }

            return await DuplicateAsync(item, ct).ConfigureAwait(false);
        }, cancellationToken);

    /// <summary>Deletes an entry nothing refers to (owner, 11 Nov 2026, items 6 and 9); otherwise it can only be switched off.</summary>
    public Task<Result<Guid>> DeleteAsync(DeleteCatalogueItemCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var item = await items.GetAsync(command.Id, ct).ConfigureAwait(false);
            if (item is null)
            {
                return Result<Guid>.Fail("Catalogue.NotFound", $"Entry {command.Id} was not found.");
            }

            if (item.Version != command.ExpectedVersion)
            {
                throw new ConcurrencyConflictException($"Entry {item.Id} was changed by someone else.");
            }

            var hasChildren = ChildKind(item.Kind) is { } childKind
                && (await items.ListAsync(childKind, item.Id, includeInactive: true, ct).ConfigureAwait(false)).Count > 0;
            item.Delete(hasChildren);
            await items.DeleteAsync(item, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(item.Id);
        }, cancellationToken);

    /// <summary>
    /// Called by another service (accounts-svc for the bank, city and currency of a bank account) before it saves data that
    /// refers to the entry, so the entry can no longer be deleted. If that save then fails, the entry merely stays undeletable.
    /// </summary>
    public Task<Result<Guid>> MarkInUseAsync(MarkCatalogueItemInUseCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var item = await items.GetAsync(command.Id, ct).ConfigureAwait(false);
            if (item is null)
            {
                return Result<Guid>.Fail("Catalogue.NotFound", $"Entry {command.Id} was not found.");
            }

            if (item.MarkInUse())
            {
                await items.SaveAsync(item, ct).ConfigureAwait(false);
            }

            return Result<Guid>.Ok(item.Id);
        }, cancellationToken);

    public Task<Result<Guid>> SetActiveAsync(SetCatalogueItemActiveCommand command, CancellationToken cancellationToken) =>
        ChangeAsync(command.IdempotencyKey, command.Id, command.ExpectedVersion, (item, _) =>
        {
            if (command.Active)
            {
                item.Activate();
            }
            else
            {
                item.Deactivate();
            }

            return Task.FromResult<Result<Guid>?>(null);
        }, cancellationToken);

    /// <summary>Entries of one list in Persian alphabetical order; inactive ones only when asked (the Basic Information screen shows them, forms do not).</summary>
    public Task<IReadOnlyList<CatalogueItemView>> ListAsync(CatalogueKind kind, Guid? parentId, bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<CatalogueItemView>>(async ct =>
        {
            var list = await items.ListAsync(kind, parentId, includeInactive, ct).ConfigureAwait(false);
            return list.OrderBy(i => i.Name, PersianOrder).Select(CatalogueItemView.From).ToList();
        }, cancellationToken);

    /// <summary>One entry of any list, active or not (asked by other services, e.g. the bank of a bank account).</summary>
    public Task<CatalogueItemView?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct => await items.GetAsync(id, ct).ConfigureAwait(false) is { } i ? CatalogueItemView.From(i) : null, cancellationToken);

    private static CatalogueKind? ChildKind(CatalogueKind kind) => kind switch
    {
        CatalogueKind.Country => CatalogueKind.Province,
        CatalogueKind.Province => CatalogueKind.City,
        _ => null,
    };

    /// <summary>The same name under the same parent; for bank names also the same name without the word "بانک".</summary>
    private async Task<Result<Guid>?> DuplicateAsync(CatalogueItem item, CancellationToken ct)
    {
        if (await items.NameTakenAsync(item.Kind, item.ParentId, item.NameKey, item.Id, ct).ConfigureAwait(false))
        {
            return Result<Guid>.Fail("Catalogue.NameTaken", $"'{item.Name}' is already in this list.");
        }

        if (item.Kind == CatalogueKind.Bank
            && (await items.ListAsync(CatalogueKind.Bank, null, includeInactive: true, ct).ConfigureAwait(false))
                .FirstOrDefault(b => b.Id != item.Id && b.MatchKey == item.MatchKey) is { } similar)
        {
            // The exact name can turn up here too, when someone else saved it just after the check above.
            return similar.NameKey == item.NameKey
                ? Result<Guid>.Fail("Catalogue.NameTaken", $"'{item.Name}' is already in this list.")
                : Result<Guid>.Fail("Catalogue.SimilarBankName", $"'{item.Name}' is the same bank as '{similar.Name}'.");
        }

        return null;
    }

    private Task<Result<Guid>> ChangeAsync(
        string idempotencyKey, Guid id, long expectedVersion, Func<CatalogueItem, CancellationToken, Task<Result<Guid>?>> change, CancellationToken cancellationToken) =>
        RunAsync(idempotencyKey, async ct =>
        {
            var item = await items.GetAsync(id, ct).ConfigureAwait(false);
            if (item is null)
            {
                return Result<Guid>.Fail("Catalogue.NotFound", $"Entry {id} was not found.");
            }

            if (item.Version != expectedVersion)
            {
                throw new ConcurrencyConflictException($"Entry {id} was changed by someone else.");
            }

            var refused = await change(item, ct).ConfigureAwait(false);
            if (refused is not null)
            {
                return refused;
            }

            await items.SaveAsync(item, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(item.Id);
        }, cancellationToken);

    private Task<Result<Guid>> RunAsync(string idempotencyKey, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken cancellationToken) =>
        _runner.RunAsync(idempotencyKey, work, id => id.ToString(), Guid.Parse, cancellationToken);
}
