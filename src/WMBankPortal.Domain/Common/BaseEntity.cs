namespace WMBankPortal.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Enforces strong identity via Guid and UTC audit timestamps.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
