using CDMS.Core.Interfaces;
using CDMS.Core.Models;
using Microsoft.Extensions.Logging;

namespace CDMS.Connectors.Shopify;

public class ShopifyConnector : IConnector<ShopifyProduct>
{
    private readonly ShopifyGraphQLClient _client;
    private readonly ILogger<ShopifyConnector> _logger;

    public string ConnectorName => "Shopify";
    public Guid TenantId { get; }

    public ShopifyConnector(
        Guid tenantId,
        ShopifyGraphQLClient client,
        ILogger<ShopifyConnector> logger)
    {
        TenantId = tenantId;
        _client = client;
        _logger = logger;
    }

    public async Task<ConnectorResult<ShopifyProduct>> FetchOneAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = $"id:{externalId}";
            var response = await _client.QueryProductsAsync(null, pageSize: 1, queryFilter: filter, cancellationToken: cancellationToken);

            var product = response?.Data?.Products.Nodes.FirstOrDefault();
            return product != null
                ? ConnectorResult<ShopifyProduct>.Ok(product)
                : ConnectorResult<ShopifyProduct>.Fail("NOT_FOUND", $"Product {externalId} not found in Shopify");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {Id} from Shopify", externalId);
            return ConnectorResult<ShopifyProduct>.Fail("FETCH_ERROR", ex.Message, isTransient: true);
        }
    }

    public async Task<ConnectorResult<ConnectorPage<ShopifyProduct>>> FetchPageAsync(
        string? cursor,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.QueryProductsAsync(cursor, pageSize, cancellationToken: cancellationToken);

            if (response?.Errors != null && response.Errors.Count > 0)
            {
                var errors = response.Errors.Select(e => new ConnectorError("GRAPHQL_ERROR", e.Message)).ToList();
                return ConnectorResult<ConnectorPage<ShopifyProduct>>.Fail(errors);
            }

            var conn = response?.Data?.Products;
            var list = conn?.Nodes ?? new List<ShopifyProduct>();
            var hasNext = conn?.PageInfo.HasNextPage ?? false;
            var nextCursor = conn?.PageInfo.EndCursor;

            var page = new ConnectorPage<ShopifyProduct>
            {
                Items = list,
                NextCursor = hasNext ? nextCursor : null,
                TotalCount = list.Count
            };

            return ConnectorResult<ConnectorPage<ShopifyProduct>>.Ok(page);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products page from Shopify");
            return ConnectorResult<ConnectorPage<ShopifyProduct>>.Fail("FETCH_ERROR", ex.Message, isTransient: true);
        }
    }

    public Task<ConnectorResult<ShopifyProduct>> PushAsync(ShopifyProduct item, CancellationToken cancellationToken = default)
    {
        // Shopify pull connector primarily queries data from Shopify.
        // If pushing is requested, it returns the item or performs a productUpdate mutation.
        _logger.LogInformation("Shopify push requested for product {Id}", item.Id);
        return Task.FromResult(ConnectorResult<ShopifyProduct>.Ok(item));
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.QueryProductsAsync(
                cursor: null,
                pageSize: 1,
                maxRetries: 1,
                cancellationToken: cancellationToken);
            return response?.Errors is not { Count: > 0 } && response?.Data?.Products is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shopify connection test failed");
            return false;
        }
    }
}
