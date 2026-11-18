using CDMS.Api.Models;
using CDMS.Core.Domain.Entities;
using CDMS.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CDMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;

    public TenantsController(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    [HttpGet]
    public async Task<IActionResult> ListTenants([FromQuery] bool includeInactive = false)
    {
        var tenants = await _tenantRepository.ListAllAsync(includeInactive);
        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null) return NotFound(new { error = $"Tenant {id} not found." });
        return Ok(tenant);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        var tenant = new Tenant
        {
            Name = request.Name,
            ShopifyStoreDomain = request.ShopifyStoreDomain,
            ShopifyAccessToken = request.ShopifyAccessToken,
            VietfulTenantId = request.VietfulTenantId,
            VietfulAccessToken = request.VietfulAccessToken,
            IsActive = true
        };

        await _tenantRepository.AddAsync(tenant);
        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null) return NotFound(new { error = $"Tenant {id} not found." });

        if (!string.IsNullOrEmpty(request.Name)) tenant.Name = request.Name;
        if (request.ShopifyStoreDomain != null) tenant.ShopifyStoreDomain = request.ShopifyStoreDomain;
        if (request.ShopifyAccessToken != null) tenant.ShopifyAccessToken = request.ShopifyAccessToken;
        if (request.VietfulTenantId != null) tenant.VietfulTenantId = request.VietfulTenantId;
        if (request.VietfulAccessToken != null) tenant.VietfulAccessToken = request.VietfulAccessToken;
        if (request.IsActive.HasValue) tenant.IsActive = request.IsActive.Value;

        await _tenantRepository.UpdateAsync(tenant);
        return Ok(tenant);
    }

    [HttpPost("{id:guid}/workers/config")]
    public async Task<IActionResult> AddWorkerConfig(Guid id, [FromBody] CreateWorkerConfigRequest request)
    {
        var config = new WorkerConfiguration
        {
            TenantId = id,
            WorkerType = request.WorkerType,
            WorkerName = request.WorkerName ?? $"{request.WorkerType.ToString().ToLower()}-{id:N}",
            PollingIntervalSeconds = request.PollingIntervalSeconds,
            BatchSize = request.BatchSize,
            IsEnabled = true
        };

        await _tenantRepository.AddWorkerConfigAsync(config);
        return Ok(config);
    }
}
