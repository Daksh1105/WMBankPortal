using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;
using Xunit;

namespace WMBankPortal.UnitTests;

public class NotificationEntityTests
{
    [Fact]
    public void Constructor_WithValidInputs_InitializesPropertiesCorrectly()
    {
        var userId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        var notification = new UserNotification(
            userId,
            "Document Approved",
            "Your proof of address has been verified.",
            NotificationType.Success,
            appId);

        Assert.Equal(userId, notification.UserId);
        Assert.Equal("Document Approved", notification.Title);
        Assert.Equal("Your proof of address has been verified.", notification.Message);
        Assert.Equal(NotificationType.Success, notification.Type);
        Assert.Equal(appId, notification.ApplicationId);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);
        Assert.True(notification.CreatedAtUtc <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankTitle_ThrowsArgumentException(string blankTitle)
    {
        Assert.Throws<ArgumentException>(() =>
            new UserNotification(Guid.NewGuid(), blankTitle, "Valid Message"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankMessage_ThrowsArgumentException(string blankMessage)
    {
        Assert.Throws<ArgumentException>(() =>
            new UserNotification(Guid.NewGuid(), "Valid Title", blankMessage));
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new UserNotification(Guid.Empty, "Valid Title", "Valid Message"));
    }

    [Fact]
    public void MarkAsRead_WhenUnread_SetsIsReadTrueAndStampsTimestamps()
    {
        var notification = new UserNotification(
            Guid.NewGuid(),
            "Action Required",
            "Please provide updated KYC documentation.",
            NotificationType.ActionRequired);

        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAtUtc);
        Assert.NotNull(notification.UpdatedAtUtc);
        Assert.True(notification.ReadAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsIdempotentAndPreservesOriginalTimestamp()
    {
        var notification = new UserNotification(
            Guid.NewGuid(),
            "Welcome",
            "Welcome to Wealth Management Portal.",
            NotificationType.Info);

        notification.MarkAsRead();
        var initialReadAt = notification.ReadAtUtc;

        // Second call
        notification.MarkAsRead();

        Assert.Equal(initialReadAt, notification.ReadAtUtc);
    }
}
