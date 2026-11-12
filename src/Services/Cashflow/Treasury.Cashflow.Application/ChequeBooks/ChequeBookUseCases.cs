using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.ChequeBooks;

namespace Treasury.Cashflow.Application.ChequeBooks;

public sealed record RegisterChequeBookCommand(string IdempotencyKey, Guid BankAccountId, string Serial, long FirstSheet, long LastSheet);

public sealed record CorrectChequeBookCommand(string IdempotencyKey, Guid Id, string Serial, long FirstSheet, long LastSheet, long ExpectedVersion);

/// <summary><paramref name="Change"/>: "activate", "deactivate" or "delete".</summary>
public sealed record ChangeChequeBookStateCommand(string IdempotencyKey, Guid Id, string Change, long ExpectedVersion);

/// <param name="NextSheet">The sheet the issue form will suggest; null when none is left or the book is off.</param>
public sealed record ChequeBookView(
    Guid Id, Guid BankAccountId, string Serial, long FirstSheet, long LastSheet, long SheetCount, long UsedSheets, long RemainingSheets,
    long? NextSheet, bool IsActive, bool IsInUse, long Version)
{
    public static ChequeBookView From(ChequeBook b) => new(
        b.Id, b.BankAccountId, b.Serial, b.FirstSheet, b.LastSheet, b.SheetCount, b.UsedSheets.Count, b.RemainingSheets, b.SuggestNext(),
        b.IsActive, b.IsInUse, b.Version);
}

/// <summary>
/// The cheque books screen (new-version manual item 6 "معرفی دسته چک ها"): register for one of our active bank accounts, correct or
/// delete while no sheet has been used, switch off and on. Sheets are handed out only by InstrumentUseCases.IssueChequeAsync.
/// </summary>
public sealed class ChequeBookUseCases(IUnitOfWork unitOfWork, IChequeBookRepository books, IBankAccountLookup bankAccounts, IIdempotencyStore idempotency)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public async Task<Result<Guid>> RegisterAsync(RegisterChequeBookCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Asked before the transaction: a call to another service never holds a database transaction open.
        BankAccountFact? account;
        try
        {
            account = await bankAccounts.GetAsync(command.BankAccountId, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Fail(ex.Code, ex.Message);
        }

        if (account is null)
        {
            return Result<Guid>.Fail("ChequeBook.BankAccountNotFound", "The bank account is not one of the company's bank accounts.");
        }

        if (!account.IsActive)
        {
            return Result<Guid>.Fail("ChequeBook.BankAccountInactive", "The bank account is switched off.");
        }

        return await RunAsync(command.IdempotencyKey, async ct =>
        {
            var book = ChequeBook.Register(Guid.NewGuid(), command.BankAccountId, command.Serial, command.FirstSheet, command.LastSheet);
            if (await OverlapsAsync(book, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("ChequeBook.Overlaps", "Another cheque book of this account and series already has these sheet numbers.");
            }

            await books.AddAsync(book, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(book.Id);
        }, cancellationToken).ConfigureAwait(false);
    }

    public Task<Result<Guid>> CorrectAsync(CorrectChequeBookCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var book = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            book.Correct(command.Serial, command.FirstSheet, command.LastSheet);
            if (await OverlapsAsync(book, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("ChequeBook.Overlaps", "Another cheque book of this account and series already has these sheet numbers.");
            }

            await books.SaveAsync(book, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(book.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeStateAsync(ChangeChequeBookStateCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var book = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            switch (command.Change)
            {
                case "activate":
                    book.Activate();
                    break;
                case "deactivate":
                    book.Deactivate();
                    break;
                case "delete":
                    book.Delete();
                    await books.DeleteAsync(book, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(book.Id);
                default:
                    return Result<Guid>.Fail("Request.UnknownChange", $"Unknown change '{command.Change}'.");
            }

            await books.SaveAsync(book, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(book.Id);
        }, cancellationToken);

    /// <summary>Books of one account or all: active ones first, then by series and first sheet.</summary>
    public Task<IReadOnlyList<ChequeBookView>> ListAsync(Guid? bankAccountId, bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<ChequeBookView>>(async ct =>
            (await books.ListAsync(bankAccountId, includeInactive, ct).ConfigureAwait(false))
                .OrderByDescending(b => b.IsActive).ThenBy(b => b.Serial, StringComparer.Ordinal).ThenBy(b => b.FirstSheet)
                .Select(ChequeBookView.From).ToList(),
            cancellationToken);

    private async Task<bool> OverlapsAsync(ChequeBook book, CancellationToken ct) =>
        (await books.ListAsync(book.BankAccountId, includeInactive: true, ct).ConfigureAwait(false)).Any(book.Overlaps);

    private async Task<ChequeBook> LoadAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var book = await books.GetForUpdateAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("ChequeBook.NotFound", $"Cheque book {id} was not found.");
        return book.Version == expectedVersion ? book : throw new ConcurrencyConflictException($"Cheque book {id} was changed by someone else.");
    }

    private Task<Result<Guid>> RunAsync(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
