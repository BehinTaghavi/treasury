using Treasury.BuildingBlocks.Domain;
using Treasury.Requests.Application.Abstractions;
using Treasury.Requests.Domain.PaymentRequests;

namespace Treasury.Requests.Tests;

/// <summary>reference-svc stand-in: a requester, two beneficiaries (one switched off), a payment and a receipt purpose group.</summary>
public sealed class FakeReference : IRequestReferenceLookup
{
    public static readonly Guid Requester = Guid.NewGuid();
    public static readonly Guid Department = Guid.NewGuid();
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

    /// <summary>A second company bank account, and a dollar one, for transfers between the company's own accounts.</summary>
    public static readonly Guid OtherBank = Guid.NewGuid();
    public static readonly Guid DollarBank = Guid.NewGuid();

    public Task<PayFromFact?> BankAccountAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(id == Bank ? new PayFromFact(PayFromKind.BankAccount, id, "0202-3572", "IRR", true)
            : id == OtherBank ? new PayFromFact(PayFromKind.BankAccount, id, "0303-9911", "IRR", true)
            : id == DollarBank ? new PayFromFact(PayFromKind.BankAccount, id, "USD-1", "USD", true) : null);

    public Task<PayFromFact?> CashDeskAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(id == Desk ? new PayFromFact(PayFromKind.CashDesk, id, "صندوق مرکزی", "IRR", true) : null);
}

/// <summary>
/// cashflow-svc stand-in: received cheques by id. <see cref="Registered"/> is «ثبت صیادی شد» and may be transferred; <see cref="InDesk"/>
/// is still only «نزد صندوق».
/// </summary>
public sealed class FakeCheques : IReceivedChequeLookup
{
    public static readonly Guid Registered = Guid.NewGuid();
    public static readonly Guid InDesk = Guid.NewGuid();
    public static readonly Guid Dollar = Guid.NewGuid();

    public const decimal Amount = 80_000_000m;

    public Task<ReceivedChequeFact?> ChequeAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(
        id == Registered ? new ReceivedChequeFact(id, "445566", "بانک ملت", FiscalDate.ParseJalali("1405/10/15"), "فروشگاه آفتاب", Amount, "IRR", true, "1234567890123456")
        : id == InDesk ? new ReceivedChequeFact(id, "445567", "بانک ملت", FiscalDate.ParseJalali("1405/10/20"), "فروشگاه آفتاب", Amount, "IRR", false, "1234567890123457")
        : id == Dollar ? new ReceivedChequeFact(id, "9001", "بانک ملی", FiscalDate.ParseJalali("1405/10/20"), "بازرگانی سپهر", 1_500m, "USD", true)
        : null);
}

public sealed class FixedUser(string id) : ICurrentUser
{
    public string UserId { get; set; } = id;
}
