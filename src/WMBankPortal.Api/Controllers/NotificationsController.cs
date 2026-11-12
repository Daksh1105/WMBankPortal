using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMBankPortal.Application.Features.Notifications.DTOs;
using WMBankPortal.Application.Features.Notifications.Services;
using WMBankPortal.Domain.Common;

namespace WMBankPortal.Api.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetMyNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetMyNotificationsAsync(userId, pageNumber, pageSize, ct);
        return HandleResult(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadNotificationCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetUnreadCount(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetUnreadCountAsync(userId, ct);
        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        return Ok(new UnreadNotificationCountDto(result.Value));
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.MarkAsReadAsync(id, userId, ct);
        return HandleResult(result);
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> MarkAllAsRead(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.MarkAllAsReadAsync(userId, ct);
        return HandleResult(result);
    }
}
