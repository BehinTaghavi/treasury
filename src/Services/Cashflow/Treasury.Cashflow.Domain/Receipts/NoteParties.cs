using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Domain.Receipts;

/// <summary>
/// An entry of «ذینفعان / متعهدین / ضامنین» (Basic Information) as reference-svc describes it, by its تفصیل: whether it is in the group of
/// obligors or of guarantors, and what is kept about it.
/// </summary>
/// <param name="Phone">The mobile number, or else the phone number with its area code.</param>
public sealed record NotePartyFact(string DetailCode, string Name, bool IsActive, bool IsObligor, bool IsGuarantor, string? NationalId = null, string? Phone = null);

/// <summary>
/// The persons of a receipt of promissory notes (owner's answer to Q54, 19 Nov 2026: «متعهد و ضامن فقط از این فهرست انتخاب شوند»): the
/// obligor is one of the «متعهدین» and every guarantor one of the «ضامنین» defined in Basic Information; a تفصیل that is not in its list,
/// or a typed name, is refused. Before, the obligor was any customer or person/company تفصیل (answer to Q22) and a guarantor a تفصیل or
/// a typed name (owner's form «فرم دریافت سفته», 16 Nov 2026).
/// </summary>
public static class NoteParties
{
    /// <summary>The issuer and obligor of the notes: defined, switched on, and in the group «متعهدین».</summary>
    public static NotePartyFact Obligor(NotePartyFact? party)
    {
        Require(party is { IsObligor: true }, "Receipt.IssuerNotObligor", "The obligor of a note is chosen from the list of obligors (متعهدین) in Basic Information.");
        Require(party!.IsActive, "Receipt.NotePartyInactive", $"{party.Name} is switched off in Basic Information.");
        return party;
    }

    /// <summary>
    /// A guarantor of a note: defined, switched on, and in the group «ضامنین». The name is the one kept in Basic Information; the national
    /// code and phone typed on the form win over the kept ones.
    /// </summary>
    public static NoteGuarantor Guarantor(NotePartyFact? party, string? nationalCode = null, string? phone = null)
    {
        Require(party is { IsGuarantor: true }, "Receipt.GuarantorNotRegistered", "A guarantor of a note is chosen from the list of guarantors (ضامنین) in Basic Information.");
        Require(party!.IsActive, "Receipt.NotePartyInactive", $"{party.Name} is switched off in Basic Information.");
        return new NoteGuarantor(party.Name, Clean(nationalCode) ?? Clean(party.NationalId), party.DetailCode, Clean(phone) ?? Clean(party.Phone));
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}
