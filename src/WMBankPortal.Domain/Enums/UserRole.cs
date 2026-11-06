namespace WMBankPortal.Domain.Enums;

/// <summary>
/// Predefined security roles in the banking platform.
/// Strictly mapped to claims and authorization policies.
/// </summary>
public enum UserRole
{
    Customer = 1,
    SupportAgent = 2,
    Officer = 3,
    Admin = 4
}
