using CDMS.Api.Models;
using CDMS.Core.Interfaces.Repositories;
using CDMS.Workers;
using Microsoft.AspNetCore.Mvc;

namespace CDMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkersController : ControllerBase
{
    private readonly WorkerRegistry _registry;
    private readonly IWorkerCheckpointRepository _checkpointRepository;
    private readonly ILogger<WorkersController> _logger;

    public WorkersController(
        WorkerRegistry registry,
        IWorkerCheckpointRepository checkpointRepository,
        ILogger<WorkersController> logger)
    {
        _registry = registry;
        _checkpointRepository = checkpointRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListWorkers()
    {
        var workers = _registry.GetAllWorkers();
        var dtos = new List<WorkerSummaryDto>();

        foreach (var w in workers)
        {
            var cp = await _checkpointRepository.GetByWorkerIdAsync(w.WorkerId);
            dtos.Add(new WorkerSummaryDto
            {
                WorkerId = w.WorkerId,
                WorkerType = w.WorkerType,
                TenantId = w.TenantId,
                State = w.State.ToString(),
                LastProcessedAt = cp?.LastProcessedAt,
                LastCursor = cp?.LastCursor,
                LastMessageOffset = cp?.LastMessageOffset
            });
        }

        return Ok(dtos);
    }

    [HttpPost("{workerId}/start")]
    public async Task<IActionResult> StartWorker(string workerId)
    {
        var worker = _registry.GetWorker(workerId);
        if (worker == null)
            return NotFound(new { error = $"Worker '{workerId}' not found in active registry." });

        _logger.LogInformation("API requested start for worker {WorkerId}", workerId);
        await _registry.StartWorkerAsync(workerId);
        var cp = await _checkpointRepository.GetByWorkerIdAsync(workerId);

        return Ok(new
        {
            workerId,
            state = worker.State.ToString(),
            message = "Worker started successfully.",
            resumedFromCheckpoint = cp != null,
            checkpoint = cp
        });
    }

    [HttpPost("{workerId}/stop")]
    public async Task<IActionResult> StopWorker(string workerId)
    {
        var worker = _registry.GetWorker(workerId);
        if (worker == null)
            return NotFound(new { error = $"Worker '{workerId}' not found in active registry." });

        _logger.LogInformation("API requested stop for worker {WorkerId}", workerId);
        await _registry.StopWorkerAsync(workerId);
        var cp = await _checkpointRepository.GetByWorkerIdAsync(workerId);

        return Ok(new
        {
            workerId,
            state = worker.State.ToString(),
            message = "Worker stopped gracefully. Checkpoint persisted.",
            savedCheckpoint = cp
        });
    }

    [HttpGet("{workerId}/checkpoint")]
    public async Task<IActionResult> GetCheckpoint(string workerId)
    {
        var cp = await _checkpointRepository.GetByWorkerIdAsync(workerId);
        if (cp == null)
            return NotFound(new { error = $"No checkpoint found for worker '{workerId}'." });

        return Ok(cp);
    }

    [HttpPost("{workerId}/reset")]
    public async Task<IActionResult> ResetCheckpoint(string workerId)
    {
        _logger.LogInformation("Resetting checkpoint for worker {WorkerId}", workerId);
        await _checkpointRepository.DeleteAsync(workerId);

        return Ok(new
        {
            workerId,
            message = "Checkpoint deleted. Worker will re-sync from the beginning on next start."
        });
    }
}
