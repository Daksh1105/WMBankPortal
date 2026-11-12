using Microsoft.EntityFrameworkCore;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Notifications.DTOs;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;

namespace WMBankPortal.Application.Features.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _context;
    private readonly IRealTimeNotificationPublisher _publisher;

    public NotificationService(
        IAppDbContext context,
        IRealTimeNotificationPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<Result<NotificationDto>> CreateAndSendAsync(
        Guid userId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        Guid? applicationId = null,
        CancellationToken ct = default)
    {
        var notification = new UserNotification(userId, title, message, type, applicationId);

        _context.UserNotifications.Add(notification);
        await _context.SaveChangesAsync(ct);

        var dto = MapToDto(notification);

        // Real-time SignalR dispatch (best-effort, non-blocking)
        try
        {
            await _publisher.PublishNotificationAsync(userId, dto, ct);
        }
        catch
        {
            // Transient push notification failures must not fail the underlying database commit
        }

        return Result<NotificationDto>.Success(dto);
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetMyNotificationsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 15,
        CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<NotificationDto>>.Success(
            new PagedResult<NotificationDto>(dtos, pageNumber, pageSize, totalCount));
    }

    public async Task<Result<int>> GetUnreadCountAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var count = await _context.UserNotifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

        return Result<int>.Success(count);
    }

    public async Task<Result> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct = default)
    {
        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, ct);

        if (notification == null)
        {
            return Result.Failure(
                Error.NotFound("Notification.NotFound", $"Notification with ID '{notificationId}' was not found."));
        }

        // IDOR protection: Users cannot mark other users' alerts as read
        if (notification.UserId != userId)
        {
            return Result.Failure(
                Error.Forbidden("Notification.Forbidden", "You do not have permission to modify this notification."));
        }

        notification.MarkAsRead();
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var unreadNotifications = await _context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unreadNotifications)
        {
            n.MarkAsRead();
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static NotificationDto MapToDto(UserNotification n) =>
        new(
            n.Id,
            n.Title,
            n.Message,
            n.Type.ToString(),
            n.ApplicationId,
            n.IsRead,
            n.CreatedAtUtc,
            n.ReadAtUtc);
}
