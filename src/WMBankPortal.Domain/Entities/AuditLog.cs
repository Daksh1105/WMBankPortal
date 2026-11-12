namespace WMBankPortal.Domain.Entities;

/// <summary>
/// Append-only record of a sensitive action. Never updated or deleted by application code.
/// Deliberately has no foreign key to the user table so the trail survives user changes.
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? UserId { get; private set; }          // null for anonymous actions like failed logins
    public string? UserRole { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? DetailsJson { get; private set; }   // must never contain PII
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime TimestampUtc { get; private set; } = DateTime.UtcNow;

    private AuditLog() { } // EF Core

    public AuditLog(
        Guid? userId, string? userRole, string action, string entityName, string? entityId,
        string? detailsJson, string? ipAddress, string? userAgent, string? correlationId)
    {
        UserId = userId;
        UserRole = userRole;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        DetailsJson = detailsJson;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        TimestampUtc = DateTime.UtcNow;
    }
}