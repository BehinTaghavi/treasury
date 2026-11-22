using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.Receipts;

/// <summary>A holder of an authorised signature as reference-svc describes it («دارندگان امضای مجاز», Basic Information).</summary>
public sealed record SignerFact(Guid Id, string Name, string? Position);

/// <summary>An authorised signatory chosen on a document, kept with the name and position it had then (printed under the signature).</summary>
public sealed record ReceiptSigner(Guid Id, string Name, string? Position);

/// <summary>
/// The signature rule of a kind of document as reference-svc describes it (owner, 16 Nov 2026, answers to QN5 and QN9–QN11): the number
/// of authorised signatures needed, the signatories whose signature is compulsory and those who may give the remaining ones. The
/// signatures themselves are given on the printed document; the form names who signs.
/// </summary>
public sealed record SignatureRuleFact(int Required, IReadOnlyList<SignerFact> Compulsory, IReadOnlyList<SignerFact> Optional)
{
    /// <summary>A kind of document without a rule: no authorised signature is needed and none can be chosen.</summary>
    public static SignatureRuleFact None { get; } = new(0, [], []);

    /// <summary>
    /// The signatories chosen on a document, in the rule's order: only signatories of the rule, every compulsory one, and at least
    /// the number of signatures the rule asks for.
    /// </summary>
    public IReadOnlyList<ReceiptSigner> Choose(IReadOnlyList<Guid>? chosen)
    {
        var ids = (chosen ?? []).Distinct().ToList();
        var named = Compulsory.Concat(Optional).ToList();
        Require(ids.All(id => named.Any(s => s.Id == id)), "Receipt.SignerNotAuthorised", "A chosen signatory is not in the signature rule of this document.");
        Require(Compulsory.All(s => ids.Contains(s.Id)), "Receipt.CompulsorySignerMissing", "A signatory whose signature is compulsory is not chosen.");
        Require(ids.Count >= Required, "Receipt.SignersTooFew", $"This document needs {Required} authorised signatures.");
        return named.Where(s => ids.Contains(s.Id)).Select(s => new ReceiptSigner(s.Id, s.Name, s.Position)).ToList();
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}
