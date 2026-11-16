using System.Text.RegularExpressions;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Organization;

namespace Treasury.Reference.Domain.Signatures;

/// <summary>
/// The documents that carry authorised signatures (owner, 16 Nov 2026, answer to Q46): cheques and notes the company issues, the
/// receipts of cheques and of notes, and the payment sheets of cheques and of notes.
/// </summary>
public enum SignedDocument
{
    IssuedCheque,
    IssuedNote,
    ChequeReceipt,
    NoteReceipt,
    ChequePaymentSheet,
    NotePaymentSheet,
}

/// <summary>
/// A holder of an authorised signature («دارندهٔ امضای مجاز»), kept in Basic Information (owner, 16 Nov 2026, answers to Q41 and Q45):
/// name, position, the staff تفصیل when there is one, national code, mobile and landline numbers. Switched off instead of deleted
/// once a signature rule names it.
/// </summary>
public sealed partial class Signatory : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    private Signatory(Guid id, string fullName, string position, string? detailCode, string? nationalCode, string? mobile, string? phone)
        : base(id)
    {
        (FullName, Position, DetailCode, NationalCode, Mobile, Phone) = (fullName, position, detailCode, nationalCode, mobile, phone);
        IsActive = true;
    }

    public string FullName { get; private set; }

    /// <summary>«سمت»: printed under the signature.</summary>
    public string Position { get; private set; }

    /// <summary>The signatory's تفصیل in the accounting system, when it has one.</summary>
    public string? DetailCode { get; private set; }

    /// <summary>کد ملی: 10 digits with a valid check digit.</summary>
    public string? NationalCode { get; private set; }

    /// <summary>Mobile number: 11 digits starting with 09.</summary>
    public string? Mobile { get; private set; }

    /// <summary>Landline number.</summary>
    public string? Phone { get; private set; }

    public bool IsActive { get; private set; }

    public string NameKey => PersianText.Key(FullName);

    /// <param name="detail">What the تفصیل list says about <paramref name="detailCode"/>; its name is taken when no name is typed.</param>
    public static Signatory Register(Guid id, string? fullName, string? position, string? detailCode, DetailFact? detail, string? nationalCode, string? mobile, string? phone)
    {
        var v = Validate(fullName, position, detailCode, detail, nationalCode, mobile, phone, null);
        var s = new Signatory(id, v.Name, v.Position, v.Detail, v.NationalCode, v.Mobile, v.Phone);
        s.Raise(SignatoryChanged.From(s));
        return s;
    }

    public void Edit(string? fullName, string? position, string? detailCode, DetailFact? detail, string? nationalCode, string? mobile, string? phone)
    {
        var v = Validate(fullName, position, detailCode, detail, nationalCode, mobile, phone, this);
        if (v == (FullName, Position, DetailCode, NationalCode, Mobile, Phone))
        {
            return;
        }

        (FullName, Position, DetailCode, NationalCode, Mobile, Phone) = v;
        Raise(SignatoryChanged.From(this));
    }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            Raise(SignatoryChanged.From(this));
        }
    }

    /// <summary>A signatory named by a signature rule is first taken out of the rule: the rule must stay possible to meet.</summary>
    public void Deactivate(bool inRule)
    {
        Rules.Require(!inRule, "Signatory.InRule", "A signature rule names this signatory; take it out of the rule first.");
        if (IsActive)
        {
            IsActive = false;
            Raise(SignatoryChanged.From(this));
        }
    }

    public void Delete(bool inRule)
    {
        Rules.Require(!inRule, "Signatory.InRule", "A signature rule names this signatory; take it out of the rule first.");
        Raise(new SignatoryDeleted(Id));
    }

    public static Signatory Restore(Guid id, string fullName, string position, string? detailCode, string? nationalCode, string? mobile, string? phone, bool isActive, long version)
    {
        var s = new Signatory(id, fullName, position, detailCode, nationalCode, mobile, phone) { IsActive = isActive };
        s.SetLoadedVersion(version);
        return s;
    }

    private static (string Name, string Position, string? Detail, string? NationalCode, string? Mobile, string? Phone) Validate(
        string? fullName, string? position, string? detailCode, DetailFact? detail, string? nationalCode, string? mobile, string? phone, Signatory? before)
    {
        var code = Rules.Optional(detailCode);
        if (code is not null)
        {
            detail = Rules.Detail(code, detail, before?.DetailCode, "Signatory.DetailNotFound", "Signatory.DetailInactive");
        }

        var name = Rules.Name(Rules.Optional(fullName) ?? (code is null ? null : detail?.Name), MaxNameLength, "Signatory.NameRequired", "Signatory.NameTooLong");
        var title = Rules.Name(position, MaxNameLength, "Signatory.PositionRequired", "Signatory.PositionTooLong");
        var national = Rules.Optional(nationalCode) is { } n ? PersianText.Key(n) : null;
        Rules.Require(national is null || Beneficiary.IsNationalCode(national), "Signatory.NationalCodeInvalid", "The national code has 10 digits and a valid check digit.");
        var cell = Rules.Optional(mobile) is { } m ? PersianText.Key(m).Replace("-", string.Empty, StringComparison.Ordinal) : null;
        Rules.Require(cell is null || MobilePattern().IsMatch(cell), "Signatory.MobileInvalid", "A mobile number has 11 digits and starts with 09.");
        var line = Rules.Optional(phone) is { } p ? PersianText.Key(p) : null;
        Rules.Require(line is null || PhonePattern().IsMatch(line), "Signatory.PhoneInvalid", "A phone number has 6 to 20 digits.");
        return (name, title, code, national, cell, line);
    }

    [GeneratedRegex("^09[0-9]{9}$")]
    private static partial Regex MobilePattern();

    [GeneratedRegex("^\\+?[0-9\\-]{6,20}$")]
    private static partial Regex PhonePattern();
}

