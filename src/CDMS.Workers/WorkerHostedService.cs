using CDMS.Connectors.Shopify;
using CDMS.Connectors.Vietful;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces;
using CDMS.Core.Interfaces.Repositories;
using CDMS.Core.Models;
using CDMS.Core.Services;
using CDMS.Infrastructure.Messaging;
using CDMS.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CDMS.Workers;

public class WorkerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerRegistry _registry;
    private readonly ILogger<WorkerHostedService> _logger;

    public WorkerHostedService(
        IServiceProvider serviceProvider,
        WorkerRegistry registry,
        ILogger<WorkerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CDMS Worker Hosted Service starting up...");

        using var scope = _serviceProvider.CreateScope();
        var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();

        try
        {
            var tenants = await tenantRepo.ListAllAsync(includeInactive: false, cancellationToken);
            _logger.LogInformation("Discovered {Count} active tenants.", tenants.Count);

            foreach (var tenant in tenants)
            {
                var configs = tenant.WorkerConfigurations.Where(c => c.IsEnabled).ToList();
                if (configs.Count == 0)
                {
                    // If no explicit configs, initialize defaults for Vietful and Shopify
                    configs = CreateDefaultConfigs(tenant.Id);
                }

                foreach (var config in configs)
                {
                    try
                    {
                        var worker = CreateWorker(scope.ServiceProvider, tenant, config);
                        if (worker != null)
                        {
                            _registry.Register(worker);
                            await worker.StartAsync(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to start worker {Name} for tenant {TenantId}", config.WorkerName, tenant.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tenant workers from database. System will run with manual worker activation.");
        }
    }

    private IWorker? CreateWorker(IServiceProvider sp, Core.Domain.Entities.Tenant tenant, Core.Domain.Entities.WorkerConfiguration config)
    {
        if (config.WorkerType == WorkerType.VietfulPush)
        {
            var options = new VietfulWorkerOptions
            {
                TenantId = tenant.Id,
                WorkerId = string.IsNullOrEmpty(config.WorkerName) ? $"vietful-{tenant.Id:N}" : config.WorkerName,
                QueueName = $"vietful.tenant.{tenant.Id:N}.queue",
                ExchangeName = "vietful.events",
                CheckpointIntervalRecords = config.BatchSize > 0 ? config.BatchSize : 50,
                MetricsIntervalSeconds = config.PollingIntervalSeconds > 0 ? config.PollingIntervalSeconds : 60
            };

            var consumer = sp.GetRequiredService<IMessageConsumer>();
            var productRepo = sp.GetRequiredService<IProductRepository>();
            var productBaseRepo = sp.GetRequiredService<IProductBaseRepository>();
            var qualityRepo = sp.GetRequiredService<IDataQualityLogRepository>();
            var validator = sp.GetRequiredService<IDataValidator<VietfulProduct>>();
            var mapper = sp.GetRequiredService<ProductMappingService>();
            var procLogger = sp.GetRequiredService<ILogger<VietfulMessageProcessor>>();
            var workerLogger = sp.GetRequiredService<ILogger<VietfulWorker>>();
            var checkpointRepo = sp.GetRequiredService<IWorkerCheckpointRepository>();
            var metricRepo = sp.GetRequiredService<ISyncMetricRepository>();

            var processor = new VietfulMessageProcessor(productRepo, productBaseRepo, qualityRepo, validator, mapper, procLogger);
            return new VietfulWorker(options, consumer, processor, checkpointRepo, metricRepo, workerLogger);
        }

        if (config.WorkerType == WorkerType.ShopifyPull)
        {
            var options = new ShopifyWorkerOptions
            {
                TenantId = tenant.Id,
                WorkerId = string.IsNullOrEmpty(config.WorkerName) ? $"shopify-{tenant.Id:N}" : config.WorkerName,
                StoreDomain = tenant.ShopifyStoreDomain ?? "mock.myshopify.com",
                AccessToken = tenant.ShopifyAccessToken ?? "mock_token",
                PollingIntervalSeconds = config.PollingIntervalSeconds > 0 ? config.PollingIntervalSeconds : 60,
                PageSize = config.BatchSize > 0 ? config.BatchSize : 50
            };

            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ShopifyClient");
            var gqlLogger = sp.GetRequiredService<ILogger<ShopifyGraphQLClient>>();
            var connLogger = sp.GetRequiredService<ILogger<ShopifyConnector>>();
            var workerLogger = sp.GetRequiredService<ILogger<ShopifyWorker>>();

            var gqlClient = new ShopifyGraphQLClient(httpClient, gqlLogger);
            var connector = new ShopifyConnector(tenant.Id, gqlClient, connLogger);

            var productRepo = sp.GetRequiredService<IProductRepository>();
            var productBaseRepo = sp.GetRequiredService<IProductBaseRepository>();
            var checkpointRepo = sp.GetRequiredService<IWorkerCheckpointRepository>();
            var metricRepo = sp.GetRequiredService<ISyncMetricRepository>();
            var qualityRepo = sp.GetRequiredService<IDataQualityLogRepository>();
            var validator = sp.GetRequiredService<IDataValidator<ShopifyProduct>>();
            var mapper = sp.GetRequiredService<ProductMappingService>();

            return new ShopifyWorker(
                options, connector, productRepo, productBaseRepo, checkpointRepo,
                metricRepo, qualityRepo, validator, mapper, workerLogger);
        }

        return null;
    }

    private static List<Core.Domain.Entities.WorkerConfiguration> CreateDefaultConfigs(Guid tenantId)
    {
        return new List<Core.Domain.Entities.WorkerConfiguration>
        {
            new()
            {
                TenantId = tenantId,
                WorkerType = WorkerType.VietfulPush,
                WorkerName = $"vietful-{tenantId:N}",
                BatchSize = 50,
                PollingIntervalSeconds = 60,
                IsEnabled = true
            },
            new()
            {
                TenantId = tenantId,
                WorkerType = WorkerType.ShopifyPull,
                WorkerName = $"shopify-{tenantId:N}",
                BatchSize = 50,
                PollingIntervalSeconds = 60,
                IsEnabled = true
            }
        };
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping all active workers in CDMS registry...");
        var workers = _registry.GetAllWorkers();

        foreach (var worker in workers)
        {
            try
            {
                await worker.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping worker {WorkerId}", worker.WorkerId);
            }
        }

        _logger.LogInformation("All workers have been stopped.");
    }
}
