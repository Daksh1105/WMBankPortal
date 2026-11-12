using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Kyc.DTOs;
using WMBankPortal.Application.Features.Notifications.Services;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;

namespace WMBankPortal.Application.Features.Kyc.Services;

public class KycService : IKycService
{
    private const int MaxActiveDocumentsPerApplication = 10;
    private const int MaxTotalDocumentsPerApplication = 25;

    private readonly IAppDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IMalwareScanner _malwareScanner;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _audit;

    public KycService(
        IAppDbContext context,
        IBlobStorageService blobStorageService,
        IMalwareScanner malwareScanner,
        INotificationService notificationService,
        IAuditService audit)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _malwareScanner = malwareScanner;
        _notificationService = notificationService;
        _audit = audit;
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

        // 4. File-count cap per application
        var totalDocCount = await _context.KycDocuments
            .CountAsync(d => d.ApplicationId == applicationId, ct);
        if (totalDocCount >= MaxTotalDocumentsPerApplication)
        {
            return Result<KycDocumentDto>.Failure(
                Error.Validation("Kyc.UploadLimitReached",
                    $"Maximum total upload limit ({MaxTotalDocumentsPerApplication}) reached for this application."));
        }

        // 5. File Validation Pipeline (Size, Extension, Double-ext, Magic Bytes)
        var validation = KycFileValidator.Validate(originalFileName, fileStream.Length, fileStream);
        if (validation.IsFailure)
        {
            return Result<KycDocumentDto>.Failure(validation.Error);
        }

        // 6. Malware Scanner Hook
        var scanResult = await _malwareScanner.ScanAsync(fileStream, originalFileName, ct);
        if (scanResult.IsFailure)
        {
            return Result<KycDocumentDto>.Failure(scanResult.Error);
        }

        // 7. Compute SHA-256 Hash
        fileStream.Position = 0;
        var hashBytes = await SHA256.HashDataAsync(fileStream, ct);
        var sha256Hex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // 8. Check for existing document of this type (versioning)
        var existingDoc = await _context.KycDocuments
            .Where(d => d.ApplicationId == applicationId && d.DocumentType == documentType && d.ReplacedByDocumentId == null)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(ct);

        if (existingDoc == null)
        {
            var activeDocCount = await _context.KycDocuments
                .CountAsync(d => d.ApplicationId == applicationId && d.ReplacedByDocumentId == null, ct);

            if (activeDocCount >= MaxActiveDocumentsPerApplication)
            {
                return Result<KycDocumentDto>.Failure(
                    Error.Validation("Kyc.ActiveLimitReached",
                        $"An application cannot have more than {MaxActiveDocumentsPerApplication} active KYC documents."));
            }
        }

        int version = existingDoc != null ? existingDoc.Version + 1 : 1;

        // 9. Generate safe, unguessable blob path ({guid}.ext)
        var safeExt = Path.GetExtension(originalFileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid():N}{safeExt}";

        // 10. Upload to private blob storage
        fileStream.Position = 0;
        await _blobStorageService.UploadAsync(fileStream, blobName, contentType, ct);

        // 11. Persist KycDocument metadata
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

        // 12. Auto-transition: If application is in KycPending, transition back to UnderReview
        var wasKycPending = application.Status == ApplicationStatus.KycPending;
        if (wasKycPending)
        {
            application.ResubmitKyc();
        }

        _audit.Record(
            "KycDocumentUploaded",
            nameof(KycDocument),
            document.Id.ToString(),
            new { applicationId = application.Id, documentType = document.DocumentType.ToString(), sha256 = sha256Hex });

        await _context.SaveChangesAsync(ct);

        if (wasKycPending)
        {
            await _notificationService.CreateAndSendAsync(
                customerId,
                "KYC Documents Submitted",
                $"Your KYC document for application {application.ApplicationNumber} has been received and underwriting review has resumed.",
                NotificationType.Info,
                application.Id,
                ct);
        }

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
        var docWithOwner = await (
            from d in _context.KycDocuments.AsNoTracking()
            where d.Id == documentId
            join a in _context.BankingApplications.AsNoTracking() on d.ApplicationId equals a.Id
            select new { Document = d, ApplicationCustomerId = a.CustomerId }
        ).FirstOrDefaultAsync(ct);

        if (docWithOwner == null)
        {
            return Result<KycDownloadTokenDto>.Failure(
                Error.NotFound("Kyc.NotFound", "The requested KYC document was not found."));
        }

        var isPrivilegedStaff =
            currentUserRole == UserRole.Officer.ToString() ||
            currentUserRole == UserRole.Admin.ToString();

        var isDocumentOwner =
            currentUserRole == UserRole.Customer.ToString() &&
            docWithOwner.Document.CustomerId == currentUserId &&
            docWithOwner.ApplicationCustomerId == currentUserId;

        if (!isPrivilegedStaff && !isDocumentOwner)
        {
            return Result<KycDownloadTokenDto>.Failure(
                Error.Forbidden("Kyc.Forbidden", "You do not have permission to access this KYC document."));
        }

        var lifetime = TimeSpan.FromMinutes(5);
        var sasUrl = await _blobStorageService.GenerateReadSasUrlAsync(
            docWithOwner.Document.StorageBlobPath,
            lifetime,
            ct);

        _audit.Record(
            "KycDownloadTokenIssued",
            nameof(KycDocument),
            docWithOwner.Document.Id.ToString(),
            new { documentId = docWithOwner.Document.Id, applicationId = docWithOwner.Document.ApplicationId });

        await _context.SaveChangesAsync(ct);

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

        bool isVerified;
        if (string.Equals(request.Status, "Verified", StringComparison.OrdinalIgnoreCase))
        {
            document.Verify(officerId);
            isVerified = true;
        }
        else if (string.Equals(request.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                return Result<KycDocumentDto>.Failure(
                    Error.Validation("Kyc.RejectionReasonRequired", "A rejection reason is mandatory when rejecting a document."));
            }
            document.Reject(officerId, request.RejectionReason);
            isVerified = false;
        }
        else
        {
            return Result<KycDocumentDto>.Failure(
                Error.Validation("Kyc.InvalidStatus", "Status must be either 'Verified' or 'Rejected'."));
        }

        _audit.Record(
            isVerified ? "KycDocumentVerified" : "KycDocumentRejected",
            nameof(KycDocument),
            document.Id.ToString(),
            new
            {
                documentId = document.Id,
                reasonCode = isVerified ? "VERIFIED" : "REJECTED_BY_OFFICER"
            });

        await _context.SaveChangesAsync(ct);

        await _notificationService.CreateAndSendAsync(
            document.CustomerId,
            isVerified ? "KYC Document Verified" : "KYC Document Rejected",
            isVerified
                ? $"Your {document.DocumentType} document has been successfully verified."
                : $"Your {document.DocumentType} document was rejected: {request.RejectionReason}. Please re-upload.",
            isVerified ? NotificationType.Success : NotificationType.ActionRequired,
            document.ApplicationId,
            ct);

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
