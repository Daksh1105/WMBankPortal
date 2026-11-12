using WMBankPortal.Application.Features.Notifications.DTOs;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Enums;

namespace WMBankPortal.Application.Features.Notifications.Services;

public interface INotificationService
{
    Task<Result<NotificationDto>> CreateAndSendAsync(
        Guid userId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        Guid? applicationId = null,
        CancellationToken ct = default);

    Task<Result<PagedResult<NotificationDto>>> GetMyNotificationsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 15,
        CancellationToken ct = default);

    Task<Result<int>> GetUnreadCountAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<Result> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct = default);

    Task<Result> MarkAllAsReadAsync(
        Guid userId,
        CancellationToken ct = default);
}
