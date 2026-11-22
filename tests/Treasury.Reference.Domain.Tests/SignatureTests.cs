using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Organization;
using Treasury.Reference.Domain.Signatures;
using Xunit;

namespace Treasury.Reference.Domain.Tests;

/// <summary>
/// «دارندگان امضای مجاز (اسناد بهادار)»: the owner's suggestion of 16 Nov 2026 with his answers to Q41 and Q45 (name, position, تفصیل,
/// national code, mobile and landline) and his report no. 8 of 21 Nov 2026, item 3 (position from a list with «سایر», extension,
/// e-mail, scan of the signature, the cheque and note ticks; the signature rule per kind of document is removed).
/// </summary>
public sealed class SignatureTests
{
    private static readonly DetailFact Staff = new("600046", "علی رضایی", false, true);
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    private static SignatoryData Data(string? name = "علی رضایی", SignatoryPosition? position = SignatoryPosition.ManagingDirector, string? other = null,
        string? detail = null, string? national = null, string? mobile = null, string? phone = null, string? extension = null, string? email = null,
        bool cheques = true, bool notes = false, bool contracts = false) =>
        new(new PersonForm(name, detail, national, mobile, phone, extension, email), position, other, cheques, notes, contracts);

    private static Signatory Signatory(SignatoryData? data = null, DetailFact? fact = null) => Signatures.Signatory.Register(Guid.NewGuid(), data ?? Data(), fact);

    private static string Code(Action action) => Assert.Throws<DomainException>(action).Code;

    [Fact]
    public void A_signatory_keeps_name_detail_national_code_mobile_and_landline_owner_answer_Q45()
    {
        var s = Signatory(Data(" علی  رضایی ", detail: "600046", national: "۰۱۲۳۴۵۶۷۸۹", mobile: "0912-1234567", phone: "021-88990011"), Staff);
        Assert.Equal(("علی رضایی", "600046", "0123456789", "09121234567", "021-88990011"),
            (s.FullName, s.Person.DetailCode, s.Person.NationalCode, s.Person.Mobile, s.Person.Phone));
        Assert.True(s.IsActive);
        Assert.False(s.IsInUse);
        Assert.IsType<SignatoryChanged>(Assert.Single(s.PendingEvents));
    }

    [Fact]
    public void Report_8_item_3a_adds_the_extension_inside_the_organisation_and_the_email_address()
    {
        var s = Signatory(Data(extension: "۲۱۴", email: " a.rezaei@nazho.example "));
        Assert.Equal(("214", "a.rezaei@nazho.example"), (s.Person.Extension, s.Person.Email));
        Assert.Equal((null, null), (Signatory().Person.Extension, Signatory().Person.Email));
        Assert.Equal("Person.ExtensionInvalid", Code(() => Signatory(Data(extension: "21-4"))));
        Assert.Equal("Person.ExtensionInvalid", Code(() => Signatory(Data(extension: "1234567"))));
        Assert.Equal("Person.EmailInvalid", Code(() => Signatory(Data(email: "rezaei.nazho"))));
        Assert.Equal("Person.EmailInvalid", Code(() => Signatory(Data(email: "rezaei@nazho"))));
    }

    [Fact]
    public void Report_8_item_3b_the_position_is_one_of_the_six_titles_or_other_with_a_typed_text()
    {
        Assert.Equal(
            ["مدیر عامل", "مدیر عامل و رییس هیئت مدیره", "رییس هیئت مدیره", "عضو هیئت مدیره (موظف)", "عضو هیئت مدیره (غیر موظف)", "مدیر و مسئول غیر عضو هیئت مدیره"],
            Enum.GetValues<SignatoryPosition>().Where(p => p != SignatoryPosition.Other).Select(p => Signatory(Data(position: p)).Position));

        // A fixed title is printed as it is; a text typed beside it is not kept.
        var chairman = Signatory(Data(position: SignatoryPosition.Chairman, other: "چیز دیگر"));
        Assert.Equal((SignatoryPosition.Chairman, "رییس هیئت مدیره"), (chairman.PositionKind, chairman.Position));

        // «سایر»: a box opens in which the position is typed.
        var other = Signatory(Data(position: SignatoryPosition.Other, other: " مدیر  مالی "));
        Assert.Equal((SignatoryPosition.Other, "مدیر مالی"), (other.PositionKind, other.Position));
        Assert.Equal("Signatory.OtherPositionRequired", Code(() => Signatory(Data(position: SignatoryPosition.Other, other: " "))));
        Assert.Equal("Signatory.PositionTooLong", Code(() => Signatory(Data(position: SignatoryPosition.Other, other: new string('م', 101)))));
        Assert.Equal("Signatory.PositionRequired", Code(() => Signatory(Data(position: null))));
        Assert.Equal("Signatory.PositionRequired", Code(() => Signatory(Data(position: (SignatoryPosition)99))));
    }

