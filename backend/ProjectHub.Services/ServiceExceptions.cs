namespace ProjectHub.Services;

/// <summary>
/// Base type for service-layer failures that callers should translate into a
/// transport-specific response (e.g. HTTP status code) at the boundary.
/// The service layer itself is transport-agnostic.
/// </summary>
public abstract class ServiceException : Exception
{
    protected ServiceException(string message) : base(message) { }
    protected ServiceException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>The caller asked for an entity that does not exist.</summary>
public sealed class NotFoundException : ServiceException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>The caller's input failed a validation rule.</summary>
public sealed class ValidationException : ServiceException
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>
/// The request was syntactically valid but cannot be processed in the
/// current state (e.g. an external command failed).
/// </summary>
public sealed class UnprocessableException : ServiceException
{
    public string? Detail { get; }

    public UnprocessableException(string message, string? detail = null) : base(message)
    {
        Detail = detail;
    }
}
