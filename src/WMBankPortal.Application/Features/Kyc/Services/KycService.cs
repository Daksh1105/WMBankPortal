using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Kyc.DTOs;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;

namespace WMBankPortal.Application.Features.Kyc.Services;

public class KycService : IKycService
{
    private readonly IAppDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IMalwareScanner _malwareScanner;

    public KycService(
        IAppDbContext context,
        IBlobStorageService blobStorageService,
        IMalwareScanner malwareScanner)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _malwareScanner = malwareScanner;
    }

    public async Task<Result<KycDocumentDto>> UploadDocumentAsync(
        Guid customerId,
        Guid applicationId,
        KycDocumentType documentType,
        string originalFileName,
        string contentType,
        Stream fileStream,
        CancellationToken ct = default)
    {
        // 1. Verify Application exists
        var application = await _context.BankingApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (application == null)
        {
            return Result<KycDocumentDto>.Failure(
                Error.NotFound("Application.NotFound", "The target banking application was not found."));
        }

        // 2. IDOR Defense: Only the application owner can upload KYC files
        if (application.CustomerId != customerId)
        {
            return Result<KycDocumentDto>.Failure(
                Error.Forbidden("Kyc.Forbidden", "You can only upload KYC documents to your own banking application."));
        }

        // 3. Application State Check (Terminal states cannot receive uploads)
        if (application.Status == ApplicationStatus.Approved ||
            application.Status == ApplicationStatus.Rejected ||
            application.Status == ApplicationStatus.Withdrawn)
        {
            return Result<KycDocumentDto>.Failure(
                Error.Conflict("Kyc.ApplicationClosed",
                    $"Cannot upload documents to an application in terminal state '{application.Status}'."));
        }

        // 4. File Validation Pipeline (Size, Extension, Double-ext, Magic Bytes)
        var validation = KycFileValidator.Validate(originalFileName, fileStream.Length, fileStream);
        if (validation.IsFailure)
        {
            return Result<KycDocumentDto>.Failure(validation.Error);
        }

        // 5. Malware Scanner Hook
        var scanResult = await _malwareScanner.ScanAsync(fileStream, originalFileName, ct);
        if (scanResult.IsFailure)
        {
            return Result<KycDocumentDto>.Failure(scanResult.Error);
        }

        // 6. Compute SHA-256 Hash
        fileStream.Position = 0;
        var hashBytes = await SHA256.HashDataAsync(fileStream, ct);
        var sha256Hex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // 7. Check for existing document of this type (versioning)
        var existingDoc = await _context.KycDocuments
            .Where(d => d.ApplicationId == applicationId && d.DocumentType == documentType && d.ReplacedByDocumentId == null)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(ct);

        int version = existingDoc != null ? existingDoc.Version + 1 : 1;

        // 8. Generate safe, unguessable blob path ({guid}.ext)
        var safeExt = Path.GetExtension(originalFileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid():N}{safeExt}";

        // 9. Upload to private blob storage
        fileStream.Position = 0;
        await _blobStorageService.UploadAsync(fileStream, blobName, contentType, ct);

        // 10. Persist KycDocument metadata
        var document = new KycDocument(
            customerId,
            applicationId,
            documentType,
            originalFileName,
            blobName,
            contentType,
            fileStream.Length,
            sha256Hex,
            version);

        if (existingDoc != null)
        {
            existingDoc.MarkReplaced(document.Id);
        }

        _context.KycDocuments.Add(document);

        // 11. Auto-transition: If application is in KycPending, transition back to UnderReview
        if (application.Status == ApplicationStatus.KycPending)
        {
            application.ResubmitKyc();
        }

        await _context.SaveChangesAsync(ct);

        return Result<KycDocumentDto>.Success(MapToDto(document));
    }

    public async Task<Result<IReadOnlyList<KycDocumentDto>>> GetDocumentsByApplicationIdAsync(
        Guid applicationId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default)
    {
        var application = await _context.BankingApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (application == null)
        {
            return Result<IReadOnlyList<KycDocumentDto>>.Failure(
                Error.NotFound("Application.NotFound", "The target banking application was not found."));
        }

        // IDOR Defense
        if (currentUserRole == UserRole.Customer.ToString() && application.CustomerId != currentUserId)
        {
            return Result<IReadOnlyList<KycDocumentDto>>.Failure(
                Error.Forbidden("Kyc.Forbidden", "You do not have permission to view documents for this application."));
        }

        IReadOnlyList<KycDocumentDto> docs = await _context.KycDocuments
            .AsNoTracking()
            .Where(d => d.ApplicationId == applicationId && d.ReplacedByDocumentId == null)
            .OrderBy(d => d.DocumentType)
            .Select(d => MapToDto(d))
            .ToListAsync(ct);

        return Result<IReadOnlyList<KycDocumentDto>>.Success(docs);
    }

    public async Task<Result<KycDownloadTokenDto>> GenerateDownloadTokenAsync(
        Guid documentId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default)
    {
        var document = await _context.KycDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document == null)
        {
            return Result<KycDownloadTokenDto>.Failure(
                Error.NotFound("Kyc.NotFound", "The requested KYC document was not found."));
        }

        // IDOR Defense
        if (currentUserRole == UserRole.Customer.ToString() && document.CustomerId != currentUserId)
        {
            return Result<KycDownloadTokenDto>.Failure(
                Error.Forbidden("Kyc.Forbidden", "You do not have permission to access this KYC document."));
        }

        var lifetime = TimeSpan.FromMinutes(5);
        var sasUrl = await _blobStorageService.GenerateReadSasUrlAsync(document.StorageBlobPath, lifetime, ct);

        return Result<KycDownloadTokenDto>.Success(new KycDownloadTokenDto(
            sasUrl,
            DateTime.UtcNow.Add(lifetime)));
    }

    public async Task<Result<KycDocumentDto>> VerifyDocumentAsync(
        Guid documentId,
        Guid officerId,
        VerifyKycDocumentRequest request,
        CancellationToken ct = default)
    {
        var document = await _context.KycDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document == null)
        {
            return Result<KycDocumentDto>.Failure(
                Error.NotFound("Kyc.NotFound", "The requested KYC document was not found."));
        }

        if (string.Equals(request.Status, "Verified", StringComparison.OrdinalIgnoreCase))
        {
            document.Verify(officerId);
        }
        else if (string.Equals(request.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                return Result<KycDocumentDto>.Failure(
                    Error.Validation("Kyc.RejectionReasonRequired", "A rejection reason is mandatory when rejecting a document."));
            }
            document.Reject(officerId, request.RejectionReason);
        }
        else
        {
            return Result<KycDocumentDto>.Failure(
                Error.Validation("Kyc.InvalidStatus", "Status must be either 'Verified' or 'Rejected'."));
        }

        await _context.SaveChangesAsync(ct);
        return Result<KycDocumentDto>.Success(MapToDto(document));
    }

    private static KycDocumentDto MapToDto(KycDocument d) =>
        new(
            d.Id,
            d.ApplicationId,
            d.DocumentType.ToString(),
            d.OriginalFileName,
            d.ContentType,
            d.FileSizeBytes,
            d.Status.ToString(),
            d.ScanStatus.ToString(),
            d.UploadedAt,
            d.RejectionReason,
            d.Version);
}
