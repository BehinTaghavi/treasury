using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Domain.Purposes;

/// <summary>Whether a purpose group is used for receipts or for payments (manual item 9 "نوع بابت"; Access Babs.Dp 1/2).</summary>
public enum PurposeDirection
{
    Receipt,
    Payment,
}

/// <summary>What the copy of the chart of accounts says about the account named on the form; null: no such account.</summary>
public sealed record PurposeLedgerFact(string Code, bool IsActive, bool IsPostable);

/// <summary>
/// A purpose group (گروه بابت), new-version manual item 9: receipt or payment, a number, a description, the معین / معین در معین
/// it is posted to, and up to two references (عطف) whose values the receipt or payment carries. Vouchers are built from it
/// (docs/domain/posting-rules.md). Old system: Access Babs rows with GBid = 0 ("شرح بابت نامشخص است"); MVC BehalfGroup and
/// ReferenceTypes_By_BehalfGroup. Its purposes (بابت) are separate aggregates (<see cref="Purpose"/>), because any user may add
/// one from the receipts form at any time.
/// </summary>
public sealed class PurposeGroup : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    private PurposeGroup(Guid id, int number, PurposeDirection direction, string name, string ledgerAccountCode, Guid? reference1Id, Guid? reference2Id)
        : base(id)
    {
        Number = number;
        Direction = direction;
        Name = name;
        LedgerAccountCode = ledgerAccountCode;
        Reference1Id = reference1Id;
        Reference2Id = reference2Id;
        IsActive = true;
    }

    /// <summary>Number shown to users (manual: "شماره گروه"), given in order by the use case.</summary>
    public int Number { get; }

    public PurposeDirection Direction { get; private set; }

    public string Name { get; private set; }

    public string NameKey => PersianText.Key(Name);

    public string LedgerAccountCode { get; private set; }

    public Guid? Reference1Id { get; private set; }

    public Guid? Reference2Id { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>A receipt or payment names this group (recorded by cashflow-svc); then it is never deleted.</summary>
    public bool IsInUse { get; private set; }

    /// <param name="ledger">The account named on the form as found in the chart of accounts; null when not found.</param>
    /// <param name="reference1">The first reference (عطف) as loaded by the use case; null when none was chosen.</param>
    public static PurposeGroup Register(
        Guid id, int number, PurposeDirection direction, string name, string ledgerAccountCode, PurposeLedgerFact? ledger,
        CatalogueItem? reference1 = null, CatalogueItem? reference2 = null)
    {
        Require(number > 0, "PurposeGroup.NumberInvalid", "The group number is a positive number.");
        var group = new PurposeGroup(id, number, direction, ValidName(name), ValidLedger(ledgerAccountCode, ledger), null, null);
        group.SetReferences(reference1, reference2, null, null);
        group.Raise(PurposeGroupChanged.From(group));
        return group;
    }

    /// <summary>Corrects the group. The direction can change only while no receipt or payment uses the group.</summary>
    public void Edit(
        PurposeDirection direction, string name, string ledgerAccountCode, PurposeLedgerFact? ledger, CatalogueItem? reference1, CatalogueItem? reference2)
    {
        var newName = ValidName(name);
        var newLedger = ValidLedger(ledgerAccountCode, ledger, keep: LedgerAccountCode);
        Require(direction == Direction || !IsInUse, "PurposeGroup.DirectionInUse", "The group is already used; receipt or payment can no longer change.");
        var before = (Direction, Name, LedgerAccountCode, Reference1Id, Reference2Id);
        Direction = direction;
        Name = newName;
        LedgerAccountCode = newLedger;
        SetReferences(reference1, reference2, before.Reference1Id, before.Reference2Id);
        if (before != (Direction, Name, LedgerAccountCode, Reference1Id, Reference2Id))
        {
            Raise(PurposeGroupChanged.From(this));
        }
    }

    public void Activate() => SetActive(true);

    /// <summary>Takes the group (and so its purposes) off the receipt and payment forms; what already uses it keeps working.</summary>
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

    /// <summary>Deletes a group typed by mistake: only while unused and without purposes (owner, 11 Nov 2026).</summary>
    public void Delete(bool hasPurposes)
    {
        Require(!IsInUse, "PurposeGroup.InUse", "The group is already used and cannot be deleted; switch it off instead.");
        Require(!hasPurposes, "PurposeGroup.HasPurposes", "Delete the purposes of this group first.");
        Raise(new PurposeGroupDeleted(Id));
    }

    public static PurposeGroup Restore(
        Guid id, int number, PurposeDirection direction, string name, string ledgerAccountCode, Guid? reference1Id, Guid? reference2Id,
        bool isActive, bool isInUse, long version)
    {
        var group = new PurposeGroup(id, number, direction, name, ledgerAccountCode, reference1Id, reference2Id) { IsActive = isActive, IsInUse = isInUse };
        group.SetLoadedVersion(version);
        return group;
    }

    private void SetReferences(CatalogueItem? reference1, CatalogueItem? reference2, Guid? keep1, Guid? keep2)
    {
        CheckReference(reference1, keep1, keep2);
        CheckReference(reference2, keep1, keep2);
        Require(reference2 is null || reference1 is not null, "PurposeGroup.Reference2WithoutReference1", "عطف 2 needs عطف 1.");
        Require(reference1 is null || reference2 is null || reference1.Id != reference2.Id, "PurposeGroup.SameReferenceTwice", "عطف 1 and عطف 2 must differ.");
        Reference1Id = reference1?.Id;
        Reference2Id = reference2?.Id;
    }

    /// <summary>A reference the group already had may stay even if it was switched off since; a newly chosen one must be active.</summary>
    private static void CheckReference(CatalogueItem? reference, Guid? keep1, Guid? keep2)
    {
        if (reference is null)
        {
            return;
        }

        Require(reference.Kind == CatalogueKind.Reference, "PurposeGroup.ReferenceNotFound", "The reference (عطف) is not in the list of references.");
        Require(reference.IsActive || reference.Id == keep1 || reference.Id == keep2, "PurposeGroup.ReferenceInactive", "The reference (عطف) is inactive.");
    }

    private void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
        Raise(PurposeGroupChanged.From(this));
    }

    private static string ValidName(string? name)
    {
        var cleaned = PersianText.Clean(name);
        Require(cleaned.Length > 0, "PurposeGroup.NameRequired", "The description of the group is required."); // Access: "شرح بابت نامشخص است"
        Require(cleaned.Length <= MaxNameLength, "PurposeGroup.NameTooLong", $"A description has at most {MaxNameLength} characters.");
        return cleaned;
    }

    /// <summary>The group's account must take entries (no lower level) and be active; an account kept unchanged may since have been switched off.</summary>
    private static string ValidLedger(string? code, PurposeLedgerFact? fact, string? keep = null)
    {
        var cleaned = PersianText.Clean(code);
        Require(cleaned.Length > 0, "PurposeGroup.LedgerAccountRequired", "The ledger account (معین / معین در معین) of the group is required.");
        Require(fact is not null, "PurposeGroup.LedgerAccountNotFound", $"Account {cleaned} is not in the chart of accounts.");
        Require(fact!.IsPostable, "PurposeGroup.LedgerAccountNotPostable", $"Account {cleaned} has a lower level; choose the lower-level account.");
        Require(fact.IsActive || cleaned == keep, "PurposeGroup.LedgerAccountInactive", $"Account {cleaned} is inactive.");
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

public sealed record PurposeGroupChanged(
    Guid PurposeGroupId, int Number, PurposeDirection Direction, string Name, string LedgerAccountCode, Guid? Reference1Id, Guid? Reference2Id, bool IsActive) : DomainEvent
{
    public static PurposeGroupChanged From(PurposeGroup g) =>
        new(g.Id, g.Number, g.Direction, g.Name, g.LedgerAccountCode, g.Reference1Id, g.Reference2Id, g.IsActive);
}

public sealed record PurposeGroupDeleted(Guid PurposeGroupId) : DomainEvent;
