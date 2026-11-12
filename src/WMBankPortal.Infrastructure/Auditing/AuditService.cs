using System.Text.Json;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Infrastructure.Persistence;

namespace WMBankPortal.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public void Record(string action, string entityName, string? entityId = null,
                       object? details = null, Guid? userIdOverride = null, string? roleOverride = null)
    {
        var entry = new AuditLog(
            userIdOverride ?? _currentUser.UserId,
            roleOverride ?? _currentUser.Role,
            action,
            entityName,
            entityId,
            details is null ? null : JsonSerializer.Serialize(details),
            _currentUser.IpAddress,
            Truncate(_currentUser.UserAgent, 256),
            _currentUser.CorrelationId);

        _db.AuditLogs.Add(entry);
    }

    private static string? Truncate(string? value, int max) =>
        value is { Length: > 0 } && value.Length > max ? value[..max] : value;
}