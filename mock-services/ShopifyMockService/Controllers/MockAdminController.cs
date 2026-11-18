using Microsoft.AspNetCore.Mvc;
using ShopifyMockService.Services;

namespace ShopifyMockService.Controllers;

[ApiController]
[Route("admin/api/mock")]
public class MockAdminController : ControllerBase
{
    private readonly MockShopifyStore _store;

    public MockAdminController(MockShopifyStore store)
    {
        _store = store;
    }

    [HttpGet("scenarios")]
    public IActionResult ListScenarios()
    {
        return Ok(new
        {
            availableScenarios = new[]
            {
                new { name = "empty_title", description = "Shopify product with missing title. CDMS should reject with MISSING_TITLE." },
                new { name = "negative_price", description = "Product variant with price = -25.00. CDMS should reject with NEGATIVE_PRICE." },
                new { name = "invalid_status", description = "Product with unrecognized status code. CDMS should reject with INVALID_STATUS." }
            },
            rateLimitingEnabled = _store.SimulateRateLimit
        });
    }

    [HttpPost("rate-limit")]
    public IActionResult SetRateLimit([FromQuery] bool enabled)
    {
        _store.SimulateRateLimit = enabled;
        return Ok(new { message = $"Simulated rate limiting set to {enabled}." });
    }

    [HttpPost("inject-bad-data")]
    public IActionResult InjectBadData([FromQuery] string scenario)
    {
        _store.InjectBadData(scenario);
        return Ok(new { message = $"Injected faulty product for scenario '{scenario}'." });
    }
}
