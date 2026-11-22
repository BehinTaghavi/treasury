using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Organization;
using Treasury.Reference.Domain.Signatures;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>
/// «دارندگان مجوزها (فرم‌ها)», owner's report no. 8 of 21 Nov 2026: the upper part of the signatories' form with «سمت سازمانی / شغل»,
/// the roles (first and second confirmer, first and second approver, «سایر» with a typed title) and under each role the treasury
/// operations in which the person has it.
/// </summary>
public sealed class PermissionHolderTests
{
    private static readonly DetailFact Staff = new("600046", "بهرام کریمی", false, true);

    private static PermissionRoleForm Role(PermissionRoleKind? kind, params TreasuryOperation[] operations) => new(kind, null, operations);

    private static PermissionHolderData Data(string? name = "بهرام کریمی", string? job = "مدیر مالی", string? detail = null, string? extension = null, string? email = null,
        params PermissionRoleForm[] roles) =>
        new(new PersonForm(name, detail, Extension: extension, Email: email), job, roles.Length > 0 ? roles : [Role(PermissionRoleKind.FirstConfirmer, TreasuryOperation.PaymentRequest)]);

    private static PermissionHolder Holder(PermissionHolderData? data = null, DetailFact? fact = null) => PermissionHolder.Register(Guid.NewGuid(), data ?? Data(), fact);

    private static string Code(Action action) => Assert.Throws<DomainException>(action).Code;

    [Fact]
    public void The_upper_part_is_the_signatories_form_with_the_job_title_in_place_of_the_position()
    {
        var h = Holder(Data(" ", " مدیر  مالی ", "600046", "۱۲۰", "karimi@nazho.example"), Staff);
        Assert.Equal(("بهرام کریمی", "مدیر مالی", "600046", "120", "karimi@nazho.example"), (h.FullName, h.JobTitle, h.Person.DetailCode, h.Person.Extension, h.Person.Email));
        Assert.True(h.IsActive);
        Assert.IsType<PermissionHolderChanged>(Assert.Single(h.PendingEvents));
        Assert.Equal("PermissionHolder.JobTitleRequired", Code(() => Holder(Data(job: " "))));
        Assert.Equal("PermissionHolder.JobTitleTooLong", Code(() => Holder(Data(job: new string('م', 101)))));
        Assert.Equal("Person.NameRequired", Code(() => Holder(Data(name: " "))));
        Assert.Equal("Person.EmailInvalid", Code(() => Holder(Data(email: "karimi"))));
    }

    [Fact]
    public void The_roles_are_first_and_second_confirmer_first_and_second_approver_and_other()
    {
        Assert.Equal(["تایید کننده (اول)", "تایید کننده (دوم)", "تصویب کننده (اول)", "تصویب کننده (دوم)"],
            Enum.GetValues<PermissionRoleKind>().Where(k => k != PermissionRoleKind.Other).Select(k => Holder(Data(roles: Role(k, TreasuryOperation.CashReceipt))).Roles[0].Title));

        // «سایر»: a field in which the user types the title of the role.
        var other = Holder(Data(roles: new PermissionRoleForm(PermissionRoleKind.Other, " ناظر  مالی ", [TreasuryOperation.PettyCash])));
        Assert.Equal((PermissionRoleKind.Other, "ناظر مالی"), (other.Roles[0].Kind, other.Roles[0].Title));
        Assert.Equal("PermissionHolder.OtherRoleTitleRequired", Code(() => Holder(Data(roles: new PermissionRoleForm(PermissionRoleKind.Other, " ", [TreasuryOperation.PettyCash])))));
        Assert.Equal("PermissionHolder.RoleTitleTooLong", Code(() => Holder(Data(roles: new PermissionRoleForm(PermissionRoleKind.Other, new string('ن', 61), [TreasuryOperation.PettyCash])))));
        Assert.Equal("PermissionHolder.RoleUnknown", Code(() => Holder(Data(roles: Role(null, TreasuryOperation.PettyCash)))));
        Assert.Equal("PermissionHolder.RoleUnknown", Code(() => Holder(Data(roles: Role((PermissionRoleKind)9, TreasuryOperation.PettyCash)))));
    }

