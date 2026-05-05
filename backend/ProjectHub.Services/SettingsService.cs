using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class SettingsService(IClaudeDataProvider data, ILogger<SettingsService> logger) : ISettingsService
{
    private const int MaxAiNameLength = 80;

    /// <inheritdoc/>
    public Task<AppSettings> GetAsync(CancellationToken cancellationToken)
        => data.GetSettingsAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<AppSettings> UpdateAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var trimmed = settings.AiName?.Trim();
        if (!string.IsNullOrEmpty(trimmed) && trimmed.Length > MaxAiNameLength)
        {
            throw new ValidationException($"AI name must be {MaxAiNameLength} characters or fewer.");
        }

        var sanitized = new AppSettings
        {
            AiName = string.IsNullOrEmpty(trimmed) ? null : trimmed
        };

        var saved = await data.UpdateSettingsAsync(sanitized, cancellationToken);
        logger.LogInformation("Settings updated (AiName: '{AiName}')", saved.AiName ?? "<none>");
        return saved;
    }
}
