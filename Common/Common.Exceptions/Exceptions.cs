namespace Common.Exceptions;

/// <summary>
/// Base application exception for custom error handling.
/// </summary>
public class ApplicationException : Exception
{
    public ApplicationException(string message) : base(message) { }
    public ApplicationException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a business rule validation fails.
/// </summary>
public class BusinessRuleException : ApplicationException
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : ApplicationException
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resourceName, object resourceId) 
        : base($"{resourceName} with ID {resourceId} was not found.") { }
}

/// <summary>
/// Exception thrown when an operation is not authorized.
/// </summary>
public class UnauthorizedAccessException : ApplicationException
{
    public UnauthorizedAccessException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when attempting a duplicate operation.
/// </summary>
public class DuplicateException : ApplicationException
{
    public DuplicateException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : ApplicationException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) 
        : base("Validation failed.")
    {
        Errors = errors;
    }
}
