using Treasury.BuildingBlocks.Domain;
using Treasury.Requests.Application.Abstractions;
using Treasury.Requests.Domain.PaymentRequests;

namespace Treasury.Requests.Tests;

/// <summary>reference-svc stand-in: a requester, two beneficiaries (one switched off), a payment and a receipt purpose group.</summary>
public sealed class FakeReference : IRequestReferenceLookup
{
    public static readonly Guid Requester = Guid.NewGuid();
    public static readonly Guid Department = Guid.NewGuid();
    public static readonly Guid OldDepartment = Guid.NewGuid();
    public static readonly Guid Supplier = Guid.NewGuid();
    public static readonly Guid OldSupplier = Guid.NewGuid();
    public static readonly Guid Purchases = Guid.NewGuid();
    public static readonly Guid Sales = Guid.NewGuid();

    public bool Down { get; set; }

    public List<(string List, Guid Id)> Marked { get; } = [];

    public Task<RequesterFact?> RequesterAsync(Guid id, CancellationToken cancellationToken) =>
        Answer(id == Requester ? new RequesterFact(id, "علی رضایی", Department, "810001", true) : null);

    public Task<BeneficiaryFact?> BeneficiaryAsync(Guid id, CancellationToken cancellationToken) => Answer(
        id == Supplier ? new BeneficiaryFact(id, "300200", "شرکت نمونه بازرگانی", "IR460120000000003123456789", true)
        : id == OldSupplier ? new BeneficiaryFact(id, "300201", "تامین کننده قدیمی", null, false) : null);

    public Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken) => Answer(
        id == Purchases ? new PurposeGroupFact(id, "خرید مواد اولیه", true, true) : id == Sales ? new PurposeGroupFact(id, "وجوه حاصل فروش", false, true) : null);

    /// <summary>Two units: «تدارکات» with cost centres 810001 and 810002, and «مالی» switched off with no list of its own.</summary>
    public Task<DepartmentFact?> DepartmentAsync(Guid id, CancellationToken cancellationToken) => Answer(
        id == Department ? new DepartmentFact(id, "تدارکات", ["810001", "810002"], true)
        : id == OldDepartment ? new DepartmentFact(id, "مالی", [], false) : null);

    /// <summary>Cost centres 810001, 810002, 810009 (switched off); 300200 is a تفصیل that is not a cost centre.</summary>
    public Task<CostCenterFact?> CostCenterAsync(string code, CancellationToken cancellationToken) => Answer(code switch
    {
        "810001" => new CostCenterFact(code, "اداری", true, true),
        "810002" => new CostCenterFact(code, "تولید", true, true),
        "810003" => new CostCenterFact(code, "فروش", true, true),
        "810009" => new CostCenterFact(code, "قدیمی", true, false),
        "300200" => new CostCenterFact(code, "شرکت نمونه بازرگانی", false, true),
        _ => null,
    });

    public Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken)
    {
        if (Down)
        {
            throw new DomainException("Reference.Unavailable", "down");
        }

        Marked.Add((list, id));
        return Task.CompletedTask;
    }

    private Task<T?> Answer<T>(T? value) => Down ? throw new DomainException("Reference.Unavailable", "down") : Task.FromResult(value);
}

/// <summary>accounts-svc stand-in: a Rial bank account and a Rial cash desk.</summary>
public sealed class FakeAccounts : IPayFromLookup
{
    public static readonly Guid Bank = Guid.NewGuid();
    public static readonly Guid Desk = Guid.NewGuid();

    public Task<PayFromFact?> BankAccountAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(id == Bank ? new PayFromFact(PayFromKind.BankAccount, id, "0202-3572", "IRR", true) : null);

    public Task<PayFromFact?> CashDeskAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(id == Desk ? new PayFromFact(PayFromKind.CashDesk, id, "صندوق مرکزی", "IRR", true) : null);
}

public sealed class FixedUser(string id) : ICurrentUser
{
    public string UserId { get; set; } = id;
}
