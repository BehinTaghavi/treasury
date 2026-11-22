using Treasury.Requests.Domain.PaymentRequests;

namespace Treasury.Requests.Application.Abstractions;

/// <summary>Payment requests of one fiscal year, numbered from 1 each year.</summary>
public interface IPaymentRequestRepository
{
    Task<PaymentRequest?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The next serial number in <paramref name="fiscalYear"/>; a taken number is never given again.</summary>
    Task<int> NextNumberAsync(int fiscalYear, CancellationToken cancellationToken);

    Task AddAsync(PaymentRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Saves changes. Throws ConcurrencyConflictException when someone else saved first. With <paramref name="newFile"/>, also stores
    /// the content of a scan just attached; scans removed from the request are deleted with it.
    /// </summary>
    Task SaveAsync(PaymentRequest request, CancellationToken cancellationToken, (Guid FileId, byte[] Content)? newFile = null);

    /// <summary>The content of one scan of a request; null when unknown.</summary>
    Task<(RequestAttachment File, byte[] Content)?> GetFileAsync(Guid requestId, Guid fileId, CancellationToken cancellationToken);

    /// <summary>Requests of a year, newest first; in one state or all.</summary>
    Task<IReadOnlyList<PaymentRequest>> ListAsync(int fiscalYear, PaymentRequestState? state, CancellationToken cancellationToken);

    /// <summary>
    /// The request of any year, registered or approved, that hands on this received cheque («ثبت انتقال»); null when none. A cheque is
    /// promised to one request at a time.
    /// </summary>
    Task<PaymentRequest?> OpenRequestOfChequeAsync(Guid instrumentId, CancellationToken cancellationToken);
}

/// <summary>Answered by reference-svc over its API (ADR 0001); null when unknown. Unreachable: DomainException "Reference.Unavailable".</summary>
public interface IRequestReferenceLookup
{
    Task<RequesterFact?> RequesterAsync(Guid id, CancellationToken cancellationToken);

    Task<BeneficiaryFact?> BeneficiaryAsync(Guid id, CancellationToken cancellationToken);

    Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tells reference-svc an entry ("requesters", "beneficiaries", "purpose-groups") is used, so it is never deleted.</summary>
    Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken);
}

/// <summary>Answered by accounts-svc over its API (ADR 0001); null when unknown. Unreachable: DomainException "Accounts.Unavailable".</summary>
public interface IPayFromLookup
{
    Task<PayFromFact?> BankAccountAsync(Guid id, CancellationToken cancellationToken);

    Task<PayFromFact?> CashDeskAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>Answered by cashflow-svc over its API (ADR 0001); null when unknown. Unreachable: DomainException "Cashflow.Unavailable".</summary>
public interface IReceivedChequeLookup
{
    /// <summary>A received cheque, for a request of the kind «ثبت انتقال (خرج چک)» (owner, 19 Nov 2026).</summary>
    Task<ReceivedChequeFact?> ChequeAsync(Guid id, CancellationToken cancellationToken);
}

public interface ICurrentUser
{
    string UserId { get; }
}
