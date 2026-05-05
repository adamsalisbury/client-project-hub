using ProjectHub.Domain.Models;

namespace ProjectHub.Services.Runner;

/// <summary>
/// Runs a single message through the Claude Code CLI in an isolated subprocess.
/// </summary>
public interface IClaudeRunner
{
    /// <summary>
    /// Invokes the Claude Code CLI with the supplied message and returns its output.
    /// </summary>
    /// <param name="message">The prompt to pass to Claude Code.</param>
    /// <param name="workingDirectory">
    /// Directory the subprocess should run from. When <see langword="null"/>
    /// the runner falls back to the configured default.
    /// </param>
    /// <param name="kind">
    /// Caller intent. <see cref="MessageKind.Chat"/> disallows mutating tools
    /// (Edit, Write, Bash, …) so the CLI cannot change files in the working
    /// directory. <see cref="MessageKind.Edit"/> runs with full tool access.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The captured stdout, exit code, duration, and any stderr output.</returns>
    Task<ClaudeResponse> RunAsync(
        string message,
        string? workingDirectory = null,
        MessageKind kind = MessageKind.Chat,
        CancellationToken cancellationToken = default);
}
