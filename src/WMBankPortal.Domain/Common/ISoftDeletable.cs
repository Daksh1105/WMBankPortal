namespace WMBankPortal.Domain.Common;

/// <summary>
/// Defines soft delete contract for banking regulatory audit compliance.
/// Banking records are never permanently purged from storage.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
