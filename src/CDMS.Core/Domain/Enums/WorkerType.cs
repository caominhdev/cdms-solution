namespace CDMS.Core.Domain.Enums;

/// <summary>
/// Identifies the type of background sync worker.
/// </summary>
public enum WorkerType
{
    /// <summary>
    /// A worker that listens for product update events pushed by the Vietful platform
    /// and writes them into CDMS for downstream distribution.
    /// </summary>
    VietfulPush = 1,

    /// <summary>
    /// A worker that periodically polls a tenant's Shopify store via GraphQL,
    /// pulling product and inventory changes into CDMS.
    /// </summary>
    ShopifyPull = 2,
}