    [Fact]
    public void A_person_may_have_several_roles_each_in_several_operations()
    {
        // «هر شخص ممکن است … تایید کننده (اول) در چند عملیات خزانه داری این نقش را داشته باشد» and «چند نقش داشته باشد».
        var h = Holder(Data(roles:
        [
            Role(PermissionRoleKind.FirstApprover, TreasuryOperation.ChequePayment, TreasuryOperation.PaymentRequest),
            Role(PermissionRoleKind.FirstConfirmer, TreasuryOperation.NoteReceipt, TreasuryOperation.CashReceipt, TreasuryOperation.NoteReceipt),
        ]));
        // Kept in the order of the form's tick boxes, each operation once, in the order of the operations list.
        Assert.Equal([PermissionRoleKind.FirstConfirmer, PermissionRoleKind.FirstApprover], h.Roles.Select(r => r.Kind));
        Assert.Equal([TreasuryOperation.CashReceipt, TreasuryOperation.NoteReceipt], h.Roles[0].Operations);
        Assert.Equal([TreasuryOperation.PaymentRequest, TreasuryOperation.ChequePayment], h.Roles[1].Operations);

        Assert.Equal(["تایید کننده (اول)"], h.RolesIn(TreasuryOperation.NoteReceipt).Select(r => r.Title));
        Assert.Equal(["تصویب کننده (اول)"], h.RolesIn(TreasuryOperation.PaymentRequest).Select(r => r.Title));
        Assert.Empty(h.RolesIn(TreasuryOperation.PettyCash));
    }

    [Fact]
    public void A_holder_has_at_least_one_role_a_role_is_ticked_once_and_has_at_least_one_operation()
    {
        Assert.Equal("PermissionHolder.RoleRequired", Code(() => PermissionHolder.Register(Guid.NewGuid(), new PermissionHolderData(new PersonForm("بهرام کریمی"), "مدیر مالی", []), null)));
        Assert.Equal("PermissionHolder.RoleRequired", Code(() => PermissionHolder.Register(Guid.NewGuid(), new PermissionHolderData(new PersonForm("بهرام کریمی"), "مدیر مالی", null), null)));
        Assert.Equal("PermissionHolder.RoleTwice", Code(() => Holder(Data(roles:
            [Role(PermissionRoleKind.FirstConfirmer, TreasuryOperation.CashReceipt), Role(PermissionRoleKind.FirstConfirmer, TreasuryOperation.NoteReceipt)]))));
        Assert.Equal("PermissionHolder.OperationRequired", Code(() => Holder(Data(roles: Role(PermissionRoleKind.SecondApprover)))));
        Assert.Equal("PermissionHolder.OperationUnknown", Code(() => Holder(Data(roles: Role(PermissionRoleKind.SecondApprover, (TreasuryOperation)999)))));
    }

    [Fact]
    public void The_operations_list_names_requests_receipts_payments_and_vouchers()
    {
        // The owner's examples: «درخواست وجه، دریافت یا پرداخت و …». The names are stored, so a rename is a data change.
        Assert.Equal(
            ["PaymentRequest", "CashReceipt", "BankDeposit", "CardDeposit", "ChequeReceipt", "NoteReceipt", "ReceivedChequeSteps", "ReceivedNoteSteps", "LegalAction",
             "ChequeTransfer", "CashPayment", "ChequePayment", "NotePayment", "ElectronicPayment", "OwnAccountTransfer", "PettyCash", "VoucherConfirmation"],
            Enum.GetNames<TreasuryOperation>());
    }

    [Fact]
    public void The_scan_of_the_signature_is_kept_and_removed_like_a_signatorys()
    {
        var h = Holder();
        Assert.Equal("image/png", h.AttachSignature([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 7]));
        Assert.True(h.HasSignature);
        Assert.Equal("Person.SignatureInvalid", Code(() => h.AttachSignature([1, 2, 3])));
        h.RemoveSignature();
        Assert.Equal("Person.NoSignature", Code(h.RemoveSignature));
    }

    [Fact]
    public void A_correction_that_changes_nothing_raises_no_event_and_a_holder_is_switched_off_or_deleted()
    {
        var h = Holder();
        h.MarkSaved(1);
        h.Edit(Data(), null);
        Assert.Empty(h.PendingEvents);
        h.Edit(Data(roles: Role(PermissionRoleKind.FirstConfirmer, TreasuryOperation.PaymentRequest, TreasuryOperation.CashReceipt)), null);
        Assert.IsType<PermissionHolderChanged>(Assert.Single(h.PendingEvents));
        h.Deactivate();
        Assert.False(h.IsActive);
        h.Activate();
        h.Delete();
        Assert.Contains(h.PendingEvents, e => e is PermissionHolderDeleted);
    }
}
