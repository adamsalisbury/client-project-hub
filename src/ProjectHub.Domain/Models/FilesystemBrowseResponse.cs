namespace ProjectHub.Domain.Models;

/// <summary>
/// One entry in a directory listing.
/// </summary>
public sealed record FilesystemEntry(string Name, string Path, bool IsDirectory);

/// <summary>
/// Response from <c>GET /api/filesystem/browse</c>.
/// </summary>
/// <param name="Path">Absolute path of the directory that was listed.</param>
/// <param name="ParentPath">Absolute path of the parent directory, or <see langword="null"/> for the filesystem root.</param>
/// <param name="HomePath">Absolute path of the API host's user-home directory, for quick navigation.</param>
/// <param name="Entries">
/// Entries inside <paramref name="Path"/>, with directories listed first and ordered by name.
/// </param>
public sealed record FilesystemBrowseResponse(
    string Path,
    string? ParentPath,
    string HomePath,
    IReadOnlyList<FilesystemEntry> Entries);
