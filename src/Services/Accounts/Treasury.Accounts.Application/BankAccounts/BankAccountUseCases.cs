using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Application.BankAccounts;

/// <summary>The bank account form as sent by the screen. Dates are Jalali text, converted at this edge (CLAUDE.md rule 5).</summary>
public sealed record BankAccountForm(
    string AccountNumber,
    Guid BankId,
    string? BranchName,
    string? BranchCode,
    Guid? CityId,
    string? OpeningDate,
    bool IsForeignCurrency,
    string? CurrencyCode,
    string LedgerAccountCode,
    string? Detail1Code,
    string? Detail2Code,
    string? CollectionAccountCode,
    string? BouncedAccountCode,
    int? CollectionPriority,
    bool IsFacility);

/// <param name="IsBackup">"پشتیبان" ticked on the form; then <paramref name="MainAccountId"/> is required.</param>
public sealed record RegisterBankAccountCommand(string IdempotencyKey, BankAccountForm Form, Guid? MainAccountId = null, bool IsBackup = false);

public sealed record ChangeBankAccountCommand(string IdempotencyKey, Guid Id, BankAccountForm Form, long ExpectedVersion);

public sealed record SetBankAccountActiveCommand(string IdempotencyKey, Guid Id, bool Active, long ExpectedVersion);

public sealed record BankAccountView(
    Guid Id, string AccountNumber, Guid BankId, string? BranchName, string? BranchCode, Guid? CityId, string? OpeningDate,
    string CurrencyCode, Guid? MainAccountId, string LedgerAccountCode, string? Detail1Code, string? Detail2Code,
    string? CollectionAccountCode, string? BouncedAccountCode, int? CollectionPriority, bool IsFacility, bool IsActive, long Version)
{
    public static BankAccountView From(BankAccount a) => new(
        a.Id, a.Data.AccountNumber, a.Data.BankId, a.Data.BranchName, a.Data.BranchCode, a.Data.CityId, a.Data.OpeningDate?.ToJalaliString(),
        a.Data.CurrencyCode, a.MainAccountId, a.Data.Ledger.AccountCode, a.Data.Ledger.Detail1Code, a.Data.Ledger.Detail2Code,
        a.Data.Ledger.CollectionAccountCode, a.Data.Ledger.BouncedAccountCode, a.Data.CollectionPriority, a.Data.IsFacility, a.IsActive, a.Version);
}

