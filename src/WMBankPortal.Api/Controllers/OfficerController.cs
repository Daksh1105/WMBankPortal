using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMBankPortal.Application.Features.Underwriting.DTOs;
using WMBankPortal.Application.Features.Underwriting.Services;

namespace WMBankPortal.Api.Controllers;

[Authorize(Roles = "Officer,Admin")]
[Route("api/v1/[controller]")]
public class OfficerController : ApiControllerBase
{
    private readonly IOfficerService _officerService;

    public OfficerController(IOfficerService officerService)
    {
        _officerService = officerService;
    }

    [HttpPost("{id:guid}/claim")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Claim([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _officerService.ClaimApplicationAsync(id, GetCurrentUserId(), ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Approve([FromRoute] Guid id, [FromBody] OfficerActionRequest request, CancellationToken ct)
    {
        var result = await _officerService.ApproveApplicationAsync(id, GetCurrentUserId(), request?.Remarks ?? string.Empty, ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Reject([FromRoute] Guid id, [FromBody] OfficerActionRequest request, CancellationToken ct)
    {
        var result = await _officerService.RejectApplicationAsync(id, GetCurrentUserId(), request?.Remarks ?? string.Empty, ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/request-kyc")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> RequestKyc([FromRoute] Guid id, [FromBody] OfficerActionRequest request, CancellationToken ct)
    {
        var result = await _officerService.RequestKycAsync(id, GetCurrentUserId(), request?.Remarks ?? string.Empty, ct);
        return HandleResult(result);
    }
}