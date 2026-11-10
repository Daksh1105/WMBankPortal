using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Enums;
using WMBankPortal.Domain.Exceptions;

namespace WMBankPortal.Domain.Entities;

public class KycDocument : BaseEntity, ISoftDeletable
{
    public Guid CustomerId { get; private set; }
    public Guid ApplicationId { get; private set; }
    public KycDocumentType DocumentType { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StorageBlobPath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public KycDocumentStatus Status { get; private set; } = KycDocumentStatus.Pending;
    public MalwareScanStatus ScanStatus { get; private set; } = MalwareScanStatus.Clean;
    public string Sha256Hash { get; private set; } = string.Empty;
    public Guid? VerifiedByOfficerId { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public int Version { get; private set; } = 1;
    public Guid? ReplacedByDocumentId { get; private set; }
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    private KycDocument() { } // For EF Core

    public KycDocument(
        Guid customerId,
        Guid applicationId,
        KycDocumentType documentType,
        string originalFileName,
        string storageBlobPath,
        string contentType,
        long fileSizeBytes,
        string sha256Hash,
        int version = 1)
    {
        CustomerId = customerId;
        ApplicationId = applicationId;
        DocumentType = documentType;
        OriginalFileName = originalFileName;
        StorageBlobPath = storageBlobPath;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        Sha256Hash = sha256Hash;
        Version = version;
        Status = KycDocumentStatus.Pending;
        ScanStatus = MalwareScanStatus.Clean;
        UploadedAt = DateTime.UtcNow;
    }

    public void Verify(Guid officerId)
    {
        Status = KycDocumentStatus.Verified;
        VerifiedByOfficerId = officerId;
        VerifiedAt = DateTime.UtcNow;
        RejectionReason = null;
    }

    public void Reject(Guid officerId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A rejection reason must be provided when rejecting a KYC document.");
        }

        Status = KycDocumentStatus.Rejected;
        VerifiedByOfficerId = officerId;
        VerifiedAt = DateTime.UtcNow;
        RejectionReason = reason;
    }

    public void MarkReplaced(Guid newDocumentId)
    {
        ReplacedByDocumentId = newDocumentId;
    }
}
