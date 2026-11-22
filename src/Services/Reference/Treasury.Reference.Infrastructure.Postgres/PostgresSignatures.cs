using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Infrastructure.Postgres;

// Written against db/postgres/034_reference_signatories.sql and 048_reference_signatories_and_permission_holders.sql.

/// <summary>What the two repositories of persons with a signature share: the person's columns, version-checked writes and the scan.</summary>
internal static class PersonRows
{
    public const string Columns = "full_name, detail_code, national_code, mobile, phone, extension, email";

    public static PersonParticulars Read(NpgsqlDataReader r, int first)
    {
        string? Text(int n) => r.IsDBNull(n) ? null : r.GetString(n);
        return new PersonParticulars(r.GetString(first), Text(first + 1), Text(first + 2), Text(first + 3), Text(first + 4), Text(first + 5), Text(first + 6));
    }

    public static void Bind(NpgsqlCommand cmd, Guid id, PersonParticulars p, string? signatureType, bool isActive)
    {
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("name", p.FullName);
        cmd.Parameters.AddWithValue("key", p.NameKey);
        Text(cmd, "detail", p.DetailCode);
        Text(cmd, "national", p.NationalCode);
        Text(cmd, "mobile", p.Mobile);
        Text(cmd, "phone", p.Phone);
        Text(cmd, "extension", p.Extension);
        Text(cmd, "email", p.Email);
        Text(cmd, "signature", signatureType);
        cmd.Parameters.AddWithValue("active", isActive);
    }

    public static void Text(NpgsqlCommand cmd, string name, string? value) =>
        cmd.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Text) { Value = (object?)value ?? DBNull.Value });

    /// <summary>Insert (expected null), version-checked update or version-checked delete; one row or a conflict.</summary>
    public static async Task ExecuteAsync(TransactionContext tx, Guid id, long? expected, string sql, Action<NpgsqlCommand> parameters, CancellationToken ct)
    {
        await using var cmd = tx.Command(sql);
        parameters(cmd);
        if (expected is { } v)
        {
            cmd.Parameters.AddWithValue("expected", v);
        }

        if (await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1)
        {
            throw new ConcurrencyConflictException($"Entry {id} was changed by someone else.");
        }
    }

    /// <summary>Keeps the scan table in step with the row: no signature type, no scan; a new scan replaces the stored one.</summary>
    public static async Task WriteScanAsync(TransactionContext tx, string table, string keyColumn, Guid id, bool hasSignature, byte[]? scan, CancellationToken ct)
    {
        if (!hasSignature)
        {
            await using var del = tx.Command($"DELETE FROM reference.{table} WHERE {keyColumn} = @id");
            del.Parameters.AddWithValue("id", id);
            await del.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        else if (scan is not null)
        {
            await using var cmd = tx.Command($"INSERT INTO reference.{table} ({keyColumn}, data) VALUES (@id, @data) ON CONFLICT ({keyColumn}) DO UPDATE SET data = excluded.data");
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlDbType.Bytea) { Value = scan });
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

    public static async Task<(string ContentType, byte[] Data)?> ReadScanAsync(string owner, string table, string keyColumn, Guid id, CancellationToken ct)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT o.signature_type, s.data FROM reference.{owner} o JOIN reference.{table} s ON s.{keyColumn} = o.id WHERE o.id = @id");
        cmd.Parameters.AddWithValue("id", id);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await r.ReadAsync(ct).ConfigureAwait(false) && !r.IsDBNull(0) ? (r.GetString(0), r.GetFieldValue<byte[]>(1)) : null;
    }
}

public sealed class PostgresSignatureRepository : ISignatureRepository
{
    private const string Columns = "id, " + PersonRows.Columns + ", position_kind, position, signs_cheques, signs_notes, signature_type, is_active, in_use, version";

    public async Task<Signatory?> GetSignatoryAsync(Guid id, CancellationToken cancellationToken) =>
        await ReadAsync("WHERE id = @id", id, cancellationToken).ConfigureAwait(false) is [var s, ..] ? s : null;

    public Task<IReadOnlyList<Signatory>> ListSignatoriesAsync(bool includeInactive, CancellationToken cancellationToken) =>
        ReadAsync(includeInactive ? string.Empty : "WHERE is_active", null, cancellationToken);

    public async Task AddSignatoryAsync(Signatory signatory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(signatory);
        var tx = PostgresUnitOfWork.Current;
        await PersonRows.ExecuteAsync(tx, signatory.Id, null,
            "INSERT INTO reference.signatory (id, full_name, name_key, detail_code, national_code, mobile, phone, extension, email, position_kind, position, " +
            "signs_cheques, signs_notes, signature_type, is_active, in_use) " +
            "VALUES (@id, @name, @key, @detail, @national, @mobile, @phone, @extension, @email, @kind, @position, @cheques, @notes, @signature, @active, @in_use)",
            cmd => Bind(cmd, signatory), cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, signatory.PendingEvents, cancellationToken).ConfigureAwait(false);
        signatory.MarkSaved(1);
    }

