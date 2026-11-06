namespace WMBankPortal.Application.Common.Interfaces;

/// <summary>
/// Cryptographic token generator and validator.
/// Handles JWT Access Tokens and cryptographically secure Refresh Tokens.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken();
    string HashToken(string token);
    int AccessTokenExpirationMinutes { get; }
}
