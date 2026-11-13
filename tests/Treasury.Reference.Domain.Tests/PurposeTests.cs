using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Purposes;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>Purpose groups and purposes: new-version manual items 8 and 9; Access Form_Babs / Form_Babss; MVC BehalfGroup, Behalfs.</summary>
public sealed class PurposeTests
{
    private static readonly PurposeLedgerFact Postable = new("2101", true, true);
    private static readonly CatalogueItem Contract = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Reference, "شماره قرارداد");
    private static readonly CatalogueItem Order = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Reference, "شماره سفارش فروش");

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    private static PurposeGroup Group(PurposeDirection direction = PurposeDirection.Receipt, string name = "پیش دریافت") =>
        PurposeGroup.Register(Guid.NewGuid(), 1, direction, name, "2101", Postable, Contract, Order);

    [Fact]
    public void A_group_has_a_direction_a_description_a_postable_account_and_up_to_two_references()
    {
        var g = Group();
        Assert.Equal((PurposeDirection.Receipt, "پیش دریافت", "2101", Contract.Id, Order.Id), (g.Direction, g.Name, g.LedgerAccountCode, g.Reference1Id, g.Reference2Id));
        Assert.IsType<PurposeGroupChanged>(Assert.Single(g.PendingEvents));
    }

    [Fact]
    public void The_description_and_the_account_are_required_as_in_form_babs()
    {
        Assert.Equal("PurposeGroup.NameRequired", Code(() => PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Payment, " ", "2101", Postable)));
        Assert.Equal("PurposeGroup.LedgerAccountRequired", Code(() => PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Payment, "هزینه", "", null)));
        Assert.Equal("PurposeGroup.LedgerAccountNotFound", Code(() => PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Payment, "هزینه", "9999", null)));
        Assert.Equal("PurposeGroup.LedgerAccountNotPostable",
            Code(() => PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Payment, "هزینه", "21", new PurposeLedgerFact("21", true, false))));
        Assert.Equal("PurposeGroup.LedgerAccountInactive",
            Code(() => PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Payment, "هزینه", "2199", new PurposeLedgerFact("2199", false, true))));
    }

    [Fact]
    public void References_must_be_references_different_and_the_second_needs_the_first()
    {
        var bank = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Bank, "بانک ملت");
        Assert.Equal("PurposeGroup.ReferenceNotFound", Code(() => PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Receipt, "x", "2101", Postable, bank)));
        Assert.Equal("PurposeGroup.SameReferenceTwice", Code(() => PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Receipt, "x", "2101", Postable, Contract, Contract)));
        Assert.Equal("PurposeGroup.Reference2WithoutReference1", Code(() => PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Receipt, "x", "2101", Postable, null, Order)));
        var off = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Reference, "شماره قدیمی");
        off.Deactivate();
        Assert.Equal("PurposeGroup.ReferenceInactive", Code(() => PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Receipt, "x", "2101", Postable, off)));
    }

    [Fact]
    public void Editing_keeps_a_reference_or_account_switched_off_since_and_raises_nothing_when_unchanged()
    {
        var reference = CatalogueItem.Add(Guid.NewGuid(), CatalogueKind.Reference, "شماره پرونده");
        var g = PurposeGroup.Register(Guid.NewGuid(), 1, PurposeDirection.Receipt, "پیش دریافت", "2101", Postable, reference);
        g.MarkSaved(1);
        reference.Deactivate();
        g.Edit(PurposeDirection.Receipt, "پیش دریافت", "2101", new PurposeLedgerFact("2101", false, true), reference, null);
        Assert.Empty(g.PendingEvents);
        g.Edit(PurposeDirection.Payment, "پیش پرداخت", "2101", Postable, null, null);
        Assert.Equal((PurposeDirection.Payment, "پیش پرداخت", (Guid?)null), (g.Direction, g.Name, g.Reference1Id));
        Assert.Single(g.PendingEvents);
    }

    [Fact]
    public void A_group_in_use_keeps_its_direction_and_is_never_deleted()
    {
        var g = Group();
        Assert.True(g.MarkInUse());
        Assert.Equal("PurposeGroup.DirectionInUse", Code(() => g.Edit(PurposeDirection.Payment, g.Name, "2101", Postable, null, null)));
        Assert.Equal("PurposeGroup.InUse", Code(() => g.Delete(hasPurposes: false)));
        Assert.Equal("PurposeGroup.HasPurposes", Code(() => Group().Delete(hasPurposes: true)));
    }

    [Fact]
    public void A_purpose_needs_its_group_and_a_description_as_in_form_babss()
    {
        var g = Group();
        var p = Purpose.Add(Guid.NewGuid(), g, "  پیش دریافت   فروش ");
        Assert.Equal(("پیش دریافت فروش", g.Id), (p.Name, p.GroupId));
        Assert.Equal("Purpose.GroupRequired", Code(() => Purpose.Add(Guid.NewGuid(), null, "x")));
        Assert.Equal("Purpose.NameRequired", Code(() => Purpose.Add(Guid.NewGuid(), g, "")));
        g.Deactivate();
        Assert.Equal("Purpose.GroupInactive", Code(() => Purpose.Add(Guid.NewGuid(), g, "y")));
        p.Edit("پیش دریافت فروش داخلی", g); // its own group, switched off since: allowed
        Assert.Equal("Purpose.GroupInactive", Code(() => Purpose.Add(Guid.NewGuid(), g, "z")));
    }

    [Fact]
    public void A_purpose_is_deleted_even_after_it_was_used_owner_report_2()
    {
        var p = Purpose.Add(Guid.NewGuid(), Group(), "اجاره");
        p.MarkSaved(1);
        p.Delete();
        Assert.IsType<PurposeDeleted>(Assert.Single(p.PendingEvents));
        // Owner's report no. 2, item 10: a purpose already used can still be deleted and corrected (documents keep their text).
        var used = Purpose.Add(Guid.NewGuid(), Group(), "حقوق");
        used.MarkInUse();
        used.MarkSaved(1);
        used.Edit("حقوق و دستمزد", Group());
        used.Delete();
        Assert.IsType<PurposeDeleted>(used.PendingEvents[^1]);
    }
}
