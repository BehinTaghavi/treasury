using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Purposes;

namespace Treasury.Reference.Infrastructure.Postgres;

// Written against db/postgres/009_reference_purposes.sql.

public sealed class PostgresPurposeRepository : IPurposeRepository
{
    private const string GroupColumns = "id, number, direction, name, ledger_account, reference1_id, reference2_id, is_active, in_use, version";
    private const string PurposeColumns = "id, group_id, name, is_active, in_use, version";

    public async Task<PurposeGroup?> GetGroupAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = Command($"SELECT {GroupColumns} FROM reference.purpose_group WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return (await ReadGroupsAsync(cmd, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<PurposeGroup>> ListGroupsAsync(PurposeDirection? direction, bool includeInactive, CancellationToken cancellationToken)
    {
        await using var cmd = Command(
            $"SELECT {GroupColumns} FROM reference.purpose_group WHERE (@direction::text IS NULL OR direction = @direction) AND (@all OR is_active)");
        cmd.Parameters.Add(new NpgsqlParameter("direction", NpgsqlDbType.Text) { Value = (object?)direction?.ToString() ?? DBNull.Value });
        cmd.Parameters.AddWithValue("all", includeInactive);
        return await ReadGroupsAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> GroupNameTakenAsync(PurposeDirection direction, string nameKey, Guid? exceptId, CancellationToken cancellationToken)
    {
        await using var cmd = Command(
            "SELECT EXISTS (SELECT 1 FROM reference.purpose_group WHERE direction = @direction AND name_key = @key AND id IS DISTINCT FROM @except)");
        cmd.Parameters.AddWithValue("direction", direction.ToString());
        cmd.Parameters.AddWithValue("key", nameKey);
        cmd.Parameters.Add(new NpgsqlParameter("except", NpgsqlDbType.Uuid) { Value = (object?)exceptId ?? DBNull.Value });
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<int> NextGroupNumberAsync(CancellationToken cancellationToken)
    {
        // Two groups added at the same moment get the same number; the unique index refuses the second (PurposeGroup.NumberTaken).
        await using var cmd = Command("SELECT coalesce(max(number), 0) + 1 FROM reference.purpose_group");
        return (int)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<bool> GroupHasPurposesAsync(Guid groupId, CancellationToken cancellationToken)
    {
        await using var cmd = Command("SELECT EXISTS (SELECT 1 FROM reference.purpose WHERE group_id = @id)");
        cmd.Parameters.AddWithValue("id", groupId);
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddGroupAsync(PurposeGroup group, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);
        await using (var cmd = Command(
            "INSERT INTO reference.purpose_group (id, number, direction, name, name_key, ledger_account, reference1_id, reference2_id, is_active, in_use, version) " +
            "VALUES (@id, @number, @direction, @name, @key, @ledger, @r1, @r2, @active, @in_use, 1)"))
        {
            GroupParameters(cmd, group);
            cmd.Parameters.AddWithValue("number", group.Number);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(PostgresUnitOfWork.Current, group.PendingEvents, cancellationToken).ConfigureAwait(false);
        group.MarkSaved(1);
    }

    public async Task SaveGroupAsync(PurposeGroup group, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);
        await using (var cmd = Command(
            "UPDATE reference.purpose_group SET direction = @direction, name = @name, name_key = @key, ledger_account = @ledger, reference1_id = @r1, " +
            "reference2_id = @r2, is_active = @active, in_use = @in_use, version = version + 1, updated_at = now() WHERE id = @id AND version = @expected"))
        {
            GroupParameters(cmd, group);
            cmd.Parameters.AddWithValue("expected", group.Version);
            Expect1(await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), group.Id);
        }

        await PostgresOutbox.WriteAsync(PostgresUnitOfWork.Current, group.PendingEvents, cancellationToken).ConfigureAwait(false);
        group.MarkSaved(group.Version + 1);
    }

    public async Task DeleteGroupAsync(PurposeGroup group, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(group);
        await using (var cmd = Command("DELETE FROM reference.purpose_group WHERE id = @id AND version = @expected AND NOT in_use"))
        {
            cmd.Parameters.AddWithValue("id", group.Id);
            cmd.Parameters.AddWithValue("expected", group.Version);
            Expect1(await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), group.Id);
        }

        await PostgresOutbox.WriteAsync(PostgresUnitOfWork.Current, group.PendingEvents, cancellationToken).ConfigureAwait(false);
        group.MarkSaved(group.Version);
    }

    public async Task<Purpose?> GetPurposeAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = Command($"SELECT {PurposeColumns} FROM reference.purpose WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return (await ReadPurposesAsync(cmd, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Purpose>> ListPurposesAsync(Guid? groupId, bool includeInactive, CancellationToken cancellationToken)
    {
        await using var cmd = Command($"SELECT {PurposeColumns} FROM reference.purpose WHERE (@group::uuid IS NULL OR group_id = @group) AND (@all OR is_active)");
        cmd.Parameters.Add(new NpgsqlParameter("group", NpgsqlDbType.Uuid) { Value = (object?)groupId ?? DBNull.Value });
        cmd.Parameters.AddWithValue("all", includeInactive);
        return await ReadPurposesAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> PurposeNameTakenAsync(Guid groupId, string nameKey, Guid? exceptId, CancellationToken cancellationToken)
    {
        await using var cmd = Command("SELECT EXISTS (SELECT 1 FROM reference.purpose WHERE group_id = @group AND name_key = @key AND id IS DISTINCT FROM @except)");
        cmd.Parameters.AddWithValue("group", groupId);
        cmd.Parameters.AddWithValue("key", nameKey);
        cmd.Parameters.Add(new NpgsqlParameter("except", NpgsqlDbType.Uuid) { Value = (object?)exceptId ?? DBNull.Value });
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddPurposeAsync(Purpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(purpose);
        await using (var cmd = Command(
            "INSERT INTO reference.purpose (id, group_id, name, name_key, is_active, in_use, version) VALUES (@id, @group, @name, @key, @active, @in_use, 1)"))
        {
            PurposeParameters(cmd, purpose);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(PostgresUnitOfWork.Current, purpose.PendingEvents, cancellationToken).ConfigureAwait(false);
        purpose.MarkSaved(1);
    }

    public async Task SavePurposeAsync(Purpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(purpose);
        await using (var cmd = Command(
            "UPDATE reference.purpose SET group_id = @group, name = @name, name_key = @key, is_active = @active, in_use = @in_use, " +
            "version = version + 1, updated_at = now() WHERE id = @id AND version = @expected"))
        {
            PurposeParameters(cmd, purpose);
            cmd.Parameters.AddWithValue("expected", purpose.Version);
            Expect1(await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), purpose.Id);
        }

        await PostgresOutbox.WriteAsync(PostgresUnitOfWork.Current, purpose.PendingEvents, cancellationToken).ConfigureAwait(false);
        purpose.MarkSaved(purpose.Version + 1);
    }

    public async Task DeletePurposeAsync(Purpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(purpose);
        await using (var cmd = Command("DELETE FROM reference.purpose WHERE id = @id AND version = @expected"))
        {
            cmd.Parameters.AddWithValue("id", purpose.Id);
            cmd.Parameters.AddWithValue("expected", purpose.Version);
            Expect1(await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), purpose.Id);
        }

        await PostgresOutbox.WriteAsync(PostgresUnitOfWork.Current, purpose.PendingEvents, cancellationToken).ConfigureAwait(false);
        purpose.MarkSaved(purpose.Version);
    }

    private static NpgsqlCommand Command(string sql) => PostgresUnitOfWork.Current.Command(sql);

    private static void Expect1(int rows, Guid id)
    {
        if (rows != 1)
        {
            throw new ConcurrencyConflictException($"Entry {id} was changed by someone else.");
        }
    }

    private static void GroupParameters(NpgsqlCommand cmd, PurposeGroup g)
    {
        cmd.Parameters.AddWithValue("id", g.Id);
        cmd.Parameters.AddWithValue("direction", g.Direction.ToString());
        cmd.Parameters.AddWithValue("name", g.Name);
        cmd.Parameters.AddWithValue("key", g.NameKey);
        cmd.Parameters.AddWithValue("ledger", g.LedgerAccountCode);
        cmd.Parameters.Add(new NpgsqlParameter("r1", NpgsqlDbType.Uuid) { Value = (object?)g.Reference1Id ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("r2", NpgsqlDbType.Uuid) { Value = (object?)g.Reference2Id ?? DBNull.Value });
        cmd.Parameters.AddWithValue("active", g.IsActive);
        cmd.Parameters.AddWithValue("in_use", g.IsInUse);
    }

    private static void PurposeParameters(NpgsqlCommand cmd, Purpose p)
    {
        cmd.Parameters.AddWithValue("id", p.Id);
        cmd.Parameters.AddWithValue("group", p.GroupId);
        cmd.Parameters.AddWithValue("name", p.Name);
        cmd.Parameters.AddWithValue("key", p.NameKey);
        cmd.Parameters.AddWithValue("active", p.IsActive);
        cmd.Parameters.AddWithValue("in_use", p.IsInUse);
    }

    private static async Task<List<PurposeGroup>> ReadGroupsAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<PurposeGroup>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(PurposeGroup.Restore(
                r.GetGuid(0), r.GetInt32(1), Enum.Parse<PurposeDirection>(r.GetString(2)), r.GetString(3), r.GetString(4),
                r.IsDBNull(5) ? null : r.GetGuid(5), r.IsDBNull(6) ? null : r.GetGuid(6), r.GetBoolean(7), r.GetBoolean(8), r.GetInt64(9)));
        }

        return list;
    }

    private static async Task<List<Purpose>> ReadPurposesAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<Purpose>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(Purpose.Restore(r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetBoolean(3), r.GetBoolean(4), r.GetInt64(5)));
        }

        return list;
    }
}
