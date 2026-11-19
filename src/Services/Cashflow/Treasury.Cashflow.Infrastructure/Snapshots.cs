using System.Text.Json;
using System.Text.Json.Serialization;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;

namespace Treasury.Cashflow.Infrastructure;

/// <summary>Flat, storable form of an instrument. Dates are ISO dates (yyyy-MM-dd), never Jalali text.</summary>
public sealed record InstrumentSnapshot(
    Guid Id,
    InstrumentDirection Direction,
    InstrumentKind Kind,
    string? Number,
    string? BankName,
    Guid? BankAccountId,
    Guid? ChequeBookId,
    Guid? PaymentId,
    decimal Amount,
    string Currency,
    decimal? RateToRial,
    DateOnly IssueDate,
    DateOnly? DueDate,
    Guid CounterpartyId,
    string CounterpartyName,
    string Purpose,
    string? Destination,
    InstrumentState State,
    int OriginFiscalYear,
    IReadOnlyList<HistorySnapshot> History,
    long Version,
    Guid? ReceiptId = null,
    ReceivedInstrumentDetails? Details = null,
    string? CounterpartyDetailCode = null,
    Guid? CollectionAccountId = null)
{
    public static InstrumentSnapshot From(Instrument i, long version) => new(
        i.Id, i.Direction, i.Kind, i.Number, i.BankName, i.BankAccountId, i.ChequeBookId, i.PaymentId,
        i.Amount.Amount, i.Amount.Currency, i.Amount.RateToRial,
        i.IssueDate.Value, i.DueDate?.Value, i.Counterparty.Id, i.Counterparty.DisplayName, i.Purpose, i.Destination,
        i.State, i.OriginFiscalYear,
        i.History.Select(h => new HistorySnapshot(h.Action, h.From, h.To, h.Date.Value, h.UserId, h.Reference, h.Note, StepDetailsSnapshot.From(h.Details))).ToList(),
        version, i.ReceiptId, i.Direction == InstrumentDirection.Received ? i.Details : null, i.Counterparty.DetailCode, i.CollectionAccountId);

    public Instrument ToDomain() => Instrument.Restore(
        Id, Direction, Kind, Number, BankName, BankAccountId, ChequeBookId, PaymentId,
        Currency == Money.Rial ? Money.Rials(Amount) : Money.Foreign(Amount, Currency, RateToRial ?? 0),
        new FiscalDate(IssueDate), DueDate is { } d ? new FiscalDate(d) : null,
        new CounterpartyRef(CounterpartyId, CounterpartyName, CounterpartyDetailCode), Purpose, Destination, State, OriginFiscalYear,
        History.Select(h => new InstrumentHistoryEntry(h.Action, h.From, h.To, new FiscalDate(h.Date), h.UserId, h.Reference, h.Note, h.Details?.ToDomain())),
        Version, ReceiptId, Details, CollectionAccountId);
}

/// <summary>Flat, storable form of a receipt. Dates are ISO dates.</summary>
public sealed record ReceiptSnapshot(
    Guid Id, ReceiptKind Kind, int FiscalYear, int Number, DateOnly Date, Guid PayerId, string PayerName, string? PayerDetailCode,
    Guid PurposeGroupId, string PurposeGroupName, string Purpose, string? Reference1, string? Reference2, Guid? DepartmentId, bool SalesProceeds,
    string? Note, string RecordedBy, ReceiptAccountKind? AccountKind, Guid? AccountId, DepositMethod? Method, string? TrackingNumber,
    decimal? Amount, string? Currency, decimal? RateToRial, IReadOnlyList<Guid> InstrumentIds, decimal TotalRial, ReceiptState State,
    DateOnly? CancelledOn, string? CancelReason, long Version, Guid? ReplacedInstrumentId = null, string? PayerSheba = null, NoteTermsSnapshot? NoteTerms = null)
{
    public static ReceiptSnapshot From(Receipt r, long version) => new(
        r.Id, r.Kind, r.FiscalYear, r.Number, r.Date.Value, r.Payer.Id, r.Payer.DisplayName, r.Payer.DetailCode, r.PurposeGroupId, r.PurposeGroupName,
        r.Purpose, r.Reference1, r.Reference2, r.DepartmentId, r.SalesProceeds, r.Note, r.RecordedBy, r.AccountKind, r.AccountId, r.Method,
        r.TrackingNumber, r.Amount?.Amount, r.Amount?.Currency, r.Amount?.RateToRial, [.. r.InstrumentIds], r.TotalRial, r.State,
        r.CancelledOn?.Value, r.CancelReason, version, r.ReplacedInstrumentId, r.PayerSheba, NoteTermsSnapshot.From(r.NoteTerms));

    public Receipt ToDomain() => Receipt.Restore(
        Id, Kind, Number, new FiscalDate(Date), new CounterpartyRef(PayerId, PayerName, PayerDetailCode), PurposeGroupId, PurposeGroupName, Purpose,
        Reference1, Reference2, DepartmentId, SalesProceeds, Note, RecordedBy, AccountKind, AccountId, Method, TrackingNumber,
        Amount is { } a ? (Currency == Money.Rial ? Money.Rials(a) : Money.Foreign(a, Currency!, RateToRial ?? 0)) : null,
        InstrumentIds, TotalRial, State, CancelledOn is { } c ? new FiscalDate(c) : null, CancelReason, Version, ReplacedInstrumentId, PayerSheba, NoteTerms?.ToDomain());
}

