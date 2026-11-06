using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Auth.DTOs;

namespace WMBankPortal.Api.Controllers;

[EnableRateLimiting("auth-limiter")]
public sealed class AuthController : ApiControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        IAuthService _authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        ICurrentUserService currentUserService)
    {
        this._authService = _authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _changePasswordValidator = changePasswordValidator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Registers a new customer account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new Application.Common.Exceptions.ValidationException(errors);
        }

        var result = await _authService.RegisterAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Authenticates user credentials. Returns JWT access token in body and sets HttpOnly refresh cookie.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new Application.Common.Exceptions.ValidationException(errors);
        }

        var result = await _authService.LoginAsync(request, _currentUserService.IpAddress, cancellationToken);
        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        var (response, refreshToken) = result.Value;
        SetRefreshTokenCookie(refreshToken);

        return Ok(response);
    }

    /// <summary>
    /// Exchanges an HttpOnly refresh cookie for a new JWT access token and rotates the refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Missing Refresh Token",
                Detail = "Refresh token cookie was not found."
            });
        }

        var result = await _authService.RefreshTokenAsync(rawRefreshToken, _currentUserService.IpAddress, cancellationToken);
        if (result.IsFailure)
        {
            ClearRefreshTokenCookie();
            return HandleResult(result);
        }

        var tokenResult = result.Value;
        SetRefreshTokenCookie(tokenResult.NewRefreshToken);

        return Ok(new AuthResponse(
            tokenResult.AccessToken,
            tokenResult.ExpiresIn,
            tokenResult.User));
    }

    /// <summary>
    /// Revokes the current refresh token and clears the HttpOnly cookie.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            await _authService.RevokeTokenAsync(rawRefreshToken, _currentUserService.IpAddress, cancellationToken);
        }

        ClearRefreshTokenCookie();
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Changes the authenticated caller's password.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new Application.Common.Exceptions.ValidationException(errors);
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _authService.ChangePasswordAsync(_currentUserService.UserId.Value, request, cancellationToken);
        return HandleResult(result);
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps, // Enforced on HTTPS requests; allows local/testing HTTP harnesses
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });
    }
}
