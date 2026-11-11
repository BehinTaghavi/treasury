using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Infrastructure.Postgres;

// Written against Npgsql 10 and db/postgres/004_reference_catalogue.sql, 007_reference_catalogue_corrections.sql.
// Integration tests against PostgreSQL 17: tests/Treasury.Reference.Infrastructure.Postgres.Tests.

public static class PostgresRegistration
{
    /// <summary>Replaces the in-memory adapters. The connection string must log in as the reference_svc role.</summary>
    public static IServiceCollection AddReferencePostgres(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IUnitOfWork, PostgresUnitOfWork>();
        services.AddSingleton<ICatalogueRepository, PostgresCatalogueRepository>();
        services.AddSingleton<IIdempotencyStore, PostgresIdempotencyStore>();
        services.AddSingleton<PostgresLedgerRepository>();
        services.AddSingleton<ILedgerRepository>(sp => sp.GetRequiredService<PostgresLedgerRepository>());
        services.AddSingleton<ILedgerQueries>(sp => sp.GetRequiredService<PostgresLedgerRepository>());
        return services;
    }
}

internal sealed class TransactionContext(NpgsqlConnection connection, NpgsqlTransaction transaction)
{
    public NpgsqlCommand Command(string sql) => new(sql, connection, transaction);
}

public sealed class PostgresUnitOfWork(NpgsqlDataSource dataSource) : IUnitOfWork
{
    private static readonly AsyncLocal<TransactionContext?> Ambient = new();

