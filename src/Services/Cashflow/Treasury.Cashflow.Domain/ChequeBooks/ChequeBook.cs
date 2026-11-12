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

    /// <summary>Series printed on the book (سری).</summary>
    public string Serial { get; private set; }

    public long FirstSheet { get; private set; }

    public long LastSheet { get; private set; }

    /// <summary>A sheet has been issued or voided: from then on the book can no longer be corrected or deleted.</summary>
    public bool IsInUse => _usedSheets.Count > 0;

    public bool IsActive { get; private set; }

    public long SheetCount => LastSheet - FirstSheet + 1;

    public IReadOnlyDictionary<long, SheetUse> UsedSheets => _usedSheets;

    public long RemainingSheets => SheetCount - _usedSheets.Count;

    public static ChequeBook Register(Guid id, Guid bankAccountId, string serial, long firstSheet, long lastSheet)
    {
        CheckRange(firstSheet, lastSheet);
        var book = new ChequeBook(id, bankAccountId, CleanSerial(serial), firstSheet, lastSheet);
        book.Raise(new ChequeBookRegistered(id, bankAccountId, book.Serial, firstSheet, lastSheet));
        return book;
    }

    /// <summary>
    /// Corrects a book typed wrongly (series, first or last sheet), only while none of its sheets has been used
    /// (owner, 11 Nov 2026: corrections and deletions only for data nothing depends on yet).
    /// </summary>
    public void Correct(string serial, long firstSheet, long lastSheet)
    {
        if (IsInUse)
        {
            throw new DomainException("ChequeBook.InUse", "Sheets of this cheque book have been used; it can no longer be corrected.");
        }

        CheckRange(firstSheet, lastSheet);
        var cleaned = CleanSerial(serial);
        if ((cleaned, firstSheet, lastSheet) == (Serial, FirstSheet, LastSheet))
        {
            return;
        }

        (Serial, FirstSheet, LastSheet) = (cleaned, firstSheet, lastSheet);
        Raise(new ChequeBookCorrected(Id, Serial, FirstSheet, LastSheet));
    }

    /// <summary>Do two books of the same account and series share sheet numbers? (same cheque number twice on one account)</summary>
    public bool Overlaps(ChequeBook other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Id != Id && other.BankAccountId == BankAccountId
            && string.Equals(PersianText.Key(other.Serial), PersianText.Key(Serial), StringComparison.Ordinal)
            && other.FirstSheet <= LastSheet && FirstSheet <= other.LastSheet;
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

    public static ChequeBook Restore(Guid id, Guid bankAccountId, string serial, long firstSheet, long lastSheet, bool isActive, IEnumerable<KeyValuePair<long, SheetUse>> usedSheets, long version)
    {
        var book = new ChequeBook(id, bankAccountId, serial, firstSheet, lastSheet) { IsActive = isActive };
        foreach (var (sheet, use) in usedSheets)
        {
            book._usedSheets[sheet] = use;
        }

        book.SetLoadedVersion(version);
        return book;
    }

    private static void CheckRange(long firstSheet, long lastSheet)
    {
        if (firstSheet <= 0 || lastSheet < firstSheet)
        {
            throw new DomainException("ChequeBook.InvalidRange", "Last sheet must be greater than or equal to the first sheet.");
        }

        if (lastSheet - firstSheet + 1 > 1000)
        {
            throw new DomainException("ChequeBook.TooManySheets", "A cheque book cannot have more than 1000 sheets.");
        }
    }

    private static string CleanSerial(string? serial) => PersianText.Clean(serial);

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

public sealed record ChequeBookRegistered(Guid ChequeBookId, Guid BankAccountId, string Serial, long FirstSheet, long LastSheet) : DomainEvent;

public sealed record ChequeSheetReserved(Guid ChequeBookId, long Sheet) : DomainEvent;

public sealed record ChequeSheetVoided(Guid ChequeBookId, long Sheet, string Reason) : DomainEvent;

public sealed record ChequeBookDeactivated(Guid ChequeBookId) : DomainEvent;

public sealed record ChequeBookActivated(Guid ChequeBookId) : DomainEvent;

public sealed record ChequeBookCorrected(Guid ChequeBookId, string Serial, long FirstSheet, long LastSheet) : DomainEvent;

public sealed record ChequeBookDeleted(Guid ChequeBookId) : DomainEvent;
