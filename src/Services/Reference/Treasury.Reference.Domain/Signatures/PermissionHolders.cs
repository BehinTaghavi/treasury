using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Organization;

namespace Treasury.Reference.Domain.Signatures;

/// <summary>
/// «فهرست عملیات خزانه داری» (owner's report no. 8, 21 Nov 2026): the operations of the treasury system for which a person can hold
/// a permission, e.g. «درخواست وجه، دریافت یا پرداخت و …». The owner named three; the list below follows the menus of the system as
/// built and planned (payment requests, receipts, payments, vouchers) and waits for his confirmation (Q86).
/// </summary>
public enum TreasuryOperation
{
    PaymentRequest,
    CashReceipt,
    BankDeposit,
    CardDeposit,
    ChequeReceipt,
    NoteReceipt,
    ReceivedChequeSteps,
    ReceivedNoteSteps,
    LegalAction,
    ChequeTransfer,
    CashPayment,
    ChequePayment,
    NotePayment,
    ElectronicPayment,
    OwnAccountTransfer,
    PettyCash,
    VoucherConfirmation,
}

/// <summary>
/// The roles of a permission holder (owner's report no. 8, field «نقش»): first and second confirmer («تایید کننده»), first and second
/// approver («تصویب کننده») and «سایر», whose title the user types. The report lists «تصویب کننده (اول)» twice; the second one is read
/// as «تصویب کننده (دوم)» (Q87).
/// </summary>
public enum PermissionRoleKind
{
    FirstConfirmer,
    SecondConfirmer,
    FirstApprover,
    SecondApprover,
    Other,
}

public static class PermissionRoleKinds
{
    /// <summary>The role as the owner wrote it; null for «سایر», where the user's title is used.</summary>
    public static string? Title(this PermissionRoleKind kind) => kind switch
    {
        PermissionRoleKind.FirstConfirmer => "تایید کننده (اول)",
        PermissionRoleKind.SecondConfirmer => "تایید کننده (دوم)",
        PermissionRoleKind.FirstApprover => "تصویب کننده (اول)",
        PermissionRoleKind.SecondApprover => "تصویب کننده (دوم)",
        _ => null,
    };
}

/// <summary>
/// Where a role under «سایر» stands among the four fixed steps of the approval flow (owner, 22 Nov 2026): «اگر برای حالت سایر مجوزی
/// باشد که بین این مراحل قرار گیرد شماره ترتیب مجوزها باید تعیین شوند تا در ورک فلو قرار گیرند». The fixed steps follow each other
/// naturally – تایید کننده (اول)، تایید کننده (دوم)، تصویب کننده (اول)، تصویب کننده (دوم) – and a step nobody holds is left out,
/// confirmation always before approval.
/// </summary>
public enum OtherRolePlace
{
    BeforeFirstConfirmer,
    AfterFirstConfirmer,
    AfterSecondConfirmer,
    AfterFirstApprover,
    AfterSecondApprover,
}

/// <summary>A role as ticked on the form with the operations ticked under it.</summary>
/// <param name="Place">«سایر» only: where the role stands in the flow; left empty it comes last.</param>
public sealed record PermissionRoleForm(PermissionRoleKind? Kind, string? Title, IReadOnlyList<TreasuryOperation>? Operations, OtherRolePlace? Place = null);

/// <summary>
/// One role of a permission holder with the treasury operations in which the person has it: «هر شخص ممکن است … در چند عملیات نقش
/// داشته باشد» (owner's report no. 8).
/// </summary>
/// <param name="Title">The role's name: the fixed title, or the text typed for «سایر».</param>
/// <param name="Place">«سایر» only: where the role stands among the fixed steps; null for the four fixed roles.</param>
public sealed record PermissionRole(PermissionRoleKind Kind, string Title, IReadOnlyList<TreasuryOperation> Operations, OtherRolePlace? Place = null)
{
    /// <summary>
    /// The order of the role in the approval flow of an operation (owner, 22 Nov 2026): first confirmer 10, second confirmer 20, first
    /// approver 30, second approver 40; a role under «سایر» stands just before the first confirmer (5) or just after the step it names
    /// (15, 25, 35, 45). Steps nobody holds in an operation are simply absent, so the others keep their order.
    /// </summary>
    public int Step => Kind switch
    {
        PermissionRoleKind.FirstConfirmer => 10,
        PermissionRoleKind.SecondConfirmer => 20,
        PermissionRoleKind.FirstApprover => 30,
        PermissionRoleKind.SecondApprover => 40,
        _ => ((int)(Place ?? OtherRolePlace.AfterSecondApprover) * 10) + 5,
    };

