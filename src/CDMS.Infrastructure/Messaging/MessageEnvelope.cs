using System.Text.Json.Serialization;

namespace CDMS.Infrastructure.Messaging;

/// <summary>
/// Standard message envelope for near real-time data push events (e.g. from Vietful Inventory Service).
/// Encapsulates tenant routing context, event metadata, and raw JSON payload.
/// </summary>
public sealed class MessageEnvelope
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "inventory.product.updated";

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("sequenceNumber")]
    public long SequenceNumber { get; set; }

    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = "vietful";
}
