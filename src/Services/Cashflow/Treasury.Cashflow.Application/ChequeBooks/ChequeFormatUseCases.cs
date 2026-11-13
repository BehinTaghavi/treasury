using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.ChequeBooks;

namespace Treasury.Cashflow.Application.ChequeBooks;

public sealed record ChequeFormatView(Guid Id, int Number, Guid? BankAccountId, ChequePrintType PrintType, bool HasImage, bool IsActive, long Version)
{
    public static ChequeFormatView From(ChequeFormat f) => new(f.Id, f.Number, f.BankAccountId, f.PrintType, f.ImageType is not null, f.IsActive, f.Version);
}

/// <summary>
/// Cheque types of our bank accounts (new-version manual item 7 "معرفی انواع چک و چاپ آنها"): one per account and print type, with
/// a scan of the cheque. Each write is one command in one transaction on one type.
/// </summary>
public sealed class ChequeFormatUseCases(IUnitOfWork unitOfWork, IChequeFormatRepository formats, IBankAccountLookup bankAccounts, IIdempotencyStore idempotency)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    /// <param name="bankAccountId">The account; null: the standard type for all accounts (owner's report no. 3, item 3).</param>
    public async Task<Result<Guid>> RegisterAsync(string key, Guid? bankAccountId, ChequePrintType printType, CancellationToken cancellationToken)
    {
        // Asked before the transaction: a call to another service never holds a database transaction open.
        try
        {
            if (bankAccountId is not { } accountId)
            {
                return await SaveNewAsync(key, null, printType, cancellationToken).ConfigureAwait(false);
            }

            var account = await bankAccounts.GetAsync(accountId, cancellationToken).ConfigureAwait(false);
            if (account is null)
            {
                return Result<Guid>.Fail("ChequeBook.BankAccountNotFound", "The bank account is not one of the company's bank accounts.");
            }

            if (!account.IsActive)
            {
                return Result<Guid>.Fail("ChequeBook.BankAccountInactive", "The bank account is switched off.");
            }

            // From now on the account is never deleted (owner's report no. 3, item 1).
            await bankAccounts.MarkInUseAsync(accountId, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Fail(ex.Code, ex.Message);
        }

        return await SaveNewAsync(key, bankAccountId, printType, cancellationToken).ConfigureAwait(false);
    }

    private Task<Result<Guid>> SaveNewAsync(string key, Guid? bankAccountId, ChequePrintType printType, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            var format = ChequeFormat.Register(Guid.NewGuid(), await formats.NextNumberAsync(ct).ConfigureAwait(false), bankAccountId, printType);
            if (await TakenAsync(format, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("ChequeFormat.Taken", "This bank account already has this cheque type.");
            }

            await formats.AddAsync(format, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(format.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangePrintTypeAsync(string key, Guid id, ChequePrintType printType, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            var format = await LoadAsync(id, expectedVersion, ct).ConfigureAwait(false);
            format.ChangePrintType(printType);
            if (await TakenAsync(format, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("ChequeFormat.Taken", "This bank account already has this cheque type.");
            }

            await formats.SaveAsync(format, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(format.Id);
        }, cancellationToken);

    /// <summary>Stores the scan of the cheque (PNG or JPEG, at most 2 MB), replacing an earlier one.</summary>
    public Task<Result<Guid>> AttachImageAsync(string key, Guid id, byte[] file, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(file);
            var format = await LoadAsync(id, expectedVersion, ct).ConfigureAwait(false);
            format.AttachImage(file);
            await formats.SaveAsync(format, ct, file).ConfigureAwait(false);
            return Result<Guid>.Ok(format.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeStateAsync(string key, Guid id, string change, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            var format = await LoadAsync(id, expectedVersion, ct).ConfigureAwait(false);
            switch (change)
            {
                case "activate":
                    format.Activate();
                    break;
                case "deactivate":
                    format.Deactivate();
                    break;
                case "delete":
                    format.Delete(await formats.IsUsedAsync(format.Id, ct).ConfigureAwait(false));
                    await formats.DeleteAsync(format, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(format.Id);
                default:
                    return Result<Guid>.Fail("Request.UnknownChange", $"Unknown change '{change}'.");
            }

            await formats.SaveAsync(format, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(format.Id);
        }, cancellationToken);

    public Task<IReadOnlyList<ChequeFormatView>> ListAsync(Guid? bankAccountId, bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<ChequeFormatView>>(async ct =>
            (await formats.ListAsync(bankAccountId, includeInactive, ct).ConfigureAwait(false)).OrderBy(f => f.Number).Select(ChequeFormatView.From).ToList(), cancellationToken);

    public Task<(string ContentType, byte[] Data)?> GetImageAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => formats.GetImageAsync(id, ct), cancellationToken);

    /// <summary>The same print type twice for one account, or twice among the standard types.</summary>
    private async Task<bool> TakenAsync(ChequeFormat format, CancellationToken ct) =>
        (await formats.ListAsync(format.BankAccountId, includeInactive: true, ct).ConfigureAwait(false))
            .Any(f => f.Id != format.Id && f.BankAccountId == format.BankAccountId && f.PrintType == format.PrintType);

    private async Task<ChequeFormat> LoadAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var format = await formats.GetAsync(id, ct).ConfigureAwait(false) ?? throw new DomainException("ChequeFormat.NotFound", $"Cheque type {id} was not found.");
        return format.Version == expectedVersion ? format : throw new ConcurrencyConflictException($"Cheque type {id} was changed by someone else.");
    }

    private Task<Result<Guid>> RunAsync(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