    internal static TransactionContext Current =>
        Ambient.Value ?? throw new InvalidOperationException("Database access must run inside IUnitOfWork.ExecuteAsync.");

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
        Ambient.Value = new TransactionContext(connection, transaction);
        try
        {
            var result = await work(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.TableName == "catalogue_item")
        {
            // Two users added the same name (or currency code) at the same moment: the index decides, the second gets the normal message.
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw ex.ConstraintName == "catalogue_item_unique_currency"
                ? new DomainException("Catalogue.CodeTaken", "This currency already exists.")
                : new DomainException("Catalogue.NameTaken", "This name is already in the list.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation && ex.TableName == "catalogue_item")
        {
            // A province or city was added under the entry while it was being deleted.
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw new DomainException("Catalogue.HasChildren", "Entries under this one must be deleted first.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        finally
        {
            Ambient.Value = null;
        }
    }
}

public sealed class PostgresCatalogueRepository : ICatalogueRepository
{
    private const string Columns = "id, kind, parent_id, name, code, is_active, version, in_use";

    public async Task<CatalogueItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM reference.catalogue_item WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        var list = await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
        return list.Count == 0 ? null : list[0];
    }

    public async Task<bool> NameTakenAsync(CatalogueKind kind, Guid? parentId, string nameKey, Guid? exceptId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT EXISTS (SELECT 1 FROM reference.catalogue_item WHERE kind = @kind AND parent_id IS NOT DISTINCT FROM @parent " +
            "AND name_key = @key AND id IS DISTINCT FROM @except)");
        cmd.Parameters.AddWithValue("kind", kind.ToString());
        cmd.Parameters.Add(new NpgsqlParameter("parent", NpgsqlDbType.Uuid) { Value = (object?)parentId ?? DBNull.Value });
        cmd.Parameters.AddWithValue("key", nameKey);
        cmd.Parameters.Add(new NpgsqlParameter("except", NpgsqlDbType.Uuid) { Value = (object?)exceptId ?? DBNull.Value });
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<bool> CurrencyCodeTakenAsync(string code, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT EXISTS (SELECT 1 FROM reference.catalogue_item WHERE code = @code)");
        cmd.Parameters.AddWithValue("code", code);
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(CatalogueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "INSERT INTO reference.catalogue_item (id, kind, parent_id, name, name_key, code, is_active, version) " +
            "VALUES (@id, @kind, @parent, @name, @key, @code, @active, 1)"))
        {
            cmd.Parameters.AddWithValue("id", item.Id);
            cmd.Parameters.AddWithValue("kind", item.Kind.ToString());
            cmd.Parameters.Add(new NpgsqlParameter("parent", NpgsqlDbType.Uuid) { Value = (object?)item.ParentId ?? DBNull.Value });
            cmd.Parameters.AddWithValue("name", item.Name);
            cmd.Parameters.AddWithValue("key", item.NameKey);
            cmd.Parameters.Add(new NpgsqlParameter("code", NpgsqlDbType.Char) { Value = (object?)item.Code ?? DBNull.Value });
            cmd.Parameters.AddWithValue("active", item.IsActive);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, item.PendingEvents, cancellationToken).ConfigureAwait(false);
        item.MarkSaved(1);
    }

    public async Task SaveAsync(CatalogueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE reference.catalogue_item SET name = @name, name_key = @key, parent_id = @parent, code = @code, is_active = @active, " +
            "in_use = @in_use, version = version + 1, updated_at = now() WHERE id = @id AND version = @expected"))
        {
            cmd.Parameters.AddWithValue("name", item.Name);
            cmd.Parameters.Add(new NpgsqlParameter("parent", NpgsqlDbType.Uuid) { Value = (object?)item.ParentId ?? DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("code", NpgsqlDbType.Char) { Value = (object?)item.Code ?? DBNull.Value });
            cmd.Parameters.AddWithValue("in_use", item.IsInUse);
            cmd.Parameters.AddWithValue("key", item.NameKey);
            cmd.Parameters.AddWithValue("active", item.IsActive);
            cmd.Parameters.AddWithValue("id", item.Id);
            cmd.Parameters.AddWithValue("expected", item.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Entry {item.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, item.PendingEvents, cancellationToken).ConfigureAwait(false);
        item.MarkSaved(item.Version + 1);
    }

    public async Task DeleteAsync(CatalogueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        var tx = PostgresUnitOfWork.Current;
        // "NOT in_use": another service may have marked the entry in the meantime; then the delete is refused like a conflict.
        await using (var cmd = tx.Command("DELETE FROM reference.catalogue_item WHERE id = @id AND version = @expected AND NOT in_use"))
        {
            cmd.Parameters.AddWithValue("id", item.Id);
            cmd.Parameters.AddWithValue("expected", item.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Entry {item.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, item.PendingEvents, cancellationToken).ConfigureAwait(false);
        item.MarkSaved(item.Version);
    }

    public async Task<IReadOnlyList<CatalogueItem>> ListAsync(CatalogueKind kind, Guid? parentId, bool includeInactive, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT {Columns} FROM reference.catalogue_item WHERE kind = @kind AND (@parent::uuid IS NULL OR parent_id = @parent) AND (@all OR is_active)");
        cmd.Parameters.AddWithValue("kind", kind.ToString());
        cmd.Parameters.Add(new NpgsqlParameter("parent", NpgsqlDbType.Uuid) { Value = (object?)parentId ?? DBNull.Value });
        cmd.Parameters.AddWithValue("all", includeInactive);
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<CatalogueItem>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<CatalogueItem>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(CatalogueItem.Restore(
                r.GetGuid(0),
                Enum.Parse<CatalogueKind>(r.GetString(1)),
                r.IsDBNull(2) ? null : r.GetGuid(2),
                r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4).Trim(),
                r.GetBoolean(5),
                r.GetInt64(6),
                r.GetBoolean(7)));
        }

        return list;
    }
}

public sealed class PostgresIdempotencyStore(NpgsqlDataSource dataSource) : IIdempotencyStore
{
    public async Task<string?> GetResultAsync(string key, CancellationToken cancellationToken)
    {
        await using var cmd = dataSource.CreateCommand("SELECT result FROM reference.idempotency WHERE key = @key");
        cmd.Parameters.AddWithValue("key", key);
        return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
    }

    public async Task SaveResultAsync(string key, string result, CancellationToken cancellationToken)
    {
        // Same transaction as the command: if the command is rolled back, so is its idempotency record.
        await using var cmd = PostgresUnitOfWork.Current.Command("INSERT INTO reference.idempotency (key, result) VALUES (@key, @result)");
        cmd.Parameters.AddWithValue("key", key);
        cmd.Parameters.AddWithValue("result", result);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal static class PostgresOutbox
{
    public static async Task WriteAsync(TransactionContext tx, IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var message in events.Select(OutboxMessage.From))
        {
            await using var cmd = tx.Command("INSERT INTO reference.outbox (id, type, payload, occurred_at) VALUES (@id, @type, @payload, @at)");
            cmd.Parameters.AddWithValue("id", message.Id);
            cmd.Parameters.AddWithValue("type", message.Type);
            cmd.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = message.Payload });
            cmd.Parameters.AddWithValue("at", message.OccurredAt);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }
}
