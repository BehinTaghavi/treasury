using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.BankAccounts;

namespace Treasury.Accounts.Application.Tests;

/// <summary>reference-svc stand-in: made-up banks, cities, currencies, ledger accounts and تفصیل.</summary>
public sealed class FakeReference : IReferenceLookup
{
    public static readonly Guid Mellat = Guid.NewGuid();
    public static readonly Guid Melli = Guid.NewGuid();
    public static readonly Guid ClosedBank = Guid.NewGuid();
    public static readonly Guid Tehran = Guid.NewGuid();

    private readonly Dictionary<Guid, ReferenceEntry> _banks = new()
    {
        [Mellat] = new(Mellat, "بانک ملت", null, true),
        [Melli] = new(Melli, "بانک ملی", null, true),
        [ClosedBank] = new(ClosedBank, "بانک منحل", null, false),
    };

    private readonly Dictionary<string, LedgerAccountFact> _accounts = new()
    {
        ["1103"] = new("1103", true, true, new HashSet<string> { "بانكها" }, new HashSet<string>()),
        ["1301"] = new("1301", true, false, new HashSet<string>(), new HashSet<string>()),
        ["130100"] = new("130100", true, true, new HashSet<string>(), new HashSet<string>()),
        ["130101"] = new("130101", true, true, new HashSet<string>(), new HashSet<string>()),
    };

    private readonly Dictionary<string, DetailFact> _details = new()
    {
        ["100000"] = new("100000", "بانكها", true),
        ["100001"] = new("100001", "بانكها", true),
        ["111078"] = new("111078", "اشخاص وشركتها", true),
    };

    public bool Down { get; set; }

    public Task<ReferenceEntry?> BankAsync(Guid id, CancellationToken cancellationToken) => Answer(_banks.GetValueOrDefault(id));

    public Task<ReferenceEntry?> CityAsync(Guid id, CancellationToken cancellationToken) =>
        Answer(id == Tehran ? new ReferenceEntry(Tehran, "تهران", null, true) : null);

    public Task<ReferenceEntry?> CurrencyAsync(string code, CancellationToken cancellationToken) =>
        Answer(code is "IRR" or "USD" or "EUR" ? new ReferenceEntry(Guid.NewGuid(), code, code, true) : null);

    public Task<LedgerAccountFact?> LedgerAccountAsync(string code, CancellationToken cancellationToken) => Answer(_accounts.GetValueOrDefault(code));

    public Task<DetailFact?> DetailAsync(string code, CancellationToken cancellationToken) => Answer(_details.GetValueOrDefault(code));

    private Task<T?> Answer<T>(T? value) =>
        Down ? throw new Treasury.BuildingBlocks.Domain.DomainException("Reference.Unavailable", "down") : Task.FromResult(value);
}
