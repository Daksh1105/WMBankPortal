namespace WMBankPortal.Domain.Enums;

/// <summary>
/// Categorization of real-time and stored customer/officer notifications.
/// </summary>
public enum NotificationType
{
    Info = 1,
    Success = 2,
    Warning = 3,
    ActionRequired = 4
}
