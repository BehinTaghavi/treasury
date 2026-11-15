using Treasury.Cashflow.Application.ChequeBooks;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Cashflow.Application.Tests;

public sealed class ChequeFormatUseCaseTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private readonly InMemoryDatabase _db = new();
    private readonly ChequeFormatUseCases _formats;
    private readonly ChequeBookUseCases _books;
    private readonly Guid _account = Guid.NewGuid();
    private readonly FakeBankAccounts _accounts = new();

    public ChequeFormatUseCaseTests()
    {
        var accounts = _accounts;
        _formats = new ChequeFormatUseCases(new InMemoryUnitOfWork(_db), new InMemoryChequeFormatRepository(_db), accounts, new InMemoryIdempotencyStore(_db));
        _books = new ChequeBookUseCases(new InMemoryUnitOfWork(_db), new InMemoryChequeBookRepository(_db), accounts, new InMemoryIdempotencyStore(_db), new InMemoryChequeFormatRepository(_db));
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    [Fact]
    public async Task Each_account_has_each_print_type_once_numbered_in_order()
    {
        var a = await _formats.RegisterAsync(Key(), _account, ChequePrintType.NewLarge, CancellationToken.None);
        var b = await _formats.RegisterAsync(Key(), _account, ChequePrintType.NewSmall, CancellationToken.None);
        Assert.True(a.IsSuccess && b.IsSuccess);
        Assert.Equal("ChequeFormat.Taken", (await _formats.RegisterAsync(Key(), _account, ChequePrintType.NewLarge, CancellationToken.None)).ErrorCode);
        Assert.Equal("ChequeBook.BankAccountNotFound", (await _formats.RegisterAsync(Key(), FakeBankAccounts.Unknown, ChequePrintType.NewLarge, CancellationToken.None)).ErrorCode);
        Assert.Equal([1, 2], (await _formats.ListAsync(_account, true, CancellationToken.None)).Select(f => f.Number));
    }

    [Fact]
    public async Task The_scan_is_stored_and_read_back()
    {
        var id = (await _formats.RegisterAsync(Key(), _account, ChequePrintType.OldLarge, CancellationToken.None)).Value;
        Assert.True((await _formats.AttachImageAsync(Key(), id, Png, 1, CancellationToken.None)).IsSuccess);
        var image = await _formats.GetImageAsync(id, CancellationToken.None);
        Assert.Equal(("image/png", Png), (image!.Value.ContentType, image.Value.Data));
        Assert.True(Assert.Single(await _formats.ListAsync(_account, true, CancellationToken.None)).HasImage);
    }

    [Fact]
    public async Task A_book_names_its_type_and_the_type_is_then_kept()
    {
        var format = (await _formats.RegisterAsync(Key(), _account, ChequePrintType.NewestSmall, CancellationToken.None)).Value;
        var book = await _books.RegisterAsync(new(Key(), _account, "1234", 100001, 100025, format, "12345678", "1"), CancellationToken.None);
        Assert.True(book.IsSuccess, book.ErrorCode);
        Assert.Equal(format, Assert.Single(await _books.ListAsync(_account, true, CancellationToken.None)).FormatId);
        Assert.Equal("ChequeFormat.InUse", (await _formats.ChangeStateAsync(Key(), format, "delete", 1, CancellationToken.None)).ErrorCode);
        var other = (await _formats.RegisterAsync(Key(), Guid.NewGuid(), ChequePrintType.NewestSmall, CancellationToken.None)).Value;
        Assert.Equal("ChequeBook.FormatOtherAccount", (await _books.CorrectAsync(new(Key(), book.Value, "1234", 100001, 100025, 1, other), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_standard_type_is_defined_once_for_all_accounts_and_a_book_of_any_account_takes_it_owner_report_3()
    {
        var standard = await _formats.RegisterAsync(Key(), null, ChequePrintType.NewestLarge, CancellationToken.None);
        Assert.True(standard.IsSuccess, standard.ErrorCode);
        Assert.Equal("ChequeFormat.Taken", (await _formats.RegisterAsync(Key(), null, ChequePrintType.NewestLarge, CancellationToken.None)).ErrorCode);
        Assert.True((await _formats.RegisterAsync(Key(), _account, ChequePrintType.NewestLarge, CancellationToken.None)).IsSuccess); // own type too
        var book = await _books.RegisterAsync(new(Key(), Guid.NewGuid(), "1234", 100_001, 100_025, standard.Value, "12345678", "1"), CancellationToken.None);
        Assert.True(book.IsSuccess, book.ErrorCode);
        Assert.Null((await _formats.ListAsync(null, true, CancellationToken.None)).Single(f => f.Id == standard.Value).BankAccountId);
    }

    [Fact]
    public async Task Cheque_types_and_books_mark_their_bank_account_in_use_owner_report_3()
    {
        await _formats.RegisterAsync(Key(), _account, ChequePrintType.NewLarge, CancellationToken.None);
        var other = Guid.NewGuid();
        await _books.RegisterAsync(new(Key(), other, "1234", 100_001, 100_025, null, "12345678", "1"), CancellationToken.None);
        Assert.Equal(new HashSet<Guid> { _account, other }, _accounts.InUse);
    }
}

