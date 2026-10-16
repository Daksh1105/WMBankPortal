namespace WMBankPortal.Application.Common.Interfaces;

/// <summary>
/// Abstraction for clock time to allow deterministic unit testing of expiry and schedules.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
