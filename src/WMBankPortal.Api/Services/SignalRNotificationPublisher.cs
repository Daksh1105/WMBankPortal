using Microsoft.AspNetCore.SignalR;
using WMBankPortal.Api.Hubs;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Notifications.DTOs;

namespace WMBankPortal.Api.Services;

/// <summary>
/// Implements IRealTimeNotificationPublisher by broadcasting over ASP.NET Core SignalR.
/// Decouples Application core from SignalR WebSockets machinery.
/// </summary>
public class SignalRNotificationPublisher : IRealTimeNotificationPublisher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    public SignalRNotificationPublisher(IHubContext<NotificationHub, INotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishNotificationAsync(Guid userId, NotificationDto notification, CancellationToken ct = default)
    {
        await _hubContext.Clients.User(userId.ToString()).ReceiveNotification(notification);
    }

    public async Task PublishApplicationStatusChangedAsync(
        Guid customerId,
        Guid applicationId,
        string newStatus,
        string remarks,
        CancellationToken ct = default)
    {
        await _hubContext.Clients.User(customerId.ToString())
            .ApplicationStatusChanged(applicationId, newStatus, remarks);
    }
}
