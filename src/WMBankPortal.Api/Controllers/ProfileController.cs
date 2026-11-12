using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMBankPortal.Application.Features.Profile.DTOs;
using WMBankPortal.Application.Features.Profile.Services;
using WMBankPortal.Domain.Common;

namespace WMBankPortal.Api.Controllers;

[Authorize(Roles = "Customer")]
[Route("api/v1/[controller]")]
public class ProfileController : ApiControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IValidator<UpdateProfileRequest> _validator;

    public ProfileController(
        IProfileService _profileService,
        IValidator<UpdateProfileRequest> validator)
    {
        this._profileService = _profileService;
        _validator = validator;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.GetProfileByUserIdAsync(userId, ct);
        return HandleResult(result);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors[0].ErrorMessage;
            return HandleResult(Result<CustomerProfileDto>.Failure(
                Error.Validation("Profile.Validation", firstError)));
        }

        var userId = GetCurrentUserId();
        var result = await _profileService.UpdateProfileAsync(userId, request, ct);
        return HandleResult(result);
    }
}
