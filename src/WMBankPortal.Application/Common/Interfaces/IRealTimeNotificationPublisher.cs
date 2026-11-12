using WMBankPortal.Application.Features.Notifications.DTOs;

namespace WMBankPortal.Application.Common.Interfaces;

/// <summary>
/// Abstraction for publishing real-time events over SignalR or WebSockets.
/// Implemented in Infrastructure/Api, keeping Application decoupled from SignalR packages.
/// </summary>
public interface IRealTimeNotificationPublisher
{
    Task PublishNotificationAsync(Guid userId, NotificationDto notification, CancellationToken ct = default);
    Task PublishApplicationStatusChangedAsync(Guid customerId, Guid applicationId, string newStatus, string remarks, CancellationToken ct = default);
}
