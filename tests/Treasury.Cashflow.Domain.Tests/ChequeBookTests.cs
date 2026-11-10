using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.ChequeBooks;
using Xunit;

namespace Treasury.Cashflow.Domain.Tests;

public sealed class ChequeBookTests
{
    private static ChequeBook Book(long first = 100_101, long last = 100_125) =>
        ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), "97/12", first, last);

    [Fact]
    public void Hands_out_sheets_in_order()
    {
        var book = Book();
        Assert.Equal(100_101, book.ReserveNext());
        Assert.Equal(100_102, book.ReserveNext());
        Assert.Equal(23, book.RemainingSheets);
    }

    [Fact]
    public void A_typed_number_must_belong_to_the_book_and_be_unused()
    {
        // Old messages: "چك متعلق به دسته چك انتخابي نيست", "شماره چك تكراري است"
        var book = Book();
        Assert.Equal("ChequeBook.SheetOutOfRange", Assert.Throws<DomainException>(() => book.Reserve(100_126)).Code);
        book.Reserve(100_110);
        Assert.Equal("ChequeBook.SheetAlreadyUsed", Assert.Throws<DomainException>(() => book.Reserve(100_110)).Code);
    }

    [Fact]
    public void Skips_sheets_that_were_typed_or_voided()
    {
        var book = Book(1, 4);
        book.Reserve(1);
        book.Void(2, "پاره شد");
        Assert.Equal(3, book.ReserveNext());
    }

    [Fact]
    public void Deactivates_when_no_unused_sheet_remains()
    {
        // Old message: "برگهاي دسته چك تمام شد.دسته چك غيرفعال خواهد شد"
        var book = Book(1, 2);
        book.ReserveNext();
        book.ReserveNext();
        Assert.False(book.IsActive);
        Assert.Null(book.SuggestNext());
        Assert.Equal("ChequeBook.Inactive", Assert.Throws<DomainException>(() => book.ReserveNext()).Code);
    }

    [Fact]
    public void A_voided_sheet_needs_a_reason_and_cannot_be_issued()
    {
        var book = Book(1, 5);
        Assert.Equal("ChequeBook.VoidReasonRequired", Assert.Throws<DomainException>(() => book.Void(3, " ")).Code);
        book.Void(3, "خط خوردگی");
        Assert.Equal("ChequeBook.SheetAlreadyUsed", Assert.Throws<DomainException>(() => book.Reserve(3)).Code);
    }

    [Theory]
    [InlineData(0L, 10L)]
    [InlineData(20L, 10L)]
    [InlineData(1L, 2000L)]
    public void Rejects_invalid_ranges(long first, long last) =>
        Assert.Throws<DomainException>(() => ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), "x", first, last));
}
