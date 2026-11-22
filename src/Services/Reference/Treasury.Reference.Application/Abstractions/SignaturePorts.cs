using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Application.Abstractions;

/// <summary>Holders of authorised signatures on valuable documents (owner, 16 Nov 2026; report no. 8, 21 Nov 2026).</summary>
public interface ISignatureRepository
{
    Task<Signatory?> GetSignatoryAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Signatory>> ListSignatoriesAsync(bool includeInactive, CancellationToken cancellationToken);

    Task AddSignatoryAsync(Signatory signatory, CancellationToken cancellationToken);

    /// <summary>
    /// Version-checked. With <paramref name="scan"/> also replaces the stored scan of the signature; a signatory without a signature
    /// type loses its stored scan, all in the same transaction.
    /// </summary>
    Task SaveSignatoryAsync(Signatory signatory, CancellationToken cancellationToken, byte[]? scan = null);

    Task DeleteSignatoryAsync(Signatory signatory, CancellationToken cancellationToken);

    Task<(string ContentType, byte[] Data)?> GetSignatureAsync(Guid signatoryId, CancellationToken cancellationToken);
}

/// <summary>Holders of permissions on the forms of treasury operations («دارندگان مجوزها», owner's report no. 8).</summary>
public interface IPermissionHolderRepository
{
    Task<PermissionHolder?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionHolder>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task AddAsync(PermissionHolder holder, CancellationToken cancellationToken);

    /// <summary>Version-checked; the scan as in <see cref="ISignatureRepository.SaveSignatoryAsync"/>.</summary>
    Task SaveAsync(PermissionHolder holder, CancellationToken cancellationToken, byte[]? scan = null);

    Task DeleteAsync(PermissionHolder holder, CancellationToken cancellationToken);

    Task<(string ContentType, byte[] Data)?> GetSignatureAsync(Guid holderId, CancellationToken cancellationToken);
}
