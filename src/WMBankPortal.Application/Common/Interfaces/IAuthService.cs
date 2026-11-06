using WMBankPortal.Application.Features.Auth.DTOs;
using WMBankPortal.Domain.Common;

namespace WMBankPortal.Application.Common.Interfaces;

/// <summary>
/// Core authentication and credential lifecycle orchestrator.
/// </summary>
public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<Result<(AuthResponse Response, string RefreshToken)>> LoginAsync(LoginRequest request, string? clientIp, CancellationToken cancellationToken = default);

    Task<Result<RefreshTokenResult>> RefreshTokenAsync(string rawRefreshToken, string? clientIp, CancellationToken cancellationToken = default);

    Task<Result> RevokeTokenAsync(string rawRefreshToken, string? clientIp, CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
