using CDMS.Core.Models;
using Microsoft.AspNetCore.Mvc;
using VietfulMockService.Services;

namespace VietfulMockService.Controllers;

[ApiController]
[Route("api/v1")]
public class WebhookController : ControllerBase
{
    private readonly VietfulMockDataStore _dataStore;

    public WebhookController(VietfulMockDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpPost("webhook/simulate")]
    public async Task<IActionResult> SimulatePush(
        [FromBody] VietfulProduct product,
        [FromQuery] string tenantId = "a1b2c3d4-0001-0000-0000-000000000001")
    {
        var envelope = await _dataStore.SimulatePushAsync(product, tenantId);
        return Ok(new
        {
            message = "Product event pushed to RabbitMQ successfully.",
            envelope
        });
    }

    [HttpGet("scenarios")]
    public IActionResult ListScenarios()
    {
        return Ok(new
        {
            availableScenarios = new[]
            {
                new { name = "valid_normal", description = "Standard valid product with all attributes, barcodes and manufacturer info." },
                new { name = "missing_sku", description = "Product missing SKU field. Expectation: rejected and logged to DataQualityLogs." },
                new { name = "negative_price", description = "Product with price = -50000 VND. Expectation: rejected." },
                new { name = "invalid_barcode", description = "Malformed barcode string. Expectation: rejected." },
                new { name = "null_name", description = "Empty product title. Expectation: rejected." },
                new { name = "overflow_quantity", description = "Negative stock quantity. Expectation: rejected." },
                new { name = "empty_payload", description = "Empty JSON payload {}. Expectation: rejected." }
            }
        });
    }

    [HttpPost("scenarios/trigger/{scenarioName}")]
    public async Task<IActionResult> TriggerScenario(
        string scenarioName,
        [FromQuery] string tenantId = "a1b2c3d4-0001-0000-0000-000000000001")
    {
        var envelope = await _dataStore.TriggerScenarioAsync(scenarioName, tenantId);
        return Ok(new
        {
            scenario = scenarioName,
            tenantId,
            message = $"Pushed scenario '{scenarioName}' to RabbitMQ message bus.",
            envelope
        });
    }
}
