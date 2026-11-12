using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.ChequeBooks;

/// <summary>What happened to a sheet of a cheque book.</summary>
public enum SheetUse
{
    Issued,
    Voided,
}

/// <summary>
/// A cheque book (دسته چک) of one of our bank accounts. Old system: dbo.Bundle (Fsheet, Lsheet, Lastused, Active).
/// Sheet numbers are handed out only through <see cref="ReserveNext"/> / <see cref="Reserve"/>, inside the same transaction
/// that saves the issued cheque, so two users can never get the same number (fixes the MVC race on LastUsedPaperNo).
/// </summary>
public sealed class ChequeBook : AggregateRoot<Guid>
{
    private readonly Dictionary<long, SheetUse> _usedSheets = [];

    private ChequeBook(Guid id, Guid bankAccountId, string serial, long firstSheet, long lastSheet)
        : base(id)
    {
        BankAccountId = bankAccountId;
        Serial = serial;
        FirstSheet = firstSheet;
        LastSheet = lastSheet;
        IsActive = true;
    }

    public Guid BankAccountId { get; }

    /// <summary>Series printed on every sheet of the book (شماره سری, 4 digits), before the serial: 1234/567801.</summary>
    public string Serial { get; private set; }

    public long FirstSheet { get; private set; }

    public long LastSheet { get; private set; }

    /// <summary>The cheque type (print design) the book is printed with (manual item 6 "شماره / نوع چک"); null: not chosen yet.</summary>
    public Guid? FormatId { get; private set; }

    /// <summary>A sheet has been issued or voided: from then on the book can no longer be corrected or deleted.</summary>
    public bool IsInUse => _usedSheets.Count > 0;

    public bool IsActive { get; private set; }

    public long SheetCount => LastSheet - FirstSheet + 1;

    public IReadOnlyDictionary<long, SheetUse> UsedSheets => _usedSheets;

    public long RemainingSheets => SheetCount - _usedSheets.Count;

    /// <summary>The Sayad numbers (شماره صیادی) of the book; null only for books from before Sayad cheques (old data).</summary>
    public ChequeBookSayad? Sayad { get; private set; }

    /// <summary>A book registered without Sayad numbers: only for old books brought over from the old system.</summary>
    public static ChequeBook Register(Guid id, Guid bankAccountId, string serial, long firstSheet, long lastSheet) =>
        Register(id, bankAccountId, serial, firstSheet, lastSheet, null);

    /// <summary>A book entered on the cheque books screen: series, serials and Sayad numbers (owner, 12 Nov 2026).</summary>
    public static ChequeBook Register(Guid id, Guid bankAccountId, string serial, long firstSheet, long lastSheet, ChequeBookSayad? sayad)
    {
        CheckRange(firstSheet, lastSheet);
        sayad?.CheckFits(lastSheet - firstSheet + 1);
        var book = new ChequeBook(id, bankAccountId, CleanSerial(serial), firstSheet, lastSheet) { Sayad = sayad };
        book.Raise(new ChequeBookRegistered(id, bankAccountId, book.Serial, firstSheet, lastSheet, sayad?.Middle, sayad?.FirstSerial));
        return book;
    }

