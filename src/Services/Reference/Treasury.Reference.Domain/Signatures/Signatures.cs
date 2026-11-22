using System.Text.RegularExpressions;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Organization;

namespace Treasury.Reference.Domain.Signatures;

/// <summary>The particulars of a person as typed on the forms «دارندگان امضای مجاز» and «دارندگان مجوزها».</summary>
/// <param name="FullName">Empty with a <paramref name="DetailCode"/>: the name of the تفصیل is taken.</param>
/// <param name="Extension">«شماره تلفن داخل سازمانی» (owner's report no. 8, item 3-الف).</param>
/// <param name="Email">«آدرس ایمیل» (owner's report no. 8, item 3-الف).</param>
public sealed record PersonForm(
    string? FullName, string? DetailCode = null, string? NationalCode = null, string? Mobile = null, string? Phone = null, string? Extension = null, string? Email = null);

/// <summary>
/// The upper part shared by the two forms (owner's report no. 8, 21 Nov 2026: the form of «دارندگان مجوزها» is "exactly like the upper
/// part of «دارندگان امضای مجاز»"): name, the staff تفصیل when there is one, national code, mobile and landline numbers (owner,
/// 16 Nov 2026, answers to Q41 and Q45), and since report no. 8 the extension inside the organisation and the e-mail address.
/// </summary>
public sealed partial record PersonParticulars(
    string FullName, string? DetailCode, string? NationalCode, string? Mobile, string? Phone, string? Extension, string? Email)
{
    public const int MaxNameLength = 100;
    public const int MaxEmailLength = 100;

    public string NameKey => PersianText.Key(FullName);

    /// <param name="detail">What the تفصیل list says about the code of the form; its name is taken when no name is typed.</param>
    /// <param name="keptDetailCode">The تفصیل the person had before: it may have been switched off since.</param>
    public static PersonParticulars Check(PersonForm? form, DetailFact? detail, string? keptDetailCode)
    {
        Rules.Require(form is not null, "Person.NameRequired", "The name is required.");
        var code = Rules.Optional(form!.DetailCode);
        if (code is not null)
        {
            detail = Rules.Detail(code, detail, keptDetailCode, "Person.DetailNotFound", "Person.DetailInactive");
        }

        var name = Rules.Name(Rules.Optional(form.FullName) ?? (code is null ? null : detail?.Name), MaxNameLength, "Person.NameRequired", "Person.NameTooLong");
        var national = Rules.Optional(form.NationalCode) is { } n ? PersianText.Key(n) : null;
        Rules.Require(national is null || Beneficiary.IsNationalCode(national), "Person.NationalCodeInvalid", "The national code has 10 digits and a valid check digit.");
        var cell = Rules.Optional(form.Mobile) is { } m ? PersianText.Key(m).Replace("-", string.Empty, StringComparison.Ordinal) : null;
        Rules.Require(cell is null || MobilePattern().IsMatch(cell), "Person.MobileInvalid", "A mobile number has 11 digits and starts with 09.");
        var line = Rules.Optional(form.Phone) is { } p ? PersianText.Key(p) : null;
        Rules.Require(line is null || PhonePattern().IsMatch(line), "Person.PhoneInvalid", "A phone number has 6 to 20 digits.");
        var extension = Rules.Optional(form.Extension) is { } x ? PersianText.Key(x) : null;
        Rules.Require(extension is null || ExtensionPattern().IsMatch(extension), "Person.ExtensionInvalid", "An extension has 1 to 6 digits.");
        var email = Rules.Optional(form.Email)?.Replace(" ", string.Empty, StringComparison.Ordinal);
        Rules.Require(email is null || (email.Length <= MaxEmailLength && EmailPattern().IsMatch(email)), "Person.EmailInvalid", "The e-mail address is not valid.");
        return new PersonParticulars(name, code, national, cell, line, extension, email);
    }

    [GeneratedRegex("^09[0-9]{9}$")]
    private static partial Regex MobilePattern();

    [GeneratedRegex("^\\+?[0-9\\-]{6,20}$")]
    private static partial Regex PhonePattern();

    [GeneratedRegex("^[0-9]{1,6}$")]
    private static partial Regex ExtensionPattern();

    [GeneratedRegex("^[^@\\s]+@[^@\\s.]+(\\.[^@\\s.]+)+$")]
    private static partial Regex EmailPattern();
}

/// <summary>
/// The scan of a person's specimen signature («اسکن نمونه امضاء», owner's report no. 8, items 3-ج and «دارندگان مجوزها»): a PNG or JPEG
/// picture of at most 1 MB, recognised by its first bytes like the company's logo.
/// </summary>
public static class SignatureScan
{
    public const int MaxBytes = 1024 * 1024;

