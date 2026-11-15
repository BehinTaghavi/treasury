using Treasury.Reference.Domain.Purposes;

namespace Treasury.Reference.Application.Abstractions;

/// <summary>Purpose groups (گروه بابت) and purposes (بابت). Names are compared on PersianText.Key.</summary>
public interface IPurposeRepository
{
    Task<PurposeGroup?> GetGroupAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PurposeGroup>> ListGroupsAsync(PurposeDirection? direction, bool includeInactive, CancellationToken cancellationToken);

    /// <summary>Is the description already used by another group of the same direction?</summary>
    Task<bool> GroupNameTakenAsync(PurposeDirection direction, string nameKey, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>The number the next group gets: one more than the highest so far, 1 for the first.</summary>
    Task<int> NextGroupNumberAsync(CancellationToken cancellationToken);

    Task<bool> GroupHasPurposesAsync(Guid groupId, CancellationToken cancellationToken);

    Task AddGroupAsync(PurposeGroup group, CancellationToken cancellationToken);

    Task SaveGroupAsync(PurposeGroup group, CancellationToken cancellationToken);

    Task DeleteGroupAsync(PurposeGroup group, CancellationToken cancellationToken);

    Task<Purpose?> GetPurposeAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Purpose>> ListPurposesAsync(Guid? groupId, bool includeInactive, CancellationToken cancellationToken);

    Task<bool> PurposeNameTakenAsync(Guid groupId, string nameKey, Guid? exceptId, CancellationToken cancellationToken);

    Task AddPurposeAsync(Purpose purpose, CancellationToken cancellationToken);

    Task SavePurposeAsync(Purpose purpose, CancellationToken cancellationToken);

    Task DeletePurposeAsync(Purpose purpose, CancellationToken cancellationToken);
}
