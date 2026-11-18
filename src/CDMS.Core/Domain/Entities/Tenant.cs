using System.ComponentModel.DataAnnotations;

namespace CDMS.Core.Domain.Entities;

/// <summary>
/// Represents a tenant (merchant / store) in the CDMS multi-tenant platform.
/// </summary>
public sealed class Tenant
{
    /// <summary>Gets or sets the unique identifier of the tenant.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the human-readable name of the tenant.</summary>
    [Required, MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the external Shopify store domain (e.g. <c>my-store.myshopify.com</c>).
    /// </summary>
    [MaxLength(512)]
    public string? ShopifyStoreDomain { get; set; }

    /// <summary>Gets or sets the Shopify API access token for this tenant.</summary>
    [MaxLength(512)]
    public string? ShopifyAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the external Vietful tenant identifier used when calling the Vietful API.
    /// </summary>
    [MaxLength(128)]
    public string? VietfulTenantId { get; set; }

    /// <summary>Gets or sets the Vietful API access token for this tenant.</summary>
    [MaxLength(512)]
    public string? VietfulAccessToken { get; set; }

    /// <summary>Gets or sets a value indicating whether the tenant is currently active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the UTC timestamp when the tenant record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when the tenant record was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property: products that belong to this tenant.</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();

    /// <summary>Navigation property: worker configurations for this tenant.</summary>
    public ICollection<WorkerConfiguration> WorkerConfigurations { get; set; } = new List<WorkerConfiguration>();
}
