namespace CDMS.Connectors.Shopify;

public class ShopifyWorkerOptions
{
    public Guid TenantId { get; set; }
    public string WorkerId { get; set; } = string.Empty;
    public string StoreDomain { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-01";
    public int PollingIntervalSeconds { get; set; } = 60;
    public int PageSize { get; set; } = 50;
    public int MaxRetriesOnRateLimit { get; set; } = 5;
    public int InitialBackoffSeconds { get; set; } = 2;
}
