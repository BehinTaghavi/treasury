using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;

namespace Treasury.Cashflow.Application.Abstractions;

public interface IInstrumentRepository
{
    Task<Instrument?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Received instruments with the same bank and number (old duplicate check, rule R1).</summary>
    Task<IReadOnlyList<Guid>> FindReceivedAsync(string bankName, string number, CancellationToken cancellationToken);

    Task AddAsync(Instrument instrument, CancellationToken cancellationToken);

    /// <summary>Saves changes and pending events. Throws <see cref="ConcurrencyConflictException"/> if someone else saved first.</summary>
    Task SaveAsync(Instrument instrument, CancellationToken cancellationToken);

    /// <summary>Received instruments in a state (e.g. in the cash desk, at the bank), by due date.</summary>
    Task<IReadOnlyList<Instrument>> ListReceivedAsync(InstrumentState state, CancellationToken cancellationToken);

    /// <summary>The cheques and notes received on one receipt, in the order they were entered.</summary>
    Task<IReadOnlyList<Instrument>> ListByReceiptAsync(Guid receiptId, CancellationToken cancellationToken);
}

/// <summary>Receipts (رسید دریافت) of one fiscal year, numbered per kind.</summary>
public interface IReceiptRepository
{
    Task<Receipt?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The next serial number of <paramref name="kind"/> in <paramref name="fiscalYear"/>; taken numbers are never reused.</summary>
    Task<int> NextNumberAsync(int fiscalYear, ReceiptKind kind, CancellationToken cancellationToken);

    Task AddAsync(Receipt receipt, CancellationToken cancellationToken);

    /// <summary>Saves a cancellation. Version-checked.</summary>
    Task SaveAsync(Receipt receipt, CancellationToken cancellationToken);

    /// <summary>Receipts of a year, newest first; of one kind or all.</summary>
    Task<IReadOnlyList<Receipt>> ListAsync(int fiscalYear, ReceiptKind? kind, CancellationToken cancellationToken);
}

/// <summary>The company's codes of the fixed accounts of the voucher template (owner's report no. 4, item 8).</summary>
public interface IVoucherTemplateRepository
{
    Task<Treasury.Cashflow.Domain.Vouchers.VoucherTemplate> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(Treasury.Cashflow.Domain.Vouchers.VoucherTemplate voucherTemplate, CancellationToken cancellationToken);
}

/// <summary>Vouchers the treasurer confirmed, one per receipt or cheque step.</summary>
public interface IConfirmedVoucherRepository
{
    Task<Treasury.Cashflow.Domain.Vouchers.ConfirmedVoucher?> FindAsync(Treasury.Cashflow.Domain.Vouchers.VoucherSource source, Guid sourceId, int stepNumber, CancellationToken cancellationToken);

    Task<IReadOnlyList<Treasury.Cashflow.Domain.Vouchers.ConfirmedVoucher>> ListBySourceAsync(Guid sourceId, CancellationToken cancellationToken);

    Task AddAsync(Treasury.Cashflow.Domain.Vouchers.ConfirmedVoucher voucher, CancellationToken cancellationToken);
}

/// <summary>Answered by accounts-svc (ADR 0001): a cash desk or bank account a receipt is booked to; null when unknown.</summary>
public interface IReceiptAccountLookup
{
    Task<ReceiptAccountFact?> CashDeskAsync(Guid id, CancellationToken cancellationToken);

    Task<ReceiptAccountFact?> BankAccountAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>Answered by reference-svc (ADR 0001). Failure to reach it: DomainException "Reference.Unavailable".</summary>
public interface IReceiptReferenceLookup
{
    /// <summary>The payer's تفصیل by its code; null when unknown.</summary>
    Task<PayerFact?> PayerAsync(string detailCode, CancellationToken cancellationToken);

    Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Name of a bank in the bank-names list; null when unknown.</summary>
    Task<string?> BankNameAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>A ledger account of the chart (copy of the accounting system); null when unknown.</summary>
    Task<Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact?> LedgerAccountAsync(string code, CancellationToken cancellationToken);

    /// <summary>Tells reference-svc an entry ("banks", "purpose-groups") is used, so it is never deleted.</summary>
    Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken);
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
/// <param name="HasCheques">The kind of account has cheque books: current Qard-al-hasan only (owner's report no. 4, item 1).</param>
public sealed record BankAccountFact(
    Guid Id, string AccountNumber, bool IsActive, bool HasCheques = true, string CurrencyCode = "IRR", string? LedgerAccountCode = null, string? Detail1Code = null);

/// <summary>Answered by accounts-svc. Failure to reach it: DomainException "Accounts.Unavailable".</summary>
public interface IBankAccountLookup
{
    Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tells accounts-svc a cheque book or cheque type is about to name the account, so it is never deleted.</summary>
    Task MarkInUseAsync(Guid id, CancellationToken cancellationToken);
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
