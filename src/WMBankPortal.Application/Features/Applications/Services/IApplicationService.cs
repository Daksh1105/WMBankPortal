using WMBankPortal.Application.Features.Applications.DTOs;
using WMBankPortal.Domain.Common;

namespace WMBankPortal.Application.Features.Applications.Services;

public interface IApplicationService
{
    Task<Result<BankingApplicationDto>> SubmitApplicationAsync(
        Guid customerId,
        SubmitApplicationRequest request,
        CancellationToken ct = default);

    Task<Result<PagedResult<BankingApplicationDto>>> GetMyApplicationsAsync(
        Guid customerId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<BankingApplicationDto>> GetApplicationByIdAsync(
        Guid applicationId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<ApplicationTimelineDto>>> GetApplicationTimelineAsync(
        Guid applicationId,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken ct = default);

    Task<Result> WithdrawApplicationAsync(
        Guid applicationId,
        Guid customerId,
        WithdrawApplicationRequest request,
        CancellationToken ct = default);

    Task<Result<CustomerDashboardDto>> GetCustomerDashboardAsync(
        Guid customerId,
        CancellationToken ct = default);
}
