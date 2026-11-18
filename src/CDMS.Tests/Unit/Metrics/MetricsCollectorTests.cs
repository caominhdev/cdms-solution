using CDMS.Core.Domain.Enums;
using CDMS.Metrics;
using Xunit;

namespace CDMS.Tests.Unit.Metrics;

public class MetricsCollectorTests
{
    private readonly MetricsCollector _collector = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void RecordSuccess_UpdatesThroughputAndLatencyPercentiles()
    {
        // Record 100 sample latencies
        for (int i = 1; i <= 100; i++)
        {
            _collector.RecordSuccess(_tenantId, WorkerType.VietfulPush, i); // latencies 1ms to 100ms
        }

        var snapshot = _collector.GetSnapshot(_tenantId, WorkerType.VietfulPush);

        Assert.Equal(100, snapshot.TotalRecordsProcessed);
        Assert.Equal(0, snapshot.TotalRecordsFailed);
        Assert.Equal(0, snapshot.TotalRecordsInvalid);
        Assert.Equal(0.0, snapshot.ErrorRatePercent);
        Assert.True(snapshot.P50LatencyMs >= 45 && snapshot.P50LatencyMs <= 55);
        Assert.True(snapshot.P95LatencyMs >= 90 && snapshot.P95LatencyMs <= 100);
        Assert.True(snapshot.P99LatencyMs >= 95 && snapshot.P99LatencyMs <= 100);
        Assert.Equal("Healthy", snapshot.HealthStatus);
    }

    [Fact]
    public void RecordInvalidAndFailure_CalculatesErrorRateCorrectly()
    {
        for (int i = 0; i < 80; i++)
            _collector.RecordSuccess(_tenantId, WorkerType.ShopifyPull, 20);

        for (int i = 0; i < 15; i++)
            _collector.RecordInvalid(_tenantId, WorkerType.ShopifyPull, 5);

        for (int i = 0; i < 5; i++)
            _collector.RecordFailure(_tenantId, WorkerType.ShopifyPull, 50);

        var snapshot = _collector.GetSnapshot(_tenantId, WorkerType.ShopifyPull);

        Assert.Equal(80, snapshot.TotalRecordsProcessed);
        Assert.Equal(15, snapshot.TotalRecordsInvalid);
        Assert.Equal(5, snapshot.TotalRecordsFailed);
        Assert.Equal(20.0, snapshot.ErrorRatePercent); // (15+5)/100 = 20%
    }
}
