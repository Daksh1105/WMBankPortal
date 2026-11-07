using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMBankPortal.Application.Features.Applications.DTOs;
using WMBankPortal.Application.Features.Applications.Services;
using WMBankPortal.Domain.Common;

namespace WMBankPortal.Api.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class ApplicationsController : ApiControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly IValidator<SubmitApplicationRequest> _validator;

    public ApplicationsController(
        IApplicationService applicationService,
        IValidator<SubmitApplicationRequest> validator)
    {
        _applicationService = applicationService;
        _validator = validator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BankingApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SubmitApplication(
        [FromBody] SubmitApplicationRequest request,
        CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors[0].ErrorMessage;
            return HandleResult(Result<BankingApplicationDto>.Failure(
                Error.Validation("Application.Validation", firstError)));
        }

        var customerId = GetCurrentUserId();
        var result = await _applicationService.SubmitApplicationAsync(customerId, request, ct);
        return HandleResult(result);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<BankingApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetMyApplications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var customerId = GetCurrentUserId();
        var result = await _applicationService.GetMyApplicationsAsync(customerId, pageNumber, pageSize, ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BankingApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetApplicationById(Guid id, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();
        var result = await _applicationService.GetApplicationByIdAsync(id, currentUserId, currentUserRole, ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/timeline")]
    [ProducesResponseType(typeof(IReadOnlyList<ApplicationTimelineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetApplicationTimeline(Guid id, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();
        var result = await _applicationService.GetApplicationTimelineAsync(id, currentUserId, currentUserRole, ct);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> WithdrawApplication(
        Guid id,
        [FromBody] WithdrawApplicationRequest request,
        CancellationToken ct)
    {
        var customerId = GetCurrentUserId();
        var result = await _applicationService.WithdrawApplicationAsync(id, customerId, request, ct);
        return HandleResult(result);
    }
}
