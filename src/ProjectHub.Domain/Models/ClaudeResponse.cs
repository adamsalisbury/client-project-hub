namespace ProjectHub.Domain.Models;

/// <summary>
/// The result of running a single message through the Claude Code CLI.
/// </summary>
/// <param name="Response">Standard output produced by the CLI, trimmed.</param>
/// <param name="ExitCode">Process exit code. Zero indicates success.</param>
/// <param name="DurationMs">Wall-clock duration of the CLI invocation, in milliseconds.</param>
/// <param name="Error">Standard error output, populated when <paramref name="ExitCode"/> is non-zero.</param>
public sealed record ClaudeResponse(string Response, int ExitCode, long DurationMs, string? Error = null);
