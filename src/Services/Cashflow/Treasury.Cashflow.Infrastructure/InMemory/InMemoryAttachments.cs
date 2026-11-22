using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Infrastructure.InMemory;

/// <summary>
/// In-memory scans of legal actions (local runs and tests). Like the in-memory vouchers, a write takes effect at once: it is the last
/// step of its command.
/// </summary>
public sealed class InMemoryInstrumentAttachmentRepository : IInstrumentAttachmentRepository
{
    private readonly object _gate = new();
    private readonly List<(InstrumentAttachment File, byte[] Content)> _files = [];

    public Task<IReadOnlyList<InstrumentAttachment>> ListAsync(Guid instrumentId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<InstrumentAttachment>>(_files.Where(f => f.File.InstrumentId == instrumentId).Select(f => Copy(f.File)).ToList());
        }
    }

    public Task<(InstrumentAttachment File, byte[] Content)?> GetAsync(Guid instrumentId, Guid fileId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var i = _files.FindIndex(f => f.File.InstrumentId == instrumentId && f.File.Id == fileId);
            return Task.FromResult<(InstrumentAttachment, byte[])?>(i < 0 ? null : (Copy(_files[i].File), _files[i].Content));
        }
    }

    public Task AddAsync(InstrumentAttachment file, byte[] content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        lock (_gate)
        {
            _files.Add((file, [.. content]));
        }

        file.MarkSaved(1);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(InstrumentAttachment file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        lock (_gate)
        {
            _files.RemoveAll(f => f.File.Id == file.Id);
        }

        return Task.CompletedTask;
    }

    private static InstrumentAttachment Copy(InstrumentAttachment f) =>
        InstrumentAttachment.Restore(f.Id, f.InstrumentId, f.Step, f.FileName, f.ContentType, f.Size, f.UploadedBy, f.UploadedAt, f.FiscalYear);
}