    /// <summary>
    /// The 16-digit Sayad ID of a sheet: the 4 digits typed when the cheque is issued (they differ on every sheet), the book's
    /// fixed 8 middle digits, and the sheet's 4-digit Sayad serial (owner, 12 Nov 2026).
    /// </summary>
    public string SayadIdOf(long sheet, string firstFourDigits)
    {
        EnsureInRange(sheet);
        var sayad = Sayad ?? throw new DomainException("ChequeBook.SayadMissing", "This cheque book has no Sayad numbers.");
        var four = PersianText.Key(firstFourDigits);
        if (four.Length != 4 || !four.All(char.IsAsciiDigit))
        {
            throw new DomainException("ChequeBook.SayadPrefixInvalid", "The first part of the Sayad ID is 4 digits.");
        }

        return four + sayad.Middle + (sayad.FirstSerial + (sheet - FirstSheet)).ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Corrects a book typed wrongly (series, first or last sheet), only while none of its sheets has been used
    /// (owner, 11 Nov 2026: corrections and deletions only for data nothing depends on yet).
    /// </summary>
    /// <param name="sayad">The Sayad numbers; null keeps the current ones.</param>
    public void Correct(string serial, long firstSheet, long lastSheet, ChequeBookSayad? sayad = null)
    {
        if (IsInUse)
        {
            throw new DomainException("ChequeBook.InUse", "Sheets of this cheque book have been used; it can no longer be corrected.");
        }

        CheckRange(firstSheet, lastSheet);
        var cleaned = CleanSerial(serial);
        var newSayad = sayad ?? Sayad;
        newSayad?.CheckFits(lastSheet - firstSheet + 1);
        if ((cleaned, firstSheet, lastSheet, newSayad) == (Serial, FirstSheet, LastSheet, Sayad))
        {
            return;
        }

        (Serial, FirstSheet, LastSheet, Sayad) = (cleaned, firstSheet, lastSheet, newSayad);
        Raise(new ChequeBookCorrected(Id, Serial, FirstSheet, LastSheet, Sayad?.Middle, Sayad?.FirstSerial));
    }

    /// <summary>
    /// Names the cheque type the book is printed with: a type of the same bank account. Allowed at any time, also after sheets were
    /// used (a type kept from before may have been switched off since).
    /// </summary>
    public void UseFormat(ChequeFormat? format)
    {
        if (format is null)
        {
            if (FormatId is not null)
            {
                FormatId = null;
                Raise(new ChequeBookFormatChosen(Id, null));
            }

            return;
        }

        if (format.BankAccountId != BankAccountId)
        {
            throw new DomainException("ChequeBook.FormatOtherAccount", "The cheque type belongs to another bank account.");
        }

        if (!format.IsActive && format.Id != FormatId)
        {
            throw new DomainException("ChequeBook.FormatInactive", "The cheque type is switched off.");
        }

        if (format.Id != FormatId)
        {
            FormatId = format.Id;
            Raise(new ChequeBookFormatChosen(Id, format.Id));
        }
    }

    /// <summary>
    /// Do two books of the same bank account share sheet numbers, whatever their series? Owner, 12 Nov 2026: "دو دسته چک از یک
    /// حساب بانک نمی‌توانند شماره برگ چک مشترک یا مشابه داشته باشند".
    /// </summary>
    public bool Overlaps(ChequeBook other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Id != Id && other.BankAccountId == BankAccountId && other.FirstSheet <= LastSheet && FirstSheet <= other.LastSheet;
    }

    /// <summary>Deletes a book registered by mistake: only while none of its sheets has been used.</summary>
    public void Delete()
    {
        if (IsInUse)
        {
            throw new DomainException("ChequeBook.InUse", "Sheets of this cheque book have been used; it can only be switched off.");
        }

        Raise(new ChequeBookDeleted(Id));
    }

    /// <summary>The number the screen should suggest. Old system: Max(Checkno) + 1, here the lowest unused sheet.</summary>
    public long? SuggestNext()
    {
        if (!IsActive)
        {
            return null;
        }

        for (var n = FirstSheet; n <= LastSheet; n++)
        {
            if (!_usedSheets.ContainsKey(n))
            {
                return n;
            }
        }

        return null;
    }

    public long ReserveNext()
    {
        var next = SuggestNext() ?? throw new DomainException(
            IsActive ? "ChequeBook.Exhausted" : "ChequeBook.Inactive",
            IsActive ? "No unused sheets remain in this cheque book." : "This cheque book is not active.");
        Reserve(next);
        return next;
    }

    /// <summary>Reserves a specific sheet the user typed in (rule R5).</summary>
    public void Reserve(long sheet)
    {
        EnsureUsable(sheet);
        _usedSheets[sheet] = SheetUse.Issued;
        Raise(new ChequeSheetReserved(Id, sheet));
        DeactivateIfExhausted();
    }

    /// <summary>A spoiled or lost sheet. It can never be used again.</summary>
    public void Void(long sheet, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("ChequeBook.VoidReasonRequired", "A reason is required to void a sheet.");
        }

        EnsureInRange(sheet);
        if (_usedSheets.TryGetValue(sheet, out var use) && use == SheetUse.Voided)
        {
            return;
        }

        _usedSheets[sheet] = SheetUse.Voided;
        Raise(new ChequeSheetVoided(Id, sheet, reason.Trim()));
        DeactivateIfExhausted();
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            Raise(new ChequeBookDeactivated(Id));
        }
    }

    /// <summary>Manual item 6: "امکان ابطال دسته چک ها" and re-activation; a book with no unused sheet stays off.</summary>
    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        if (RemainingSheets == 0)
        {
            throw new DomainException("ChequeBook.Exhausted", "No unused sheets remain in this cheque book.");
        }

        IsActive = true;
        Raise(new ChequeBookActivated(Id));
    }

    public static ChequeBook Restore(
        Guid id, Guid bankAccountId, string serial, long firstSheet, long lastSheet, bool isActive, IEnumerable<KeyValuePair<long, SheetUse>> usedSheets, long version,
        Guid? formatId = null, ChequeBookSayad? sayad = null)
    {
        var book = new ChequeBook(id, bankAccountId, serial, firstSheet, lastSheet) { IsActive = isActive, FormatId = formatId, Sayad = sayad };
        foreach (var (sheet, use) in usedSheets)
        {
            book._usedSheets[sheet] = use;
        }

        book.SetLoadedVersion(version);
        return book;
    }

    /// <summary>Serial numbers (شماره سریال) printed on the sheets have 6 to 8 digits (owner, 12 Nov 2026).</summary>
    public const long LowestSerial = 100_000;

    public const long HighestSerial = 99_999_999;

    /// <summary>Only the last two digits of the serial change within a book (owner, 12 Nov 2026), so a book has at most 100 sheets.</summary>
    public const int MaxSheets = 100;

    /// <summary>The serial of a sheet as printed: series and serial separated by a slash, e.g. 1234/567801.</summary>
    public string SheetLabel(long sheet) => Serial.Length > 0 ? $"{Serial}/{sheet}" : sheet.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static void CheckRange(long firstSheet, long lastSheet)
    {
        if (firstSheet <= 0 || lastSheet < firstSheet)
        {
            throw new DomainException("ChequeBook.InvalidRange", "Last sheet must be greater than or equal to the first sheet.");
        }

        if (firstSheet < LowestSerial || lastSheet > HighestSerial)
        {
            throw new DomainException("ChequeBook.SerialInvalid", "A serial number (شماره سریال) has 6 to 8 digits.");
        }

        if (lastSheet - firstSheet + 1 > MaxSheets)
        {
            throw new DomainException("ChequeBook.TooManySheets", $"A cheque book has at most {MaxSheets} sheets.");
        }
    }

    /// <summary>
    /// The series (شماره سری): a fixed 4-digit number printed on every sheet of the book before the serial, separated by a slash
    /// (owner, 12 Nov 2026). Persian or Arabic digits are accepted and stored as 0-9.
    /// </summary>
    private static string CleanSerial(string? serial)
    {
        var digits = PersianText.Key(serial);
        if (digits.Length != 4 || !digits.All(char.IsAsciiDigit))
        {
            throw new DomainException("ChequeBook.SeriesInvalid", "The series (شماره سری) is a 4-digit number.");
        }

        return digits;
    }

    private void EnsureUsable(long sheet)
    {
        if (!IsActive)
        {
            throw new DomainException("ChequeBook.Inactive", "This cheque book is not active.");
        }

        EnsureInRange(sheet);
        if (_usedSheets.ContainsKey(sheet))
        {
            throw new DomainException("ChequeBook.SheetAlreadyUsed", $"Sheet {sheet} has already been used."); // old: "شماره چك تكراري است"
        }
    }

    private void EnsureInRange(long sheet)
    {
        if (sheet < FirstSheet || sheet > LastSheet)
        {
            throw new DomainException("ChequeBook.SheetOutOfRange", $"Sheet {sheet} does not belong to this cheque book ({FirstSheet}–{LastSheet})."); // old: "چك متعلق به دسته چك انتخابي نيست"
        }
    }

    /// <summary>
    /// Old system deactivated the book when its last sheet number was printed ("برگهاي دسته چك تمام شد").
    /// Here the book is deactivated when no unused sheet remains — confirm with the domain expert (open question Q3).
    /// </summary>
    private void DeactivateIfExhausted()
    {
        if (RemainingSheets == 0)
        {
            Deactivate();
        }
    }
}

