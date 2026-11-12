using WMBankPortal.Domain.Common;

namespace WMBankPortal.Application.Features.Underwriting.Services;

public interface IOfficerService
{
    Task<Result> ClaimApplicationAsync(Guid applicationId, Guid officerId, CancellationToken ct = default);
    Task<Result> ApproveApplicationAsync(Guid applicationId, Guid officerId, string remarks, CancellationToken ct = default);
    Task<Result> RejectApplicationAsync(Guid applicationId, Guid officerId, string remarks, CancellationToken ct = default);
    Task<Result> RequestKycAsync(Guid applicationId, Guid officerId, string remarks, CancellationToken ct = default);
}