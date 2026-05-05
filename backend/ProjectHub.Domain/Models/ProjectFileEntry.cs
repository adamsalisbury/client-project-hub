namespace ProjectHub.Domain.Models;

/// <summary>
/// One entry inside a project's working directory.
/// </summary>
public sealed record ProjectFileEntry(string Name, string RelativePath, bool IsDirectory, long? Size);

/// <summary>
/// Response from <c>GET /api/projects/{id}/files</c>.
/// </summary>
/// <param name="RelativePath">Path that was listed, relative to the project root (empty string for root).</param>
/// <param name="ParentRelativePath">Relative path of the parent, or <see langword="null"/> at the root.</param>
/// <param name="Entries">Entries at the listed path, directories first then files, each ordered by name.</param>
public sealed record ProjectFileListing(
    string RelativePath,
    string? ParentRelativePath,
    IReadOnlyList<ProjectFileEntry> Entries);

/// <summary>
/// Response from <c>GET /api/projects/{id}/file</c>.
/// </summary>
/// <param name="RelativePath">Path that was read, relative to the project root.</param>
/// <param name="Size">File size in bytes.</param>
/// <param name="IsBinary">True when the file looks binary (NUL byte detected); <see cref="Content"/> is null in that case.</param>
/// <param name="Truncated">True when the file exceeded the size cap and was not returned.</param>
/// <param name="Content">UTF-8 file content; null if binary or truncated.</param>
public sealed record ProjectFileContent(
    string RelativePath,
    long Size,
    bool IsBinary,
    bool Truncated,
    string? Content);
