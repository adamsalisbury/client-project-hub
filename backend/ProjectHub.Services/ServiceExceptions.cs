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

/// <summary>Helpers for translating model state into service-layer errors.</summary>
public static class ProjectGuards
{
    /// <summary>
    /// Returns the project's working directory, or throws <see cref="ValidationException"/>
    /// when the project has no repo assigned.
    /// </summary>
    public static string RequireWorkingDirectory(this Domain.Models.ClaudeProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (string.IsNullOrWhiteSpace(project.WorkingDirectory))
        {
            throw new ValidationException(
                "This project has no repo assigned. Add a repo on the project page before running this action.");
        }
        return project.WorkingDirectory;
    }
}
