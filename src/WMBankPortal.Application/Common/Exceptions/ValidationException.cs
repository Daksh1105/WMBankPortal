namespace WMBankPortal.Application.Common.Exceptions;

/// <summary>
/// Thrown when request data violates validation rules.
/// Maps to HTTP 400 Bad Request with ProblemDetails validation dictionary.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = [errorMessage]
        };
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
