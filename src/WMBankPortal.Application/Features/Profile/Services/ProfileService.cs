using Microsoft.EntityFrameworkCore;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Profile.DTOs;
using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Entities;

namespace WMBankPortal.Application.Features.Profile.Services;

public class ProfileService : IProfileService
{
    private readonly IAppDbContext _context;

    public ProfileService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CustomerProfileDto>> GetProfileByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await _context.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile == null)
        {
            return Result<CustomerProfileDto>.Failure(
                Error.NotFound("Profile.NotFound", "Customer profile has not been created yet."));
        }

        return Result<CustomerProfileDto>.Success(MapToDto(profile));
    }

    public async Task<Result<CustomerProfileDto>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        var profile = await _context.CustomerProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile == null)
        {
            profile = new CustomerProfile(
                userId,
                request.FirstName.Trim(),
                request.LastName.Trim(),
                request.PhoneNumber.Trim(),
                request.DateOfBirth,
                request.AddressLine1.Trim(),
                request.City.Trim(),
                request.PostalCode.Trim(),
                request.Country.Trim());

            _context.CustomerProfiles.Add(profile);
        }
        else
        {
            profile.Update(
                request.FirstName.Trim(),
                request.LastName.Trim(),
                request.PhoneNumber.Trim(),
                request.DateOfBirth,
                request.AddressLine1.Trim(),
                request.City.Trim(),
                request.PostalCode.Trim(),
                request.Country.Trim());
        }

        await _context.SaveChangesAsync(ct);
        return Result<CustomerProfileDto>.Success(MapToDto(profile));
    }

    private static CustomerProfileDto MapToDto(CustomerProfile p) =>
        new(
            p.Id,
            p.UserId,
            p.FirstName,
            p.LastName,
            p.PhoneNumber,
            p.DateOfBirth,
            p.AddressLine1,
            p.City,
            p.PostalCode,
            p.Country,
            p.UpdatedAt);
}
