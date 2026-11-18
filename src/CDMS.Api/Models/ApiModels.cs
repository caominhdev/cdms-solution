using CDMS.Core.Domain.Enums;

namespace CDMS.Api.Models;

public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ShopifyStoreDomain { get; set; }
    public string? ShopifyAccessToken { get; set; }
    public string? VietfulTenantId { get; set; }
    public string? VietfulAccessToken { get; set; }
}

public class UpdateTenantRequest
{
    public string? Name { get; set; }
    public string? ShopifyStoreDomain { get; set; }
    public string? ShopifyAccessToken { get; set; }
    public string? VietfulTenantId { get; set; }
    public string? VietfulAccessToken { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateWorkerConfigRequest
{
    public Guid TenantId { get; set; }
    public WorkerType WorkerType { get; set; }
    public string? WorkerName { get; set; }
    public int PollingIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
}

public class WorkerSummaryDto
{
    public string WorkerId { get; set; } = string.Empty;
    public WorkerType WorkerType { get; set; }
    public Guid TenantId { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime? LastProcessedAt { get; set; }
    public string? LastCursor { get; set; }
    public long? LastMessageOffset { get; set; }
}
