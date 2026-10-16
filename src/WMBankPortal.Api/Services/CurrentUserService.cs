using System.Security.Claims;
using WMBankPortal.Application.Common.Interfaces;

namespace WMBankPortal.Api.Services;

/// <summary>
/// Extracts user context, roles, and network parameters from the active HttpContext.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

    public Guid? UserId
    {
        get
        {
            var idClaim = HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? HttpContext?.User.FindFirst("sub")?.Value;

            return Guid.TryParse(idClaim, out var guid) ? guid : null;
        }
    }

    public string? Email =>
        HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;

    public string? Role =>
        HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAuthenticated =>
        HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public string? IpAddress =>
        HttpContext?.Connection.RemoteIpAddress?.ToString()
        ?? HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();

    public string? UserAgent =>
        HttpContext?.Request.Headers.UserAgent.ToString();
}
