using System.Net.Http.Json;
using CDMS.Core.Interfaces;
using CDMS.Core.Models;
using Microsoft.Extensions.Logging;

namespace CDMS.Connectors.Vietful;

public class VietfulConnector : IConnector<VietfulProduct>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VietfulConnector> _logger;

    public string ConnectorName => "Vietful";
    public Guid TenantId { get; }

    public VietfulConnector(
        Guid tenantId,
        HttpClient httpClient,
        ILogger<VietfulConnector> logger)
    {
        TenantId = tenantId;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ConnectorResult<VietfulProduct>> FetchOneAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/products/{externalId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return ConnectorResult<VietfulProduct>.Fail(
                    $"HTTP_{(int)response.StatusCode}",
                    $"Vietful API returned {(int)response.StatusCode}: {error}",
                    (int)response.StatusCode >= 500);
            }

            var product = await response.Content.ReadFromJsonAsync<VietfulProduct>(cancellationToken: cancellationToken);
            return product != null
                ? ConnectorResult<VietfulProduct>.Ok(product)
                : ConnectorResult<VietfulProduct>.Fail("EMPTY_BODY", "Received empty response from Vietful API");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching product {Id} from Vietful", externalId);
            return ConnectorResult<VietfulProduct>.Fail("NETWORK_ERROR", ex.Message, isTransient: true);
        }
    }

    public async Task<ConnectorResult<ConnectorPage<VietfulProduct>>> FetchPageAsync(
        string? cursor,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/v1/products?limit={pageSize}";
            if (!string.IsNullOrEmpty(cursor))
            {
                url += $"&page={cursor}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ConnectorResult<ConnectorPage<VietfulProduct>>.Fail(
                    $"HTTP_{(int)response.StatusCode}",
                    $"Vietful API error {(int)response.StatusCode}",
                    (int)response.StatusCode >= 500);
            }

            var list = await response.Content.ReadFromJsonAsync<List<VietfulProduct>>(cancellationToken: cancellationToken)
                       ?? new List<VietfulProduct>();

            int currentPage = int.TryParse(cursor, out var p) ? p : 1;
            string? nextCursor = list.Count == pageSize ? (currentPage + 1).ToString() : null;

            var page = new ConnectorPage<VietfulProduct>
            {
                Items = list,
                NextCursor = nextCursor,
                TotalCount = list.Count
            };

            return ConnectorResult<ConnectorPage<VietfulProduct>>.Ok(page);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching page from Vietful");
            return ConnectorResult<ConnectorPage<VietfulProduct>>.Fail("FETCH_ERROR", ex.Message, isTransient: true);
        }
    }

    public async Task<ConnectorResult<VietfulProduct>> PushAsync(VietfulProduct item, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/products", item, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return ConnectorResult<VietfulProduct>.Fail(
                    $"HTTP_{(int)response.StatusCode}",
                    $"Push failed: {error}",
                    (int)response.StatusCode >= 500);
            }

            var created = await response.Content.ReadFromJsonAsync<VietfulProduct>(cancellationToken: cancellationToken);
            return created != null
                ? ConnectorResult<VietfulProduct>.Ok(created)
                : ConnectorResult<VietfulProduct>.Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing product {Sku} to Vietful", item.Sku);
            return ConnectorResult<VietfulProduct>.Fail("PUSH_ERROR", ex.Message, isTransient: true);
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/api/v1/products?limit=1", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Vietful connection test failed");
            return false;
        }
    }
}
