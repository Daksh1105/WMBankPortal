namespace WMBankPortal.Application.Common.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Adds an audit row to the current unit of work. It is saved when the caller
    /// calls SaveChangesAsync, so it commits atomically with the business change.
    /// Never put names, emails, phone numbers, file names or document contents in details.
    /// </summary>
    void Record(string action, string entityName, string? entityId = null,
                object? details = null, Guid? userIdOverride = null, string? roleOverride = null);
}