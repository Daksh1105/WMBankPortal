using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMBankPortal.Application.Features.Applications.DTOs;
using WMBankPortal.Application.Features.Applications.Services;

namespace WMBankPortal.Api.Controllers;

[Authorize(Roles = "Customer")]
[Route("api/v1/[controller]")]
public class CustomerController : ApiControllerBase
{
    private readonly IApplicationService _applicationService;

    public CustomerController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(CustomerDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetDashboard(CancellationToken ct)
    {
        var customerId = GetCurrentUserId();
        var result = await _applicationService.GetCustomerDashboardAsync(customerId, ct);
        return HandleResult(result);
    }
}
