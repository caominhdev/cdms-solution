using CDMS.Infrastructure.Messaging;
using VietfulMockService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Vietful Inventory Service (Mock API & Event Bus)",
        Version = "v1",
        Description = "Mocks Vietful Inventory API and simulates near real-time RabbitMQ push events"
    });
});

// RabbitMQ publisher for pushing mock events
builder.Services.AddSingleton<IMessagePublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
    var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
    var port = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var p) ? p : 5672;
    var user = builder.Configuration["RabbitMQ:Username"] ?? "guest";
    var pass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

    return new RabbitMqPublisher(host, port, user, pass, logger);
});

builder.Services.AddSingleton<VietfulMockDataStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vietful Mock API v1");
    c.RoutePrefix = string.Empty;
});

app.UseAuthorization();
app.MapControllers();

app.Run();