    public async Task SaveSignatoryAsync(Signatory signatory, CancellationToken cancellationToken, byte[]? scan = null)
    {
        ArgumentNullException.ThrowIfNull(signatory);
        var tx = PostgresUnitOfWork.Current;
        await PersonRows.ExecuteAsync(tx, signatory.Id, signatory.Version,
            "UPDATE reference.signatory SET full_name = @name, name_key = @key, detail_code = @detail, national_code = @national, mobile = @mobile, phone = @phone, " +
            "extension = @extension, email = @email, position_kind = @kind, position = @position, signs_cheques = @cheques, signs_notes = @notes, " +
            "signature_type = @signature, is_active = @active, in_use = @in_use, version = version + 1, updated_at = now() WHERE id = @id AND version = @expected",
            cmd => Bind(cmd, signatory), cancellationToken).ConfigureAwait(false);
        await PersonRows.WriteScanAsync(tx, "signatory_signature", "signatory_id", signatory.Id, signatory.HasSignature, scan, cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, signatory.PendingEvents, cancellationToken).ConfigureAwait(false);
        signatory.MarkSaved(signatory.Version + 1);
    }

    public async Task DeleteSignatoryAsync(Signatory signatory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(signatory);
        var tx = PostgresUnitOfWork.Current;
        // "NOT in_use": a bank account that named the signatory since it was loaded refuses the delete.
        await PersonRows.ExecuteAsync(tx, signatory.Id, signatory.Version, "DELETE FROM reference.signatory WHERE id = @id AND version = @expected AND NOT in_use",
            cmd => cmd.Parameters.AddWithValue("id", signatory.Id), cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, signatory.PendingEvents, cancellationToken).ConfigureAwait(false);
        signatory.MarkSaved(signatory.Version);
    }

    public Task<(string ContentType, byte[] Data)?> GetSignatureAsync(Guid signatoryId, CancellationToken cancellationToken) =>
        PersonRows.ReadScanAsync("signatory", "signatory_signature", "signatory_id", signatoryId, cancellationToken);

    private static void Bind(NpgsqlCommand cmd, Signatory s)
    {
        PersonRows.Bind(cmd, s.Id, s.Person, s.SignatureType, s.IsActive);
        cmd.Parameters.AddWithValue("kind", s.PositionKind.ToString());
        cmd.Parameters.AddWithValue("position", s.Position);
        cmd.Parameters.AddWithValue("cheques", s.SignsCheques);
        cmd.Parameters.AddWithValue("notes", s.SignsNotes);
        cmd.Parameters.AddWithValue("in_use", s.IsInUse);
    }

    private static async Task<IReadOnlyList<Signatory>> ReadAsync(string where, Guid? id, CancellationToken ct)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM reference.signatory {where}");
        if (id is { } i)
        {
            cmd.Parameters.AddWithValue("id", i);
        }

        var list = new List<Signatory>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(Signatory.Restore(
                r.GetGuid(0), PersonRows.Read(r, 1), Enum.Parse<SignatoryPosition>(r.GetString(8)), r.GetString(9), r.GetBoolean(10), r.GetBoolean(11),
                r.IsDBNull(12) ? null : r.GetString(12), r.GetBoolean(13), r.GetBoolean(14), r.GetInt64(15)));
        }

        return list;
    }
}

public sealed class PostgresPermissionHolderRepository : IPermissionHolderRepository
{
    private const string Columns = "id, " + PersonRows.Columns + ", job_title, signature_type, is_active, version";

    public async Task<PermissionHolder?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await ReadAsync("WHERE id = @id", id, cancellationToken).ConfigureAwait(false) is [var h, ..] ? h : null;

    public Task<IReadOnlyList<PermissionHolder>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        ReadAsync(includeInactive ? string.Empty : "WHERE is_active", null, cancellationToken);

    public async Task AddAsync(PermissionHolder holder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(holder);
        var tx = PostgresUnitOfWork.Current;
        await PersonRows.ExecuteAsync(tx, holder.Id, null,
            "INSERT INTO reference.permission_holder (id, full_name, name_key, detail_code, national_code, mobile, phone, extension, email, job_title, signature_type, is_active) " +
            "VALUES (@id, @name, @key, @detail, @national, @mobile, @phone, @extension, @email, @job, @signature, @active)",
            cmd => Bind(cmd, holder), cancellationToken).ConfigureAwait(false);
        await WriteRolesAsync(tx, holder, cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, holder.PendingEvents, cancellationToken).ConfigureAwait(false);
        holder.MarkSaved(1);
    }

