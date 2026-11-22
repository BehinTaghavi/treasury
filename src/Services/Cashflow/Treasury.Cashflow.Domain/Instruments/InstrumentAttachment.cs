using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.Instruments;

/// <summary>
/// A scan attached to the legal action of a received cheque (owner, 18 Nov 2026: the screen «اقدام حقوقی» has a place for the scans of
/// the action's papers). PDF, JPEG or PNG, at most 5 MB and 20 files a cheque, as for the scans of a payment request (owner's report
/// no. 4, item 12). The file itself is kept by the repository; this is what the system knows about it. Scans are added and removed
/// while the cheque is in legal action.
/// </summary>
public sealed class InstrumentAttachment : AggregateRoot<Guid>
{
    public const int MaxBytes = 5 * 1024 * 1024;

    public const int MaxFilesPerCheque = 20;

    private InstrumentAttachment(Guid id, Guid instrumentId, int step, string fileName, string contentType, int size, string uploadedBy, DateTimeOffset uploadedAt, int fiscalYear)
        : base(id)
    {
        (InstrumentId, Step, FileName, ContentType, Size, UploadedBy, UploadedAt, FiscalYear) = (instrumentId, step, fileName, contentType, size, uploadedBy, uploadedAt, fiscalYear);
    }

    public Guid InstrumentId { get; }

    /// <summary>The step of the cheque's history the scan belongs to (the legal action).</summary>
    public int Step { get; }

    public string FileName { get; }

    /// <summary>application/pdf, image/jpeg or image/png, read from the file itself.</summary>
    public string ContentType { get; }

    public int Size { get; }

    public string UploadedBy { get; }

    public DateTimeOffset UploadedAt { get; }

    /// <summary>The fiscal year the scan was added in.</summary>
    public int FiscalYear { get; }

    /// <param name="filesAlready">How many scans the cheque already has.</param>
    /// <param name="today">Today's date; the scan belongs to its fiscal year.</param>
    public static InstrumentAttachment ForLegalAction(Guid id, Instrument cheque, int filesAlready, string? fileName, ReadOnlySpan<byte> content, string userId,
        DateTimeOffset at, FiscalDate today)
    {
        ArgumentNullException.ThrowIfNull(cheque);
        var step = cheque.Direction == InstrumentDirection.Received ? cheque.LegalActionStep : null;
        Require(step is not null, "Instrument.NotInLegalAction", "Scans are attached to a cheque that is in legal action.");
        Require(!string.IsNullOrWhiteSpace(userId), "Instrument.UserRequired", "User is required.");
        Require(content.Length > 0, "Instrument.AttachmentEmpty", "The file is empty.");
        Require(content.Length <= MaxBytes, "Instrument.AttachmentTooLarge", "A scan is at most 5 MB.");
        Require(filesAlready < MaxFilesPerCheque, "Instrument.TooManyAttachments", "At most 20 scans on one cheque.");
        var type = FileTypeOf(content) ?? throw new DomainException("Instrument.AttachmentType", "A scan is a PDF, JPEG or PNG file.");

        // Only the file's own name: browsers on Windows may send the whole path, with \ or /.
        var raw = (fileName ?? string.Empty).Trim();
        var name = raw[(raw.LastIndexOfAny(['/', '\\']) + 1)..];
        var file = new InstrumentAttachment(id, cheque.Id, step!.Value, string.IsNullOrWhiteSpace(name) ? "scan" : name[..Math.Min(name.Length, 200)], type, content.Length,
            userId, at, today.JalaliYear);
        file.Raise(new InstrumentFileAttached(cheque.Id, id, file.Step, file.FileName, userId));
        return file;
    }

    /// <summary>A scan attached by mistake is removed while the cheque is still in legal action.</summary>
    public void Remove(Instrument cheque, string userId)
    {
        ArgumentNullException.ThrowIfNull(cheque);
        Require(cheque.Id == InstrumentId && cheque.LegalActionStep == Step, "Instrument.NotInLegalAction", "Scans are removed while the cheque is in legal action.");
        Require(!string.IsNullOrWhiteSpace(userId), "Instrument.UserRequired", "User is required.");
        Raise(new InstrumentFileRemoved(InstrumentId, Id, userId));
    }

    public static InstrumentAttachment Restore(Guid id, Guid instrumentId, int step, string fileName, string contentType, int size, string uploadedBy, DateTimeOffset uploadedAt,
        int fiscalYear)
    {
        var file = new InstrumentAttachment(id, instrumentId, step, fileName, contentType, size, uploadedBy, uploadedAt, fiscalYear);
        file.SetLoadedVersion(1);
        return file;
    }

    private static string? FileTypeOf(ReadOnlySpan<byte> b) =>
        b.Length >= 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46 ? "application/pdf"
        : b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ? "image/png"
        : b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ? "image/jpeg"
        : null;

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

public sealed record InstrumentFileAttached(Guid InstrumentId, Guid FileId, int Step, string FileName, string UserId) : DomainEvent;

public sealed record InstrumentFileRemoved(Guid InstrumentId, Guid FileId, string UserId) : DomainEvent;
