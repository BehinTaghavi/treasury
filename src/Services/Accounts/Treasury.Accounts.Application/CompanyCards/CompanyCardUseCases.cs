using System.Globalization;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Domain.CompanyCards;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Application.CompanyCards;

/// <summary>The form «کارت‌های بانک شرکت (بدون حساب بانکی)» as sent by the screen.</summary>
public sealed record CompanyCardForm(string CardNumber, string HolderName, Guid BankId, string LedgerAccountCode, string? DetailCode);

public sealed record RegisterCompanyCardCommand(string IdempotencyKey, CompanyCardForm Form);

public sealed record ChangeCompanyCardCommand(string IdempotencyKey, Guid Id, CompanyCardForm Form, long ExpectedVersion);

public sealed record SetCompanyCardActiveCommand(string IdempotencyKey, Guid Id, bool Active, long ExpectedVersion);

public sealed record CompanyCardView(Guid Id, string CardNumber, string HolderName, Guid BankId, string LedgerAccountCode, string? DetailCode, bool IsActive, long Version);

/// <summary>
/// Use cases of the company's bank cards without a bank account (owner's answer to Q69, 20 Nov 2026). Each write is one command in one
/// transaction on one card.
/// </summary>
public sealed class CompanyCardUseCases(
    IUnitOfWork unitOfWork, ICompanyCardRepository cards, IBankAccountRepository bankAccounts, IReferenceLookup reference, IIdempotencyStore idempotency)
{
    private static readonly StringComparer PersianOrder = StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true);
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> RegisterAsync(RegisterCompanyCardCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (data, ledger) = await PrepareAsync(command.Form, ct).ConfigureAwait(false);
            var card = CompanyCard.Register(Guid.NewGuid(), data, ledger);
            if (await RefusalAsync(card, ct).ConfigureAwait(false) is { } refusal)
            {
                return refusal;
            }

            await cards.AddAsync(card, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(card.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeAsync(ChangeCompanyCardCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var card = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var (data, ledger) = await PrepareAsync(command.Form, ct).ConfigureAwait(false);
            card.Change(data, ledger);
            if (await RefusalAsync(card, ct).ConfigureAwait(false) is { } refusal)
            {
                return refusal;
            }

            await cards.SaveAsync(card, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(card.Id);
        }, cancellationToken);

    public Task<Result<Guid>> SetActiveAsync(SetCompanyCardActiveCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var card = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            if (command.Active)
            {
                card.Activate();
            }
            else
            {
                card.Deactivate();
            }

            await cards.SaveAsync(card, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(card.Id);
        }, cancellationToken);

    /// <summary>The cards by the name on them.</summary>
    public async Task<IReadOnlyList<CompanyCardView>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var list = await unitOfWork.ExecuteAsync(ct => cards.ListAsync(includeInactive, ct), cancellationToken).ConfigureAwait(false);
        return list.OrderBy(c => c.Data.HolderName, PersianOrder).ThenBy(c => c.Data.CardNumber, StringComparer.Ordinal)
            .Select(c => new CompanyCardView(c.Id, c.Data.CardNumber, c.Data.HolderName, c.Data.BankId, c.Data.LedgerAccountCode, c.Data.DetailCode, c.IsActive, c.Version))
            .ToList();
    }

    /// <summary>A card number is entered once: not on another company card, and not on one of the company's bank accounts.</summary>
    private async Task<Result<Guid>?> RefusalAsync(CompanyCard card, CancellationToken ct)
    {
        if (await cards.NumberTakenAsync(card.Data.CardNumber, card.Id, ct).ConfigureAwait(false))
        {
            return Result<Guid>.Fail("CompanyCard.NumberTaken", $"Card {card.Data.CardNumber} is already in the list.");
        }

        if (await bankAccounts.CardTakenAsync(card.Data.CardNumber, null, ct).ConfigureAwait(false))
        {
            return Result<Guid>.Fail("CompanyCard.OnBankAccount", $"Card {card.Data.CardNumber} belongs to one of the company's bank accounts.");
        }

        // The bank becomes "in use" in reference-svc (it can no longer be deleted there).
        return await reference.MarkInUseAsync("banks", card.Data.BankId, ct).ConfigureAwait(false)
            ? null
            : Result<Guid>.Fail("CompanyCard.BankNotFound", "The bank is not an active bank of the list.");
    }

    private async Task<CompanyCard> LoadAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var card = await cards.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("CompanyCard.NotFound", $"Company card {id} was not found.");
        return card.Version == expectedVersion ? card : throw new ConcurrencyConflictException($"Company card {id} was changed by someone else.");
    }

    private async Task<(CompanyCardData Data, LedgerFacts Ledger)> PrepareAsync(CompanyCardForm form, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (form.BankId != Guid.Empty)
        {
            var bank = await reference.BankAsync(form.BankId, ct).ConfigureAwait(false);
            if (bank is not { IsActive: true })
            {
                throw new DomainException("CompanyCard.BankNotFound", "The bank is not an active bank of the list.");
            }
        }

        var accounts = new Dictionary<string, LedgerAccountFact>();
        if (Trim(form.LedgerAccountCode) is { } code && await reference.LedgerAccountAsync(code, ct).ConfigureAwait(false) is { } account)
        {
            accounts[code] = account;
        }

        var details = new Dictionary<string, DetailFact>();
        if (Trim(form.DetailCode) is { } d && await reference.DetailAsync(d, ct).ConfigureAwait(false) is { } fact)
        {
            details[d] = fact;
        }

        return (new CompanyCardData(form.CardNumber, form.HolderName, form.BankId, form.LedgerAccountCode, form.DetailCode), new LedgerFacts(accounts, details));
    }

    private Task<Result<Guid>> Run(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : PersianText.Clean(s);
}
