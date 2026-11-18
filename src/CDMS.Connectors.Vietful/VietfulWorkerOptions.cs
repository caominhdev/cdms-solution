namespace CDMS.Connectors.Vietful;

public class VietfulWorkerOptions
{
    public Guid TenantId { get; set; }
    public string WorkerId { get; set; } = string.Empty;
    public string QueueName { get; set; } = "vietful.inventory.updates";
    public string ExchangeName { get; set; } = "vietful.events";
    public string RoutingKey { get; set; } = "tenant.*.inventory.#";
    public int CheckpointIntervalRecords { get; set; } = 50;
    public int MetricsIntervalSeconds { get; set; } = 60;
}
