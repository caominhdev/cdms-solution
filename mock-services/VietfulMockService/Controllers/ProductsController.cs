using CDMS.Core.Models;
using Microsoft.AspNetCore.Mvc;
using VietfulMockService.Services;

namespace VietfulMockService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly VietfulMockDataStore _dataStore;

    public ProductsController(VietfulMockDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpGet]
    public IActionResult ListProducts([FromQuery] int page = 1, [FromQuery] int limit = 50)
    {
        var items = _dataStore.ListProducts(page, limit);
        return Ok(new
        {
            page,
            limit,
            data = items
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetProduct(string id)
    {
        var p = _dataStore.GetById(id);
        if (p == null) return NotFound(new { error = $"Product {id} not found." });
        return Ok(p);
    }

    [HttpPost]
    public IActionResult CreateProduct([FromBody] VietfulProduct product)
    {
        if (string.IsNullOrEmpty(product.Id))
        {
            product.Id = $"VF-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        }
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        _dataStore.AddOrUpdate(product);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
}
