using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ShopifyMockService.Services;

namespace ShopifyMockService.Controllers;

[ApiController]
[Route("admin/api/{version}/graphql.json")]
public class GraphQLController : ControllerBase
{
    private readonly MockShopifyStore _store;
    private readonly ILogger<GraphQLController> _logger;

    public GraphQLController(MockShopifyStore store, ILogger<GraphQLController> logger)
    {
        _store = store;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult HandleGraphQL([FromBody] JsonElement body)
    {
        // 1. Simulate rate limiting (429) if enabled
        if (_store.SimulateRateLimit)
        {
            _logger.LogWarning("Shopify mock simulating rate limit 429 response.");
            Response.Headers.Append("Retry-After", "2");
            Response.Headers.Append("X-Shopify-Shop-Api-Call-Limit", "40/40");
            return StatusCode(429, new { errors = "Exceeded 40 calls per second limit. Please slow down." });
        }

        Response.Headers.Append("X-Shopify-Shop-Api-Call-Limit", "5/40");

        // 2. Parse variables
        int first = 10;
        string? after = null;

        if (body.TryGetProperty("variables", out var variables))
        {
            if (variables.TryGetProperty("first", out var firstProp) && firstProp.TryGetInt32(out var f))
            {
                first = f;
            }

            if (variables.TryGetProperty("after", out var afterProp) && afterProp.ValueKind == JsonValueKind.String)
            {
                after = afterProp.GetString();
            }
        }

        var (items, hasNextPage, endCursor) = _store.GetPage(after, first);

        var response = new
        {
            data = new
            {
                products = new
                {
                    pageInfo = new
                    {
                        hasNextPage,
                        endCursor
                    },
                    nodes = items
                }
            }
        };

        return Ok(response);
    }
}
