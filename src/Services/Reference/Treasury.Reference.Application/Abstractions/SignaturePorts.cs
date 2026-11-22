using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Application.Abstractions;

/// <summary>Holders of authorised signatures and the signature rule of each kind of document (owner, 16 Nov 2026).</summary>
public interface ISignatureRepository
{
    Task<Signatory?> GetSignatoryAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Signatory>> ListSignatoriesAsync(bool includeInactive, CancellationToken cancellationToken);

    Task AddSignatoryAsync(Signatory signatory, CancellationToken cancellationToken);

    /// <summary>Version-checked.</summary>
    Task SaveSignatoryAsync(Signatory signatory, CancellationToken cancellationToken);

    Task DeleteSignatoryAsync(Signatory signatory, CancellationToken cancellationToken);

    /// <summary>The rule of a kind of document; null when none was ever set.</summary>
    Task<SignatureRule?> GetRuleAsync(SignedDocument document, CancellationToken cancellationToken);

    Task<IReadOnlyList<SignatureRule>> ListRulesAsync(CancellationToken cancellationToken);

    Task AddRuleAsync(SignatureRule rule, CancellationToken cancellationToken);

    /// <summary>Version-checked.</summary>
    Task SaveRuleAsync(SignatureRule rule, CancellationToken cancellationToken);
}
