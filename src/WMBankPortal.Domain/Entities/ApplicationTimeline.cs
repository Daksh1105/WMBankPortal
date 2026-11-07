using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Enums;

namespace WMBankPortal.Domain.Entities;

public class ApplicationTimeline : BaseEntity
{
    public Guid ApplicationId { get; private set; }
    public ApplicationStatus FromStatus { get; private set; }
    public ApplicationStatus ToStatus { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string ChangedByRole { get; private set; } = string.Empty;
    public string? Comments { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    private ApplicationTimeline() { } // For EF Core

    public ApplicationTimeline(
        Guid applicationId,
        ApplicationStatus fromStatus,
        ApplicationStatus toStatus,
        Guid changedByUserId,
        string changedByRole,
        string? comments = null)
    {
        ApplicationId = applicationId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        ChangedByRole = changedByRole;
        Comments = comments;
        Timestamp = DateTime.UtcNow;
    }
}