    /// <summary>The media type of the picture; refuses anything that is not a PNG or JPEG of at most 1 MB.</summary>
    public static string TypeOf(ReadOnlySpan<byte> file)
    {
        Rules.Require(file.Length > 0, "Person.SignatureInvalid", "The signature file is empty.");
        Rules.Require(file.Length <= MaxBytes, "Person.SignatureTooLarge", "The signature file is larger than 1 MB.");
        var type = file.Length >= 8 && file[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ? "image/png"
            : file.Length >= 3 && file[0] == 0xFF && file[1] == 0xD8 && file[2] == 0xFF ? "image/jpeg"
            : null;
        return type ?? throw new DomainException("Person.SignatureInvalid", "The signature must be a PNG or JPEG picture.");
    }
}

/// <summary>
/// The choices of the field «سمت» of an authorised signatory (owner's report no. 8, 21 Nov 2026, item 3-ب), in the owner's order.
/// With <see cref="Other"/> the user types the position.
/// </summary>
public enum SignatoryPosition
{
    ManagingDirector,
    ManagingDirectorAndChairman,
    Chairman,
    ExecutiveBoardMember,
    NonExecutiveBoardMember,
    NonBoardManager,
    Other,
}

public static class SignatoryPositions
{
    /// <summary>The position as the owner wrote it; it is printed under the signature. Null for «سایر», where the user's text is used.</summary>
    public static string? Title(this SignatoryPosition position) => position switch
    {
        SignatoryPosition.ManagingDirector => "مدیر عامل",
        SignatoryPosition.ManagingDirectorAndChairman => "مدیر عامل و رییس هیئت مدیره",
        SignatoryPosition.Chairman => "رییس هیئت مدیره",
        SignatoryPosition.ExecutiveBoardMember => "عضو هیئت مدیره (موظف)",
        SignatoryPosition.NonExecutiveBoardMember => "عضو هیئت مدیره (غیر موظف)",
        SignatoryPosition.NonBoardManager => "مدیر و مسئول غیر عضو هیئت مدیره",
        _ => null,
    };
}

/// <summary>What the form «دارندگان امضای مجاز (اسناد بهادار)» sends.</summary>
/// <param name="OtherPosition">The position typed by the user when <paramref name="Position"/> is «سایر».</param>
/// <param name="SignsCheques">Tick «دارنده امضاء مجاز (چک)»: cheques, and by the owner's note also notes and contracts.</param>
/// <param name="SignsNotes">Tick «دارنده مجوز امضاء (سفته)».</param>
public sealed record SignatoryData(PersonForm? Person, SignatoryPosition? Position, string? OtherPosition, bool SignsCheques, bool SignsNotes);

/// <summary>
/// A holder of an authorised signature on valuable documents («دارندگان امضای مجاز (اسناد بهادار)»), kept in Basic Information.
/// Owner, 16 Nov 2026 (answers to Q41 and Q45): name, position, the staff تفصیل when there is one, national code, mobile and landline
/// numbers. Owner's report no. 8 (21 Nov 2026): the position is chosen from a list, the extension, the e-mail address and the scan of
/// the specimen signature are added, and two ticks say what the person may sign: cheques, notes or both. Who signs the cheques of a
/// bank account is set on the bank account (accounts-svc), which marks the signatory as in use: from then on it is switched off
/// instead of deleted, and keeps its cheque tick. The signature rule per kind of document of 16 Nov 2026 was removed by the same report.
/// </summary>
public sealed class Signatory : AggregateRoot<Guid>
{
    public const int MaxPositionLength = 100;

    private Signatory(Guid id, PersonParticulars person, SignatoryPosition positionKind, string position, bool signsCheques, bool signsNotes)
        : base(id)
    {
        (Person, PositionKind, Position, SignsCheques, SignsNotes) = (person, positionKind, position, signsCheques, signsNotes);
        IsActive = true;
    }

    public PersonParticulars Person { get; private set; }

    public string FullName => Person.FullName;

    public string NameKey => Person.NameKey;

    public SignatoryPosition PositionKind { get; private set; }

    /// <summary>«سمت» as printed under the signature: the title of the chosen position, or the text typed for «سایر».</summary>
    public string Position { get; private set; }

    public bool SignsCheques { get; private set; }

    public bool SignsNotes { get; private set; }

