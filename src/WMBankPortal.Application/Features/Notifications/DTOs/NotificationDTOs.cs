namespace WMBankPortal.Application.Features.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    Guid? ApplicationId,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public record UnreadNotificationCountDto(int Count);