/// <summary>Use cases of the company's bank accounts (manual section 5). Each write is one command in one transaction.</summary>
public sealed class BankAccountUseCases(IUnitOfWork unitOfWork, IBankAccountRepository accounts, IReferenceLookup reference, IIdempotencyStore idempotency)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> RegisterAsync(RegisterBankAccountCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (data, ledger, used) = await PrepareAsync(command.Form, ct).ConfigureAwait(false);
            BankAccount? main = null;
            var mainHasBackup = false;
            if (command.MainAccountId is { } mainId)
            {
                main = await accounts.GetAsync(mainId, ct).ConfigureAwait(false);
                if (main is null)
                {
                    return Result<Guid>.Fail("BankAccount.MainAccountNotFound", $"Main account {mainId} was not found.");
                }

                mainHasBackup = await accounts.HasBackupAsync(mainId, null, ct).ConfigureAwait(false);
            }

            var account = BankAccount.Register(Guid.NewGuid(), data, ledger, main, mainHasBackup, command.IsBackup);
            if (await accounts.NumberTakenAsync(account.NumberKey, null, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("BankAccount.NumberTaken", "This account number is already defined."); // "شماره حساب تكراري است"
            }

            await MarkInUseAsync(used, ct).ConfigureAwait(false);
            await accounts.AddAsync(account, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(account.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeAsync(ChangeBankAccountCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var account = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var (data, ledger, used) = await PrepareAsync(command.Form, ct).ConfigureAwait(false);
            account.Change(data, ledger);
            if (await accounts.NumberTakenAsync(account.NumberKey, account.Id, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("BankAccount.NumberTaken", "This account number is already defined.");
            }

            await MarkInUseAsync(used, ct).ConfigureAwait(false);
            await accounts.SaveAsync(account, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(account.Id);
        }, cancellationToken);

    public Task<Result<Guid>> SetActiveAsync(SetBankAccountActiveCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var account = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            if (command.Active)
            {
                account.Activate();
            }
            else
            {
                account.Deactivate();
            }

            await accounts.SaveAsync(account, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(account.Id);
        }, cancellationToken);

    /// <summary>
    /// Manual: accounts with "اولویت واگذاری" come first in the bank lists so they are easier to choose, priority 1 first
    /// (owner, 11 Nov 2026, item 15); then accounts without a priority; within each, by number.
    /// Inactive accounts only when asked (the Basic Information screen shows them, the receipt and payment forms do not).
    /// </summary>
    public Task<IReadOnlyList<BankAccountView>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<BankAccountView>>(async ct =>
            (await accounts.ListAsync(includeInactive, ct).ConfigureAwait(false))
                .OrderBy(a => a.Data.CollectionPriority ?? int.MaxValue)
                .ThenBy(a => a.NumberKey, StringComparer.Ordinal)
                .Select(BankAccountView.From)
                .ToList(),
            cancellationToken);

    /// <summary>One bank account, active or not (asked by cashflow-svc when a cheque book is registered); null when unknown.</summary>
    public Task<BankAccountView?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct => await accounts.GetAsync(id, ct).ConfigureAwait(false) is { } a ? BankAccountView.From(a) : null, cancellationToken);

    private async Task<BankAccount> LoadAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var account = await accounts.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("BankAccount.NotFound", $"Bank account {id} was not found.");
        return account.Version == expectedVersion
            ? account
            : throw new ConcurrencyConflictException($"Bank account {id} was changed by someone else.");
    }

    /// <summary>
    /// Tells reference-svc which of its entries the account names (bank, city, foreign currency), so they can no longer be
    /// deleted there. Runs just before the save: if the save then fails, the entries merely stay undeletable.
    /// </summary>
    private async Task MarkInUseAsync(IEnumerable<(string List, Guid Id, string MissingCode)> used, CancellationToken ct)
    {
        foreach (var (list, id, missingCode) in used)
        {
            Require(await reference.MarkInUseAsync(list, id, ct).ConfigureAwait(false), missingCode, $"Entry {id} of {list} no longer exists.");
        }
    }

    /// <summary>
    /// Checks the form against reference-svc (bank, city, currency, ledger accounts and تفصیل) and builds the domain data.
    /// Also returns the reference-svc entries the account names (list, id, error code if it has disappeared).
    /// </summary>
    private async Task<(BankAccountData Data, LedgerFacts Ledger, List<(string List, Guid Id, string MissingCode)> Used)> PrepareAsync(BankAccountForm form, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(form);
        var used = new List<(string List, Guid Id, string MissingCode)>();
        var bank = await reference.BankAsync(form.BankId, ct).ConfigureAwait(false);
        Require(form.BankId == Guid.Empty || bank is not null, "BankAccount.BankNotFound", "The bank is not in the bank list.");
        Require(bank is null || bank.IsActive, "BankAccount.BankInactive", "The bank is inactive in the bank list.");
        if (bank is not null)
        {
            used.Add(("banks", bank.Id, "BankAccount.BankNotFound"));
        }

        if (form.CityId is { } cityId && cityId != Guid.Empty)
        {
            var city = await reference.CityAsync(cityId, ct).ConfigureAwait(false);
            Require(city is not null, "BankAccount.CityNotFound", "The city is not in the list.");
            used.Add(("cities", cityId, "BankAccount.CityNotFound"));
        }

        // Form_Bankacc: the Rial unless "ارزی" is ticked; then the currency is chosen from the list.
        var currencyCode = form.IsForeignCurrency ? (form.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant() : BankAccount.Rial;
        Require(!form.IsForeignCurrency || currencyCode != BankAccount.Rial, "BankAccount.ForeignCurrencyRequired", "Choose the foreign currency of the account.");
        if (form.IsForeignCurrency)
        {
            var currency = await reference.CurrencyAsync(currencyCode, ct).ConfigureAwait(false);
            Require(currency is { IsActive: true }, "BankAccount.CurrencyNotFound", "The currency is not an active currency of the list.");
            used.Add(("currencies", currency!.Id, "BankAccount.CurrencyNotFound"));
        }

        var accountsByCode = new Dictionary<string, LedgerAccountFact>();
        foreach (var code in new[] { form.LedgerAccountCode, form.CollectionAccountCode, form.BouncedAccountCode }.Select(Trim).OfType<string>().Distinct())
        {
            if (await reference.LedgerAccountAsync(code, ct).ConfigureAwait(false) is { } fact)
            {
                accountsByCode[code] = fact;
            }
        }

        var detailsByCode = new Dictionary<string, DetailFact>();
        foreach (var code in new[] { form.Detail1Code, form.Detail2Code }.Select(Trim).OfType<string>().Distinct())
        {
            if (await reference.DetailAsync(code, ct).ConfigureAwait(false) is { } fact)
            {
                detailsByCode[code] = fact;
            }
        }

        var data = new BankAccountData(
            form.AccountNumber,
            form.BankId,
            form.BranchName,
            form.BranchCode,
            form.CityId,
            string.IsNullOrWhiteSpace(form.OpeningDate) ? null : FiscalDate.ParseJalali(form.OpeningDate),
            currencyCode,
            new BankLedger(form.LedgerAccountCode, form.Detail1Code, form.Detail2Code, form.CollectionAccountCode, form.BouncedAccountCode),
            form.CollectionPriority,
            form.IsFacility);
        return (data, new LedgerFacts(accountsByCode, detailsByCode), used);
    }

    private Task<Result<Guid>> Run(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : PersianText.Clean(s);

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}
