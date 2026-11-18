using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces.Repositories;
using CDMS.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace CDMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly MetricsCollector _collector;
    private readonly PerformanceMonitor _monitor;
    private readonly ISyncMetricRepository _metricRepository;
    private readonly IDataQualityLogRepository _qualityLogRepository;

    public MetricsController(
        MetricsCollector collector,
        PerformanceMonitor monitor,
        ISyncMetricRepository metricRepository,
        IDataQualityLogRepository qualityLogRepository)
    {
        _collector = collector;
        _monitor = monitor;
        _metricRepository = metricRepository;
        _qualityLogRepository = qualityLogRepository;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var snapshots = _collector.GetAllSnapshots();
        return Ok(snapshots);
    }

    [HttpGet("tenant/{tenantId:guid}")]
    public IActionResult GetTenantMetrics(Guid tenantId, [FromQuery] WorkerType? workerType)
    {
        if (workerType.HasValue)
        {
            var snap = _collector.GetSnapshot(tenantId, workerType.Value);
            return Ok(snap);
        }

        var vietfulSnap = _collector.GetSnapshot(tenantId, WorkerType.VietfulPush);
        var shopifySnap = _collector.GetSnapshot(tenantId, WorkerType.ShopifyPull);

        return Ok(new
        {
            tenantId,
            vietful = vietfulSnap,
            shopify = shopifySnap
        });
    }

    [HttpGet("errors")]
    public async Task<IActionResult> GetErrors(
        [FromQuery] Guid tenantId,
        [FromQuery] WorkerType? workerType,
        [FromQuery] string? errorCode,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        if (tenantId == Guid.Empty)
            return BadRequest(new { error = "tenantId query parameter is required." });

        var errors = await _qualityLogRepository.ListAsync(tenantId, workerType, errorCode, skip, take);
        var counts = await _qualityLogRepository.CountUnresolvedByErrorCodeAsync(tenantId);

        return Ok(new
        {
            tenantId,
            unresolvedByErrorCode = counts,
            items = errors
        });
    }

    [HttpGet("report")]
    public IActionResult GetMarkdownReport()
    {
        var snapshots = _collector.GetAllSnapshots();
        var markdown = MetricsReport.GenerateMarkdownReport(snapshots);
        return Content(markdown, "text/markdown");
    }

    [HttpGet("health")]
    public IActionResult CheckHealth()
    {
        var alerts = _monitor.CheckHealth();
        return Ok(new
        {
            status = alerts.Any(a => a.Severity == Severity.Critical) ? "Critical" :
                     alerts.Any(a => a.Severity == Severity.Warning) ? "Warning" : "Healthy",
            activeAlerts = alerts
        });
    }
}
