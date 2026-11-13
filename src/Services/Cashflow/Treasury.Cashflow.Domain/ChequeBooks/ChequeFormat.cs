using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.ChequeBooks;

/// <summary>
/// The six cheque designs of the manual (item 7, "به صورت Hard Code در این کومبو تعریف شده"): banks change their large and small
/// cheque designs over time, so one account may have old, new and newest books at the same time.
/// </summary>
public enum ChequePrintType
{
    /// <summary>جدید – قطع بزرگ</summary>
    NewLarge,

    /// <summary>جدیدترین – قطع بزرگ</summary>
    NewestLarge,

    /// <summary>قدیمی – قطع بزرگ</summary>
    OldLarge,

    /// <summary>جدید – قطع کوچک</summary>
    NewSmall,

    /// <summary>جدیدترین – قطع کوچک</summary>
    NewestSmall,

    /// <summary>قطع کوچک (the manual's sixth entry: the old small design)</summary>
    OldSmall,
}

/// <summary>
/// A cheque type of one of our bank accounts (معرفی انواع چک و چاپ آنها), new-version manual item 7; MVC ChequeFormatTypes:
/// a print number, the print type and a scan of the cheque (فایل نمونه اسکن شکل چک) used when cheques are printed. Each account has
/// each print type once. Cheque books name the type they are printed with. The positions of the printed fields come with cheque
/// printing (Phase E). A type without a bank account is the standard cheque of all banks (owner's report no. 3, item 3: "تمامی
/// بانکها از یک فرمت استاندارد برای چک استفاده میکنند"): defined once and usable by every account's cheque books.
/// </summary>
public sealed class ChequeFormat : AggregateRoot<Guid>
{
    public const int MaxImageBytes = 2 * 1024 * 1024;

    private ChequeFormat(Guid id, int number, Guid? bankAccountId, ChequePrintType printType)
        : base(id)
    {
        Number = number;
        BankAccountId = bankAccountId;
        PrintType = printType;
        IsActive = true;
    }

    /// <summary>Manual: "شماره چاپ" – given in order.</summary>
    public int Number { get; }

    /// <summary>The account the type belongs to; null: the standard type for all accounts.</summary>
    public Guid? BankAccountId { get; }

    public bool IsStandard => BankAccountId is null;

    public ChequePrintType PrintType { get; private set; }

    /// <summary>Media type of the stored scan (image/png or image/jpeg); null: no scan yet.</summary>
    public string? ImageType { get; private set; }

    public bool IsActive { get; private set; }

    public static ChequeFormat Register(Guid id, int number, Guid? bankAccountId, ChequePrintType printType)
    {
        Require(number > 0, "ChequeFormat.NumberInvalid", "The print number is a positive number.");
        Require(Enum.IsDefined(printType), "ChequeFormat.PrintTypeInvalid", "Unknown print type.");
        var f = new ChequeFormat(id, number, bankAccountId, printType);
        f.Raise(ChequeFormatChanged.From(f));
        return f;
    }

    public void ChangePrintType(ChequePrintType printType)
    {
        Require(Enum.IsDefined(printType), "ChequeFormat.PrintTypeInvalid", "Unknown print type.");
        if (printType != PrintType)
        {
            PrintType = printType;
            Raise(ChequeFormatChanged.From(this));
        }
    }

    /// <summary>Checks a scan file (PNG or JPEG by its first bytes, at most 2 MB) and records its media type.</summary>
    public string AttachImage(ReadOnlySpan<byte> file)
    {
        Require(file.Length > 0, "ChequeFormat.ImageInvalid", "The scan file is empty.");
        Require(file.Length <= MaxImageBytes, "ChequeFormat.ImageTooLarge", "The scan file is larger than 2 MB.");
        var type = ImageTypeOf(file) ?? throw new DomainException("ChequeFormat.ImageInvalid", "The scan must be a PNG or JPEG picture.");
        ImageType = type;
        Raise(ChequeFormatChanged.From(this));
        return type;
    }

    public void Activate() => SetActive(true);

    public void Deactivate() => SetActive(false);

    /// <summary>A type no cheque book names may be deleted.</summary>
    public void Delete(bool usedByChequeBook)
    {
        Require(!usedByChequeBook, "ChequeFormat.InUse", "Cheque books are printed with this type; switch it off instead.");
        Raise(new ChequeFormatDeleted(Id));
    }

    public static ChequeFormat Restore(Guid id, int number, Guid? bankAccountId, ChequePrintType printType, string? imageType, bool isActive, long version)
    {
        var f = new ChequeFormat(id, number, bankAccountId, printType) { ImageType = imageType, IsActive = isActive };
        f.SetLoadedVersion(version);
        return f;
    }

    private static string? ImageTypeOf(ReadOnlySpan<byte> b) =>
        b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ? "image/png"
        : b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ? "image/jpeg"
        : null;

    private void SetActive(bool active)
    {
        if (IsActive != active)
        {
            IsActive = active;
            Raise(ChequeFormatChanged.From(this));
        }
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

public sealed record ChequeFormatChanged(Guid ChequeFormatId, int Number, Guid? BankAccountId, ChequePrintType PrintType, bool HasImage, bool IsActive) : DomainEvent
{
    public static ChequeFormatChanged From(ChequeFormat f) => new(f.Id, f.Number, f.BankAccountId, f.PrintType, f.ImageType is not null, f.IsActive);
}

public sealed record ChequeFormatDeleted(Guid ChequeFormatId) : DomainEvent;
