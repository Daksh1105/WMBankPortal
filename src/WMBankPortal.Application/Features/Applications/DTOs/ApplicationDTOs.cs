namespace WMBankPortal.Application.Features.Applications.DTOs;

public record SubmitApplicationRequest(
    Guid ProductId,
    decimal RequestedAmount,
    string FormValuesJson);

public record WithdrawApplicationRequest(
    string? Remarks);

public record BankingApplicationDto(
    Guid Id,
    string ApplicationNumber,
    Guid CustomerId,
    Guid ProductId,
    string ProductName,
    string ProductCategory,
    decimal RequestedAmount,
    string Status,
    string FormValuesJson,
    string? OfficerRemarks,
    DateTime SubmittedAt,
    DateTime? DecidedAt,
    byte[] RowVersion);

public record ApplicationTimelineDto(
    Guid Id,
    string FromStatus,
    string ToStatus,
    string ChangedByRole,
    string? Comments,
    DateTime Timestamp);

public record CustomerDashboardDto(
    int TotalApplications,
    int PendingApplications,
    int ApprovedApplications,
    int RejectedApplications,
    IReadOnlyList<BankingApplicationDto> RecentApplications);
