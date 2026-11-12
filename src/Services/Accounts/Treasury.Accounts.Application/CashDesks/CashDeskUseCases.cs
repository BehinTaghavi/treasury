using System.Globalization;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Domain.CashDesks;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Application.CashDesks;

/// <summary>The cash desk form as sent by the screen.</summary>
public sealed record CashDeskForm(string Name, string LedgerAccountCode, string HolderDetailCode, string? Detail2Code, bool IsForeignCurrency, string? CurrencyCode);

public sealed record RegisterCashDeskCommand(string IdempotencyKey, CashDeskForm Form);

public sealed record ChangeCashDeskCommand(string IdempotencyKey, Guid Id, CashDeskForm Form, long ExpectedVersion);

public sealed record SetCashDeskActiveCommand(string IdempotencyKey, Guid Id, bool Active, long ExpectedVersion);

/// <param name="HolderName">Name of the holder's تفصیل (manual: "نام ... از طریق انتخاب حساب تفصیل مربوط در این فیلد پر می شود").</param>
public sealed record CashDeskView(
    Guid Id, string Name, string LedgerAccountCode, string HolderDetailCode, string? HolderName, string? Detail2Code, string CurrencyCode, bool IsActive, long Version);

/// <summary>Use cases of the company's cash desks (manual item 10). Each write is one command in one transaction.</summary>
public sealed class CashDeskUseCases(IUnitOfWork unitOfWork, ICashDeskRepository desks, IReferenceLookup reference, IIdempotencyStore idempotency)
{
    private static readonly StringComparer PersianOrder = StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true);
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> RegisterAsync(RegisterCashDeskCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (data, ledger, currencyId) = await PrepareAsync(command.Form, ct).ConfigureAwait(false);
            var desk = CashDesk.Register(Guid.NewGuid(), data, ledger);
            if (await desks.NameTakenAsync(desk.NameKey, null, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("CashDesk.NameTaken", $"A cash desk named '{desk.Data.Name}' already exists.");
            }

            await MarkCurrencyAsync(currencyId, ct).ConfigureAwait(false);
            await desks.AddAsync(desk, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(desk.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeAsync(ChangeCashDeskCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var desk = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            var (data, ledger, currencyId) = await PrepareAsync(command.Form, ct).ConfigureAwait(false);
            desk.Change(data, ledger);
            if (await desks.NameTakenAsync(desk.NameKey, desk.Id, ct).ConfigureAwait(false))
            {
                return Result<Guid>.Fail("CashDesk.NameTaken", $"A cash desk named '{desk.Data.Name}' already exists.");
            }

            await MarkCurrencyAsync(currencyId, ct).ConfigureAwait(false);
            await desks.SaveAsync(desk, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(desk.Id);
        }, cancellationToken);

    public Task<Result<Guid>> SetActiveAsync(SetCashDeskActiveCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var desk = await LoadAsync(command.Id, command.ExpectedVersion, ct).ConfigureAwait(false);
            if (command.Active)
            {
                desk.Activate();
            }
            else
            {
                desk.Deactivate();
            }

            await desks.SaveAsync(desk, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(desk.Id);
        }, cancellationToken);

    /// <summary>Cash desks by name, each with the name of its holder's تفصیل (asked from reference-svc).</summary>
    public async Task<IReadOnlyList<CashDeskView>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var list = await unitOfWork.ExecuteAsync(ct => desks.ListAsync(includeInactive, ct), cancellationToken).ConfigureAwait(false);
        var views = new List<CashDeskView>();
        foreach (var d in list.OrderBy(d => d.Data.Name, PersianOrder))
        {
            string? holder = null;
            try
            {
                holder = await reference.DetailNameAsync(d.Data.HolderDetailCode, cancellationToken).ConfigureAwait(false);
            }
            catch (DomainException)
            {
                // reference-svc cannot answer now: the list still shows, without the holder's name.
            }

            views.Add(new(d.Id, d.Data.Name, d.Data.LedgerAccountCode, d.Data.HolderDetailCode, holder, d.Data.Detail2Code, d.Data.CurrencyCode, d.IsActive, d.Version));
        }

        return views;
    }

    private async Task<CashDesk> LoadAsync(Guid id, long expectedVersion, CancellationToken ct)
    {
        var desk = await desks.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new DomainException("CashDesk.NotFound", $"Cash desk {id} was not found.");
        return desk.Version == expectedVersion ? desk : throw new ConcurrencyConflictException($"Cash desk {id} was changed by someone else.");
    }

    /// <summary>A foreign currency becomes "in use" in reference-svc (it can no longer be deleted or change its code).</summary>
    private async Task MarkCurrencyAsync(Guid? currencyId, CancellationToken ct)
    {
        if (currencyId is { } id && !await reference.MarkInUseAsync("currencies", id, ct).ConfigureAwait(false))
        {
            throw new DomainException("CashDesk.CurrencyNotFound", "The currency is not an active currency of the list.");
        }
    }

    private async Task<(CashDeskData Data, LedgerFacts Ledger, Guid? CurrencyId)> PrepareAsync(CashDeskForm form, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(form);
        var currencyCode = form.IsForeignCurrency ? (form.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant() : BankAccount.Rial;
        Require(!form.IsForeignCurrency || currencyCode != BankAccount.Rial, "CashDesk.ForeignCurrencyRequired", "Choose the foreign currency of the cash desk.");
        Guid? currencyId = null;
        if (form.IsForeignCurrency)
        {
            var currency = await reference.CurrencyAsync(currencyCode, ct).ConfigureAwait(false);
            Require(currency is { IsActive: true }, "CashDesk.CurrencyNotFound", "The currency is not an active currency of the list.");
            currencyId = currency!.Id;
        }

        var accounts = new Dictionary<string, LedgerAccountFact>();
        if (Trim(form.LedgerAccountCode) is { } code && await reference.LedgerAccountAsync(code, ct).ConfigureAwait(false) is { } account)
        {
            accounts[code] = account;
        }

        var details = new Dictionary<string, DetailFact>();
        foreach (var d in new[] { form.HolderDetailCode, form.Detail2Code }.Select(Trim).OfType<string>().Distinct())
        {
            if (await reference.DetailAsync(d, ct).ConfigureAwait(false) is { } fact)
            {
                details[d] = fact;
            }
        }

        var data = new CashDeskData(form.Name, form.LedgerAccountCode, form.HolderDetailCode, form.Detail2Code, currencyCode);
        return (data, new LedgerFacts(accounts, details), currencyId);
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
