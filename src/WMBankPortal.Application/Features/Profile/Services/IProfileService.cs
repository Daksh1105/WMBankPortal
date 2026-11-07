using WMBankPortal.Application.Features.Profile.DTOs;
using WMBankPortal.Domain.Common;

namespace WMBankPortal.Application.Features.Profile.Services;

public interface IProfileService
{
    Task<Result<CustomerProfileDto>> GetProfileByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<CustomerProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
}