/// <summary>Storable form of <see cref="NoteReceiptTerms"/> (JSON in cashflow.receipt.note_terms); the date is an ISO date.</summary>
public sealed record NoteTermsSnapshot(
    NoteReason Reason, NoteForm Form, NoteMaturity Maturity, string? IssuerNationalCode = null, string? IssuerPhone = null, string? CommitmentSubject = null,
    string? ReturnTerms = null, string? CustodyPlace = null, bool OriginalReceived = false, DateOnly? OriginalReceivedOn = null, string? Discrepancy = null,
    NoteAttachments? Attachments = null, string? DeliveredBy = null, IReadOnlyList<ReceiptSigner>? Signers = null)
{
    public static NoteTermsSnapshot? From(NoteReceiptTerms? t) => t is null ? null
        : new(t.Reason, t.Form, t.Maturity, t.IssuerNationalCode, t.IssuerPhone, t.CommitmentSubject, t.ReturnTerms, t.CustodyPlace, t.OriginalReceived,
            t.OriginalReceivedOn?.Value, t.Discrepancy, t.Attachments, t.DeliveredBy, t.Signers);

    public NoteReceiptTerms ToDomain() => new(
        Reason, Form, Maturity, IssuerNationalCode, IssuerPhone, CommitmentSubject, ReturnTerms, CustodyPlace, OriginalReceived,
        OriginalReceivedOn is { } d ? new FiscalDate(d) : null, Discrepancy, Attachments, DeliveredBy, Signers);
}

public sealed record HistorySnapshot(
    InstrumentAction Action, InstrumentState? From, InstrumentState To, DateOnly Date, string UserId, string? Reference, string? Note, StepDetailsSnapshot? Details = null);

/// <summary>Storable form of <see cref="StepDetails"/> (JSON in cashflow.instrument_event.details); dates are ISO dates.</summary>
public sealed record StepDetailsSnapshot(
    Guid? AccountId = null, string? PartyDetailCode = null, string? PartyName = null, DateOnly? DocumentDate = null, Guid? PurposeGroupId = null,
    string? Purpose = null, string? SayadTracking = null, Guid? ReceiptId = null, Guid? PaymentRequestId = null, string? TrackingCode = null,
    int? SheetNumber = null, IReadOnlyList<ReceiptSigner>? Signers = null)
{
    public static StepDetailsSnapshot? From(StepDetails? d) => d is null ? null
        : new(d.AccountId, d.PartyDetailCode, d.PartyName, d.DocumentDate?.Value, d.PurposeGroupId, d.Purpose, d.SayadTracking, d.ReceiptId, d.PaymentRequestId, d.TrackingCode,
            d.SheetNumber, d.Signers);

    public StepDetails ToDomain() =>
        new(AccountId, PartyDetailCode, PartyName, DocumentDate is { } d ? new FiscalDate(d) : null, PurposeGroupId, Purpose, SayadTracking, ReceiptId, PaymentRequestId,
            TrackingCode, SheetNumber, Signers);
}

public sealed record ChequeBookSnapshot(
    Guid Id, Guid BankAccountId, string Serial, long FirstSheet, long LastSheet, bool IsActive, IReadOnlyDictionary<long, SheetUse> UsedSheets, long Version,
    Guid? FormatId = null, string? SayadMiddle = null, int? SayadFirstSerial = null, bool PartlyUsed = false, int? SerialDigits = null)
{
    public static ChequeBookSnapshot From(ChequeBook b, long version) =>
        new(b.Id, b.BankAccountId, b.Serial, b.FirstSheet, b.LastSheet, b.IsActive, new Dictionary<long, SheetUse>(b.UsedSheets), version, b.FormatId,
            b.Sayad?.Middle, b.Sayad?.FirstSerial, b.PartlyUsed, b.SerialDigits);

    public ChequeBook ToDomain() => ChequeBook.Restore(Id, BankAccountId, Serial, FirstSheet, LastSheet, IsActive, UsedSheets, Version, FormatId,
        SayadMiddle is { } m && SayadFirstSerial is { } s ? ChequeBookSayad.Restore(m, s) : null, PartlyUsed, SerialDigits);
}

public sealed record ChequeFormatSnapshot(Guid Id, int Number, Guid? BankAccountId, ChequePrintType PrintType, string? ImageType, bool IsActive, long Version)
{
    public static ChequeFormatSnapshot From(ChequeFormat f, long version) => new(f.Id, f.Number, f.BankAccountId, f.PrintType, f.ImageType, f.IsActive, version);

    public ChequeFormat ToDomain() => ChequeFormat.Restore(Id, Number, BankAccountId, PrintType, ImageType, IsActive, Version);
}

/// <summary>A domain event waiting in the outbox. Saved in the same transaction as the aggregate; published afterwards.</summary>
public sealed record OutboxMessage(Guid Id, string Type, string Payload, DateTimeOffset OccurredAt)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        Converters = { new JsonStringEnumConverter(), new FiscalDateJsonConverter() },
    };

    public static OutboxMessage From(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return new(domainEvent.EventId, domainEvent.GetType().Name, JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Json), domainEvent.OccurredAt);
    }
}

/// <summary>Writes a FiscalDate as an ISO date in events, so other services never parse Jalali text.</summary>
public sealed class FiscalDateJsonConverter : JsonConverter<FiscalDate>
{
    public override FiscalDate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(DateOnly.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture));

    public override void Write(Utf8JsonWriter writer, FiscalDate value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
    }
}
