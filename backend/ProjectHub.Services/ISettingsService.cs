using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Application-wide settings.</summary>
public interface ISettingsService
{
    /// <summary>Returns the current settings (defaults applied).</summary>
    Task<AppSettings> GetAsync(CancellationToken cancellationToken);

    /// <summary>Replaces the current settings.</summary>
    Task<AppSettings> UpdateAsync(AppSettings settings, CancellationToken cancellationToken);
}
