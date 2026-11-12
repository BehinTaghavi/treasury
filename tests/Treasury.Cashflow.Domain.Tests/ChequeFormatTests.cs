using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.ChequeBooks;
using Xunit;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>Cheque types (new-version manual item 7, MVC ChequeFormatTypes) and choosing one for a cheque book.</summary>
public sealed class ChequeFormatTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 16];

    [Fact]
    public void A_scan_is_a_png_or_jpeg_of_at_most_2_mb()
    {
        var f = ChequeFormat.Register(Guid.NewGuid(), 1, Guid.NewGuid(), ChequePrintType.NewLarge);
        Assert.Equal("image/png", f.AttachImage(Png));
        Assert.Equal("image/jpeg", f.AttachImage(Jpeg));
        Assert.Equal("ChequeFormat.ImageInvalid", Assert.Throws<DomainException>(() => f.AttachImage("%PDF-1.7"u8)).Code);
        Assert.Equal("ChequeFormat.ImageInvalid", Assert.Throws<DomainException>(() => f.AttachImage([])).Code);
        Assert.Equal("ChequeFormat.ImageTooLarge", Assert.Throws<DomainException>(() => f.AttachImage(new byte[ChequeFormat.MaxImageBytes + 1])).Code);
    }

    [Fact]
    public void A_type_used_by_a_cheque_book_is_not_deleted() =>
        Assert.Equal("ChequeFormat.InUse", Assert.Throws<DomainException>(() =>
            ChequeFormat.Register(Guid.NewGuid(), 1, Guid.NewGuid(), ChequePrintType.OldSmall).Delete(usedByChequeBook: true)).Code);

    [Fact]
    public void A_cheque_book_takes_a_type_of_its_own_account_at_any_time()
    {
        var account = Guid.NewGuid();
        var book = ChequeBook.Register(Guid.NewGuid(), account, "1234", 200_001, 200_010);
        var mine = ChequeFormat.Register(Guid.NewGuid(), 1, account, ChequePrintType.NewestLarge);
        var other = ChequeFormat.Register(Guid.NewGuid(), 2, Guid.NewGuid(), ChequePrintType.NewestLarge);
        book.ReserveNext(); // already in use: the type may still be chosen
        book.UseFormat(mine);
        Assert.Equal(mine.Id, book.FormatId);
        Assert.Equal("ChequeBook.FormatOtherAccount", Assert.Throws<DomainException>(() => book.UseFormat(other)).Code);
        mine.Deactivate();
        book.UseFormat(mine); // kept from before
        var off = ChequeFormat.Register(Guid.NewGuid(), 3, account, ChequePrintType.OldLarge);
        off.Deactivate();
        Assert.Equal("ChequeBook.FormatInactive", Assert.Throws<DomainException>(() => book.UseFormat(off)).Code);
    }
}
