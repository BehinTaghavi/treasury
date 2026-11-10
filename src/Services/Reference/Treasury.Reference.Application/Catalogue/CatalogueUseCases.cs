using System.Globalization;
using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Application.Catalogue;

public sealed record AddCatalogueItemCommand(string IdempotencyKey, CatalogueKind Kind, string Name, Guid? ParentId = null, string? Code = null);

/// <param name="ExpectedVersion">The version the user saw; a save over someone else's newer change is refused.</param>
public sealed record RenameCatalogueItemCommand(string IdempotencyKey, Guid Id, string Name, long ExpectedVersion);

public sealed record SetCatalogueItemActiveCommand(string IdempotencyKey, Guid Id, bool Active, long ExpectedVersion);

public sealed record CatalogueItemView(Guid Id, string Kind, Guid? ParentId, string Name, string? Code, bool IsActive, long Version)
{
    public static CatalogueItemView From(CatalogueItem i) => new(i.Id, i.Kind.ToString(), i.ParentId, i.Name, i.Code, i.IsActive, i.Version);
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
            if (await items.NameTakenAsync(item.Kind, item.ParentId, item.NameKey, null, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Catalogue.NameTaken", $"'{item.Name}' is already in this list.");
            }

            if (item.Code is { } code && await items.CurrencyCodeTakenAsync(code, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Catalogue.CodeTaken", $"Currency {code} already exists.");
            }

            await items.AddAsync(item, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(item.Id);
        }, cancellationToken);

    public Task<Result<Guid>> RenameAsync(RenameCatalogueItemCommand command, CancellationToken cancellationToken) =>
        ChangeAsync(command.IdempotencyKey, command.Id, command.ExpectedVersion, async (item, ct) =>
        {
            item.Rename(command.Name);
            if (await items.NameTakenAsync(item.Kind, item.ParentId, item.NameKey, item.Id, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("Catalogue.NameTaken", $"'{item.Name}' is already in this list.");
            }

            return null;
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
