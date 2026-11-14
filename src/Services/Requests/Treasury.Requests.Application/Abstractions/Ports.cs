using Treasury.Requests.Domain.PaymentRequests;

namespace Treasury.Requests.Application.Abstractions;

/// <summary>Payment requests of one fiscal year, numbered from 1 each year.</summary>
public interface IPaymentRequestRepository
{
    Task<PaymentRequest?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The next serial number in <paramref name="fiscalYear"/>; a taken number is never given again.</summary>
    Task<int> NextNumberAsync(int fiscalYear, CancellationToken cancellationToken);

    Task AddAsync(PaymentRequest request, CancellationToken cancellationToken);

    /// <summary>Saves changes. Throws ConcurrencyConflictException when someone else saved first.</summary>
    Task SaveAsync(PaymentRequest request, CancellationToken cancellationToken);

    /// <summary>Requests of a year, newest first; in one state or all.</summary>
    Task<IReadOnlyList<PaymentRequest>> ListAsync(int fiscalYear, PaymentRequestState? state, CancellationToken cancellationToken);
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

public interface ICurrentUser
{
    string UserId { get; }
}
