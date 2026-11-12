using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WMBankPortal.Application.Features.Notifications.DTOs;

namespace WMBankPortal.Api.Hubs;

/// <summary>
/// Strongly-typed contract for client methods invoked from the server.
/// </summary>
public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);
    Task ApplicationStatusChanged(Guid applicationId, string status, string comments);
}

/// <summary>
/// SignalR Hub for real-time customer and underwriting notification pushes.
/// Authenticated via JWT bearer token extracted from query string.
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