public sealed record ChequeBookRegistered(Guid ChequeBookId, Guid BankAccountId, string Serial, long FirstSheet, long LastSheet, string? SayadMiddle = null, int? SayadFirstSerial = null) : DomainEvent;

/// <summary>
/// The Sayad numbers of a cheque book (شماره صیادی, 16 digits per sheet, owner 12 Nov 2026): the 8 middle digits are the same on
/// every sheet and typed once with the book; the last 4 run in sequence from <see cref="FirstSerial"/>, one per sheet; the first 4
/// differ on every sheet and are typed when the cheque is issued.
/// </summary>
public sealed record ChequeBookSayad
{
    private ChequeBookSayad(string middle, int firstSerial)
    {
        Middle = middle;
        FirstSerial = firstSerial;
    }

    /// <summary>The fixed 8 middle digits.</summary>
    public string Middle { get; }

    /// <summary>The 4-digit Sayad serial of the first sheet.</summary>
    public int FirstSerial { get; }

    /// <summary>Both parts are required on the cheque books screen; Persian or Arabic digits are accepted.</summary>
    public static ChequeBookSayad Create(string? middle, string? firstSerial)
    {
        var m = PersianText.Key(middle);
        var s = PersianText.Key(firstSerial);
        if (m.Length == 0 && s.Length == 0)
        {
            throw new DomainException("ChequeBook.SayadRequired", "Enter the 8 middle digits and the first 4-digit serial of the Sayad ID.");
        }

        if (m.Length != 8 || !m.All(char.IsAsciiDigit))
        {
            throw new DomainException("ChequeBook.SayadMiddleInvalid", "The middle part of the Sayad ID is 8 digits.");
        }

        if (s.Length is 0 or > 4 || !s.All(char.IsAsciiDigit))
        {
            throw new DomainException("ChequeBook.SayadSerialInvalid", "The Sayad serial is up to 4 digits.");
        }

        return new ChequeBookSayad(m, int.Parse(s, System.Globalization.CultureInfo.InvariantCulture));
    }

