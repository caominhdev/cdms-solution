using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CDMS.Core.Models;
using Microsoft.Extensions.Logging;

namespace CDMS.Connectors.Shopify;

public class ShopifyGraphQLClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShopifyGraphQLClient> _logger;

    public ShopifyGraphQLClient(HttpClient httpClient, ILogger<ShopifyGraphQLClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public static readonly string ProductsQuery = @"
query GetProducts($first: Int!, $after: String, $query: String) {
  products(first: $first, after: $after, query: $query) {
    pageInfo {
      hasNextPage
      endCursor
    }
    nodes {
      id
      title
      handle
      descriptionHtml
      productType
      vendor
      tags
      status
      createdAt
      updatedAt
      variants(first: 10) {
        nodes {
          id
          title
          sku
          price
          inventoryQuantity
          barcode
        }
      }
      images(first: 5) {
        nodes {
          id
          url
          altText
          width
          height
        }
      }
      metafields(first: 10) {
        nodes {
          id
          namespace
          key
          value
          type
        }
      }
    }
  }
}";

    public async Task<GraphQLResponse<ProductsData>?> QueryProductsAsync(
        string? cursor,
        int pageSize = 50,
        string? queryFilter = null,
        int maxRetries = 5,
        CancellationToken cancellationToken = default)
    {
        var variables = new Dictionary<string, object?>
        {
            { "first", pageSize },
            { "after", cursor },
            { "query", queryFilter }
        };

        var requestBody = new
        {
            query = ProductsQuery,
            variables
        };

        var json = JsonSerializer.Serialize(requestBody);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("", content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests) // 429
            {
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? Math.Pow(2, attempt);
                _logger.LogWarning("Shopify GraphQL rate limited (429). Retrying after {Seconds} seconds (attempt {Attempt}/{MaxRetries})",
                    retryAfter, attempt, maxRetries);

                await Task.Delay(TimeSpan.FromSeconds(retryAfter), cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shopify GraphQL returned error {Status}: {Body}", response.StatusCode, errorText);
                throw new HttpRequestException($"Shopify GraphQL returned {(int)response.StatusCode}: {errorText}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<GraphQLResponse<ProductsData>>(responseJson);
        }

        throw new TimeoutException("Exceeded maximum retries for Shopify GraphQL requests due to rate limiting.");
    }
}

public class GraphQLResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<GraphQLError>? Errors { get; set; }
}

public class GraphQLError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class ProductsData
{
    [JsonPropertyName("products")]
    public ShopifyProductConnection Products { get; set; } = new();
}

public class ShopifyProductConnection
{
    [JsonPropertyName("pageInfo")]
    public ShopifyPageInfo PageInfo { get; set; } = new();

    [JsonPropertyName("nodes")]
    public List<ShopifyProduct> Nodes { get; set; } = new();
}

public class ShopifyPageInfo
{
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("endCursor")]
    public string? EndCursor { get; set; }
}
