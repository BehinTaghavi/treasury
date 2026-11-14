using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.OpeningBalances;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Application.OpeningBalances;

/// <param name="Date">Jalali date the balance holds on, e.g. 1405/08/24 (the start date in the first year).</param>
/// <param name="ExpectedVersion">Version of the entry being corrected; null for a new entry.</param>
public sealed record EnterOpeningBalanceCommand(
    string IdempotencyKey, BalanceAccountKind Kind, Guid AccountId, int FiscalYear, string Date, decimal Amount, decimal? RialEquivalent, long? ExpectedVersion = null);

public sealed record RemoveOpeningBalanceCommand(string IdempotencyKey, Guid Id, long ExpectedVersion);

/// <summary>The balance entered for this year.</summary>
public sealed record OpeningBalanceEntry(Guid Id, string Date, decimal Amount, decimal? RialEquivalent, long Version);

/// <summary>A balance carried from an earlier year (no entry for this year).</summary>
/// <param name="FromYear">The year whose entry is carried.</param>
public sealed record CarriedBalance(int FromYear, decimal Amount, decimal? RialEquivalent);

/// <summary>One account's opening balance for a year: entered, carried from an earlier year, or neither.</summary>
public sealed record OpeningBalanceView(BalanceAccountKind Kind, Guid AccountId, OpeningBalanceEntry? Entry, CarriedBalance? Carried);

/// <summary>
/// Opening balances (موجودی اول دوره) of bank accounts, cash desks and petty cash (owner, 14 Nov 2026). A year without an entry
/// takes the balance of the latest earlier year that has one; once receipts and payments are recorded, their movements of the years
/// in between are added (Phase C/D). An entry for a later year is a correction and wins. Treasury sends no voucher for them.
/// </summary>
public sealed class OpeningBalanceUseCases(
    IUnitOfWork unitOfWork, IOpeningBalanceRepository balances, IBankAccountRepository bankAccounts, ICashDeskRepository cashDesks,
    IPettyCashRepository pettyCash, IIdempotencyStore idempotency)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> EnterAsync(EnterOpeningBalanceCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            ArgumentNullException.ThrowIfNull(command);
            var account = await AccountAsync(command.Kind, command.AccountId, ct).ConfigureAwait(false);
            if (account is null)
            {
                return Result<Guid>.Fail("OpeningBalance.AccountNotFound", "The account was not found.");
            }

            var date = FiscalDate.ParseJalali(command.Date);
            var existing = await balances.FindAsync(command.Kind, command.AccountId, command.FiscalYear, ct).ConfigureAwait(false);
            if (existing is null)
            {
                if (command.ExpectedVersion is not null)
                {
                    throw new ConcurrencyConflictException("The opening balance was removed by someone else.");
                }

                var entry = OpeningBalance.Enter(Guid.NewGuid(), account, command.FiscalYear, date, command.Amount, command.RialEquivalent);
                await balances.AddAsync(entry, ct).ConfigureAwait(false);
                return Result<Guid>.Ok(entry.Id);
            }

            if (existing.Version != command.ExpectedVersion)
            {
                throw new ConcurrencyConflictException("The opening balance was changed by someone else.");
            }

            existing.Correct(account, date, command.Amount, command.RialEquivalent);
            await balances.SaveAsync(existing, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(existing.Id);
        }, cancellationToken);

    public Task<Result<Guid>> RemoveAsync(RemoveOpeningBalanceCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            ArgumentNullException.ThrowIfNull(command);
            var entry = await balances.GetAsync(command.Id, ct).ConfigureAwait(false) ?? throw new DomainException("OpeningBalance.NotFound", "The opening balance was not found.");
            if (entry.Version != command.ExpectedVersion)
            {
                throw new ConcurrencyConflictException("The opening balance was changed by someone else.");
            }

            entry.Remove();
            await balances.DeleteAsync(entry, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(entry.Id);
        }, cancellationToken);

    /// <summary>Every account that has a balance for <paramref name="fiscalYear"/>, entered or carried from an earlier year.</summary>
    public Task<IReadOnlyList<OpeningBalanceView>> ListAsync(int fiscalYear, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<OpeningBalanceView>>(async ct =>
            (await balances.ListUpToAsync(fiscalYear, ct).ConfigureAwait(false))
                .GroupBy(b => (b.AccountKind, b.AccountId))
                .Select(g =>
                {
                    var thisYear = g.FirstOrDefault(b => b.FiscalYear == fiscalYear);
                    if (thisYear is not null)
                    {
                        return new OpeningBalanceView(g.Key.AccountKind, g.Key.AccountId,
                            new(thisYear.Id, thisYear.Date.ToJalaliString(), thisYear.Amount, thisYear.RialEquivalent, thisYear.Version), null);
                    }

                    var latest = g.OrderByDescending(b => b.FiscalYear).First();
                    return new OpeningBalanceView(g.Key.AccountKind, g.Key.AccountId, null, new(latest.FiscalYear, latest.Amount, latest.RialEquivalent));
                })
                .ToList(), cancellationToken);

    private async Task<BalanceAccountFact?> AccountAsync(BalanceAccountKind kind, Guid id, CancellationToken ct) => kind switch
    {
        BalanceAccountKind.BankAccount => await bankAccounts.GetAsync(id, ct).ConfigureAwait(false) is { } a
            ? new BalanceAccountFact(kind, id, a.Data.CurrencyCode, MayBeNegative: true) : null,
        BalanceAccountKind.CashDesk => await cashDesks.GetAsync(id, ct).ConfigureAwait(false) is { } d
            ? new BalanceAccountFact(kind, id, d.Data.CurrencyCode, MayBeNegative: false) : null,
        BalanceAccountKind.PettyCash => await pettyCash.GetAsync(id, ct).ConfigureAwait(false) is not null
            ? new BalanceAccountFact(kind, id, "IRR", MayBeNegative: false) : null,
        _ => null,
    };

    private Task<Result<Guid>> Run(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
