using CDMS.Core.Interfaces;
using CDMS.Core.Models;
using CDMS.Core.Services;
using CDMS.Infrastructure;
using CDMS.Metrics;
using CDMS.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        // Infrastructure (EF Core, CockroachDB / PostgreSQL, RabbitMQ)
        services.AddCdmsInfrastructure(config);

        // Core domain services
        services.AddSingleton<ProductMappingService>();
        services.AddSingleton<IDataValidator<VietfulProduct>, VietfulProductValidator>();
        services.AddSingleton<IDataValidator<ShopifyProduct>, ShopifyProductValidator>();

        // Metrics
        services.AddSingleton<MetricsCollector>();
        services.AddSingleton<PerformanceMonitor>();

        // HTTP Client for Shopify GraphQL API
        services.AddHttpClient("ShopifyClient", client =>
        {
            var shopifyUrl = config["Shopify:BaseUrl"] ?? "http://localhost:5200/admin/api/2024-01/graphql.json";
            client.BaseAddress = new Uri(shopifyUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Worker registry & hosted background service
        services.AddSingleton<WorkerRegistry>();
        services.AddHostedService<WorkerHostedService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

Console.WriteLine("==================================================================");
Console.WriteLine("  CDMS Background Worker Host (Multi-Tenant Sync Engine)");
Console.WriteLine("  Operating on CockroachDB / PostgreSQL + RabbitMQ Messaging");
Console.WriteLine("==================================================================");

await host.RunAsync();
