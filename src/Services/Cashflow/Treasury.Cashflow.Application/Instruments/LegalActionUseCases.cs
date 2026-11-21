using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Application.Instruments;

/// <summary>A scan of a legal action as the screen lists it (without the file itself).</summary>
public sealed record LegalFileView(Guid Id, int Step, string FileName, string ContentType, int Size, string UploadedBy, DateTimeOffset UploadedAt)
{
    public static LegalFileView From(InstrumentAttachment f) => new(f.Id, f.Step, f.FileName, f.ContentType, f.Size, f.UploadedBy, f.UploadedAt);
}

/// <summary>
/// The scans of a cheque's legal action (owner, 18 Nov 2026: the screen «اقدام حقوقی» has a place for the scans of the action's papers).
/// The action itself is the step SendToLegal (<see cref="InstrumentUseCases.ChangeStateAsync"/>) with its date and description; the
/// scans are attached after it, each in its own command, while the cheque is in legal action.
/// </summary>
public sealed class LegalActionUseCases(
    IUnitOfWork unitOfWork,
    IInstrumentRepository instruments,
    IInstrumentAttachmentRepository files,
    IFiscalPeriodPolicy fiscalPeriods,
    IIdempotencyStore idempotency,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> AttachAsync(string idempotencyKey, Guid instrumentId, string fileName, byte[] content, CancellationToken cancellationToken) =>
        _runner.RunAsync(idempotencyKey, async ct =>
        {
            var cheque = await ChequeAsync(instrumentId, ct).ConfigureAwait(false);
            var today = await OpenTodayAsync(ct).ConfigureAwait(false);
            var existing = await files.ListAsync(instrumentId, ct).ConfigureAwait(false);
            var file = InstrumentAttachment.ForLegalAction(Guid.NewGuid(), cheque, existing.Count, fileName, content, currentUser.UserId, clock.GetUtcNow(), today);
            await files.AddAsync(file, content, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(file.Id);
        }, id => id.ToString(), Guid.Parse, cancellationToken);

    public Task<Result<Guid>> RemoveAsync(string idempotencyKey, Guid instrumentId, Guid fileId, CancellationToken cancellationToken) =>
        _runner.RunAsync(idempotencyKey, async ct =>
        {
            var cheque = await ChequeAsync(instrumentId, ct).ConfigureAwait(false);
            var found = await files.GetAsync(instrumentId, fileId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Instrument.AttachmentNotFound", "The scan was not found.");
            await OpenTodayAsync(ct).ConfigureAwait(false);
            found.File.Remove(cheque, currentUser.UserId);
            await files.RemoveAsync(found.File, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(fileId);
        }, id => id.ToString(), Guid.Parse, cancellationToken);

    /// <summary>The scans of a cheque, oldest first.</summary>
    public Task<IReadOnlyList<LegalFileView>> ListAsync(Guid instrumentId, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<LegalFileView>>(async ct =>
            (await files.ListAsync(instrumentId, ct).ConfigureAwait(false)).Select(LegalFileView.From).ToList(), cancellationToken);

    public Task<(LegalFileView File, byte[] Content)?> GetAsync(Guid instrumentId, Guid fileId, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<(LegalFileView File, byte[] Content)?>(async ct =>
            await files.GetAsync(instrumentId, fileId, ct).ConfigureAwait(false) is { } f ? (LegalFileView.From(f.File), f.Content) : null, cancellationToken);

    private async Task<Instrument> ChequeAsync(Guid id, CancellationToken ct) =>
        await instruments.GetAsync(id, ct).ConfigureAwait(false) ?? throw new DomainException("Instrument.NotFound", $"Instrument {id} was not found.");

    /// <summary>A scan belongs to the fiscal year it is added in, which must be open (rule R2).</summary>
    private async Task<FiscalDate> OpenTodayAsync(CancellationToken ct)
    {
        var today = IranClock.Today(clock);
        if (!await fiscalPeriods.IsOpenAsync(today, ct).ConfigureAwait(false))
        {
            throw new DomainException("FiscalPeriod.Closed", $"The fiscal period of {today} is closed for new operations.");
        }

        return today;
    }
}