    public bool Equals(PermissionRole? other) =>
        other is not null && Kind == other.Kind && Title == other.Title && Place == other.Place && Operations.SequenceEqual(other.Operations);

    public override int GetHashCode() => HashCode.Combine(Kind, Title, Place, Operations.Count);
}

/// <summary>What the form «دارندگان مجوزها (فرم‌ها)» sends.</summary>
/// <param name="JobTitle">«سمت سازمانی / شغل», in place of the signatories' «سمت».</param>
public sealed record PermissionHolderData(PersonForm? Person, string? JobTitle, IReadOnlyList<PermissionRoleForm>? Roles);

/// <summary>
/// A holder of a permission («دارندگان مجوزها (فرم‌ها)», owner's report no. 8, 21 Nov 2026): one of the company's staff, managers and
/// officers included, who may or may not be a user of the system and who has the right to confirm («تایید») or approve («تصویب») some
/// operations of the treasury system. The upper part of the form is the same as for an authorised signatory, with «سمت سازمانی / شغل»
/// in place of «سمت» and the scan of the signature; under «نقش» each ticked role opens the list of treasury operations, and a person
/// may have several roles, each in several operations. Until the approval workflow is built (proposal in
/// docs/domain/permissions-and-approvals.md), the forms that used the signature rule of 16 Nov 2026 – the receipt of notes and the
/// cheque payment sheet – list the holders of their operation as the persons who sign.
/// </summary>
public sealed class PermissionHolder : AggregateRoot<Guid>
{
    public const int MaxJobTitleLength = 100;
    public const int MaxRoleTitleLength = 60;

    private PermissionHolder(Guid id, PersonParticulars person, string jobTitle, IReadOnlyList<PermissionRole> roles)
        : base(id)
    {
        (Person, JobTitle, Roles) = (person, jobTitle, roles);
        IsActive = true;
    }

    public PersonParticulars Person { get; private set; }

    public string FullName => Person.FullName;

    public string NameKey => Person.NameKey;

    public string JobTitle { get; private set; }

    /// <summary>The roles in the order of <see cref="PermissionRoleKind"/>, each with its operations in the order of <see cref="TreasuryOperation"/>.</summary>
    public IReadOnlyList<PermissionRole> Roles { get; private set; }

    public string? SignatureType { get; private set; }

    public bool HasSignature => SignatureType is not null;

    public bool IsActive { get; private set; }

    /// <summary>The roles the person has in one operation, in the order of the approval flow (<see cref="PermissionRole.Step"/>).</summary>
    public IReadOnlyList<PermissionRole> RolesIn(TreasuryOperation operation) => Roles.Where(r => r.Operations.Contains(operation)).OrderBy(r => r.Step).ToList();

    public static PermissionHolder Register(Guid id, PermissionHolderData data, DetailFact? detail)
    {
        var v = Validate(data, detail, null);
        var h = new PermissionHolder(id, v.Person, v.JobTitle, v.Roles);
        h.Raise(PermissionHolderChanged.From(h));
        return h;
    }

    public void Edit(PermissionHolderData data, DetailFact? detail)
    {
        var v = Validate(data, detail, this);
        if (v.Person == Person && v.JobTitle == JobTitle && v.Roles.SequenceEqual(Roles))
        {
            return;
        }

        (Person, JobTitle, Roles) = v;
        Raise(PermissionHolderChanged.From(this));
    }

