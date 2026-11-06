using WMBankPortal.Domain.Common;

namespace WMBankPortal.Domain.Entities;

/// <summary>
/// Represents a refresh token session for an authenticated user.
/// Stored as a cryptographically secure SHA-256 hash (never in plaintext).
/// Implements token family tracking to enable reuse detection (compromise detection).
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    private RefreshToken()
    {
        // Parameterless constructor required by EF Core
    }

    public RefreshToken(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTime expiresAtUtc,
        string? createdByIp)
    {
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public Guid FamilyId { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedReason { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>
    /// Revokes this refresh token and records the replacement token hash upon normal rotation.
    /// </summary>
    public void Revoke(string reason, string? replacedByHash = null)
    {
        RevokedAtUtc = DateTime.UtcNow;
        RevokedReason = reason;
        ReplacedByTokenHash = replacedByHash;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
