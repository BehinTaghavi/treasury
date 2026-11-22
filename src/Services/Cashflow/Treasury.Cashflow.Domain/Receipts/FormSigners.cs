using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.Receipts;

/// <summary>
/// A holder of a permission in a treasury operation as reference-svc describes it («دارندگان مجوزها», Basic Information): the name and
/// what is printed over the signature box – the roles held in the operation and the job title.
/// </summary>
public sealed record SignerFact(Guid Id, string Name, string? Position);

/// <summary>A person chosen to sign a document, kept with the name and position it had then (printed over the signature box).</summary>
public sealed record ReceiptSigner(Guid Id, string Name, string? Position);

/// <summary>
/// Who may sign the form of a treasury operation besides the parties: the holders of a permission in that operation. Owner's report
/// no. 8 (21 Nov 2026) removed the signature rule per kind of document (16 Nov 2026: a number of signatures, compulsory and optional
/// signatories) and put the confirmers and approvers of the forms in «دارندگان مجوزها (فرم‌ها)». Until the approval workflow he asks
/// for is built, the form names who signs and the signatures are given on the printed document; nobody is compulsory.
/// </summary>
public sealed record FormSigners(IReadOnlyList<SignerFact> Holders)
{
    /// <summary>An operation in which nobody holds a permission: nobody can be chosen.</summary>
    public static FormSigners None { get; } = new([]);

    /// <summary>The persons chosen on a document, in the order of the roles: only holders of a permission in its operation, each once.</summary>
    public IReadOnlyList<ReceiptSigner> Choose(IReadOnlyList<Guid>? chosen)
    {
        var ids = (chosen ?? []).Distinct().ToList();
        if (!ids.All(id => Holders.Any(s => s.Id == id)))
        {
            throw new DomainException("Receipt.SignerNotAuthorised", "A chosen person holds no permission in this operation.");
        }

        return Holders.Where(s => ids.Contains(s.Id)).Select(s => new ReceiptSigner(s.Id, s.Name, s.Position)).ToList();
    }
}
