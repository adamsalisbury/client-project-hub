using System.Text;
using System.Text.Json;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Runner;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class TicketService(
    IClaudeDataProvider data,
    IClaudeRunner runner,
    ILogger<TicketService> logger) : ITicketService
{
    private static readonly string[] s_allowedImageContentTypes =
    [
        "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif"
    ];

    private const int MaxScreenshots = 10;
    private const long MaxScreenshotBytes = 10 * 1024 * 1024;

    private static readonly JsonSerializerOptions s_extractedTicketOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Ticket>> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await EnsureProjectAsync(projectId, cancellationToken);
        return await data.ListTicketsByProjectAsync(projectId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Ticket> CreateAsync(Guid projectId, string code, string title, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title) || body is null)
        {
            throw new ValidationException("code, title, and body are all required.");
        }

        await EnsureProjectAsync(projectId, cancellationToken);

        var ticket = await data.CreateTicketAsync(projectId, code, title, body, cancellationToken);
        logger.LogInformation("Created ticket {TicketId} '{Code}' in project {ProjectId}", ticket.Id, ticket.Code, projectId);
        return ticket;
    }

    /// <inheritdoc/>
    public async Task<ExtractedTicket> ExtractFromScreenshotsAsync(Guid projectId, IReadOnlyList<TicketScreenshot> screenshots, CancellationToken cancellationToken)
    {
        if (screenshots is null || screenshots.Count == 0)
        {
            throw new ValidationException("Upload at least one screenshot file under the 'files' field.");
        }
        if (screenshots.Count > MaxScreenshots)
        {
            throw new ValidationException($"At most {MaxScreenshots} screenshots may be uploaded at once.");
        }
        foreach (var f in screenshots)
        {
            if (f.Length <= 0)
            {
                throw new ValidationException($"File '{f.FileName}' is empty.");
            }
            if (f.Length > MaxScreenshotBytes)
            {
                throw new ValidationException($"File '{f.FileName}' exceeds the {MaxScreenshotBytes / (1024 * 1024)} MB limit.");
            }
            if (!s_allowedImageContentTypes.Contains(f.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                throw new ValidationException($"File '{f.FileName}' has unsupported content type '{f.ContentType}'.");
            }
        }

        var project = await EnsureProjectAsync(projectId, cancellationToken);

        var tempDir = Path.Combine(Path.GetTempPath(), "claude-ticket-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var savedPaths = new List<string>(screenshots.Count);

        try
        {
            var index = 0;
            foreach (var screenshot in screenshots)
            {
                index++;
                var extension = Path.GetExtension(screenshot.FileName);
                if (string.IsNullOrEmpty(extension))
                {
                    extension = screenshot.ContentType switch
                    {
                        "image/png" => ".png",
                        "image/jpeg" or "image/jpg" => ".jpg",
                        "image/webp" => ".webp",
                        "image/gif" => ".gif",
                        _ => ".bin"
                    };
                }

                var safeName = $"screenshot-{index:00}{extension}";
                var savePath = Path.Combine(tempDir, safeName);
                await using var sink = File.Create(savePath);
                await screenshot.CopyToAsync(sink, cancellationToken);
                savedPaths.Add(savePath);
            }

            var prompt = BuildExtractionPrompt(savedPaths);

            logger.LogInformation(
                "Extracting ticket from {Count} screenshot(s) for project {ProjectId} (temp: {TempDir})",
                savedPaths.Count, projectId, tempDir);

            var result = await runner.RunAsync(prompt, project.WorkingDirectory, MessageKind.Chat, cancellationToken);

            if (result.ExitCode != 0)
            {
                logger.LogWarning("Claude exited with code {ExitCode} during ticket extraction", result.ExitCode);
                throw new UnprocessableException("Claude Code failed to process the screenshots.", result.Error);
            }

            if (!TryParseExtractedTicket(result.Response, out var extracted, out var parseError))
            {
                logger.LogWarning("Failed to parse Claude response: {ParseError}. Raw: {Raw}", parseError, result.Response);
                throw new UnprocessableException($"Could not parse ticket data from Claude's reply: {parseError}", result.Response);
            }

            return extracted;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete temp dir {TempDir}", tempDir);
            }
        }
    }

    private async Task<ClaudeProject> EnsureProjectAsync(Guid projectId, CancellationToken cancellationToken)
        => await data.GetProjectAsync(projectId, cancellationToken)
           ?? throw new NotFoundException($"No project found with id {projectId}.");

    private static string BuildExtractionPrompt(IReadOnlyList<string> screenshotPaths)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are extracting ticket information from screenshot(s) of a ticketing system");
        sb.AppendLine("such as Jira, Linear, GitHub Issues, Asana, or similar.");
        sb.AppendLine();
        sb.AppendLine("Use your Read tool to view each of these image files:");
        foreach (var path in screenshotPaths)
        {
            sb.Append("- ").AppendLine(path);
        }
        sb.AppendLine();
        sb.AppendLine("Reply with ONLY a single valid JSON object: no commentary, no prose, no markdown");
        sb.AppendLine("fences. The JSON must have exactly these three string fields:");
        sb.AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("  \"code\": \"the ticket identifier shown, e.g. PROJ-123 (empty string if none)\",");
        sb.AppendLine("  \"title\": \"the ticket title or summary\",");
        sb.AppendLine("  \"body\": \"the ticket description as markdown, preserving lists, headings, and code blocks where reasonable\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("If a field cannot be determined from the screenshots, set it to an empty string.");
        return sb.ToString();
    }

    private static bool TryParseExtractedTicket(string raw, out ExtractedTicket ticket, out string error)
    {
        ticket = new ExtractedTicket(string.Empty, string.Empty, string.Empty);
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "empty response";
            return false;
        }

        var json = ExtractJsonObject(raw);
        if (json is null)
        {
            error = "no JSON object found in reply";
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ExtractedTicket>(json, s_extractedTicketOptions);
            if (parsed is null)
            {
                error = "JSON deserialised to null";
                return false;
            }
            ticket = new ExtractedTicket(
                parsed.Code ?? string.Empty,
                parsed.Title ?? string.Empty,
                parsed.Body ?? string.Empty);
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string? ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        if (start < 0)
        {
            return null;
        }

        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = start; i < raw.Length; i++)
        {
            var ch = raw[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (ch == '\\' && inString)
            {
                escape = true;
                continue;
            }

            if (ch == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return raw.Substring(start, i - start + 1);
                }
            }
        }

        return null;
    }
}
