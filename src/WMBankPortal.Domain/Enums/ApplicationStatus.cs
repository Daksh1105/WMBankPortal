namespace WMBankPortal.Domain.Enums;

public enum ApplicationStatus
{
    Submitted = 0,
    UnderReview = 1,
    KycPending = 2,
    Approved = 3,
    Rejected = 4,
    Withdrawn = 5
}
