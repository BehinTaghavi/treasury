using System.Text.Json;
using System.Text.Json.Serialization;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;

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
    long Version)
{
    public static InstrumentSnapshot From(Instrument i, long version) => new(
        i.Id, i.Direction, i.Kind, i.Number, i.BankName, i.BankAccountId, i.ChequeBookId, i.PaymentId,
        i.Amount.Amount, i.Amount.Currency, i.Amount.RateToRial,
        i.IssueDate.Value, i.DueDate?.Value, i.Counterparty.Id, i.Counterparty.DisplayName, i.Purpose, i.Destination,
        i.State, i.OriginFiscalYear,
        i.History.Select(h => new HistorySnapshot(h.Action, h.From, h.To, h.Date.Value, h.UserId, h.Reference, h.Note)).ToList(),
        version);

    public Instrument ToDomain() => Instrument.Restore(
        Id, Direction, Kind, Number, BankName, BankAccountId, ChequeBookId, PaymentId,
        Currency == Money.Rial ? Money.Rials(Amount) : Money.Foreign(Amount, Currency, RateToRial ?? 0),
        new FiscalDate(IssueDate), DueDate is { } d ? new FiscalDate(d) : null,
        new CounterpartyRef(CounterpartyId, CounterpartyName), Purpose, Destination, State, OriginFiscalYear,
        History.Select(h => new InstrumentHistoryEntry(h.Action, h.From, h.To, new FiscalDate(h.Date), h.UserId, h.Reference, h.Note)),
        Version);
}

public sealed record HistorySnapshot(InstrumentAction Action, InstrumentState? From, InstrumentState To, DateOnly Date, string UserId, string? Reference, string? Note);

public sealed record ChequeBookSnapshot(
    Guid Id, Guid BankAccountId, string Serial, long FirstSheet, long LastSheet, bool IsActive, IReadOnlyDictionary<long, SheetUse> UsedSheets, long Version,
    Guid? FormatId = null, string? SayadMiddle = null, int? SayadFirstSerial = null, bool PartlyUsed = false)
{
    public static ChequeBookSnapshot From(ChequeBook b, long version) =>
        new(b.Id, b.BankAccountId, b.Serial, b.FirstSheet, b.LastSheet, b.IsActive, new Dictionary<long, SheetUse>(b.UsedSheets), version, b.FormatId,
            b.Sayad?.Middle, b.Sayad?.FirstSerial, b.PartlyUsed);

    public ChequeBook ToDomain() => ChequeBook.Restore(Id, BankAccountId, Serial, FirstSheet, LastSheet, IsActive, UsedSheets, Version, FormatId,
        SayadMiddle is { } m && SayadFirstSerial is { } s ? ChequeBookSayad.Restore(m, s) : null, PartlyUsed);
}

public sealed record ChequeFormatSnapshot(Guid Id, int Number, Guid BankAccountId, ChequePrintType PrintType, string? ImageType, bool IsActive, long Version)
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
