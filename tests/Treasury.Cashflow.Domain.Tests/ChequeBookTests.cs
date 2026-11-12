using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.ChequeBooks;
using Xunit;

namespace Treasury.Cashflow.Domain.Tests;

public sealed class ChequeBookTests
{
    private static ChequeBook Book(long first = 100_101, long last = 100_125) =>
        ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), "9712", first, last);

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
        var book = Book(200_001, 200_004);
        book.Reserve(200_001);
        book.Void(200_002, "پاره شد");
        Assert.Equal(200_003, book.ReserveNext());
    }

    [Fact]
    public void Deactivates_when_no_unused_sheet_remains()
    {
        // Old message: "برگهاي دسته چك تمام شد.دسته چك غيرفعال خواهد شد"
        var book = Book(200_001, 200_002);
        book.ReserveNext();
        book.ReserveNext();
        Assert.False(book.IsActive);
        Assert.Null(book.SuggestNext());
        Assert.Equal("ChequeBook.Inactive", Assert.Throws<DomainException>(() => book.ReserveNext()).Code);
    }

    [Fact]
    public void A_voided_sheet_needs_a_reason_and_cannot_be_issued()
    {
        var book = Book(200_001, 200_005);
        Assert.Equal("ChequeBook.VoidReasonRequired", Assert.Throws<DomainException>(() => book.Void(200_003, " ")).Code);
        book.Void(200_003, "خط خوردگی");
        Assert.Equal("ChequeBook.SheetAlreadyUsed", Assert.Throws<DomainException>(() => book.Reserve(200_003)).Code);
    }

    [Theory]
    [InlineData(0L, 10L, "ChequeBook.InvalidRange")]
    [InlineData(200_020L, 200_010L, "ChequeBook.InvalidRange")]
    [InlineData(200_001L, 200_101L, "ChequeBook.TooManySheets")]
    [InlineData(1_001L, 1_050L, "ChequeBook.SerialInvalid")]
    [InlineData(99_999_990L, 100_000_009L, "ChequeBook.SerialInvalid")]
    public void Rejects_invalid_ranges_owner_12_nov(long first, long last, string code) =>
        Assert.Equal(code, Assert.Throws<DomainException>(() => ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), "1234", first, last)).Code);

    [Theory]
    [InlineData("1234", "1234")]
    [InlineData(" ۱۲۳۴ ", "1234")]
    public void The_series_is_four_digits_written_in_any_digits(string typed, string stored)
    {
        var book = ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), typed, 567_801, 567_850);
        Assert.Equal(stored, book.Serial);
        Assert.Equal("1234/567801", book.SheetLabel(567_801));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12/34")]
    [InlineData("الف")]
    public void A_series_that_is_not_four_digits_is_refused(string typed) =>
        Assert.Equal("ChequeBook.SeriesInvalid", Assert.Throws<DomainException>(() => ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), typed, 567_801, 567_850)).Code);

    [Fact]
    public void An_exhausted_book_cannot_be_switched_on_again()
    {
        var book = ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), "1234", 200_001, 200_001);
        book.ReserveNext();
        Assert.False(book.IsActive);
        Assert.Equal("ChequeBook.Exhausted", Assert.Throws<DomainException>(book.Activate).Code);
    }

    [Fact]
    public void Books_of_one_account_never_share_sheet_numbers_whatever_their_series()
    {
        var account = Guid.NewGuid();
        var a = ChequeBook.Register(Guid.NewGuid(), account, "1111", 300_100, 300_150);
        Assert.True(a.Overlaps(ChequeBook.Register(Guid.NewGuid(), account, "1111", 300_150, 300_200)));
        Assert.False(a.Overlaps(ChequeBook.Register(Guid.NewGuid(), account, "1111", 300_151, 300_200)));
        Assert.True(a.Overlaps(ChequeBook.Register(Guid.NewGuid(), account, "2222", 300_120, 300_130)));
        Assert.False(a.Overlaps(ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), "1111", 300_120, 300_130)));
        Assert.False(a.Overlaps(a));
    }

    [Fact]
    public void Correcting_raises_one_event_and_an_unchanged_book_none()
    {
        var book = ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), "1234", 200_001, 200_050);
        book.MarkSaved(1);
        book.Correct("1234", 200_001, 200_050);
        Assert.Empty(book.PendingEvents);
        book.Correct("5678", 200_011, 200_060);
        Assert.Equal(("5678", 200_011L, 200_060L), (book.Serial, book.FirstSheet, book.LastSheet));
        Assert.IsType<ChequeBookCorrected>(Assert.Single(book.PendingEvents));
    }

    [Fact]
    public void The_sayad_id_of_a_sheet_is_the_typed_four_digits_the_books_eight_and_the_sheets_serial_owner_12_nov()
    {
        var sayad = ChequeBookSayad.Create("۱۲۳۴۵۶۷۸", "0021");
        var book = ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), "1234", 567_801, 567_850, sayad);
        Assert.Equal(("12345678", 21, 70), (book.Sayad!.Middle, book.Sayad.FirstSerial, book.Sayad.LastSerial(book.SheetCount)));
        Assert.Equal("9876123456780021", book.SayadIdOf(567_801, "9876"));
        Assert.Equal("9876123456780030", book.SayadIdOf(567_810, "۹۸۷۶"));
        Assert.Equal("ChequeBook.SayadPrefixInvalid", Assert.Throws<DomainException>(() => book.SayadIdOf(567_801, "98")).Code);
        Assert.Equal("ChequeBook.SheetOutOfRange", Assert.Throws<DomainException>(() => book.SayadIdOf(567_851, "9876")).Code);
        Assert.Equal("ChequeBook.SayadMissing", Assert.Throws<DomainException>(() => Book().SayadIdOf(100_101, "9876")).Code);
    }

    [Theory]
    [InlineData("", "", "ChequeBook.SayadRequired")]
    [InlineData("1234567", "1", "ChequeBook.SayadMiddleInvalid")]
    [InlineData("123456789", "1", "ChequeBook.SayadMiddleInvalid")]
    [InlineData("12345678", "", "ChequeBook.SayadSerialInvalid")]
    [InlineData("12345678", "10000", "ChequeBook.SayadSerialInvalid")]
    public void Sayad_numbers_are_eight_middle_digits_and_a_four_digit_serial(string middle, string first, string code) =>
        Assert.Equal(code, Assert.Throws<DomainException>(() => ChequeBookSayad.Create(middle, first)).Code);

    [Fact]
    public void The_sayad_serials_of_a_book_stay_within_four_digits() =>
        Assert.Equal("ChequeBook.SayadSerialInvalid", Assert.Throws<DomainException>(() =>
            ChequeBook.Register(Guid.NewGuid(), Guid.NewGuid(), "1234", 567_801, 567_850, ChequeBookSayad.Create("12345678", "9990"))).Code);
}
