using System.Globalization;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Ledger;
using Treasury.Reference.Domain.Organization;
using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Application.Signatures;

/// <summary>The form «دارندگان امضای مجاز (اسناد بهادار)» as the screen sends it.</summary>
/// <param name="FullName">Empty with a <paramref name="DetailCode"/>: the name of the تفصیل is taken.</param>
/// <param name="Position">One of <see cref="SignatoryPosition"/>; with Other, <paramref name="OtherPosition"/> is the typed position.</param>
public sealed record SignatoryForm(
    string? FullName, SignatoryPosition? Position, string? OtherPosition = null, string? DetailCode = null, string? NationalCode = null, string? Mobile = null,
    string? Phone = null, string? Extension = null, string? Email = null, bool SignsCheques = true, bool SignsNotes = false)
{
    public SignatoryData ToData() => new(new PersonForm(FullName, DetailCode, NationalCode, Mobile, Phone, Extension, Email), Position, OtherPosition, SignsCheques, SignsNotes);
}

public sealed record SignatoryView(
    Guid Id, string FullName, string PositionKind, string Position, string? DetailCode, string? NationalCode, string? Mobile, string? Phone, string? Extension,
    string? Email, bool SignsCheques, bool SignsNotes, bool HasSignature, bool IsActive, bool IsInUse, long Version)
{
    public static SignatoryView From(Signatory s) => new(
        s.Id, s.FullName, s.PositionKind.ToString(), s.Position, s.Person.DetailCode, s.Person.NationalCode, s.Person.Mobile, s.Person.Phone, s.Person.Extension,
        s.Person.Email, s.SignsCheques, s.SignsNotes, s.HasSignature, s.IsActive, s.IsInUse, s.Version);
}

public sealed record SignatureStateCommand(string IdempotencyKey, Guid Id, string Change, long ExpectedVersion);

