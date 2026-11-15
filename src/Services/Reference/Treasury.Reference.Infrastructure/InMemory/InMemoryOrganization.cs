using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Organization;

namespace Treasury.Reference.Infrastructure.InMemory;

public sealed record DepartmentSnapshot(Guid Id, int Number, string Name, IReadOnlyList<string> CostCenters, bool IsActive, long Version);

public sealed record RequesterSnapshot(Guid Id, int Number, string FullName, Guid? DepartmentId, string CostCenter, bool IsActive, bool IsInUse, long Version, string? PersonDetailCode = null);

public sealed record BeneficiarySnapshot(Guid Id, string DetailCode, string LegalName, BeneficiaryBank Bank, string? Phone, bool IsActive, bool IsInUse, long Version, BeneficiaryIdentity? Identity = null);

/// <summary>In-memory departments, requesters and beneficiaries (local runs, tests): same version checks and outbox as the database.</summary>
public sealed class InMemoryOrganizationRepository(InMemoryReferenceDatabase db) : IOrganizationRepository
{
    private const string Departments = "departments";
    private const string Requesters = "requesters";
    private const string Beneficiaries = "beneficiaries";

    public Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows<DepartmentSnapshot>(Departments).GetValueOrDefault(id) is { } s ? ToDomain(s) : null);

    public Task<IReadOnlyList<Department>> ListDepartmentsAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Department>>(Rows<DepartmentSnapshot>(Departments).Values.Where(s => includeInactive || s.IsActive).Select(ToDomain).ToList());

    public Task AddDepartmentAsync(Department department, CancellationToken cancellationToken) => Write(Departments, department, 1, null, Snapshot);

    public Task SaveDepartmentAsync(Department department, CancellationToken cancellationToken) =>
        Write(Departments, department, department.Version + 1, department.Version, Snapshot);

    public Task DeleteDepartmentAsync(Department department, CancellationToken cancellationToken) => Delete<DepartmentSnapshot>(Departments, department, s => s.Version);

    public Task<Requester?> GetRequesterAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows<RequesterSnapshot>(Requesters).GetValueOrDefault(id) is { } s ? ToDomain(s) : null);

    public Task<IReadOnlyList<Requester>> ListRequestersAsync(Guid? departmentId, bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Requester>>(Rows<RequesterSnapshot>(Requesters).Values
            .Where(s => (departmentId is null || s.DepartmentId == departmentId) && (includeInactive || s.IsActive)).Select(ToDomain).ToList());

    public Task AddRequesterAsync(Requester requester, CancellationToken cancellationToken) => Write(Requesters, requester, 1, null, Snapshot);

    public Task SaveRequesterAsync(Requester requester, CancellationToken cancellationToken) =>
        Write(Requesters, requester, requester.Version + 1, requester.Version, Snapshot);

    public Task DeleteRequesterAsync(Requester requester, CancellationToken cancellationToken) => Delete<RequesterSnapshot>(Requesters, requester, s => s.Version);

    public Task<Beneficiary?> GetBeneficiaryAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows<BeneficiarySnapshot>(Beneficiaries).GetValueOrDefault(id) is { } s ? ToDomain(s) : null);

    public Task<IReadOnlyList<Beneficiary>> ListBeneficiariesAsync(bool includeInactive, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Beneficiary>>(Rows<BeneficiarySnapshot>(Beneficiaries).Values.Where(s => includeInactive || s.IsActive).Select(ToDomain).ToList());

    public Task AddBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken) => Write(Beneficiaries, beneficiary, 1, null, Snapshot);

    public Task SaveBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken) =>
        Write(Beneficiaries, beneficiary, beneficiary.Version + 1, beneficiary.Version, Snapshot);

    public Task DeleteBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken) => Delete<BeneficiarySnapshot>(Beneficiaries, beneficiary, s => s.Version);

    private static DepartmentSnapshot Snapshot(Department d, long v) => new(d.Id, d.Number, d.Name, d.CostCenterCodes, d.IsActive, v);

    private static RequesterSnapshot Snapshot(Requester r, long v) => new(r.Id, r.Number, r.FullName, r.DepartmentId, r.CostCenterCode, r.IsActive, r.IsInUse, v, r.PersonDetailCode);

    private static BeneficiarySnapshot Snapshot(Beneficiary b, long v) => new(b.Id, b.DetailCode, b.LegalName, b.Bank, b.Phone, b.IsActive, b.IsInUse, v, b.Identity);

    private static Department ToDomain(DepartmentSnapshot s) => Department.Restore(s.Id, s.Number, s.Name, s.CostCenters, s.IsActive, s.Version);

    private static Requester ToDomain(RequesterSnapshot s) => Requester.Restore(s.Id, s.Number, s.FullName, s.DepartmentId, s.CostCenter, s.IsActive, s.IsInUse, s.Version, s.PersonDetailCode);

    private static Beneficiary ToDomain(BeneficiarySnapshot s) => Beneficiary.Restore(s.Id, s.DetailCode, s.LegalName, s.Bank, s.Phone, s.IsActive, s.IsInUse, s.Version, s.Identity);

    /// <summary>Committed rows of a table overlaid with this transaction's own changes.</summary>
    private Dictionary<Guid, T> Rows<T>(string table)
        where T : class
    {
        var pending = InMemoryUnitOfWork.Pending;
        lock (db.Gate)
        {
            var all = db.Tables.TryGetValue(table, out var committed) ? committed.ToDictionary(kv => kv.Key, kv => (T)kv.Value) : [];
            if (pending.Tables.TryGetValue(table, out var mine))
            {
                foreach (var (id, row) in mine)
                {
                    if (row is null)
                    {
                        all.Remove(id);
                    }
                    else
                    {
                        all[id] = (T)row;
                    }
                }
            }

            return all;
        }
    }

    private Task Write<TAggregate, TSnapshot>(string table, TAggregate aggregate, long newVersion, long? expected, Func<TAggregate, long, TSnapshot> snapshot)
        where TAggregate : AggregateRoot<Guid>
        where TSnapshot : class
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        if (expected is { } v && Version(table, aggregate.Id) != v)
        {
            throw new ConcurrencyConflictException($"Entry {aggregate.Id} was changed by someone else.");
        }

        Pending(table)[aggregate.Id] = snapshot(aggregate, newVersion);
        InMemoryUnitOfWork.Pending.Outbox.AddRange(aggregate.PendingEvents.Select(OutboxMessage.From));
        aggregate.MarkSaved(newVersion);
        return Task.CompletedTask;
    }

    private Task Delete<TSnapshot>(string table, AggregateRoot<Guid> aggregate, Func<TSnapshot, long> version)
        where TSnapshot : class
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        if (Rows<TSnapshot>(table).GetValueOrDefault(aggregate.Id) is not { } s || version(s) != aggregate.Version)
        {
            throw new ConcurrencyConflictException($"Entry {aggregate.Id} was changed by someone else.");
        }

        Pending(table)[aggregate.Id] = null;
        InMemoryUnitOfWork.Pending.Outbox.AddRange(aggregate.PendingEvents.Select(OutboxMessage.From));
        aggregate.MarkSaved(aggregate.Version);
        return Task.CompletedTask;
    }

    private long? Version(string table, Guid id) => Rows<object>(table).GetValueOrDefault(id) switch
    {
        DepartmentSnapshot d => d.Version,
        RequesterSnapshot r => r.Version,
        BeneficiarySnapshot b => b.Version,
        _ => null,
    };

    private static Dictionary<Guid, object?> Pending(string table)
    {
        var tables = InMemoryUnitOfWork.Pending.Tables;
        if (!tables.TryGetValue(table, out var rows))
        {
            rows = [];
            tables[table] = rows;
        }

        return rows;
    }
}
