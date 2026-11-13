using Npgsql;
using NpgsqlTypes;
using Treasury.BuildingBlocks.Application;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.ChequeBooks;

namespace Treasury.Cashflow.Infrastructure.Postgres;

// Written against db/postgres/014_cashflow_cheque_formats.sql.

public sealed class PostgresChequeFormatRepository : IChequeFormatRepository
{
    private const string Columns = "id, number, bank_account_id, print_type, image_type, is_active, version";

    public async Task<ChequeFormat?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM cashflow.cheque_format WHERE id = @id");
        cmd.Parameters.AddWithValue("id", id);
        var list = await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
        return list.Count == 0 ? null : list[0];
    }

    public async Task<IReadOnlyList<ChequeFormat>> ListAsync(Guid? bankAccountId, bool includeInactive, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            $"SELECT {Columns} FROM cashflow.cheque_format WHERE (@account::uuid IS NULL OR bank_account_id = @account) AND (@all OR is_active)");
        cmd.Parameters.Add(new NpgsqlParameter("account", NpgsqlDbType.Uuid) { Value = (object?)bankAccountId ?? DBNull.Value });
        cmd.Parameters.AddWithValue("all", includeInactive);
        return await ReadAsync(cmd, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> NextNumberAsync(CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT coalesce(max(number), 0) + 1 FROM cashflow.cheque_format");
        return (int)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<bool> IsUsedAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command("SELECT EXISTS (SELECT 1 FROM cashflow.cheque_book WHERE format_id = @id)");
        cmd.Parameters.AddWithValue("id", id);
        return (bool)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task AddAsync(ChequeFormat format, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(format);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "INSERT INTO cashflow.cheque_format (id, number, bank_account_id, print_type, image_type, is_active) VALUES (@id, @number, @account, @type, @image, @active)"))
        {
            Parameters(cmd, format);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, format.PendingEvents, cancellationToken).ConfigureAwait(false);
        format.MarkSaved(1);
    }

    public async Task SaveAsync(ChequeFormat format, CancellationToken cancellationToken, byte[]? image = null)
    {
        ArgumentNullException.ThrowIfNull(format);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "UPDATE cashflow.cheque_format SET print_type = @type, image_type = @image, is_active = @active, version = version + 1 WHERE id = @id AND version = @expected"))
        {
            Parameters(cmd, format);
            cmd.Parameters.AddWithValue("expected", format.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Cheque type {format.Id} was changed by someone else.");
            }
        }

        if (image is not null)
        {
            await using var cmd = tx.Command(
                "INSERT INTO cashflow.cheque_format_image (format_id, data) VALUES (@id, @data) ON CONFLICT (format_id) DO UPDATE SET data = excluded.data");
            cmd.Parameters.AddWithValue("id", format.Id);
            cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlDbType.Bytea) { Value = image });
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, format.PendingEvents, cancellationToken).ConfigureAwait(false);
        format.MarkSaved(format.Version + 1);
    }

    public async Task DeleteAsync(ChequeFormat format, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(format);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            "DELETE FROM cashflow.cheque_format f WHERE id = @id AND version = @expected AND NOT EXISTS (SELECT 1 FROM cashflow.cheque_book b WHERE b.format_id = f.id)"))
        {
            cmd.Parameters.AddWithValue("id", format.Id);
            cmd.Parameters.AddWithValue("expected", format.Version);
            if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            {
                throw new ConcurrencyConflictException($"Cheque type {format.Id} was changed by someone else.");
            }
        }

        await PostgresOutbox.WriteAsync(tx, format.PendingEvents, cancellationToken).ConfigureAwait(false);
        format.MarkSaved(format.Version);
    }

    public async Task<(string ContentType, byte[] Data)?> GetImageAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command(
            "SELECT f.image_type, i.data FROM cashflow.cheque_format f JOIN cashflow.cheque_format_image i ON i.format_id = f.id WHERE f.id = @id");
        cmd.Parameters.AddWithValue("id", id);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await r.ReadAsync(cancellationToken).ConfigureAwait(false) && !r.IsDBNull(0) ? (r.GetString(0), r.GetFieldValue<byte[]>(1)) : null;
    }

    private static void Parameters(NpgsqlCommand cmd, ChequeFormat f)
    {
        cmd.Parameters.AddWithValue("id", f.Id);
        cmd.Parameters.AddWithValue("number", f.Number);
        cmd.Parameters.Add(new NpgsqlParameter("account", NpgsqlDbType.Uuid) { Value = (object?)f.BankAccountId ?? DBNull.Value });
        cmd.Parameters.AddWithValue("type", f.PrintType.ToString());
        cmd.Parameters.Add(new NpgsqlParameter("image", NpgsqlDbType.Text) { Value = (object?)f.ImageType ?? DBNull.Value });
        cmd.Parameters.AddWithValue("active", f.IsActive);
    }

    private static async Task<List<ChequeFormat>> ReadAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var list = new List<ChequeFormat>();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(ChequeFormat.Restore(r.GetGuid(0), r.GetInt32(1), r.IsDBNull(2) ? null : r.GetGuid(2), Enum.Parse<ChequePrintType>(r.GetString(3)),
                r.IsDBNull(4) ? null : r.GetString(4), r.GetBoolean(5), r.GetInt64(6)));
        }

        return list;
    }
}
