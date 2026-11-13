using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Domain.CashDesks;
using Treasury.Accounts.Domain.PettyCash;

namespace Treasury.Accounts.Application.Abstractions;

public interface IBankAccountRepository
{
    Task<BankAccount?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Is the number key (BankAccount.NumberKeyOf) already used by another account? Form_Bankacc: "شماره حساب تكراري است".</summary>
    Task<bool> NumberTakenAsync(string numberKey, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>Does another account already name <paramref name="mainAccountId"/> as its main account?</summary>
    Task<bool> HasBackupAsync(Guid mainAccountId, Guid? exceptId, CancellationToken cancellationToken);

    Task AddAsync(BankAccount account, CancellationToken cancellationToken);

    Task SaveAsync(BankAccount account, CancellationToken cancellationToken);

    /// <summary>Removes the account (version-checked; refused when it was marked in use meanwhile).</summary>
    Task DeleteAsync(BankAccount account, CancellationToken cancellationToken);

    Task<IReadOnlyList<BankAccount>> ListAsync(bool includeInactive, CancellationToken cancellationToken);
}

public interface ICashDeskRepository
{
    Task<CashDesk?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Is the name (CashDesk.NameKey) already used by another cash desk?</summary>
    Task<bool> NameTakenAsync(string nameKey, Guid? exceptId, CancellationToken cancellationToken);

    Task AddAsync(CashDesk desk, CancellationToken cancellationToken);

    Task SaveAsync(CashDesk desk, CancellationToken cancellationToken);

    Task<IReadOnlyList<CashDesk>> ListAsync(bool includeInactive, CancellationToken cancellationToken);
}

public interface IPettyCashRepository
{
    Task<PettyCashFund?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Does another petty cash have this holder? (one petty cash per holder)</summary>
    Task<bool> HolderTakenAsync(string holderDetailCode, Guid? exceptId, CancellationToken cancellationToken);

    Task AddAsync(PettyCashFund fund, CancellationToken cancellationToken);

    Task SaveAsync(PettyCashFund fund, CancellationToken cancellationToken);

    Task<IReadOnlyList<PettyCashFund>> ListAsync(bool includeInactive, CancellationToken cancellationToken);
}

/// <summary>An entry of a reference-svc list (bank name, city, currency) as reference-svc returns it.</summary>
public sealed record ReferenceEntry(Guid Id, string Name, string? Code, bool IsActive);

/// <summary>
/// Answers owned by reference-svc (Basic Information lists and the copy of the accounting system's accounts).
/// accounts-svc never reads reference-svc's tables (ADR 0001); the adapter calls its HTTP API.
/// A failure to reach reference-svc is reported as DomainException "Reference.Unavailable".
/// </summary>
public interface IReferenceLookup
{
    Task<ReferenceEntry?> BankAsync(Guid id, CancellationToken cancellationToken);

    Task<ReferenceEntry?> CityAsync(Guid id, CancellationToken cancellationToken);

    Task<ReferenceEntry?> CurrencyAsync(string code, CancellationToken cancellationToken);

    Task<LedgerAccountFact?> LedgerAccountAsync(string code, CancellationToken cancellationToken);

    Task<DetailFact?> DetailAsync(string code, CancellationToken cancellationToken);

    /// <summary>A purpose group of reference-svc; null when unknown.</summary>
    Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Name of a تفصیل (e.g. the holder of a cash desk); null when unknown.</summary>
    Task<string?> DetailNameAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Tells reference-svc that a bank account is about to name this entry (<paramref name="list"/>: banks, cities, currencies),
    /// so it can no longer be deleted there. Called before the save; false when the entry no longer exists.
    /// </summary>
    Task<bool> MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken);
}
