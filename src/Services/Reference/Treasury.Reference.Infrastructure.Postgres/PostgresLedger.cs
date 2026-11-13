using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Ledger;

namespace Treasury.Reference.Infrastructure.Postgres;

/// <summary>
/// The ledger aggregates in db/postgres/005_reference_ledger.sql. The header row carries the aggregate's version;
/// only changed accounts are written, in one statement each list (unnest arrays), so a 10,000-row import stays one round trip.
/// </summary>
public sealed class PostgresLedgerRepository : ILedgerRepository, ILedgerQueries
{
    public async Task<ChartOfAccounts?> GetChartAsync(CancellationToken cancellationToken)
    {
        var tx = PostgresUnitOfWork.Current;
        Guid id;
        AccountCodeFormat format;
        long version;
        await using (var cmd = tx.Command("SELECT id, group_length, general_length, sub_length, subsub_length, detail_length, version FROM reference.ledger_chart"))
        await using (var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            id = r.GetGuid(0);
            format = AccountCodeFormat.Create(r.GetInt16(1), r.GetInt16(2), r.GetInt16(3), r.GetInt16(4), r.GetInt16(5));
            version = r.GetInt64(6);
        }

        var accounts = new List<LedgerAccount>();
        await using (var cmd = tx.Command("SELECT code, name, level, parent_code, is_active, is_postable, detail1_types, detail2_types FROM reference.ledger_account"))
        await using (var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                accounts.Add(LedgerAccount.Restore(
                    r.GetString(0), r.GetString(1), (LedgerLevel)r.GetInt16(2), r.IsDBNull(3) ? null : r.GetString(3),
                    r.GetBoolean(4), r.GetBoolean(5), r.GetFieldValue<string[]>(6), r.GetFieldValue<string[]>(7)));
            }
        }

