using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMBankPortal.Application.Features.Kyc.DTOs;
using WMBankPortal.Application.Features.Kyc.Services;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Enums;

namespace WMBankPortal.Api.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class KycController : ApiControllerBase
{
    private readonly IKycService _kycService;

    public KycController(IKycService kycService)
    {
        _kycService = kycService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(KycDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UploadDocument(
        [FromForm] IFormFile file,
        [FromForm] Guid applicationId,
        [FromForm] string documentType,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return HandleResult(Result<KycDocumentDto>.Failure(
                Error.Validation("Kyc.FileMissing", "A file must be selected for upload.")));
        }

        if (!Enum.TryParse<KycDocumentType>(documentType, true, out var parsedType))
        {
            return HandleResult(Result<KycDocumentDto>.Failure(
                Error.Validation("Kyc.InvalidType",
                    $"Invalid document type '{documentType}'. Valid types: {string.Join(", ", Enum.GetNames<KycDocumentType>())}")));
        }

        var customerId = GetCurrentUserId();

        using var stream = file.OpenReadStream();
        var result = await _kycService.UploadDocumentAsync(
            customerId,
            applicationId,
            parsedType,
            file.FileName,
            file.ContentType,
            stream,
            ct);

        return HandleResult(result);
    }

    [HttpGet("application/{applicationId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<KycDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetDocumentsForApplication(Guid applicationId, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        var result = await _kycService.GetDocumentsByApplicationIdAsync(
            applicationId,
            currentUserId,
            currentUserRole,
            ct);

        return HandleResult(result);
    }

    [HttpGet("{id:guid}/download-token")]
    [ProducesResponseType(typeof(KycDownloadTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GenerateDownloadToken(Guid id, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        var result = await _kycService.GenerateDownloadTokenAsync(
            id,
            currentUserId,
            currentUserRole,
            ct);

        return HandleResult(result);
    }

    [HttpPost("{id:guid}/verify")]
    [Authorize(Roles = "Officer,Admin")]
    [ProducesResponseType(typeof(KycDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> VerifyDocument(
        Guid id,
        [FromBody] VerifyKycDocumentRequest request,
        CancellationToken ct)
    {
        var officerId = GetCurrentUserId();
        var result = await _kycService.VerifyDocumentAsync(id, officerId, request, ct);
        return HandleResult(result);
    }
}
