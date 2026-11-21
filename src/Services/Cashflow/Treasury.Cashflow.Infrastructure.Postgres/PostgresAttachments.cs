using Npgsql;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Infrastructure.Postgres;

// Written against db/postgres/034_cashflow_legal_action_files.sql.

public sealed class PostgresInstrumentAttachmentRepository : IInstrumentAttachmentRepository
{
    private const string Columns = "file_id, instrument_id, step, file_name, content_type, size, uploaded_by, uploaded_at, fiscal_year";

    public async Task<IReadOnlyList<InstrumentAttachment>> ListAsync(Guid instrumentId, CancellationToken cancellationToken)
    {
        var list = new List<InstrumentAttachment>();
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns} FROM cashflow.instrument_file WHERE instrument_id = @instrument ORDER BY uploaded_at, file_name");
        cmd.Parameters.AddWithValue("instrument", instrumentId);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await r.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(Read(r));
        }

        return list;
    }

    public async Task<(InstrumentAttachment File, byte[] Content)?> GetAsync(Guid instrumentId, Guid fileId, CancellationToken cancellationToken)
    {
        await using var cmd = PostgresUnitOfWork.Current.Command($"SELECT {Columns}, content FROM cashflow.instrument_file WHERE instrument_id = @instrument AND file_id = @file");
        cmd.Parameters.AddWithValue("instrument", instrumentId);
        cmd.Parameters.AddWithValue("file", fileId);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await r.ReadAsync(cancellationToken).ConfigureAwait(false) ? (Read(r), r.GetFieldValue<byte[]>(9)) : null;
    }

    public async Task AddAsync(InstrumentAttachment file, byte[] content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command(
            $"INSERT INTO cashflow.instrument_file ({Columns}, content) VALUES (@file, @instrument, @step, @name, @type, @size, @by, @at, @year, @content)"))
        {
            cmd.Parameters.AddWithValue("file", file.Id);
            cmd.Parameters.AddWithValue("instrument", file.InstrumentId);
            cmd.Parameters.AddWithValue("step", file.Step);
            cmd.Parameters.AddWithValue("name", file.FileName);
            cmd.Parameters.AddWithValue("type", file.ContentType);
            cmd.Parameters.AddWithValue("size", file.Size);
            cmd.Parameters.AddWithValue("by", file.UploadedBy);
            cmd.Parameters.AddWithValue("at", file.UploadedAt);
            cmd.Parameters.AddWithValue("year", file.FiscalYear);
            cmd.Parameters.AddWithValue("content", content);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, file.PendingEvents, cancellationToken).ConfigureAwait(false);
        file.MarkSaved(1);
    }

    public async Task RemoveAsync(InstrumentAttachment file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var tx = PostgresUnitOfWork.Current;
        await using (var cmd = tx.Command("DELETE FROM cashflow.instrument_file WHERE fiscal_year = @year AND file_id = @file"))
        {
            cmd.Parameters.AddWithValue("year", file.FiscalYear);
            cmd.Parameters.AddWithValue("file", file.Id);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await PostgresOutbox.WriteAsync(tx, file.PendingEvents, cancellationToken).ConfigureAwait(false);
    }

    private static InstrumentAttachment Read(NpgsqlDataReader r) => InstrumentAttachment.Restore(
        r.GetGuid(0), r.GetGuid(1), r.GetInt32(2), r.GetString(3), r.GetString(4), r.GetInt32(5), r.GetString(6), r.GetFieldValue<DateTimeOffset>(7), r.GetInt32(8));
}
