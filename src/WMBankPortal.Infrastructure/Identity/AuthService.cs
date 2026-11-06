using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Auth.DTOs;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;
using WMBankPortal.Infrastructure.Persistence;

namespace WMBankPortal.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly AppDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtTokenService jwtTokenService,
        AppDbContext dbContext,
        IOptions<JwtSettings> jwtOptions,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _dbContext = dbContext;
        _jwtSettings = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result.Failure<AuthResponse>(
                Error.Conflict("AUTH_EMAIL_EXISTS", "A user with this email address already exists."));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true // Email confirmation mocked for seamless local dev
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var firstError = createResult.Errors.FirstOrDefault()?.Description ?? "Registration failed.";
            return Result.Failure<AuthResponse>(Error.Validation("AUTH_REGISTRATION_FAILED", firstError));
        }

        // Ensure default role exists and assign Customer role
        string defaultRole = UserRole.Customer.ToString();
        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            await _roleManager.CreateAsync(new ApplicationRole(defaultRole));
        }

        await _userManager.AddToRoleAsync(user, defaultRole);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, defaultRole);
        var userDto = new UserDto(user.Id, user.Email, defaultRole);

        _logger.LogInformation("New user registered successfully with ID: {UserId}", user.Id);

        return Result.Success(new AuthResponse(
            accessToken,
            _jwtTokenService.AccessTokenExpirationMinutes * 60,
            userDto));
    }

    public async Task<Result<(AuthResponse Response, string RefreshToken)>> LoginAsync(
        LoginRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result.Failure<(AuthResponse, string)>(
                Error.Unauthorized("AUTH_INVALID_CREDENTIALS", "Invalid email or password."));
        }

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login attempt for locked out account: {Email}", request.Email);
            return Result.Failure<(AuthResponse, string)>(
                Error.Unauthorized("AUTH_ACCOUNT_LOCKED", "Account is locked due to multiple failed attempts. Please try again in 15 minutes."));
        }

        // Validate password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Account locked out after failed attempt: {Email}", request.Email);
                return Result.Failure<(AuthResponse, string)>(
                    Error.Unauthorized("AUTH_ACCOUNT_LOCKED", "Account is locked due to multiple failed attempts. Please try again in 15 minutes."));
            }

            return Result.Failure<(AuthResponse, string)>(
                Error.Unauthorized("AUTH_INVALID_CREDENTIALS", "Invalid email or password."));
        }

        // Reset failed attempt counter upon successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        // Fetch user roles
        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? UserRole.Customer.ToString();

        // Generate Access Token & Refresh Token
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole);
        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var tokenHash = _jwtTokenService.HashToken(rawRefreshToken);

        // Create new refresh token family
        var familyId = Guid.NewGuid();
        var expiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        var refreshTokenEntity = new RefreshToken(user.Id, tokenHash, familyId, expiresAtUtc, clientIp);
        _dbContext.RefreshTokens.Add(refreshTokenEntity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new AuthResponse(
            accessToken,
            _jwtTokenService.AccessTokenExpirationMinutes * 60,
            new UserDto(user.Id, user.Email!, primaryRole));

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        return Result.Success((response, rawRefreshToken));
    }

    public async Task<Result<RefreshTokenResult>> RefreshTokenAsync(
        string rawRefreshToken,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Result.Failure<RefreshTokenResult>(
                Error.Unauthorized("AUTH_EMPTY_REFRESH_TOKEN", "Refresh token was not provided."));
        }

        var incomingHash = _jwtTokenService.HashToken(rawRefreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == incomingHash, cancellationToken);

        if (existingToken == null)
        {
            _logger.LogWarning("Refresh token not found for hash: {Hash}", incomingHash);
            return Result.Failure<RefreshTokenResult>(
                Error.Unauthorized("AUTH_INVALID_REFRESH_TOKEN", "Invalid refresh token."));
        }

        // =========================================================================
        // REUSE DETECTION (Compromise Alert)
        // If an already-revoked token is presented, an attacker has stolen and replayed
        // an old token! Invalidate the ENTIRE token family immediately.
        // =========================================================================
        if (existingToken.IsRevoked)
        {
            _logger.LogCritical(
                "SECURITY ALERT: Refresh token reuse detected for User {UserId}, Family {FamilyId}! Revoking all tokens in family.",
                existingToken.UserId, existingToken.FamilyId);

            var familyTokens = await _dbContext.RefreshTokens
                .Where(t => t.FamilyId == existingToken.FamilyId && !t.RevokedAtUtc.HasValue)
                .ToListAsync(cancellationToken);

            foreach (var token in familyTokens)
            {
                token.Revoke("Security cascade revocation: Token reuse detected");
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Failure<RefreshTokenResult>(
                Error.Unauthorized("AUTH_TOKEN_REUSED", "Session compromised. All sessions have been terminated. Please log in again."));
        }

        if (existingToken.IsExpired)
        {
            return Result.Failure<RefreshTokenResult>(
                Error.Unauthorized("AUTH_REFRESH_TOKEN_EXPIRED", "Refresh token has expired. Please log in again."));
        }

        // =========================================================================
        // LEGITIMATE TOKEN ROTATION
        // =========================================================================
        var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user == null || user.IsDeactivated)
        {
            return Result.Failure<RefreshTokenResult>(
                Error.Unauthorized("AUTH_USER_DISABLED", "User account is no longer active."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? UserRole.Customer.ToString();

        // 1. Generate new tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole);
        var newRawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newTokenHash = _jwtTokenService.HashToken(newRawRefreshToken);

        // 2. Revoke old token and record replacement
        existingToken.Revoke("Rotated during token refresh", newTokenHash);

        // 3. Issue new token with SAME FamilyId
        var expiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var newRefreshTokenEntity = new RefreshToken(
            user.Id,
            newTokenHash,
            existingToken.FamilyId,
            expiresAtUtc,
            clientIp);

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token rotated successfully for User {UserId}", user.Id);

        return Result.Success(new RefreshTokenResult(
            newAccessToken,
            newRawRefreshToken,
            _jwtTokenService.AccessTokenExpirationMinutes * 60,
            new UserDto(user.Id, user.Email!, primaryRole)));
    }

    public async Task<Result> RevokeTokenAsync(
        string rawRefreshToken,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Result.Success();
        }

        var tokenHash = _jwtTokenService.HashToken(rawRefreshToken);
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token != null && token.IsActive)
        {
            token.Revoke("Revoked on explicit user logout");
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Refresh token revoked for User {UserId}", token.UserId);
        }

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure(Error.NotFound("USER_NOT_FOUND", "User not found."));
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ?? "Failed to change password.";
            return Result.Failure(Error.Validation("AUTH_PASSWORD_CHANGE_FAILED", firstError));
        }

        _logger.LogInformation("Password changed successfully for User {UserId}", userId);
        return Result.Success();
    }
}
