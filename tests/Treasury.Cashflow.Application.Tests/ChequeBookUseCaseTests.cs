using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.ChequeBooks;
using Treasury.Cashflow.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Cashflow.Application.Tests;

/// <summary>accounts-svc stand-in: known accounts, one of them switched off; can be "down".</summary>
public sealed class FakeBankAccounts : IBankAccountLookup
{
    public static readonly Guid Closed = Guid.NewGuid();
    public static readonly Guid Unknown = Guid.NewGuid();

    public bool Down { get; set; }

    public Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Down ? throw new Treasury.BuildingBlocks.Domain.DomainException("Accounts.Unavailable", "down")
        : Task.FromResult(id == Unknown ? null : new BankAccountFact(id, "0101-" + id.ToString()[..4], id != Closed));
}

/// <summary>The cheque books screen: new-version manual item 6; corrections and deletions only while no sheet is used (owner, 11 Nov 2026).</summary>
public sealed class ChequeBookUseCaseTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private readonly FakeBankAccounts _accounts = new();
    private readonly ChequeBookUseCases _books;
    private readonly Guid _account = Guid.NewGuid();

    public ChequeBookUseCaseTests() =>
        _books = new ChequeBookUseCases(new InMemoryUnitOfWork(_db), new InMemoryChequeBookRepository(_db), _accounts, new InMemoryIdempotencyStore(_db));

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private async Task<Guid> Register(string serial, long first, long last, Guid? account = null)
    {
        var r = await _books.RegisterAsync(new(Key(), account ?? _account, serial, first, last), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<ChequeBookView> View(Guid id) => (await _books.ListAsync(null, true, CancellationToken.None)).Single(b => b.Id == id);

    [Fact]
    public async Task A_book_is_registered_for_an_active_bank_account_of_the_company_only()
    {
        var id = await Register("ب/12", 1001, 1050);
        var v = await View(id);
        Assert.Equal((50L, 0L, 50L, (long?)1001, true, false), (v.SheetCount, v.UsedSheets, v.RemainingSheets, v.NextSheet, v.IsActive, v.IsInUse));
        Assert.Equal("ChequeBook.BankAccountNotFound", (await _books.RegisterAsync(new(Key(), FakeBankAccounts.Unknown, "A", 1, 10), CancellationToken.None)).ErrorCode);
        Assert.Equal("ChequeBook.BankAccountInactive", (await _books.RegisterAsync(new(Key(), FakeBankAccounts.Closed, "A", 1, 10), CancellationToken.None)).ErrorCode);
        _accounts.Down = true;
        Assert.Equal("Accounts.Unavailable", (await _books.RegisterAsync(new(Key(), _account, "A", 1, 10), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_same_sheet_numbers_twice_for_one_account_and_series_are_refused()
    {
        await Register("A", 1001, 1050);
        Assert.Equal("ChequeBook.Overlaps", (await _books.RegisterAsync(new(Key(), _account, " a ", 1040, 1060), CancellationToken.None)).ErrorCode);
        await Register("B", 1040, 1060);                         // other series
        await Register("A", 1040, 1060, Guid.NewGuid());          // other account
        await Register("A", 1051, 1100);                          // right after
    }

    [Fact]
    public async Task An_unused_book_can_be_corrected_and_deleted()
    {
        var id = await Register("A", 1001, 1050);
        Assert.True((await _books.CorrectAsync(new(Key(), id, "A", 2001, 2025, 1), CancellationToken.None)).IsSuccess);
        Assert.Equal((2001L, 2025L, 25L), ((await View(id)).FirstSheet, (await View(id)).LastSheet, (await View(id)).SheetCount));
        Assert.Equal("ChequeBook.InvalidRange", (await _books.CorrectAsync(new(Key(), id, "A", 30, 10, 2), CancellationToken.None)).ErrorCode);
        Assert.True((await _books.ChangeStateAsync(new(Key(), id, "delete", 2), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _books.ListAsync(null, true, CancellationToken.None));
    }

    [Fact]
    public async Task A_book_with_a_used_sheet_is_neither_corrected_nor_deleted_but_can_be_switched_off_and_on()
    {
        var id = await Register("A", 1, 3);
        var repo = new InMemoryChequeBookRepository(_db);
        await new InMemoryUnitOfWork(_db).ExecuteAsync(async ct =>
        {
            var book = (await repo.GetForUpdateAsync(id, ct))!;
            book.Void(2, "پاره شد");
            await repo.SaveAsync(book, ct);
            return 0;
        }, CancellationToken.None);

        Assert.True((await View(id)).IsInUse);
        Assert.Equal("ChequeBook.InUse", (await _books.CorrectAsync(new(Key(), id, "A", 1, 5, 2), CancellationToken.None)).ErrorCode);
        Assert.Equal("ChequeBook.InUse", (await _books.ChangeStateAsync(new(Key(), id, "delete", 2), CancellationToken.None)).ErrorCode);
        Assert.True((await _books.ChangeStateAsync(new(Key(), id, "deactivate", 2), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _books.ListAsync(_account, false, CancellationToken.None));
        Assert.True((await _books.ChangeStateAsync(new(Key(), id, "activate", 3), CancellationToken.None)).IsSuccess);
        Assert.Equal((long?)1, (await View(id)).NextSheet);
    }
}
