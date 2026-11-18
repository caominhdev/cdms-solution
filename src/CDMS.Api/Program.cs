using CDMS.Core.Interfaces;
using CDMS.Core.Models;
using CDMS.Core.Services;
using CDMS.Infrastructure;
using CDMS.Metrics;
using CDMS.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CDMS Management & Sync API",
        Version = "v1",
        Description = "Centralised Data Management System API for Multi-Tenant Inventory Sync (Vietful & Shopify)"
    });
});

// Infrastructure & persistence
builder.Services.AddCdmsInfrastructure(builder.Configuration);

// Domain services & validators
builder.Services.AddSingleton<ProductMappingService>();
builder.Services.AddSingleton<IDataValidator<VietfulProduct>, VietfulProductValidator>();
builder.Services.AddSingleton<IDataValidator<ShopifyProduct>, ShopifyProductValidator>();

// Metrics
builder.Services.AddSingleton<MetricsCollector>();
builder.Services.AddSingleton<PerformanceMonitor>();

// Worker registry
builder.Services.AddSingleton<WorkerRegistry>();

// Enable CORS for dashboard UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CDMS API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at application root
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