    public static ChequeBookSayad Restore(string middle, int firstSerial) => new(middle, firstSerial);

    /// <summary>The Sayad serial of the last sheet must still have 4 digits.</summary>
    public void CheckFits(long sheetCount)
    {
        if (FirstSerial + sheetCount - 1 > 9999)
        {
            throw new DomainException("ChequeBook.SayadSerialInvalid", "The Sayad serials of this book would go past 9999.");
        }
    }

    public int LastSerial(long sheetCount) => (int)(FirstSerial + sheetCount - 1);
}

public sealed record ChequeSheetReserved(Guid ChequeBookId, long Sheet) : DomainEvent;

public sealed record ChequeSheetVoided(Guid ChequeBookId, long Sheet, string Reason) : DomainEvent;

public sealed record ChequeBookDeactivated(Guid ChequeBookId) : DomainEvent;

public sealed record ChequeBookActivated(Guid ChequeBookId) : DomainEvent;

public sealed record ChequeBookCorrected(Guid ChequeBookId, string Serial, long FirstSheet, long LastSheet, string? SayadMiddle = null, int? SayadFirstSerial = null) : DomainEvent;

public sealed record ChequeBookDeleted(Guid ChequeBookId) : DomainEvent;

public sealed record ChequeBookFormatChosen(Guid ChequeBookId, Guid? ChequeFormatId) : DomainEvent;
