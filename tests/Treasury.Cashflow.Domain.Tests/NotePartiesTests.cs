using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Receipts;
using Xunit;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// The persons of a note receipt come from «ذینفعان / متعهدین / ضامنین» only (owner's answer to Q54, 19 Nov 2026: «متعهد و ضامن فقط از این
/// فهرست انتخاب شوند»).
/// </summary>
public sealed class NotePartiesTests
{
    private static readonly NotePartyFact Obligor = new("200015", "شرکت نمونه", true, IsObligor: true, IsGuarantor: false);
    private static readonly NotePartyFact Guarantor = new("200016", "رضا احمدی", true, IsObligor: false, IsGuarantor: true, NationalId: "0123456789", Phone: "09121112233");

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void The_obligor_is_defined_among_the_obligors_and_switched_on()
    {
        Assert.Equal(Obligor, NoteParties.Obligor(Obligor));
        Assert.Equal("Receipt.IssuerNotObligor", Code(() => NoteParties.Obligor(null)));       // not in the list at all
        Assert.Equal("Receipt.IssuerNotObligor", Code(() => NoteParties.Obligor(Guarantor))); // in the list, but in another group
        Assert.Equal("Receipt.NotePartyInactive", Code(() => NoteParties.Obligor(Obligor with { IsActive = false })));
    }

    [Fact]
    public void A_guarantor_is_defined_among_the_guarantors_and_takes_its_name_from_the_list()
    {
        Assert.Equal(new("رضا احمدی", "0123456789", "200016", "09121112233"), NoteParties.Guarantor(Guarantor));
        // What is typed on the form for the national code and phone wins; the name is always the list's.
        Assert.Equal(new("رضا احمدی", "10101010103", "200016", "021-88"), NoteParties.Guarantor(Guarantor, " 10101010103 ", "021-88"));
        Assert.Equal("Receipt.GuarantorNotRegistered", Code(() => NoteParties.Guarantor(null)));    // a typed name, or a تفصیل outside the list
        Assert.Equal("Receipt.GuarantorNotRegistered", Code(() => NoteParties.Guarantor(Obligor)));
        Assert.Equal("Receipt.NotePartyInactive", Code(() => NoteParties.Guarantor(Guarantor with { IsActive = false })));
    }
}
