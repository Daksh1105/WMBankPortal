namespace WMBankPortal.Application.Features.Profile.DTOs;

public record CustomerProfileDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateTime DateOfBirth,
    string AddressLine1,
    string City,
    string PostalCode,
    string Country,
    DateTime UpdatedAt);

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateTime DateOfBirth,
    string AddressLine1,
    string City,
    string PostalCode,
    string Country);
