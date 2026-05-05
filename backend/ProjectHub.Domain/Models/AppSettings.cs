namespace ProjectHub.Domain.Models;

/// <summary>
/// Persisted application-wide settings. There is exactly one of these in the
/// store; the persistence layer creates an empty instance on first load.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Display name the AI should use when introducing itself in chat. The
    /// prompt builder includes this in chat-mode messages so the AI replies
    /// as the operator's chosen identity.
    /// </summary>
    public string? AiName { get; set; }
}

/// <summary>Wire format for <c>GET/PUT /api/settings</c>.</summary>
public sealed record AppSettingsResponse(string? AiName)
{
    public static AppSettingsResponse FromSettings(AppSettings settings)
        => new(settings.AiName);
}

/// <summary>Request body for <c>PUT /api/settings</c>.</summary>
public sealed record UpdateAppSettingsRequest(string? AiName);