/// <summary>
/// «دارندگان امضای مجاز (اسناد بهادار)» (owner, 16 Nov 2026; redesigned by his report no. 8 of 21 Nov 2026): the persons who sign the
/// company's cheques and notes, with their position, contact details, the scan of their specimen signature and what they may sign.
/// Which of them sign the cheques of a bank account, and how many signatures it needs, is set on the bank account (accounts-svc).
/// </summary>
public sealed class SignatureUseCases(IUnitOfWork unitOfWork, ISignatureRepository signatures, ILedgerQueries ledger, IIdempotencyStore idempotency)
{
    private static readonly StringComparer PersianOrder = StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true);
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> AddSignatoryAsync(string key, SignatoryForm form, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var signatory = Signatory.Register(Guid.NewGuid(), form.ToData(), await PersonLookup.DetailAsync(ledger, form.DetailCode, ct).ConfigureAwait(false));
            if (await TakenAsync(signatory, ct).ConfigureAwait(false) is { } refused)
            {
                return refused;
            }

            await signatures.AddSignatoryAsync(signatory, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(signatory.Id);
        }, cancellationToken);

    public Task<Result<Guid>> EditSignatoryAsync(string key, Guid id, SignatoryForm form, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var signatory = Check(await signatures.GetSignatoryAsync(id, ct).ConfigureAwait(false), expectedVersion, id);
            signatory.Edit(form.ToData(), await PersonLookup.DetailAsync(ledger, form.DetailCode, ct).ConfigureAwait(false));
            if (await TakenAsync(signatory, ct).ConfigureAwait(false) is { } refused)
            {
                return refused;
            }

            await signatures.SaveSignatoryAsync(signatory, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(signatory.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeSignatoryAsync(SignatureStateCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var signatory = Check(await signatures.GetSignatoryAsync(command.Id, ct).ConfigureAwait(false), command.ExpectedVersion, command.Id);
            switch (command.Change)
            {
                case "activate":
                    signatory.Activate();
                    break;
                case "deactivate":
                    signatory.Deactivate();
                    break;
                case "delete":
                    signatory.Delete();
                    await signatures.DeleteSignatoryAsync(signatory, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(signatory.Id);
                default:
                    return Result<Guid>.Fail("Request.UnknownChange", $"Unknown change '{command.Change}'.");
            }

            await signatures.SaveSignatoryAsync(signatory, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(signatory.Id);
        }, cancellationToken);

    /// <summary>Stores the scan of the specimen signature (PNG or JPEG, at most 1 MB), replacing an earlier one.</summary>
    public Task<Result<Guid>> AttachSignatureAsync(string key, Guid id, byte[] file, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(file);
            var signatory = Check(await signatures.GetSignatoryAsync(id, ct).ConfigureAwait(false), expectedVersion, id);
            signatory.AttachSignature(file);
            await signatures.SaveSignatoryAsync(signatory, ct, file).ConfigureAwait(false);
            return Result<Guid>.Ok(signatory.Id);
        }, cancellationToken);

    public Task<Result<Guid>> RemoveSignatureAsync(string key, Guid id, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            var signatory = Check(await signatures.GetSignatoryAsync(id, ct).ConfigureAwait(false), expectedVersion, id);
            signatory.RemoveSignature();
            await signatures.SaveSignatoryAsync(signatory, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(signatory.Id);
        }, cancellationToken);

    /// <summary>
    /// accounts-svc is about to save a bank account naming the signatory: from then on it is never deleted. Same pattern as the
    /// "in use" lists (ADR 0001: no service reads another's schema).
    /// </summary>
    public Task<Result<Guid>> MarkInUseAsync(string key, Guid id, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            if (await signatures.GetSignatoryAsync(id, ct).ConfigureAwait(false) is not { } signatory)
            {
                return Result<Guid>.Fail("Signatory.NotFound", $"Signatory {id} was not found.");
            }

            if (signatory.MarkInUse())
            {
                await signatures.SaveSignatoryAsync(signatory, ct).ConfigureAwait(false);
            }

            return Result<Guid>.Ok(signatory.Id);
        }, cancellationToken);

    public Task<IReadOnlyList<SignatoryView>> ListSignatoriesAsync(bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<SignatoryView>>(async ct =>
            (await signatures.ListSignatoriesAsync(includeInactive, ct).ConfigureAwait(false)).OrderBy(s => s.FullName, PersianOrder).Select(SignatoryView.From).ToList(),
            cancellationToken);

    /// <summary>One signatory, active or not (asked by accounts-svc when a bank account names it); null when unknown.</summary>
    public Task<SignatoryView?> GetSignatoryAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct => await signatures.GetSignatoryAsync(id, ct).ConfigureAwait(false) is { } s ? SignatoryView.From(s) : null, cancellationToken);

    public Task<(string ContentType, byte[] Data)?> GetSignatureAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => signatures.GetSignatureAsync(id, ct), cancellationToken);

    /// <summary>The same name or the same national code as another signatory.</summary>
    private async Task<Result<Guid>?> TakenAsync(Signatory signatory, CancellationToken ct)
    {
        var others = (await signatures.ListSignatoriesAsync(includeInactive: true, ct).ConfigureAwait(false)).Where(s => s.Id != signatory.Id).ToList();
        if (others.Any(s => s.NameKey == signatory.NameKey))
        {
            return Result<Guid>.Fail("Signatory.NameTaken", $"'{signatory.FullName}' is already a signatory.");
        }

        return signatory.Person.NationalCode is { } code && others.Any(s => s.Person.NationalCode == code)
            ? Result<Guid>.Fail("Signatory.NationalCodeTaken", $"National code {code} belongs to another signatory.")
            : null;
    }

    private static Signatory Check(Signatory? signatory, long expectedVersion, Guid id) =>
        signatory is null ? throw new DomainException("Signatory.NotFound", $"Signatory {id} was not found.")
        : signatory.Version == expectedVersion ? signatory
        : throw new ConcurrencyConflictException($"Signatory {id} was changed by someone else.");

    private Task<Result<Guid>> RunAsync(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}

/// <summary>What the تفصیل list says about the code typed on a person's form.</summary>
internal static class PersonLookup
{
    public static async Task<DetailFact?> DetailAsync(ILedgerQueries ledger, string? code, CancellationToken ct) =>
        PersianText.Clean(code) is { Length: > 0 } c && await ledger.DetailAsync(c, ct).ConfigureAwait(false) is { } d
            ? new DetailFact(d.Code, d.Name, d.IsCostCenter, d.IsActive, DetailRoles.Of(d.Type, d.IsCostCenter))
            : null;
}
