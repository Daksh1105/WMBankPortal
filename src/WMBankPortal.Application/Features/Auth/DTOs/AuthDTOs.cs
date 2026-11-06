namespace WMBankPortal.Application.Features.Auth.DTOs;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string ConfirmPassword);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record UserDto(
    Guid Id,
    string Email,
    string Role);

public sealed record AuthResponse(
    string AccessToken,
    int ExpiresIn,
    UserDto User);

public sealed record RefreshTokenResult(
    string AccessToken,
    string NewRefreshToken,
    int ExpiresIn,
    UserDto User);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
