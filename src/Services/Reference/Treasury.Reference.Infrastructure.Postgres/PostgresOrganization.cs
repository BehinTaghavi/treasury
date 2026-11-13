using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Organization;

namespace Treasury.Reference.Infrastructure.Postgres;

// Written against db/postgres/012_reference_organization.sql.

public sealed class PostgresOrganizationRepository : IOrganizationRepository
{
    public async Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken) =>
        First(await ReadDepartmentsAsync("WHERE id = @id", id, cancellationToken).ConfigureAwait(false));

    public Task<IReadOnlyList<Department>> ListDepartmentsAsync(bool includeInactive, CancellationToken cancellationToken) =>
        ReadDepartmentsAsync(includeInactive ? string.Empty : "WHERE is_active", null, cancellationToken);

    public Task AddDepartmentAsync(Department department, CancellationToken cancellationToken) =>
        WriteAsync(department, null,
            "INSERT INTO reference.department (id, number, name, name_key, cost_centers, is_active) VALUES (@id, @number, @name, @key, @centers, @active)",
            cmd => DepartmentParameters(cmd, department), cancellationToken);

    public Task SaveDepartmentAsync(Department department, CancellationToken cancellationToken) =>
        WriteAsync(department, department.Version,
            "UPDATE reference.department SET number = @number, name = @name, name_key = @key, cost_centers = @centers, is_active = @active, " +
            "version = version + 1, updated_at = now() WHERE id = @id AND version = @expected",
            cmd => DepartmentParameters(cmd, department), cancellationToken);

    public Task DeleteDepartmentAsync(Department department, CancellationToken cancellationToken) =>
        DeleteAsync(department, "DELETE FROM reference.department WHERE id = @id AND version = @expected", cancellationToken);

    public async Task<Requester?> GetRequesterAsync(Guid id, CancellationToken cancellationToken) =>
        First(await ReadRequestersAsync("WHERE id = @id", ("id", id), cancellationToken).ConfigureAwait(false));

    public Task<IReadOnlyList<Requester>> ListRequestersAsync(Guid? departmentId, bool includeInactive, CancellationToken cancellationToken) =>
        ReadRequestersAsync(
            "WHERE (@department::uuid IS NULL OR department_id = @department) AND (@all OR is_active)",
            null, cancellationToken, departmentId, includeInactive);

    public Task AddRequesterAsync(Requester requester, CancellationToken cancellationToken) =>
        WriteAsync(requester, null,
            "INSERT INTO reference.requester (id, number, full_name, department_id, cost_center, is_active, in_use) VALUES (@id, @number, @name, @department, @center, @active, @in_use)",
            cmd => RequesterParameters(cmd, requester), cancellationToken);

    public Task SaveRequesterAsync(Requester requester, CancellationToken cancellationToken) =>
        WriteAsync(requester, requester.Version,
            "UPDATE reference.requester SET number = @number, full_name = @name, department_id = @department, cost_center = @center, is_active = @active, " +
            "in_use = @in_use, version = version + 1, updated_at = now() WHERE id = @id AND version = @expected",
            cmd => RequesterParameters(cmd, requester), cancellationToken);

    public Task DeleteRequesterAsync(Requester requester, CancellationToken cancellationToken) =>
        DeleteAsync(requester, "DELETE FROM reference.requester WHERE id = @id AND version = @expected AND NOT in_use", cancellationToken);

    public async Task<Beneficiary?> GetBeneficiaryAsync(Guid id, CancellationToken cancellationToken) =>
        First(await ReadBeneficiariesAsync("WHERE id = @id", id, cancellationToken).ConfigureAwait(false));

    public Task<IReadOnlyList<Beneficiary>> ListBeneficiariesAsync(bool includeInactive, CancellationToken cancellationToken) =>
        ReadBeneficiariesAsync(includeInactive ? string.Empty : "WHERE is_active", null, cancellationToken);

    public Task AddBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken) =>
        WriteAsync(beneficiary, null,
            "INSERT INTO reference.beneficiary (id, detail_code, legal_name, bank_id, branch_name, account_number, sheba, phone, is_active, in_use, person_kind, national_id, postal_address, postal_code) " +
            "VALUES (@id, @detail, @name, @bank, @branch, @account, @sheba, @phone, @active, @in_use, @kind, @national_id, @address, @postal)",
            cmd => BeneficiaryParameters(cmd, beneficiary), cancellationToken);

    public Task SaveBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken) =>
        WriteAsync(beneficiary, beneficiary.Version,
            "UPDATE reference.beneficiary SET detail_code = @detail, legal_name = @name, bank_id = @bank, branch_name = @branch, account_number = @account, " +
            "sheba = @sheba, phone = @phone, is_active = @active, in_use = @in_use, " +
            "person_kind = @kind, national_id = @national_id, postal_address = @address, postal_code = @postal, version = version + 1, updated_at = now() " +
            "WHERE id = @id AND version = @expected",
            cmd => BeneficiaryParameters(cmd, beneficiary), cancellationToken);

    public Task DeleteBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken) =>
        DeleteAsync(beneficiary, "DELETE FROM reference.beneficiary WHERE id = @id AND version = @expected AND NOT in_use", cancellationToken);

    private static T? First<T>(IReadOnlyList<T> rows)
        where T : class => rows.Count == 0 ? null : rows[0];

    private static void DepartmentParameters(NpgsqlCommand cmd, Department d)
    {
        cmd.Parameters.AddWithValue("id", d.Id);
        cmd.Parameters.AddWithValue("number", d.Number);
        cmd.Parameters.AddWithValue("name", d.Name);
        cmd.Parameters.AddWithValue("key", d.NameKey);
        cmd.Parameters.Add(new NpgsqlParameter("centers", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = d.CostCenterCodes.ToArray() });
        cmd.Parameters.AddWithValue("active", d.IsActive);
    }

    private static void RequesterParameters(NpgsqlCommand cmd, Requester r)
    {
        cmd.Parameters.AddWithValue("id", r.Id);
        cmd.Parameters.AddWithValue("number", r.Number);
        cmd.Parameters.AddWithValue("name", r.FullName);
        cmd.Parameters.AddWithValue("department", r.DepartmentId);
        cmd.Parameters.AddWithValue("center", r.CostCenterCode);
        cmd.Parameters.AddWithValue("active", r.IsActive);
        cmd.Parameters.AddWithValue("in_use", r.IsInUse);
    }

    private static void BeneficiaryParameters(NpgsqlCommand cmd, Beneficiary b)
    {
        cmd.Parameters.AddWithValue("id", b.Id);
        cmd.Parameters.AddWithValue("detail", b.DetailCode);
        cmd.Parameters.AddWithValue("name", b.LegalName);
        cmd.Parameters.Add(new NpgsqlParameter("bank", NpgsqlDbType.Uuid) { Value = (object?)b.Bank.BankId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("branch", NpgsqlDbType.Text) { Value = (object?)b.Bank.BranchName ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("account", NpgsqlDbType.Text) { Value = (object?)b.Bank.AccountNumber ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("sheba", NpgsqlDbType.Text) { Value = (object?)b.Bank.Sheba ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("phone", NpgsqlDbType.Text) { Value = (object?)b.Phone ?? DBNull.Value });
        cmd.Parameters.AddWithValue("active", b.IsActive);
        cmd.Parameters.AddWithValue("in_use", b.IsInUse);
        cmd.Parameters.Add(new NpgsqlParameter("kind", NpgsqlDbType.Text) { Value = (object?)b.Identity?.Kind.ToString() ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("national_id", NpgsqlDbType.Text) { Value = (object?)b.Identity?.NationalId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("address", NpgsqlDbType.Text) { Value = (object?)b.Identity?.PostalAddress ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("postal", NpgsqlDbType.Text) { Value = (object?)b.Identity?.PostalCode ?? DBNull.Value });
    }

    /// <summary>Insert (expected null) or version-checked update, then the pending events into the outbox.</summary>
    private static async Task WriteAsync(AggregateRoot<Guid> aggregate, long? expected, string sql, Action<NpgsqlCommand> parameters, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(sql))
        {
            parameters(cmd);
            if (expected is { } v)
            {
                cmd.Parameters.AddWithValue("expected", v);
            }

            if (await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Entry {aggregate.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, aggregate.PendingEvents, ct).ConfigureAwait(false);
        aggregate.MarkSaved(expected is { } e ? e + 1 : 1);
    }

    private static async Task DeleteAsync(AggregateRoot<Guid> aggregate, string sql, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(sql))
        {
            cmd.Parameters.AddWithValue("id", aggregate.Id);
            cmd.Parameters.AddWithValue("expected", aggregate.Version);
            if (await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Entry {aggregate.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, aggregate.PendingEvents, ct).ConfigureAwait(false);
        aggregate.MarkSaved(aggregate.Version);
    }

    private static async Task<IReadOnlyList<Department>> ReadDepartmentsAsync(string where, Guid? id, CancellationToken ct)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT id, number, name, cost_centers, is_active, version FROM reference.department {where}");
        if (id is { } i)
        {
            cmd.Parameters.AddWithValue("id", i);
        }

        var list = new List<Department>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(Department.Restore(r.GetGuid(0), r.GetInt32(1), r.GetString(2), r.GetFieldValue<string[]>(3), r.GetBoolean(4), r.GetInt64(5)));
        }

        return list;
    }

    private static async Task<IReadOnlyList<Requester>> ReadRequestersAsync(
        string where, (string Name, Guid Value)? id, CancellationToken ct, Guid? departmentId = null, bool includeInactive = true)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT id, number, full_name, department_id, cost_center, is_active, in_use, version FROM reference.requester {where}");
        if (id is { } p)
        {
            cmd.Parameters.AddWithValue(p.Name, p.Value);
        }
        else
        {
            cmd.Parameters.Add(new NpgsqlParameter("department", NpgsqlDbType.Uuid) { Value = (object?)departmentId ?? DBNull.Value });
            cmd.Parameters.AddWithValue("all", includeInactive);
        }

        var list = new List<Requester>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(Requester.Restore(r.GetGuid(0), r.GetInt32(1), r.GetString(2), r.GetGuid(3), r.GetString(4), r.GetBoolean(5), r.GetBoolean(6), r.GetInt64(7)));
        }

        return list;
    }

    private static async Task<IReadOnlyList<Beneficiary>> ReadBeneficiariesAsync(string where, Guid? id, CancellationToken ct)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT id, detail_code, legal_name, bank_id, branch_name, account_number, sheba, phone, is_active, in_use, version, " +
            $"person_kind, national_id, postal_address, postal_code FROM reference.beneficiary {where}");
        if (id is { } i)
        {
            cmd.Parameters.AddWithValue("id", i);
        }

        var list = new List<Beneficiary>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            string? S(int i) => r.IsDBNull(i) ? null : r.GetString(i);
            var bank = new BeneficiaryBank(r.IsDBNull(3) ? null : r.GetGuid(3), S(4), S(5), S(6));
            list.Add(Beneficiary.Restore(r.GetGuid(0), r.GetString(1), r.GetString(2), bank, S(7), r.GetBoolean(8), r.GetBoolean(9), r.GetInt64(10),
                S(11) is { } kind ? new BeneficiaryIdentity(Enum.Parse<BeneficiaryKind>(kind), S(12), S(13), S(14)) : null));
        }

        return list;
    }
}
