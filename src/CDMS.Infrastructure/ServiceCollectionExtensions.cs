using CDMS.Core.Interfaces.Repositories;
using CDMS.Infrastructure.Messaging;
using CDMS.Infrastructure.Persistence;
using CDMS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CDMS.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCdmsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Context (PostgreSQL / CockroachDB compatible)
        var connectionString = configuration.GetConnectionString("CockroachDb")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=26257;Database=cdms;Username=root;Password=;SSL Mode=Disable;";

        services.AddDbContext<CdmsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
        });

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductBaseRepository, ProductBaseRepository>();
        services.AddScoped<IWorkerCheckpointRepository, WorkerCheckpointRepository>();
        services.AddScoped<ISyncMetricRepository, SyncMetricRepository>();
        services.AddScoped<IDataQualityLogRepository, DataQualityLogRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();

        // Messaging - RabbitMQ
        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
            var host = configuration["RabbitMQ:Host"] ?? "localhost";
            var port = int.TryParse(configuration["RabbitMQ:Port"], out var p) ? p : 5672;
            var user = configuration["RabbitMQ:Username"] ?? "guest";
            var pass = configuration["RabbitMQ:Password"] ?? "guest";

            return new RabbitMqPublisher(host, port, user, pass, logger);
        });

        services.AddTransient<IMessageConsumer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMqConsumer>>();
            var host = configuration["RabbitMQ:Host"] ?? "localhost";
            var port = int.TryParse(configuration["RabbitMQ:Port"], out var p) ? p : 5672;
            var user = configuration["RabbitMQ:Username"] ?? "guest";
            var pass = configuration["RabbitMQ:Password"] ?? "guest";

            return new RabbitMqConsumer(host, port, user, pass, logger);
        });

        return services;
    }
}