    [Fact]
    public void A_signatory_signs_cheques_notes_contracts_or_any_combination_owner_answer_Q88()
    {
        // Report no. 8, item 3-د, as the owner settled it on 22 Nov 2026: three ticks «چک»، «سفته»، «قراردادها و سایر اسناد تعهدی»;
        // «ترکیبی از آنها قابل انتخاب خواهد بود».
        foreach (var (cheques, notes, contracts) in new[] { (true, false, false), (false, true, false), (false, false, true), (true, true, false), (true, false, true), (false, true, true), (true, true, true) })
        {
            var s = Signatory(Data(cheques: cheques, notes: notes, contracts: contracts));
            Assert.Equal((cheques, notes, contracts), (s.SignsCheques, s.SignsNotes, s.SignsContracts));
        }

        Assert.Equal("Signatory.DocumentRequired", Code(() => Signatory(Data(cheques: false, notes: false, contracts: false))));

        // Changing only the third tick is a change.
        var only = Signatory(Data(cheques: true));
        only.MarkSaved(1);
        only.Edit(Data(cheques: true, contracts: true), null);
        Assert.True(only.SignsContracts);
        Assert.True(Assert.IsType<SignatoryChanged>(Assert.Single(only.PendingEvents)).SignsContracts);
    }

    [Fact]
    public void Report_8_item_3c_the_scan_of_the_signature_is_a_png_or_jpeg_of_at_most_1_MB()
    {
        var s = Signatory();
        Assert.False(s.HasSignature);
        Assert.Equal("image/png", s.AttachSignature(Png));
        Assert.True(s.HasSignature);
        Assert.Equal("image/jpeg", s.AttachSignature(Jpeg));
        Assert.Equal("Person.SignatureInvalid", Code(() => s.AttachSignature([])));
        Assert.Equal("Person.SignatureInvalid", Code(() => s.AttachSignature("%PDF-1.7"u8.ToArray())));
        Assert.Equal("Person.SignatureTooLarge", Code(() => s.AttachSignature(new byte[SignatureScan.MaxBytes + 1])));
        Assert.Equal("image/jpeg", s.SignatureType);

        s.RemoveSignature();
        Assert.False(s.HasSignature);
        Assert.Equal("Person.NoSignature", Code(s.RemoveSignature));
    }

    [Fact]
    public void A_signatory_chosen_from_the_details_list_takes_its_name()
    {
        Assert.Equal("علی رضایی", Signatory(Data(name: " ", detail: "600046"), Staff).FullName);
        Assert.Equal("Person.DetailNotFound", Code(() => Signatory(Data(detail: "999999"), null)));
        Assert.Equal("Person.DetailInactive", Code(() => Signatory(Data(detail: "600046"), Staff with { IsActive = false })));
    }

    [Fact]
    public void The_name_is_required_and_numbers_are_checked()
    {
        Assert.Equal("Person.NameRequired", Code(() => Signatory(Data(name: " "))));
        Assert.Equal("Person.NameTooLong", Code(() => Signatory(Data(name: new string('ع', 101)))));
        Assert.Equal("Person.NationalCodeInvalid", Code(() => Signatory(Data(national: "0123456788"))));
        Assert.Equal("Person.MobileInvalid", Code(() => Signatory(Data(mobile: "0212345678"))));
        Assert.Equal("Person.PhoneInvalid", Code(() => Signatory(Data(phone: "12"))));
    }

    [Fact]
    public void A_correction_that_changes_nothing_raises_no_event_and_a_kept_detail_may_be_switched_off()
    {
        var s = Signatory(Data(detail: "600046"), Staff);
        s.MarkSaved(1);
        s.Edit(Data(detail: "600046"), Staff with { IsActive = false });
        Assert.Empty(s.PendingEvents);
        s.Edit(Data(detail: "600046", position: SignatoryPosition.Chairman, notes: true), Staff);
        Assert.Equal(("رییس هیئت مدیره", true), (s.Position, s.SignsNotes));
        Assert.IsType<SignatoryChanged>(Assert.Single(s.PendingEvents));
    }

    [Fact]
    public void A_signatory_named_by_a_bank_account_is_switched_off_not_deleted_and_keeps_its_cheque_tick()
    {
        var s = Signatory(Data(cheques: true, notes: true));
        Assert.True(s.MarkInUse());
        Assert.False(s.MarkInUse());
        Assert.Equal("Signatory.InUse", Code(s.Delete));
        Assert.Equal("Signatory.ChequesInUse", Code(() => s.Edit(Data(cheques: false, notes: true), null)));
        s.Deactivate();
        Assert.False(s.IsActive);
        s.Activate();
        Assert.True(s.IsActive);

        var unused = Signatory();
        unused.Delete();
        Assert.Contains(unused.PendingEvents, e => e is SignatoryDeleted);
    }
}
