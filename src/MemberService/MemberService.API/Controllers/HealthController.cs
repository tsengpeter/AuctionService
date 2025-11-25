using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MemberService.Infrastructure.Persistence;

namespace MemberService.API.Controllers;

/// <summary>
/// Health check endpoints for service monitoring and Kubernetes readiness probes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly MemberDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(MemberDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Liveness probe endpoint for Kubernetes
    /// Returns 200 OK if service is running
    /// </summary>
    /// <returns>
    /// 200 OK with status "healthy"
    /// </returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        _logger.LogDebug("Health check requested");

        return Ok(new
        {
            status = "healthy",
            service = "MemberService",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Readiness probe endpoint for Kubernetes
    /// Returns 200 OK only if database is accessible
    /// </summary>
    /// <returns>
    /// 200 OK if database is ready, 503 Service Unavailable if not accessible
    /// </returns>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReady()
    {
        _logger.LogDebug("Readiness check requested");

        try
        {
            // Test database connectivity by executing a simple query
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");

            _logger.LogDebug("Database readiness check passed");

            return Ok(new
            {
                status = "ready",
                service = "MemberService",
                database = "connected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database readiness check failed");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unavailable",
                service = "MemberService",
                database = "disconnected",
                error = "Database connection failed",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