public sealed record SignatoryChanged(Guid SignatoryId, string FullName, string Position, bool IsActive) : DomainEvent
{
    public static SignatoryChanged From(Signatory s) => new(s.Id, s.FullName, s.Position, s.IsActive);
}

public sealed record SignatoryDeleted(Guid SignatoryId) : DomainEvent;

/// <summary>What the signatories list says about a signatory when a rule names it.</summary>
public sealed record SignatoryFact(Guid Id, bool IsActive);

/// <summary>
/// The signature rule of one kind of document (owner, 16 Nov 2026): how many authorised signatures it needs, whose signature is
/// compulsory, and who may give the remaining ones. The same rule serves every bank account and amount (answer to Q46). A rule with
/// no signatures needed means the document is not signed by authorised signatories.
/// </summary>
public sealed class SignatureRule : AggregateRoot<Guid>
{
    public const int MaxSignatures = 10;

    private SignatureRule(Guid id, SignedDocument document)
        : base(id) => Document = document;

    public SignedDocument Document { get; }

    /// <summary>«تعداد امضای لازم».</summary>
    public int Required { get; private set; }

    /// <summary>Signatories whose signature is compulsory («اجباری»).</summary>
    public IReadOnlyList<Guid> Compulsory { get; private set; } = [];

    /// <summary>Signatories who may give the remaining signatures («اختیاری»).</summary>
    public IReadOnlyList<Guid> Optional { get; private set; } = [];

    public bool Names(Guid signatoryId) => Compulsory.Contains(signatoryId) || Optional.Contains(signatoryId);

    public static SignatureRule Define(Guid id, SignedDocument document, int required, IEnumerable<Guid>? compulsory, IEnumerable<Guid>? optional,
        IReadOnlyDictionary<Guid, SignatoryFact> signatories)
    {
        Rules.Require(Enum.IsDefined(document), "SignatureRule.UnknownDocument", "Unknown kind of document.");
        var rule = new SignatureRule(id, document);
        rule.Set(required, compulsory, optional, signatories);
        return rule;
    }

    public void Change(int required, IEnumerable<Guid>? compulsory, IEnumerable<Guid>? optional, IReadOnlyDictionary<Guid, SignatoryFact> signatories) =>
        Set(required, compulsory, optional, signatories);

    public static SignatureRule Restore(Guid id, SignedDocument document, int required, IEnumerable<Guid> compulsory, IEnumerable<Guid> optional, long version)
    {
        var rule = new SignatureRule(id, document) { Required = required, Compulsory = [.. compulsory], Optional = [.. optional] };
        rule.SetLoadedVersion(version);
        return rule;
    }

    private void Set(int required, IEnumerable<Guid>? compulsory, IEnumerable<Guid>? optional, IReadOnlyDictionary<Guid, SignatoryFact> signatories)
    {
        ArgumentNullException.ThrowIfNull(signatories);
        var must = (compulsory ?? []).ToList();
        var may = (optional ?? []).ToList();
        Rules.Require(must.Concat(may).Distinct().Count() == must.Count + may.Count, "SignatureRule.SignatoryTwice", "A signatory is named once: compulsory or optional.");
        foreach (var id in must.Concat(may))
        {
            Rules.Require(signatories.TryGetValue(id, out var s), "SignatureRule.SignatoryNotFound", "A signatory of the rule is not in the list.");
            Rules.Require(s!.IsActive || Names(id), "SignatureRule.SignatoryInactive", "A signatory of the rule is switched off.");
        }

        Rules.Require(required is >= 0 and <= MaxSignatures, "SignatureRule.CountInvalid", $"The number of signatures is between 0 and {MaxSignatures}.");
        Rules.Require(required >= must.Count, "SignatureRule.CountTooSmall", "The number of signatures is less than the compulsory signatories.");
        Rules.Require(required <= must.Count + may.Count, "SignatureRule.CountTooLarge", "The number of signatures is more than the signatories named.");
        if (required == Required && must.SequenceEqual(Compulsory) && may.SequenceEqual(Optional))
        {
            return;
        }

        (Required, Compulsory, Optional) = (required, must, may);
        Raise(new SignatureRuleChanged(Document, Required, Compulsory, Optional));
    }
}

public sealed record SignatureRuleChanged(SignedDocument Document, int Required, IReadOnlyList<Guid> Compulsory, IReadOnlyList<Guid> Optional) : DomainEvent;
