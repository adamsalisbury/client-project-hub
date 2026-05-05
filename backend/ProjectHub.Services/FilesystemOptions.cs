namespace ProjectHub.Services;

/// <summary>
/// Configuration for the filesystem path browser exposed at
/// <c>GET /api/filesystem/browse</c>.
/// </summary>
public sealed class FilesystemOptions
{
    public const string SectionName = "Filesystem";

    /// <summary>
    /// Absolute path the browser opens at by default and that the "home"
    /// breadcrumb returns to. When <see langword="null"/> or empty, falls
    /// back to the user's home directory (<c>$HOME</c> on Linux,
    /// <c>%USERPROFILE%</c> on Windows). Useful when running in a container
    /// that mounts the host's projects directory at a fixed path.
    /// </summary>
    public string? BrowseRoot { get; set; }
}
