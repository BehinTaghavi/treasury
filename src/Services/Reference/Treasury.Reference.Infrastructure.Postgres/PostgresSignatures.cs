using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Infrastructure.Postgres;

// Written against db/postgres/040_reference_signatories.sql.

public sealed class PostgresSignatureRepository : ISignatureRepository
{
    private const string SignatoryColumns = "id, full_name, position, detail_code, national_code, mobile, phone, is_active, version";
    private const string RuleColumns = "id, document, required_count, compulsory, optional, version";

    public async Task<Signatory?> GetSignatoryAsync(Guid id, CancellationToken cancellationToken) =>
        await ReadSignatoriesAsync("WHERE id = @id", id, cancellationToken).ConfigureAwait(false) is [var s, ..] ? s : null;

    public Task<IReadOnlyList<Signatory>> ListSignatoriesAsync(bool includeInactive, CancellationToken cancellationToken) =>
        ReadSignatoriesAsync(includeInactive ? string.Empty : "WHERE is_active", null, cancellationToken);

    public Task AddSignatoryAsync(Signatory signatory, CancellationToken cancellationToken) =>
        WriteAsync(signatory, null,
            "INSERT INTO reference.signatory (id, full_name, name_key, position, detail_code, national_code, mobile, phone, is_active) " +
            "VALUES (@id, @name, @key, @position, @detail, @national, @mobile, @phone, @active)",
            cmd => SignatoryParameters(cmd, signatory), cancellationToken);

    public Task SaveSignatoryAsync(Signatory signatory, CancellationToken cancellationToken) =>
        WriteAsync(signatory, signatory.Version,
            "UPDATE reference.signatory SET full_name = @name, name_key = @key, position = @position, detail_code = @detail, national_code = @national, " +
            "mobile = @mobile, phone = @phone, is_active = @active, version = version + 1, updated_at = now() WHERE id = @id AND version = @expected",
            cmd => SignatoryParameters(cmd, signatory), cancellationToken);

    public async Task DeleteSignatoryAsync(Signatory signatory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(signatory);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command("DELETE FROM reference.signatory WHERE id = @id AND version = @expected"))
        {
            cmd.Parameters.AddWithValue("id", signatory.Id);
            cmd.Parameters.AddWithValue("expected", signatory.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Entry {signatory.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, signatory.PendingEvents, cancellationToken).ConfigureAwait(false);
        signatory.MarkSaved(signatory.Version);
    }

    public async Task<SignatureRule?> GetRuleAsync(SignedDocument document, CancellationToken cancellationToken) =>
        await ReadRulesAsync("WHERE document = @document", document, cancellationToken).ConfigureAwait(false) is [var r, ..] ? r : null;

    public Task<IReadOnlyList<SignatureRule>> ListRulesAsync(CancellationToken cancellationToken) => ReadRulesAsync(string.Empty, null, cancellationToken);

    public Task AddRuleAsync(SignatureRule rule, CancellationToken cancellationToken) =>
        WriteAsync(rule, null,
            "INSERT INTO reference.signature_rule (id, document, required_count, compulsory, optional) VALUES (@id, @document, @required, @compulsory, @optional)",
            cmd => RuleParameters(cmd, rule), cancellationToken);

    public Task SaveRuleAsync(SignatureRule rule, CancellationToken cancellationToken) =>
        WriteAsync(rule, rule.Version,
            "UPDATE reference.signature_rule SET required_count = @required, compulsory = @compulsory, optional = @optional, version = version + 1, updated_at = now() " +
            "WHERE id = @id AND document = @document AND version = @expected",
            cmd => RuleParameters(cmd, rule), cancellationToken);

    private static void SignatoryParameters(NpgsqlCommand cmd, Signatory s)
    {
        object Db(string? value) => (object?)value ?? DBNull.Value;
        cmd.Parameters.AddWithValue("id", s.Id);
        cmd.Parameters.AddWithValue("name", s.FullName);
        cmd.Parameters.AddWithValue("key", s.NameKey);
        cmd.Parameters.AddWithValue("position", s.Position);
        cmd.Parameters.Add(new NpgsqlParameter("detail", NpgsqlDbType.Text) { Value = Db(s.DetailCode) });
        cmd.Parameters.Add(new NpgsqlParameter("national", NpgsqlDbType.Text) { Value = Db(s.NationalCode) });
        cmd.Parameters.Add(new NpgsqlParameter("mobile", NpgsqlDbType.Text) { Value = Db(s.Mobile) });
        cmd.Parameters.Add(new NpgsqlParameter("phone", NpgsqlDbType.Text) { Value = Db(s.Phone) });
        cmd.Parameters.AddWithValue("active", s.IsActive);
    }

    private static void RuleParameters(NpgsqlCommand cmd, SignatureRule r)
    {
        cmd.Parameters.AddWithValue("id", r.Id);
        cmd.Parameters.AddWithValue("document", r.Document.ToString());
        cmd.Parameters.AddWithValue("required", r.Required);
        cmd.Parameters.Add(new NpgsqlParameter("compulsory", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = r.Compulsory.ToArray() });
        cmd.Parameters.Add(new NpgsqlParameter("optional", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = r.Optional.ToArray() });
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

    private static async Task<IReadOnlyList<Signatory>> ReadSignatoriesAsync(string where, Guid? id, CancellationToken ct)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {SignatoryColumns} FROM reference.signatory {where}");
        if (id is { } i)
        {
            cmd.Parameters.AddWithValue("id", i);
        }

        var list = new List<Signatory>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            string? Text(int n) => r.IsDBNull(n) ? null : r.GetString(n);
            list.Add(Signatory.Restore(r.GetGuid(0), r.GetString(1), r.GetString(2), Text(3), Text(4), Text(5), Text(6), r.GetBoolean(7), r.GetInt64(8)));
        }

        return list;
    }

    private static async Task<IReadOnlyList<SignatureRule>> ReadRulesAsync(string where, SignedDocument? document, CancellationToken ct)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {RuleColumns} FROM reference.signature_rule {where}");
        if (document is { } d)
        {
            cmd.Parameters.AddWithValue("document", d.ToString());
        }

        var list = new List<SignatureRule>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(SignatureRule.Restore(r.GetGuid(0), Enum.Parse<SignedDocument>(r.GetString(1)), r.GetInt32(2), r.GetFieldValue<Guid[]>(3), r.GetFieldValue<Guid[]>(4), r.GetInt64(5)));
        }

        return list;
    }
}
