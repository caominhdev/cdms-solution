using CDMS.Connectors.Shopify;
using CDMS.Connectors.Vietful;
using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces;
using CDMS.Core.Interfaces.Repositories;
using CDMS.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CDMS.Tests.Unit.Workers;

public class WorkerCheckpointTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task VietfulWorker_ResumesFromPersistedCheckpoint()
    {
        var workerId = $"vietful-{_tenantId:N}";
        var mockCheckpointRepo = new Mock<IWorkerCheckpointRepository>();
        var mockMetricRepo = new Mock<ISyncMetricRepository>();
        var mockConsumer = new Mock<IMessageConsumer>();
        var mockLogger = new Mock<ILogger<VietfulWorker>>();

        var existingCheckpoint = new WorkerCheckpoint
        {
            WorkerId = workerId,
            TenantId = _tenantId,
            WorkerType = WorkerType.VietfulPush,
            LastMessageOffset = 1500,
            LastProcessedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        mockCheckpointRepo.Setup(r => r.GetByWorkerIdAsync(workerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCheckpoint);

        var options = new VietfulWorkerOptions
        {
            TenantId = _tenantId,
            WorkerId = workerId
        };

        // Note: Passing null processor as we only test checkpoint loading in StartAsync
        var worker = new VietfulWorker(
            options,
            mockConsumer.Object,
            null!,
            mockCheckpointRepo.Object,
            mockMetricRepo.Object,
            mockLogger.Object);

        await worker.StartAsync();

        Assert.Equal(WorkerState.Running, worker.State);
        mockCheckpointRepo.Verify(r => r.GetByWorkerIdAsync(workerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WorkerRegistry_ManagesWorkerLifecycle()
    {
        var mockLogger = new Mock<ILogger<CDMS.Workers.WorkerRegistry>>();
        var registry = new CDMS.Workers.WorkerRegistry(mockLogger.Object);

        var mockWorker = new Mock<CDMS.Core.Interfaces.IWorker>();
        mockWorker.Setup(w => w.WorkerId).Returns("test-worker-1");
        mockWorker.Setup(w => w.TenantId).Returns(_tenantId);
        mockWorker.Setup(w => w.WorkerType).Returns(WorkerType.VietfulPush);
        mockWorker.Setup(w => w.State).Returns(WorkerState.Idle);

        registry.Register(mockWorker.Object);

        Assert.NotNull(registry.GetWorker("test-worker-1"));
        Assert.Single(registry.GetWorkersByTenant(_tenantId));

        await registry.StartWorkerAsync("test-worker-1");
        mockWorker.Verify(w => w.StartAsync(It.IsAny<CancellationToken>()), Times.Once);

        await registry.StopWorkerAsync("test-worker-1");
        mockWorker.Verify(w => w.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
