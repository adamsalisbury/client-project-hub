using System.Diagnostics;
using System.Text;
using ProjectHub.Domain.Models;
using Microsoft.Extensions.Options;

namespace ProjectHub.Services.Runner;

/// <inheritdoc/>
public sealed class ClaudeRunner(
    IOptions<ClaudeRunnerOptions> options,
    ILogger<ClaudeRunner> logger) : IClaudeRunner
{
    private readonly ClaudeRunnerOptions _options = options.Value;
    private readonly ILogger<ClaudeRunner> _logger = logger;

    /// <summary>
    /// Tools the CLI must not invoke when the message is <see cref="MessageKind.Chat"/>.
    /// Read-only tools (Read, Grep, Glob, WebFetch, …) remain enabled.
    /// </summary>
    private static readonly string[] s_chatDisallowedTools =
    [
        "Bash", "Edit", "Write", "MultiEdit", "NotebookEdit", "Task"
    ];

    /// <inheritdoc/>
    public async Task<ClaudeResponse> RunAsync(
        string message,
        string? workingDirectory = null,
        MessageKind kind = MessageKind.Chat,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var resolvedDirectory = !string.IsNullOrWhiteSpace(workingDirectory)
            ? workingDirectory
            : _options.WorkingDirectory ?? Path.GetTempPath();

        if (!Directory.Exists(resolvedDirectory))
        {
            throw new InvalidOperationException(
                $"Working directory '{resolvedDirectory}' does not exist.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            WorkingDirectory = resolvedDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.ArgumentList.Add("--dangerously-skip-permissions");
        startInfo.ArgumentList.Add("--print");
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("text");

        if (kind == MessageKind.Chat)
        {
            startInfo.ArgumentList.Add("--disallowedTools");
            startInfo.ArgumentList.Add(string.Join(",", s_chatDisallowedTools));
        }

        // The prompt is sent on stdin rather than as a positional argument.
        // The CLI's --disallowedTools is variadic and would otherwise consume
        // a trailing message argument as another tool name.

        _logger.LogInformation(
            "Invoking Claude Code: {Executable} (cwd: {WorkingDirectory}, kind: {Kind}, message length: {Length})",
            _options.ExecutablePath,
            resolvedDirectory,
            kind,
            message.Length);

        using var process = new Process { StartInfo = startInfo };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Failed to start Claude Code at {Executable}", _options.ExecutablePath);
            throw new InvalidOperationException(
                $"Could not start Claude Code executable '{_options.ExecutablePath}'. Ensure it is installed and on PATH.",
                ex);
        }

        await process.StandardInput.WriteAsync(message.AsMemory(), cancellationToken);
        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            stopwatch.Stop();
            throw new TimeoutException(
                $"Claude Code did not complete within {_options.TimeoutSeconds} seconds.");
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        stopwatch.Stop();

        _logger.LogInformation(
            "Claude Code exited with code {ExitCode} after {DurationMs}ms",
            process.ExitCode,
            stopwatch.ElapsedMilliseconds);

        return new ClaudeResponse(
            Response: stdout.TrimEnd(),
            ExitCode: process.ExitCode,
            DurationMs: stopwatch.ElapsedMilliseconds,
            Error: process.ExitCode == 0 ? null : stderr.TrimEnd());
    }

    private void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to kill Claude Code process; it may have already exited.");
        }
    }
}
