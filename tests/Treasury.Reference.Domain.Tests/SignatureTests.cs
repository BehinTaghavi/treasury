using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Organization;
using Treasury.Reference.Domain.Signatures;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>
/// «دارندگان امضای مجاز» and the signature rules: the owner's suggestion of 16 Nov 2026 and his answers to Q45–Q47 (name, position,
/// تفصیل, national code, mobile and landline; one rule per kind of document with the number of signatures, the compulsory and the
/// optional signatories).
/// </summary>
public sealed class SignatureTests
{
    private static readonly DetailFact Staff = new("600046", "علی رضایی", false, true);

    private static Signatory Signatory(string? name = "علی رضایی", string? position = "مدیر عامل", string? detail = null, DetailFact? fact = null,
        string? national = null, string? mobile = null, string? phone = null) =>
        Signatures.Signatory.Register(Guid.NewGuid(), name, position, detail, fact, national, mobile, phone);

    private static string Code(Action action) => Assert.Throws<DomainException>(action).Code;

    [Fact]
    public void A_signatory_keeps_name_position_detail_national_code_mobile_and_landline_owner_answer_Q45()
    {
        var s = Signatory(" علی  رضایی ", " مدیر عامل ", "600046", Staff, "۰۱۲۳۴۵۶۷۸۹", "0912-1234567", "021-88990011");
        Assert.Equal(("علی رضایی", "مدیر عامل", "600046", "0123456789", "09121234567", "021-88990011"),
            (s.FullName, s.Position, s.DetailCode, s.NationalCode, s.Mobile, s.Phone));
        Assert.True(s.IsActive);
        Assert.IsType<SignatoryChanged>(Assert.Single(s.PendingEvents));
    }

    [Fact]
    public void A_signatory_chosen_from_the_details_list_takes_its_name()
    {
        Assert.Equal("علی رضایی", Signatory(name: " ", detail: "600046", fact: Staff).FullName);
        Assert.Equal("Signatory.DetailNotFound", Code(() => Signatory(detail: "999999", fact: null)));
        Assert.Equal("Signatory.DetailInactive", Code(() => Signatory(detail: "600046", fact: Staff with { IsActive = false })));
    }

    [Fact]
    public void Name_and_position_are_required_and_numbers_are_checked()
    {
        Assert.Equal("Signatory.NameRequired", Code(() => Signatory(name: " ")));
        Assert.Equal("Signatory.PositionRequired", Code(() => Signatory(position: null)));
        Assert.Equal("Signatory.NameTooLong", Code(() => Signatory(name: new string('ن', 101))));
        Assert.Equal("Signatory.NationalCodeInvalid", Code(() => Signatory(national: "0123456788")));
        Assert.Equal("Signatory.MobileInvalid", Code(() => Signatory(mobile: "02188990011")));
        Assert.Equal("Signatory.MobileInvalid", Code(() => Signatory(mobile: "0912123456")));
        Assert.Equal("Signatory.PhoneInvalid", Code(() => Signatory(phone: "تلفن")));
    }

    [Fact]
    public void A_signatory_named_by_a_rule_is_not_switched_off_or_deleted()
    {
        var s = Signatory();
        Assert.Equal("Signatory.InRule", Code(() => s.Deactivate(inRule: true)));
        Assert.Equal("Signatory.InRule", Code(() => s.Delete(inRule: true)));
        s.Deactivate(inRule: false);
        Assert.False(s.IsActive);
        s.Activate();
        Assert.True(s.IsActive);
        s.Delete(inRule: false);
        Assert.IsType<SignatoryDeleted>(s.PendingEvents[^1]);
    }

    private static readonly Guid Ceo = Guid.NewGuid();
    private static readonly Guid Cfo = Guid.NewGuid();
    private static readonly Guid Board = Guid.NewGuid();
    private static readonly Guid Former = Guid.NewGuid();

    private static readonly Dictionary<Guid, SignatoryFact> People = new()
    {
        [Ceo] = new(Ceo, true), [Cfo] = new(Cfo, true), [Board] = new(Board, true), [Former] = new(Former, false),
    };

    private static SignatureRule Rule(int required, Guid[] compulsory, Guid[] optional, SignedDocument document = SignedDocument.NoteReceipt) =>
        SignatureRule.Define(Guid.NewGuid(), document, required, compulsory, optional, People);

    [Fact]
    public void A_rule_names_the_number_of_signatures_the_compulsory_and_the_optional_signatories_owner_suggestion()
    {
        var rule = Rule(2, [Ceo], [Cfo, Board]);
        Assert.Equal((SignedDocument.NoteReceipt, 2), (rule.Document, rule.Required));
        Assert.Equal([Ceo], rule.Compulsory);
        Assert.Equal([Cfo, Board], rule.Optional);
        Assert.True(rule.Names(Board));
        Assert.False(rule.Names(Former));
        Assert.IsType<SignatureRuleChanged>(Assert.Single(rule.PendingEvents));
    }

    [Theory]
    [InlineData(SignedDocument.IssuedCheque)]
    [InlineData(SignedDocument.IssuedNote)]
    [InlineData(SignedDocument.ChequeReceipt)]
    [InlineData(SignedDocument.NoteReceipt)]
    [InlineData(SignedDocument.ChequePaymentSheet)]
    [InlineData(SignedDocument.NotePaymentSheet)]
    public void Every_kind_of_document_the_owner_named_has_a_rule_answer_Q46(SignedDocument document) =>
        Assert.Equal(document, Rule(1, [Ceo], [], document).Document);

    [Fact]
    public void The_number_of_signatures_lies_between_the_compulsory_ones_and_all_those_named()
    {
        Assert.Equal("SignatureRule.CountTooSmall", Code(() => Rule(1, [Ceo, Cfo], [])));
        Assert.Equal("SignatureRule.CountTooLarge", Code(() => Rule(3, [Ceo], [Cfo])));
        Assert.Equal("SignatureRule.CountInvalid", Code(() => Rule(-1, [], [])));
        Assert.Equal(0, Rule(0, [], []).Required); // no authorised signature needed
        Assert.Equal(2, Rule(2, [Ceo, Cfo], [Board]).Required); // more may sign than needed
    }

    [Fact]
    public void A_rule_names_known_active_signatories_once()
    {
        Assert.Equal("SignatureRule.SignatoryTwice", Code(() => Rule(1, [Ceo], [Ceo])));
        Assert.Equal("SignatureRule.SignatoryTwice", Code(() => Rule(1, [], [Cfo, Cfo])));
        Assert.Equal("SignatureRule.SignatoryNotFound", Code(() => Rule(1, [Guid.NewGuid()], [])));
        Assert.Equal("SignatureRule.SignatoryInactive", Code(() => Rule(1, [Former], [])));
    }

    [Fact]
    public void Changing_a_rule_raises_an_event_only_when_something_changed()
    {
        var rule = Rule(1, [Ceo], []);
        rule.MarkSaved(1);
        rule.Change(1, [Ceo], [], People);
        Assert.Empty(rule.PendingEvents);
        rule.Change(2, [Ceo], [Cfo], People);
        Assert.Equal(2, ((SignatureRuleChanged)Assert.Single(rule.PendingEvents)).Required);
    }
}
