using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Enums;
using WMBankPortal.Domain.Exceptions;

namespace WMBankPortal.Domain.Entities;

public class BankingApplication : BaseEntity, ISoftDeletable
{
    private readonly List<ApplicationTimeline> _timelines = new();

    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ApplicationNumber { get; private set; } = string.Empty;
    public string FormValuesJson { get; private set; } = "{}";
    public decimal RequestedAmount { get; private set; }
    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Submitted;
    public string? OfficerRemarks { get; private set; }
    public Guid? ReviewedByOfficerId { get; private set; }
    public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public IReadOnlyCollection<ApplicationTimeline> Timelines => _timelines.AsReadOnly();

    private BankingApplication() { } // For EF Core

    public BankingApplication(
        Guid customerId,
        Guid productId,
        string applicationNumber,
        decimal requestedAmount,
        string formValuesJson)
    {
        CustomerId = customerId;
        ProductId = productId;
        ApplicationNumber = applicationNumber;
        RequestedAmount = requestedAmount;
        FormValuesJson = string.IsNullOrWhiteSpace(formValuesJson) ? "{}" : formValuesJson;
        Status = ApplicationStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;

        _timelines.Add(new ApplicationTimeline(
            Id,
            ApplicationStatus.Submitted,
            ApplicationStatus.Submitted,
            customerId,
            "Customer",
            "Application submitted"));
    }

    public void Claim(Guid officerId)
    {
        if (Status != ApplicationStatus.Submitted)
        {
            throw new InvalidStateTransitionException(
                nameof(BankingApplication),
                Status.ToString(),
                ApplicationStatus.UnderReview.ToString(),
                "Only applications in 'Submitted' state can be claimed for review.");
        }

        var prev = Status;
        Status = ApplicationStatus.UnderReview;
        ReviewedByOfficerId = officerId;

        _timelines.Add(new ApplicationTimeline(
            Id,
            prev,
            Status,
            officerId,
            "Officer",
            "Application claimed for review"));
    }

    public void RequestKyc(Guid officerId, string remarks)
    {
        if (Status != ApplicationStatus.UnderReview)
        {
            throw new InvalidStateTransitionException(
                nameof(BankingApplication),
                Status.ToString(),
                ApplicationStatus.KycPending.ToString(),
                "KYC documents can only be requested when the application is 'UnderReview'.");
        }

        if (string.IsNullOrWhiteSpace(remarks))
        {
            throw new InvalidStateTransitionException(
                nameof(BankingApplication),
                Status.ToString(),
                ApplicationStatus.KycPending.ToString(),
                "Officer remarks are mandatory when requesting KYC documentation.");
        }

        var prev = Status;
        Status = ApplicationStatus.KycPending;
        OfficerRemarks = remarks;
        ReviewedByOfficerId = officerId;

        _timelines.Add(new ApplicationTimeline(
            Id,
            prev,
            Status,
            officerId,
            "Officer",
            remarks));
    }

    public void Approve(Guid officerId, string? remarks = null)
    {
        if (Status != ApplicationStatus.UnderReview)
        {
            throw new InvalidStateTransitionException(
                nameof(BankingApplication),
                Status.ToString(),
                ApplicationStatus.Approved.ToString(),
                "Only applications 'UnderReview' can be approved.");
        }

        var prev = Status;
        Status = ApplicationStatus.Approved;
        ReviewedByOfficerId = officerId;
        OfficerRemarks = remarks;
        DecidedAt = DateTime.UtcNow;

        _timelines.Add(new ApplicationTimeline(
            Id,
            prev,
            Status,
            officerId,
            "Officer",
            remarks ?? "Application approved"));
    }

    public void Reject(Guid officerId, string remarks)
    {
        if (Status != ApplicationStatus.UnderReview)
        {
            throw new InvalidStateTransitionException(
                nameof(BankingApplication),
                Status.ToString(),
                ApplicationStatus.Rejected.ToString(),
                "Only applications 'UnderReview' can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(remarks))
        {
            throw new InvalidStateTransitionException(
                nameof(BankingApplication),
                Status.ToString(),
                ApplicationStatus.Rejected.ToString(),
                "Officer remarks explaining the rejection reason are mandatory.");
        }

        var prev = Status;
        Status = ApplicationStatus.Rejected;
        ReviewedByOfficerId = officerId;
        OfficerRemarks = remarks;
        DecidedAt = DateTime.UtcNow;

        _timelines.Add(new ApplicationTimeline(
            Id,
            prev,
            Status,
            officerId,
            "Officer",
            remarks));
    }

    public void ResubmitKyc()
    {
        if (Status != ApplicationStatus.KycPending)
        {
            throw new InvalidStateTransitionException(
                nameof(BankingApplication),
                Status.ToString(),
                ApplicationStatus.UnderReview.ToString(),
                "Application must be in 'KycPending' state to resubmit KYC.");
        }

        var prev = Status;
        Status = ApplicationStatus.UnderReview;

        _timelines.Add(new ApplicationTimeline(
            Id,
            prev,
            Status,
            CustomerId,
            "Customer",
            "KYC documents re-uploaded and returned to underwriting review"));
    }

    public void Withdraw(Guid customerId, string? remarks = null)
    {
        if (customerId != CustomerId)
        {
            throw new InvalidStateTransitionException(
                nameof(BankingApplication),
                Status.ToString(),
                ApplicationStatus.Withdrawn.ToString(),
                "Only the customer who submitted the application can withdraw it.");
        }

        if (Status != ApplicationStatus.Submitted && Status != ApplicationStatus.KycPending)
        {
            throw new InvalidStateTransitionException(
                nameof(BankingApplication),
                Status.ToString(),
                ApplicationStatus.Withdrawn.ToString(),
                "Applications can only be withdrawn while in 'Submitted' or 'KycPending' state.");
        }

        var prev = Status;
        Status = ApplicationStatus.Withdrawn;
        DecidedAt = DateTime.UtcNow;

        _timelines.Add(new ApplicationTimeline(
            Id,
            prev,
            Status,
            customerId,
            "Customer",
            remarks ?? "Application withdrawn by customer"));
    }
}