    public async Task SaveAsync(PermissionHolder holder, CancellationToken cancellationToken, byte[]? scan = null)
    {
        ArgumentNullException.ThrowIfNull(holder);
        var tx = PostgresUnitOfWork.Current;
        await PersonRows.ExecuteAsync(tx, holder.Id, holder.Version,
            "UPDATE reference.permission_holder SET full_name = @name, name_key = @key, detail_code = @detail, national_code = @national, mobile = @mobile, " +
            "phone = @phone, extension = @extension, email = @email, job_title = @job, signature_type = @signature, is_active = @active, version = version + 1, " +
            "updated_at = now() WHERE id = @id AND version = @expected",
            cmd => Bind(cmd, holder), cancellationToken).ConfigureAwait(false);
        await WriteRolesAsync(tx, holder, cancellationToken).ConfigureAwait(false);
        await PersonRows.WriteScanAsync(tx, "permission_holder_signature", "holder_id", holder.Id, holder.HasSignature, scan, cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, holder.PendingEvents, cancellationToken).ConfigureAwait(false);
        holder.MarkSaved(holder.Version + 1);
    }

    public async Task DeleteAsync(PermissionHolder holder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(holder);
        var tx = PostgresUnitOfWork.Current;
        await PersonRows.ExecuteAsync(tx, holder.Id, holder.Version, "DELETE FROM reference.permission_holder WHERE id = @id AND version = @expected",
            cmd => cmd.Parameters.AddWithValue("id", holder.Id), cancellationToken).ConfigureAwait(false);
        await PostgresOutbox.WriteAsync(tx, holder.PendingEvents, cancellationToken).ConfigureAwait(false);
        holder.MarkSaved(holder.Version);
    }

    public Task<(string ContentType, byte[] Data)?> GetSignatureAsync(Guid holderId, CancellationToken cancellationToken) =>
        PersonRows.ReadScanAsync("permission_holder", "permission_holder_signature", "holder_id", holderId, cancellationToken);

    private static void Bind(NpgsqlCommand cmd, PermissionHolder h)
    {
        PersonRows.Bind(cmd, h.Id, h.Person, h.SignatureType, h.IsActive);
        cmd.Parameters.AddWithValue("job", h.JobTitle);
    }

    /// <summary>The roles are replaced as a whole: the form sends every ticked role with its operations.</summary>
    private static async Task WriteRolesAsync(TransactionContext tx, PermissionHolder h, CancellationToken ct)
    {
        await using (var del = tx.Command("DELETE FROM reference.permission_holder_role WHERE holder_id = @id"))
        {
            del.Parameters.AddWithValue("id", h.Id);
            await del.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        foreach (var role in h.Roles)
        {
            await using var cmd = tx.Command("INSERT INTO reference.permission_holder_role (holder_id, role, title, operations, place) VALUES (@id, @role, @title, @operations, @place)");
            PersonRows.Text(cmd, "place", role.Place?.ToString());
            cmd.Parameters.AddWithValue("id", h.Id);
            cmd.Parameters.AddWithValue("role", role.Kind.ToString());
            cmd.Parameters.AddWithValue("title", role.Title);
            cmd.Parameters.Add(new NpgsqlParameter("operations", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = role.Operations.Select(o => o.ToString()).ToArray() });
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

    private static async Task<IReadOnlyList<PermissionHolder>> ReadAsync(string where, Guid? id, CancellationToken ct)
    {
        var tx = PostgresUnitOfWork.Current;
        var roles = new Dictionary<Guid, List<PermissionRole>>();
        await using (var cmd = tx.Command($"SELECT holder_id, role, title, operations, place FROM reference.permission_holder_role {(id is null ? string.Empty : "WHERE holder_id = @id")}"))
        {
            if (id is { } i)
            {
                cmd.Parameters.AddWithValue("id", i);
            }

            await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await r.ReadAsync(ct).ConfigureAwait(false))
            {
                if (!roles.TryGetValue(r.GetGuid(0), out var list))
                {
                    list = [];
                    roles[r.GetGuid(0)] = list;
                }

                // An operation the code no longer knows (dropped from the list) is left out instead of failing the page.
                var operations = r.GetFieldValue<string[]>(3).Select(o => Enum.TryParse<TreasuryOperation>(o, out var op) ? op : (TreasuryOperation?)null).OfType<TreasuryOperation>().Order().ToList();
                list.Add(new PermissionRole(Enum.Parse<PermissionRoleKind>(r.GetString(1)), r.GetString(2), operations, r.IsDBNull(4) ? null : Enum.Parse<OtherRolePlace>(r.GetString(4))));
            }
        }

        var holders = new List<PermissionHolder>();
        await using (var cmd = tx.Command($"SELECT {Columns} FROM reference.permission_holder {where}"))
        {
            if (id is { } i)
            {
                cmd.Parameters.AddWithValue("id", i);
            }

            await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await r.ReadAsync(ct).ConfigureAwait(false))
            {
                var holderId = r.GetGuid(0);
                holders.Add(PermissionHolder.Restore(
                    holderId, PersonRows.Read(r, 1), r.GetString(8), roles.GetValueOrDefault(holderId) ?? [], r.IsDBNull(9) ? null : r.GetString(9), r.GetBoolean(10), r.GetInt64(11)));
            }
        }

        return holders;
    }
}
