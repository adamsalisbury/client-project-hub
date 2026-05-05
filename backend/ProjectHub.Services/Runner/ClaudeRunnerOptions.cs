namespace ProjectHub.Services.Runner;

/// <summary>
/// Configuration for <see cref="ClaudeRunner"/>. Bound from the
/// <c>ClaudeRunner</c> section of <c>appsettings.json</c>.
/// </summary>
public sealed class ClaudeRunnerOptions
{
    public const string SectionName = "ClaudeRunner";

    /// <summary>Path to the Claude Code executable. Defaults to <c>claude</c> on PATH.</summary>
    public string ExecutablePath { get; init; } = "claude";

    /// <summary>Maximum time to wait for the CLI to complete, in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 300;

    /// <summary>Working directory for the subprocess. Defaults to the system temp directory.</summary>
    public string? WorkingDirectory { get; init; }
}