    public string AttachSignature(ReadOnlySpan<byte> file)
    {
        SignatureType = SignatureScan.TypeOf(file);
        Raise(PermissionHolderChanged.From(this));
        return SignatureType;
    }

    public void RemoveSignature()
    {
        Rules.Require(HasSignature, "Person.NoSignature", "No scan of the signature is stored.");
        SignatureType = null;
        Raise(PermissionHolderChanged.From(this));
    }

    public void Activate() => SetActive(true);

    public void Deactivate() => SetActive(false);

    /// <summary>Forms keep the name and role of who signed them, so a holder can always be deleted.</summary>
    public void Delete() => Raise(new PermissionHolderDeleted(Id));

    public static PermissionHolder Restore(
        Guid id, PersonParticulars person, string jobTitle, IEnumerable<PermissionRole> roles, string? signatureType, bool isActive, long version)
    {
        var h = new PermissionHolder(id, person, jobTitle, [.. roles.OrderBy(r => r.Kind)]) { SignatureType = signatureType, IsActive = isActive };
        h.SetLoadedVersion(version);
        return h;
    }

    private void SetActive(bool active)
    {
        if (IsActive != active)
        {
            IsActive = active;
            Raise(PermissionHolderChanged.From(this));
        }
    }

    private static (PersonParticulars Person, string JobTitle, IReadOnlyList<PermissionRole> Roles) Validate(PermissionHolderData? data, DetailFact? detail, PermissionHolder? before)
    {
        ArgumentNullException.ThrowIfNull(data);
        var person = PersonParticulars.Check(data.Person, detail, before?.Person.DetailCode);
        var job = Rules.Name(data.JobTitle, MaxJobTitleLength, "PermissionHolder.JobTitleRequired", "PermissionHolder.JobTitleTooLong");
        var roles = new List<PermissionRole>();
        foreach (var role in data.Roles ?? [])
        {
            Rules.Require(role?.Kind is { } k && Enum.IsDefined(k), "PermissionHolder.RoleUnknown", "Unknown role.");
            var kind = role!.Kind!.Value;
            Rules.Require(roles.All(r => r.Kind != kind), "PermissionHolder.RoleTwice", "A role is ticked once.");
            // Report no. 8: «در مقابل انتخاب چک باکس سایر فیلدی در اختیار کاربر قرار میگیرد تا عنوان نقش را وارد نماید».
            var title = kind.Title() ?? Rules.Name(role.Title, MaxRoleTitleLength, "PermissionHolder.OtherRoleTitleRequired", "PermissionHolder.RoleTitleTooLong");
            var operations = (role.Operations ?? []).Distinct().ToList();
            Rules.Require(operations.All(o => Enum.IsDefined(o)), "PermissionHolder.OperationUnknown", "Unknown treasury operation.");
            // A role without an operation gives no permission: the list of operations opens with the role and at least one is ticked.
            Rules.Require(operations.Count > 0, "PermissionHolder.OperationRequired", "Tick at least one treasury operation for each role.");
            // Owner, 22 Nov 2026: a role under «سایر» is given its place among the steps; without one it comes after the last approval.
            Rules.Require(role.Place is not { } place || Enum.IsDefined(place), "PermissionHolder.PlaceUnknown", "Unknown place of the role in the approval flow.");
            roles.Add(new PermissionRole(kind, title, [.. operations.Order()], kind == PermissionRoleKind.Other ? role.Place ?? OtherRolePlace.AfterSecondApprover : null));
        }

        Rules.Require(roles.Count > 0, "PermissionHolder.RoleRequired", "Tick at least one role.");
        return (person, job, [.. roles.OrderBy(r => r.Kind)]);
    }
}

public sealed record PermissionHolderChanged(Guid HolderId, string FullName, string JobTitle, IReadOnlyList<PermissionRole> Roles, bool HasSignature, bool IsActive) : DomainEvent
{
    public static PermissionHolderChanged From(PermissionHolder h) => new(h.Id, h.FullName, h.JobTitle, h.Roles, h.HasSignature, h.IsActive);
}

public sealed record PermissionHolderDeleted(Guid HolderId) : DomainEvent;
