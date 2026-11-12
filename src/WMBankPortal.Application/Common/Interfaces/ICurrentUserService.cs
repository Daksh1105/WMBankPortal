namespace WMBankPortal.Application.Common.Interfaces;

/// <summary>
/// Provides claims context of the currently authenticated caller.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? CorrelationId { get; }
}
