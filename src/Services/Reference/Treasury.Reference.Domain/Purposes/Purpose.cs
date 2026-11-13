using Treasury.BuildingBlocks.Domain;

namespace Treasury.Reference.Domain.Purposes;

/// <summary>
/// A purpose (بابت) under a purpose group, e.g. "پیش دریافت فروش" under "پیش دریافت". Old system: Access Babs rows with
/// GBid ≠ 0 ("گروه عمومي بابت نامشخص است", "شرح بابت نامشخص است"); MVC Behalfs. Any Treasury user may add one, also from the
/// receipts form (owner, 10 Nov 2026). Receipt or payment comes from its group.
/// </summary>
public sealed class Purpose : AggregateRoot<Guid>
{
    public const int MaxNameLength = 150;

    private Purpose(Guid id, Guid groupId, string name)
        : base(id)
    {
        GroupId = groupId;
        Name = name;
        IsActive = true;
    }

    public Guid GroupId { get; private set; }

    public string Name { get; private set; }

    public string NameKey => PersianText.Key(Name);

    public bool IsActive { get; private set; }

    /// <summary>A receipt or payment names this purpose (recorded by cashflow-svc); then it is never deleted.</summary>
    public bool IsInUse { get; private set; }

    /// <param name="group">The group as loaded by the use case; null when none was chosen or it does not exist.</param>
    public static Purpose Add(Guid id, PurposeGroup? group, string name)
    {
        var purpose = new Purpose(id, ValidGroup(group, null), ValidName(name));
        purpose.Raise(PurposeChanged.From(purpose));
        return purpose;
    }

    /// <summary>Corrects the name or moves the purpose to another group (a newly chosen group must be active).</summary>
    public void Edit(string name, PurposeGroup? group)
    {
        var newName = ValidName(name);
        var newGroup = ValidGroup(group, GroupId);
        if (newName == Name && newGroup == GroupId)
        {
            return;
        }

        Name = newName;
        GroupId = newGroup;
        Raise(PurposeChanged.From(this));
    }

    public void Activate() => SetActive(true);

    public void Deactivate() => SetActive(false);

    public bool MarkInUse()
    {
        if (IsInUse)
        {
            return false;
        }

        IsInUse = true;
        return true;
    }

    /// <summary>
    /// A purpose may be deleted or corrected even after it was used (owner's report no. 2, item 10): receipts, cheques and vouchers
    /// keep the purpose text they were saved with, so their history does not change.
    /// </summary>
    public void Delete() => Raise(new PurposeDeleted(Id));

    public static Purpose Restore(Guid id, Guid groupId, string name, bool isActive, bool isInUse, long version)
    {
        var purpose = new Purpose(id, groupId, name) { IsActive = isActive, IsInUse = isInUse };
        purpose.SetLoadedVersion(version);
        return purpose;
    }

    private void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
        Raise(PurposeChanged.From(this));
    }

    private static Guid ValidGroup(PurposeGroup? group, Guid? current)
    {
        Require(group is not null, "Purpose.GroupRequired", "The purpose group is required."); // Access: "گروه عمومي بابت نامشخص است"
        Require(group!.IsActive || group.Id == current, "Purpose.GroupInactive", "The purpose group is inactive.");
        return group.Id;
    }

    private static string ValidName(string? name)
    {
        var cleaned = PersianText.Clean(name);
        Require(cleaned.Length > 0, "Purpose.NameRequired", "The description of the purpose is required."); // Access: "شرح بابت نامشخص است"
        Require(cleaned.Length <= MaxNameLength, "Purpose.NameTooLong", $"A purpose has at most {MaxNameLength} characters.");
        return cleaned;
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

public sealed record PurposeChanged(Guid PurposeId, Guid GroupId, string Name, bool IsActive) : DomainEvent
{
    public static PurposeChanged From(Purpose p) => new(p.Id, p.GroupId, p.Name, p.IsActive);
}

public sealed record PurposeDeleted(Guid PurposeId) : DomainEvent;