        return ChartOfAccounts.Restore(id, format, accounts, version);
    }

    public async Task<DetailAccountDirectory?> GetDetailsAsync(CancellationToken cancellationToken)
    {
        var tx = PostgresUnitOfWork.Current;
        Guid id;
        long version;
        await using (var cmd = tx.Command("SELECT id, version FROM reference.detail_directory"))
        await using (var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            id = r.GetGuid(0);
            version = r.GetInt64(1);
        }

        var details = new List<DetailAccount>();
        await using (var cmd = tx.Command("SELECT code, name, type, is_cost_center, is_active FROM reference.detail_account"))
        await using (var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                details.Add(DetailAccount.Restore(r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.GetBoolean(3), r.GetBoolean(4)));
            }
        }

        return DetailAccountDirectory.Restore(id, details, version);
    }

    public async Task SaveChartAsync(ChartOfAccounts chart, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chart);
        var tx = PostgresUnitOfWork.Current;
        var f = chart.Format;
        await SaveHeaderAsync(
            tx,
            chart.Version,
            "INSERT INTO reference.ledger_chart (id, group_length, general_length, sub_length, subsub_length, detail_length, version) VALUES (@id, @g, @k, @m, @s, @d, 1)",
            "UPDATE reference.ledger_chart SET group_length = @g, general_length = @k, sub_length = @m, subsub_length = @s, detail_length = @d, version = version + 1 WHERE id = @id AND version = @expected",
            cmd =>
            {
                cmd.Parameters.AddWithValue("id", chart.Id);
                cmd.Parameters.AddWithValue("g", (short)f.GroupLength);
                cmd.Parameters.AddWithValue("k", (short)f.GeneralLength);
                cmd.Parameters.AddWithValue("m", (short)f.SubsidiaryLength);
                cmd.Parameters.AddWithValue("s", (short)f.SubSubsidiaryLength);
                cmd.Parameters.AddWithValue("d", (short)f.DetailLength);
                cmd.Parameters.AddWithValue("expected", chart.Version);
            },
            "chart of accounts",
            cancellationToken).ConfigureAwait(false);

        var changed = chart.ChangedCodes.Select(c => chart.Find(c)!).ToList();
        if (changed.Count > 0)
        {
            // Parent and child in the same statement: foreign keys are checked at the end of the statement.
            await using var cmd = tx.Command(
                "INSERT INTO reference.ledger_account (code, name, level, parent_code, is_active, is_postable, detail1_types, detail2_types) " +
                "SELECT code, name, level, parent, active, postable, string_to_array(d1, chr(31)), string_to_array(d2, chr(31)) " +
                "FROM unnest(@code, @name, @level, @parent, @active, @postable, @d1, @d2) AS u(code, name, level, parent, active, postable, d1, d2) " +
                "ON CONFLICT (code) DO UPDATE SET name = excluded.name, is_active = excluded.is_active, is_postable = excluded.is_postable, " +
                "detail1_types = excluded.detail1_types, detail2_types = excluded.detail2_types, updated_at = now()");
            cmd.Parameters.Add(Array("code", NpgsqlDbType.Text, changed.Select(a => a.Code)));
            cmd.Parameters.Add(Array("name", NpgsqlDbType.Text, changed.Select(a => a.Name)));
            cmd.Parameters.Add(Array("level", NpgsqlDbType.Smallint, changed.Select(a => (short)a.Level)));
            cmd.Parameters.Add(Array("parent", NpgsqlDbType.Text, changed.Select(a => a.ParentCode)));
            cmd.Parameters.Add(Array("active", NpgsqlDbType.Boolean, changed.Select(a => a.IsActive)));
            cmd.Parameters.Add(Array("postable", NpgsqlDbType.Boolean, changed.Select(a => a.IsPostable)));
            // Text arrays cannot be nested in unnest: each set travels as one string joined by the unit separator (never in a name).
            cmd.Parameters.Add(Array("d1", NpgsqlDbType.Text, changed.Select(a => string.Join('\u001f', a.Detail1Types.Order(StringComparer.Ordinal)))));
            cmd.Parameters.Add(Array("d2", NpgsqlDbType.Text, changed.Select(a => string.Join('\u001f', a.Detail2Types.Order(StringComparer.Ordinal)))));
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, chart.PendingEvents, cancellationToken).ConfigureAwait(false);
        chart.MarkSaved(chart.Version + 1);
        chart.ClearChanges();
    }

    public async Task SaveDetailsAsync(DetailAccountDirectory directory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(directory);
        var tx = PostgresUnitOfWork.Current;
        await SaveHeaderAsync(
            tx,
            directory.Version,
            "INSERT INTO reference.detail_directory (id, version) VALUES (@id, 1)",
            "UPDATE reference.detail_directory SET version = version + 1 WHERE id = @id AND version = @expected",
            cmd =>
            {
                cmd.Parameters.AddWithValue("id", directory.Id);
                cmd.Parameters.AddWithValue("expected", directory.Version);
            },
            "detail account list",
            cancellationToken).ConfigureAwait(false);

        var changed = directory.ChangedCodes.Select(c => directory.Find(c)!).ToList();
        if (changed.Count > 0)
        {
            await using var cmd = tx.Command(
                "INSERT INTO reference.detail_account (code, name, name_key, type, is_cost_center, is_active) " +
                "SELECT * FROM unnest(@code, @name, @key, @type, @cost, @active) " +
                "ON CONFLICT (code) DO UPDATE SET name = excluded.name, name_key = excluded.name_key, type = excluded.type, " +
                "is_cost_center = excluded.is_cost_center, is_active = excluded.is_active, updated_at = now()");
            cmd.Parameters.Add(Array("code", NpgsqlDbType.Text, changed.Select(d => d.Code)));
            cmd.Parameters.Add(Array("name", NpgsqlDbType.Text, changed.Select(d => d.Name)));
            cmd.Parameters.Add(Array("key", NpgsqlDbType.Text, changed.Select(d => PersianText.Key(d.Name))));
            cmd.Parameters.Add(Array("type", NpgsqlDbType.Text, changed.Select(d => d.Type)));
            cmd.Parameters.Add(Array("cost", NpgsqlDbType.Boolean, changed.Select(d => d.IsCostCenter)));
            cmd.Parameters.Add(Array("active", NpgsqlDbType.Boolean, changed.Select(d => d.IsActive)));
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, directory.PendingEvents, cancellationToken).ConfigureAwait(false);
        directory.MarkSaved(directory.Version + 1);
        directory.ClearChanges();
    }

    public async Task<IReadOnlyList<LedgerAccountView>> AccountsAsync(bool postableOnly, bool includeInactive, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT code, name, level, parent_code, is_postable, is_active, detail1_types, detail2_types FROM reference.ledger_account " +
            "WHERE (NOT @postable OR is_postable) AND (@all OR is_active) ORDER BY code");
        cmd.Parameters.AddWithValue("postable", postableOnly);
        cmd.Parameters.AddWithValue("all", includeInactive);
        var list = new List<LedgerAccountView>();
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(new LedgerAccountView(
                r.GetString(0), r.GetString(1), ((LedgerLevel)r.GetInt16(2)).ToString(), r.IsDBNull(3) ? null : r.GetString(3),
                r.GetBoolean(4), r.GetBoolean(5), r.GetFieldValue<string[]>(6), r.GetFieldValue<string[]>(7)));
        }

        return list;
    }

    private const string DetailFilter =
        "WHERE (@all OR is_active) AND (@type::text IS NULL OR type = @type) " +
        "AND (CASE WHEN @types::text[] IS NULL THEN (NOT @centers OR is_cost_center) ELSE (type = ANY(@types) OR (@centers AND is_cost_center)) END) " +
        "AND (@pattern = '%%' OR code LIKE @pattern OR name_key LIKE @pattern)";

    public async Task<IReadOnlyList<DetailAccountView>> SearchDetailsAsync(
        string searchKey, string? type, bool includeInactive, int take, CancellationToken cancellationToken, bool costCentersOnly = false, int skip = 0,
        IReadOnlyList<string>? anyOfTypes = null)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT code, name, type, is_cost_center, is_active FROM reference.detail_account {DetailFilter} ORDER BY code LIMIT @take OFFSET @skip");
        DetailFilterParameters(cmd, searchKey, type, includeInactive, costCentersOnly, anyOfTypes);
        cmd.Parameters.AddWithValue("take", take);
        cmd.Parameters.AddWithValue("skip", skip);
        var list = new List<DetailAccountView>();
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(new DetailAccountView(r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.GetBoolean(3), r.GetBoolean(4)));
        }

        return list;
    }

    public async Task<int> CountDetailsAsync(
        string searchKey, string? type, bool includeInactive, bool costCentersOnly, CancellationToken cancellationToken, IReadOnlyList<string>? anyOfTypes = null)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT count(*)::int FROM reference.detail_account {DetailFilter}");
        DetailFilterParameters(cmd, searchKey, type, includeInactive, costCentersOnly, anyOfTypes);
        return (int)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    private static void DetailFilterParameters(NpgsqlCommand cmd, string searchKey, string? type, bool includeInactive, bool costCentersOnly, IReadOnlyList<string>? anyOfTypes)
    {
        cmd.Parameters.Add(new NpgsqlParameter("types", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = anyOfTypes is null ? DBNull.Value : anyOfTypes.ToArray() });
        cmd.Parameters.AddWithValue("all", includeInactive);
        cmd.Parameters.Add(new NpgsqlParameter("type", NpgsqlDbType.Text) { Value = (object?)type ?? DBNull.Value });
        cmd.Parameters.AddWithValue("pattern", "%" + searchKey.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%");
        cmd.Parameters.AddWithValue("centers", costCentersOnly);
    }

    public async Task<IReadOnlyList<DetailTypeView>> DetailTypesAsync(CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT type, count(*)::int FROM reference.detail_account WHERE is_active AND type IS NOT NULL GROUP BY type ORDER BY count(*) DESC, type");
        var list = new List<DetailTypeView>();
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(new DetailTypeView(r.GetString(0), r.GetInt32(1)));
        }

        return list;
    }

    public async Task<LedgerAccountView?> AccountAsync(string code, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT code, name, level, parent_code, is_postable, is_active, detail1_types, detail2_types FROM reference.ledger_account WHERE code = @code");
        cmd.Parameters.AddWithValue("code", code);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await r.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? new LedgerAccountView(r.GetString(0), r.GetString(1), ((LedgerLevel)r.GetInt16(2)).ToString(), r.IsDBNull(3) ? null : r.GetString(3),
                r.GetBoolean(4), r.GetBoolean(5), r.GetFieldValue<string[]>(6), r.GetFieldValue<string[]>(7))
            : null;
    }

    public async Task<DetailAccountView?> DetailAsync(string code, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT code, name, type, is_cost_center, is_active FROM reference.detail_account WHERE code = @code");
        cmd.Parameters.AddWithValue("code", code);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await r.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? new DetailAccountView(r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.GetBoolean(3), r.GetBoolean(4))
            : null;
    }

    private static async Task SaveHeaderAsync(
        TransactionContext tx, long loadedVersion, string insertSql, string updateSql, Action<NpgsqlCommand> bind, string what, CancellationToken ct)
    {
        await using var cmd = tx.Command(loadedVersion == 0 ? insertSql : updateSql);
        bind(cmd);
        try
        {
            if (await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"The {what} was changed by someone else.");
            }
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Two first saves at the same moment: the single-row index lets one win.
            throw new ConcurrencyConflictException($"The {what} was created by someone else at the same moment.");
        }
    }

    private static NpgsqlParameter Array<T>(string name, NpgsqlDbType type, IEnumerable<T> values) =>
        new(name, NpgsqlDbType.Array | type) { Value = values.ToArray() };
}
