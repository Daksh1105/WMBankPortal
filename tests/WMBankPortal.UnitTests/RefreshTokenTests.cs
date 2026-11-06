using WMBankPortal.Domain.Entities;
using Xunit;

namespace WMBankPortal.UnitTests;

public class RefreshTokenTests
{
    [Fact]
    public void RefreshToken_WhenCreated_ShouldBeActiveAndUnrevoked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var tokenHash = "dummy-sha256-hash-value";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var token = new RefreshToken(userId, tokenHash, familyId, expiresAt, "127.0.0.1");

        // Assert
        Assert.Equal(userId, token.UserId);
        Assert.Equal(familyId, token.FamilyId);
        Assert.Equal(tokenHash, token.TokenHash);
        Assert.True(token.IsActive);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsExpired);
        Assert.Null(token.RevokedAtUtc);
        Assert.Null(token.RevokedReason);
    }

    [Fact]
    public void RefreshToken_WhenRevoked_ShouldNotBeActive()
    {
        // Arrange
        var token = new RefreshToken(Guid.NewGuid(), "hash", Guid.NewGuid(), DateTime.UtcNow.AddDays(7), null);

        // Act
        token.Revoke("Rotated during token refresh", "new-replacement-hash");

        // Assert
        Assert.False(token.IsActive);
        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAtUtc);
        Assert.Equal("Rotated during token refresh", token.RevokedReason);
        Assert.Equal("new-replacement-hash", token.ReplacedByTokenHash);
    }

    [Fact]
    public void RefreshToken_WhenExpired_ShouldReportIsExpiredTrue()
    {
        // Arrange: Token expired 1 hour ago
        var expiredAt = DateTime.UtcNow.AddHours(-1);
        var token = new RefreshToken(Guid.NewGuid(), "hash", Guid.NewGuid(), expiredAt, null);

        // Assert
        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }
}
