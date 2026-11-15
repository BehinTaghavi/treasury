using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Application.Abstractions;

public interface ICatalogueRepository
{
    Task<CatalogueItem?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Is <paramref name="nameKey"/> (see PersianText.Key) already used by another entry of the same list and parent?</summary>
    Task<bool> NameTakenAsync(CatalogueKind kind, Guid? parentId, string nameKey, Guid? exceptId, CancellationToken cancellationToken);

    Task<bool> CurrencyCodeTakenAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(CatalogueItem item, CancellationToken cancellationToken);

    /// <summary>Saves changes and pending events. Throws <see cref="ConcurrencyConflictException"/> if someone else saved first.</summary>
    Task SaveAsync(CatalogueItem item, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogueItem>> ListAsync(CatalogueKind kind, Guid? parentId, bool includeInactive, CancellationToken cancellationToken);
}
