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
        _books = new ChequeBookUseCases(new InMemoryUnitOfWork(_db), new InMemoryChequeBookRepository(_db), _accounts, new InMemoryIdempotencyStore(_db), new InMemoryChequeFormatRepository(_db));

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private async Task<Guid> Register(string serial, long first, long last, Guid? account = null)
    {
        var r = await _books.RegisterAsync(new(Key(), account ?? _account, serial, first, last, null, "12345678", "1", PartlyUsed: true), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    private async Task<ChequeBookView> View(Guid id) => (await _books.ListAsync(null, true, CancellationToken.None)).Single(b => b.Id == id);

    [Fact]
    public async Task A_book_is_registered_for_an_active_bank_account_of_the_company_only()
    {
        var id = await Register("1234", 101001, 101050);
        var v = await View(id);
        Assert.Equal((50L, 0L, 50L, (long?)101001, true, false), (v.SheetCount, v.UsedSheets, v.RemainingSheets, v.NextSheet, v.IsActive, v.IsInUse));
        Assert.Equal("ChequeBook.BankAccountNotFound", (await _books.RegisterAsync(new(Key(), FakeBankAccounts.Unknown, "1234", 100001, 100010, null, "12345678", "1"), CancellationToken.None)).ErrorCode);
        Assert.Equal("ChequeBook.BankAccountInactive", (await _books.RegisterAsync(new(Key(), FakeBankAccounts.Closed, "1234", 100001, 100010, null, "12345678", "1"), CancellationToken.None)).ErrorCode);
        _accounts.Down = true;
        Assert.Equal("Accounts.Unavailable", (await _books.RegisterAsync(new(Key(), _account, "1234", 100001, 100010, null, "12345678", "1"), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_book_on_the_screen_needs_its_sayad_numbers_and_shows_the_last_sayad_serial()
    {
        Assert.Equal("ChequeBook.SayadRequired", (await _books.RegisterAsync(new(Key(), _account, "1234", 100001, 100025), CancellationToken.None)).ErrorCode);
        Assert.Equal("ChequeBook.SeriesInvalid", (await _books.RegisterAsync(new(Key(), _account, "12/3", 100001, 100025, null, "12345678", "1"), CancellationToken.None)).ErrorCode);
        var id = (await _books.RegisterAsync(new(Key(), _account, "1234", 100001, 100025, null, "12345678", "0101"), CancellationToken.None)).Value;
        var v = await View(id);
        Assert.Equal(("12345678", (int?)101, (int?)125), (v.SayadMiddle, v.SayadFirstSerial, v.SayadLastSerial));
        Assert.True((await _books.CorrectAsync(new(Key(), id, "1234", 100001, 100025, 1, null, "87654321", "1"), CancellationToken.None)).IsSuccess);
        Assert.Equal("87654321", (await View(id)).SayadMiddle);
    }

    [Fact]
    public async Task The_same_sheet_numbers_twice_for_one_account_are_refused_whatever_the_series()
    {
        await Register("1234", 101001, 101050);
        Assert.Equal("ChequeBook.Overlaps", (await _books.RegisterAsync(new(Key(), _account, "1234", 101040, 101060, null, "12345678", "1", PartlyUsed: true), CancellationToken.None)).ErrorCode);
        Assert.Equal("ChequeBook.Overlaps", (await _books.RegisterAsync(new(Key(), _account, "5678", 101040, 101060, null, "12345678", "1", PartlyUsed: true), CancellationToken.None)).ErrorCode);
        await Register("1234", 101040, 101060, Guid.NewGuid());          // other account
        await Register("1234", 101051, 101100);                          // right after
    }

    [Fact]
    public async Task An_unused_book_can_be_corrected_and_deleted()
    {
        var id = await Register("1234", 101001, 101050);
        Assert.True((await _books.CorrectAsync(new(Key(), id, "1234", 102001, 102025, 1), CancellationToken.None)).IsSuccess);
        Assert.Equal((102001L, 102025L, 25L), ((await View(id)).FirstSheet, (await View(id)).LastSheet, (await View(id)).SheetCount));
        Assert.Equal("ChequeBook.InvalidRange", (await _books.CorrectAsync(new(Key(), id, "1234", 102030, 102010, 2), CancellationToken.None)).ErrorCode);
        Assert.True((await _books.ChangeStateAsync(new(Key(), id, "delete", 2), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _books.ListAsync(null, true, CancellationToken.None));
    }

    [Fact]
    public async Task A_book_with_a_used_sheet_is_neither_corrected_nor_deleted_but_can_be_switched_off_and_on()
    {
        var id = await Register("1234", 100001, 100003);
        var repo = new InMemoryChequeBookRepository(_db);
        await new InMemoryUnitOfWork(_db).ExecuteAsync(async ct =>
        {
            var book = (await repo.GetForUpdateAsync(id, ct))!;
            book.Void(100002, "پاره شد");
            await repo.SaveAsync(book, ct);
            return 0;
        }, CancellationToken.None);

        Assert.True((await View(id)).IsInUse);
        Assert.Equal("ChequeBook.InUse", (await _books.CorrectAsync(new(Key(), id, "1234", 100001, 100005, 2), CancellationToken.None)).ErrorCode);
        Assert.Equal("ChequeBook.InUse", (await _books.ChangeStateAsync(new(Key(), id, "delete", 2), CancellationToken.None)).ErrorCode);
        Assert.True((await _books.ChangeStateAsync(new(Key(), id, "deactivate", 2), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _books.ListAsync(_account, false, CancellationToken.None));
        Assert.True((await _books.ChangeStateAsync(new(Key(), id, "activate", 3), CancellationToken.None)).IsSuccess);
        Assert.Equal((long?)100001, (await View(id)).NextSheet);
    }

    [Fact]
    public async Task A_new_book_has_25_50_or_100_sheets_and_a_partly_used_one_any_count_owner_12_nov()
    {
        Assert.Equal("ChequeBook.SheetCountInvalid", (await _books.RegisterAsync(new(Key(), _account, "1234", 100001, 100030, null, "12345678", "1"), CancellationToken.None)).ErrorCode);
        var r = await _books.RegisterAsync(new(Key(), _account, "1234", 100018, 100050, null, "12345678", "18", PartlyUsed: true), CancellationToken.None);
        var v = await View(r.Value);
        Assert.Equal((33L, true, (int?)18, (int?)50), (v.SheetCount, v.PartlyUsed, v.SayadFirstSerial, v.SayadLastSerial));
        Assert.True((await _books.CorrectAsync(new(Key(), r.Value, "1234", 100001, 100050, 1, null, null, null, PartlyUsed: false), CancellationToken.None)).IsSuccess);
        Assert.False((await View(r.Value)).PartlyUsed);
    }
}

