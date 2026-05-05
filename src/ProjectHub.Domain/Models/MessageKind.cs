namespace ProjectHub.Domain.Models;

/// <summary>
/// Caller-declared intent for a single message.
/// </summary>
public enum MessageKind
{
    /// <summary>
    /// A read/answer message. The CLI is invoked with mutating tools
    /// (Edit, Write, Bash, …) disallowed so it physically cannot change
    /// files in the working directory.
    /// </summary>
    Chat,

    /// <summary>
    /// A change-the-code message. The CLI is invoked with the full tool
    /// set; the worker captures which files in the working directory
    /// changed during the run and surfaces them on the response.
    /// </summary>
    Edit
}
