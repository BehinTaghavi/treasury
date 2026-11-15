using Treasury.Reference.Domain.Organization;

namespace Treasury.Reference.Application.Abstractions;

/// <summary>Departments, requesters and beneficiaries (new-version manual items 12–14).</summary>
public interface IOrganizationRepository
{
    Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Department>> ListDepartmentsAsync(bool includeInactive, CancellationToken cancellationToken);

    Task AddDepartmentAsync(Department department, CancellationToken cancellationToken);

    Task SaveDepartmentAsync(Department department, CancellationToken cancellationToken);

    Task DeleteDepartmentAsync(Department department, CancellationToken cancellationToken);

    Task<Requester?> GetRequesterAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Requester>> ListRequestersAsync(Guid? departmentId, bool includeInactive, CancellationToken cancellationToken);

    Task AddRequesterAsync(Requester requester, CancellationToken cancellationToken);

    Task SaveRequesterAsync(Requester requester, CancellationToken cancellationToken);

    Task DeleteRequesterAsync(Requester requester, CancellationToken cancellationToken);

    Task<Beneficiary?> GetBeneficiaryAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Beneficiary>> ListBeneficiariesAsync(bool includeInactive, CancellationToken cancellationToken);

    Task AddBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken);

    Task SaveBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken);

    Task DeleteBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken);
}