    /// <summary>Media type of the stored scan of the signature (image/png or image/jpeg); null: no scan yet.</summary>
    public string? SignatureType { get; private set; }

    public bool HasSignature => SignatureType is not null;

    public bool IsActive { get; private set; }

    /// <summary>A bank account names this signatory (recorded by accounts-svc); then it is never deleted.</summary>
    public bool IsInUse { get; private set; }

    public static Signatory Register(Guid id, SignatoryData data, DetailFact? detail)
    {
        var v = Validate(data, detail, null);
        var s = new Signatory(id, v.Person, v.Kind, v.Position, v.Cheques, v.Notes);
        s.Raise(SignatoryChanged.From(s));
        return s;
    }

    public void Edit(SignatoryData data, DetailFact? detail)
    {
        var v = Validate(data, detail, this);
        if (v == (Person, PositionKind, Position, SignsCheques, SignsNotes))
        {
            return;
        }

        (Person, PositionKind, Position, SignsCheques, SignsNotes) = v;
        Raise(SignatoryChanged.From(this));
    }

    /// <summary>Checks the scan of the specimen signature and records its media type.</summary>
    public string AttachSignature(ReadOnlySpan<byte> file)
    {
        SignatureType = SignatureScan.TypeOf(file);
        Raise(SignatoryChanged.From(this));
        return SignatureType;
    }

    public void RemoveSignature()
    {
        Rules.Require(HasSignature, "Person.NoSignature", "No scan of the signature is stored.");
        SignatureType = null;
        Raise(SignatoryChanged.From(this));
    }

    public void Activate() => SetActive(true);

    /// <summary>A switched-off signatory stays on the bank accounts that name it and can no longer be chosen on another one.</summary>
    public void Deactivate() => SetActive(false);

    /// <summary>Marks the signatory as named by a bank account; false when it already was.</summary>
    public bool MarkInUse()
    {
        if (IsInUse)
        {
            return false;
        }

        IsInUse = true;
        return true;
    }

    public void Delete()
    {
        Rules.Require(!IsInUse, "Signatory.InUse", "A bank account names this signatory; switch it off instead.");
        Raise(new SignatoryDeleted(Id));
    }

    public static Signatory Restore(
        Guid id, PersonParticulars person, SignatoryPosition positionKind, string position, bool signsCheques, bool signsNotes, string? signatureType,
        bool isActive, bool isInUse, long version)
    {
        var s = new Signatory(id, person, positionKind, position, signsCheques, signsNotes) { SignatureType = signatureType, IsActive = isActive, IsInUse = isInUse };
        s.SetLoadedVersion(version);
        return s;
    }

    private void SetActive(bool active)
    {
        if (IsActive != active)
        {
            IsActive = active;
            Raise(SignatoryChanged.From(this));
        }
    }

    private static (PersonParticulars Person, SignatoryPosition Kind, string Position, bool Cheques, bool Notes) Validate(SignatoryData? data, DetailFact? detail, Signatory? before)
    {
        ArgumentNullException.ThrowIfNull(data);
        var person = PersonParticulars.Check(data.Person, detail, before?.Person.DetailCode);
        Rules.Require(data.Position is { } p && Enum.IsDefined(p), "Signatory.PositionRequired", "Choose the position.");
        var kind = data.Position!.Value;
        // Report no. 8, item 3-ب: with «سایر» a box opens in which the position is typed.
        var position = kind.Title() ?? Rules.Name(data.OtherPosition, MaxPositionLength, "Signatory.OtherPositionRequired", "Signatory.PositionTooLong");
        // Report no. 8, item 3-د: «اگر مجوز امضای هر دو را داشته باشد هر دو چک باکس تیک میشود» – a signatory signs at least one of them.
        Rules.Require(data.SignsCheques || data.SignsNotes, "Signatory.DocumentRequired", "Tick what the signatory may sign: cheques, notes or both.");
        Rules.Require(before is not { IsInUse: true } || data.SignsCheques, "Signatory.ChequesInUse", "A bank account names this signatory for its cheques; the cheque tick stays.");
        return (person, kind, position, data.SignsCheques, data.SignsNotes);
    }
}

public sealed record SignatoryChanged(Guid SignatoryId, string FullName, string Position, bool SignsCheques, bool SignsNotes, bool HasSignature, bool IsActive) : DomainEvent
{
    public static SignatoryChanged From(Signatory s) => new(s.Id, s.FullName, s.Position, s.SignsCheques, s.SignsNotes, s.HasSignature, s.IsActive);
}

public sealed record SignatoryDeleted(Guid SignatoryId) : DomainEvent;
