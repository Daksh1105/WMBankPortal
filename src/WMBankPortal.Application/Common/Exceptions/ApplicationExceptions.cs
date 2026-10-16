namespace WMBankPortal.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist. Maps to HTTP 404 Not Found.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key ({key}) was not found.")
    {
        EntityName = entityName;
        Key = key;
    }

    public string EntityName { get; }
    public object Key { get; }
}

/// <summary>
/// Thrown when an authenticated user attempts to access an unauthorized resource (e.g. IDOR violation).
/// Maps to HTTP 403 Forbidden.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to access this resource.")
        : base(message)
    {
    }
}

/// <summary>
/// Thrown when a resource conflict occurs (e.g. optimistic concurrency or duplicate key).
/// Maps to HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
