using WMBankPortal.Domain.Common;

namespace WMBankPortal.Domain.Entities;

public class CustomerProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public DateTime DateOfBirth { get; private set; }
    public string AddressLine1 { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = "India";
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private CustomerProfile() { } // For EF Core

    public CustomerProfile(
        Guid userId,
        string firstName,
        string lastName,
        string phoneNumber,
        DateTime dateOfBirth,
        string addressLine1,
        string city,
        string postalCode,
        string country = "India")
    {
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        DateOfBirth = dateOfBirth;
        AddressLine1 = addressLine1;
        City = city;
        PostalCode = postalCode;
        Country = string.IsNullOrWhiteSpace(country) ? "India" : country;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string firstName,
        string lastName,
        string phoneNumber,
        DateTime dateOfBirth,
        string addressLine1,
        string city,
        string postalCode,
        string country)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        DateOfBirth = dateOfBirth;
        AddressLine1 = addressLine1;
        City = city;
        PostalCode = postalCode;
        Country = string.IsNullOrWhiteSpace(country) ? "India" : country;
        UpdatedAt = DateTime.UtcNow;
    }
}
