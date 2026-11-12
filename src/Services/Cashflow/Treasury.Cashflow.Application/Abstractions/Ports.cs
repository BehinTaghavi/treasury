using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Application.Abstractions;

public interface IInstrumentRepository
{
    Task<Instrument?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Received instruments with the same bank and number (old duplicate check, rule R1).</summary>
    Task<IReadOnlyList<Guid>> FindReceivedAsync(string bankName, string number, CancellationToken cancellationToken);

    Task AddAsync(Instrument instrument, CancellationToken cancellationToken);

    /// <summary>Saves changes and pending events. Throws <see cref="ConcurrencyConflictException"/> if someone else saved first.</summary>
    Task SaveAsync(Instrument instrument, CancellationToken cancellationToken);
}

public interface IChequeBookRepository
{
    /// <summary>Loads the book locked for update until the transaction ends (SELECT … FOR UPDATE).</summary>
    Task<ChequeBook?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ChequeBook book, CancellationToken cancellationToken);

    /// <summary>Saves the book (series, range, active) and its newly used sheets. Version-checked.</summary>
    Task SaveAsync(ChequeBook book, CancellationToken cancellationToken);

    /// <summary>Removes a book none of whose sheets was used. Version-checked.</summary>
    Task DeleteAsync(ChequeBook book, CancellationToken cancellationToken);

    /// <summary>Books with their used sheets; of one bank account or all.</summary>
    Task<IReadOnlyList<ChequeBook>> ListAsync(Guid? bankAccountId, bool includeInactive, CancellationToken cancellationToken);
}

/// <summary>Cheque types (print designs) of our bank accounts, with their scans.</summary>
public interface IChequeFormatRepository
{
    Task<ChequeFormat?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChequeFormat>> ListAsync(Guid? bankAccountId, bool includeInactive, CancellationToken cancellationToken);

    /// <summary>One more than the highest print number so far.</summary>
    Task<int> NextNumberAsync(CancellationToken cancellationToken);

    /// <summary>Does a cheque book name this type?</summary>
    Task<bool> IsUsedAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ChequeFormat format, CancellationToken cancellationToken);

    /// <summary>Saves the type; with <paramref name="image"/> also replaces its scan, in the same transaction.</summary>
    Task SaveAsync(ChequeFormat format, CancellationToken cancellationToken, byte[]? image = null);

    Task DeleteAsync(ChequeFormat format, CancellationToken cancellationToken);

    Task<(string ContentType, byte[] Data)?> GetImageAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>What accounts-svc says about one of our bank accounts (asked over its API, ADR 0001); null: unknown.</summary>
public sealed record BankAccountFact(Guid Id, string AccountNumber, bool IsActive);

/// <summary>Answered by accounts-svc. Failure to reach it: DomainException "Accounts.Unavailable".</summary>
public interface IBankAccountLookup
{
    Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>Answered by the Fiscal Periods context: may operations be recorded on this date? (rule R2)</summary>
public interface IFiscalPeriodPolicy
{
    Task<bool> IsOpenAsync(FiscalDate operationDate, CancellationToken cancellationToken);
}

/// <summary>Current balance of one of our bank accounts, in Rial (old: Sum(Bed) - Sum(Bes) over Subs).</summary>
public interface IBankBalanceQuery
{
    Task<decimal> GetBalanceAsync(Guid bankAccountId, CancellationToken cancellationToken);
}

/// <summary>Answered by the Payment Requests context (rule R7).</summary>
public interface IPaymentApprovalQuery
{
    Task<bool> IsApprovedAsync(Guid paymentId, CancellationToken cancellationToken);
}

public interface ICurrentUser
{
    string UserId { get; }
}
