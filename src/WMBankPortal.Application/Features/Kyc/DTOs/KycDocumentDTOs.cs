namespace WMBankPortal.Application.Features.Kyc.DTOs;

public record KycDocumentDto(
    Guid Id,
    Guid ApplicationId,
    string DocumentType,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Status,
    string ScanStatus,
    DateTime UploadedAt,
    string? RejectionReason,
    int Version);

public record KycDownloadTokenDto(
    string DownloadUrl,
    DateTime ExpiresAtUtc);

public record VerifyKycDocumentRequest(
    string Status,
    string? RejectionReason);
