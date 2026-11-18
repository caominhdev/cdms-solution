using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CDMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IProductBaseRepository _productBaseRepository;

    public ProductsController(
        IProductRepository productRepository,
        IProductBaseRepository productBaseRepository)
    {
        _productRepository = productRepository;
        _productBaseRepository = productBaseRepository;
    }

    [HttpGet]
    public async Task<IActionResult> ListProducts(
        [FromQuery] Guid tenantId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        if (tenantId == Guid.Empty)
            return BadRequest(new { error = "tenantId query parameter is required." });

        var products = await _productRepository.ListByTenantAsync(tenantId, skip, take);
        var total = await _productRepository.CountByTenantAsync(tenantId);

        return Ok(new
        {
            tenantId,
            total,
            skip,
            take,
            items = products
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound(new { error = $"Product {id} not found." });

        return Ok(product);
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> ListByStatus([FromRoute] SyncStatus status, [FromQuery] Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            return BadRequest(new { error = "tenantId query parameter is required." });

        var products = await _productRepository.ListByStatusAsync(tenantId, status);
        return Ok(products);
    }

    [HttpGet("base/{gtin}")]
    public async Task<IActionResult> GetBaseProduct(string gtin)
    {
        var baseProduct = await _productBaseRepository.GetByGtinAsync(gtin);
        if (baseProduct == null)
            return NotFound(new { error = $"Shared product base record with GTIN '{gtin}' not found." });

        return Ok(baseProduct);
    }
}
