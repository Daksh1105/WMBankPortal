using Microsoft.AspNetCore.Mvc;

namespace WMBankPortal.Api.Controllers;

/// <summary>
/// Provides baseline diagnostic and system metadata for monitoring.
/// </summary>
public sealed class SystemController : ApiControllerBase
{
    private readonly IHostEnvironment _environment;

    public SystemController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Returns basic system and environment status.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(SystemInfoResponse), StatusCodes.Status200OK)]
    public IActionResult GetSystemInfo()
    {
        return Ok(new SystemInfoResponse(
            ApplicationName: "WM Bank Portal API",
            Version: "v1.0.0-dev",
            Environment: _environment.EnvironmentName,
            ServerTimeUtc: DateTime.UtcNow,
            Status: "Online"
        ));
    }
}

public sealed record SystemInfoResponse(
    string ApplicationName,
    string Version,
    string Environment,
    DateTime ServerTimeUtc,
    string Status);
