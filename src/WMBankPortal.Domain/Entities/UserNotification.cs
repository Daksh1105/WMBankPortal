using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Enums;

namespace WMBankPortal.Domain.Entities;

/// <summary>
/// Represents an in-app and real-time alert dispatched to a specific user.
/// Stored for persistence and broadcast via SignalR.
/// </summary>
public class UserNotification : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; } = NotificationType.Info;
    public Guid? ApplicationId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    private UserNotification() { } // For EF Core

    public UserNotification(
        Guid userId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        Guid? applicationId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Notification recipient user ID cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Notification title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Notification message content is required.", nameof(message));

        UserId = userId;
        Title = title.Trim();
        Message = message.Trim();
        Type = type;
        ApplicationId = applicationId;
        IsRead = false;
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAtUtc = DateTime.UtcNow;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
