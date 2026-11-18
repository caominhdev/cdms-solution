using ShopifyMockService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Shopify Admin GraphQL Mock Service",
        Version = "v1",
        Description = "Mocks Shopify Admin GraphQL API with cursor pagination, rate limiting and bad data scenarios"
    });
});

builder.Services.AddSingleton<MockShopifyStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shopify Mock API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();
app.MapControllers();

app.Run();
