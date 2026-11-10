using WMBankPortal.Application.Features.Kyc.DTOs;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Enums;

namespace WMBankPortal.Application.Features.Kyc.Services;

public interface IKycService
{
    Task<Result<KycDocumentDto>> UploadDocumentAsync(
        Guid customerId,
        Guid applicationId,
        KycDocumentType documentType,
        string originalFileName,
        string contentType,
        Stream fileStream,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<KycDocumentDto>>> GetDocumentsByApplicationIdAsync(
        Guid applicationId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default);

    Task<Result<KycDownloadTokenDto>> GenerateDownloadTokenAsync(
        Guid documentId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default);

    Task<Result<KycDocumentDto>> VerifyDocumentAsync(
        Guid documentId,
        Guid officerId,
        VerifyKycDocumentRequest request,
        CancellationToken ct = default);
}
