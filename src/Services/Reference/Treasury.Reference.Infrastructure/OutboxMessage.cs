using System.Text.Json;
using System.Text.Json.Serialization;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Reference.Infrastructure;

/// <summary>A domain event waiting in the outbox. Saved in the same transaction as the entry; published afterwards.</summary>
public sealed record OutboxMessage(Guid Id, string Type, string Payload, DateTimeOffset OccurredAt)
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    public static OutboxMessage From(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return new(domainEvent.EventId, domainEvent.GetType().Name, JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Json), domainEvent.OccurredAt);
    }
}
