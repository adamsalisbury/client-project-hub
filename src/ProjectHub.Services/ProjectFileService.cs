using System.Text;
using ProjectHub.Domain.Models;
using ProjectHub.Services.Storage;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class ProjectFileService(IClaudeDataProvider data, ILogger<ProjectFileService> logger) : IProjectFileService
{
    private const long MaxFileSizeBytes = 1024 * 1024;
    private const int BinaryProbeBytes = 8 * 1024;

    /// <inheritdoc/>
    public async Task<ProjectFileListing> ListAsync(Guid projectId, string? path, CancellationToken cancellationToken)
    {
        var project = await data.GetProjectAsync(projectId, cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");

        if (!ProjectPathResolver.TryResolve(project.WorkingDirectory, path, out var resolved, out var error))
        {
            throw new ValidationException(error);
        }
        if (!Directory.Exists(resolved))
        {
            throw new NotFoundException($"Directory '{path ?? string.Empty}' does not exist within the project.");
        }

        IEnumerable<string> directories;
        IEnumerable<string> files;
        try
        {
            directories = Directory.EnumerateDirectories(resolved);
            files = Directory.EnumerateFiles(resolved);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnprocessableException("Access denied.");
        }

        var entries = new List<ProjectFileEntry>();

        foreach (var dir in directories.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(dir);
            if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            entries.Add(new ProjectFileEntry(
                Name: name,
                RelativePath: ProjectPathResolver.ToRelative(project.WorkingDirectory, dir),
                IsDirectory: true,
                Size: null));
        }

        foreach (var file in files.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(file);
            long? size = null;
            try
            {
                size = new FileInfo(file).Length;
            }
            catch (IOException) { /* skip */ }

            entries.Add(new ProjectFileEntry(
                Name: name,
                RelativePath: ProjectPathResolver.ToRelative(project.WorkingDirectory, file),
                IsDirectory: false,
                Size: size));
        }

        var relative = ProjectPathResolver.ToRelative(project.WorkingDirectory, resolved);
        var parent = relative.Length == 0
            ? null
            : ProjectPathResolver.ToRelative(project.WorkingDirectory, Path.GetDirectoryName(resolved)!);

        return new ProjectFileListing(relative, parent, entries);
    }

    /// <inheritdoc/>
    public async Task<ProjectFileContent> ReadAsync(Guid projectId, string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ValidationException("The 'path' query parameter is required.");
        }

        var project = await data.GetProjectAsync(projectId, cancellationToken)
            ?? throw new NotFoundException($"No project found with id {projectId}.");

        if (!ProjectPathResolver.TryResolve(project.WorkingDirectory, path, out var resolved, out var error))
        {
            throw new ValidationException(error);
        }

        if (!File.Exists(resolved))
        {
            throw new NotFoundException($"File '{path}' does not exist within the project.");
        }

        long size;
        try
        {
            size = new FileInfo(resolved).Length;
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Failed to stat {Path}", resolved);
            throw new UnprocessableException(ex.Message);
        }

        var relative = ProjectPathResolver.ToRelative(project.WorkingDirectory, resolved);

        if (size > MaxFileSizeBytes)
        {
            return new ProjectFileContent(relative, size, IsBinary: false, Truncated: true, Content: null);
        }

        byte[] bytes;
        try
        {
            bytes = await File.ReadAllBytesAsync(resolved, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnprocessableException("Access denied.");
        }

        if (LooksBinary(bytes))
        {
            return new ProjectFileContent(relative, size, IsBinary: true, Truncated: false, Content: null);
        }

        var content = Encoding.UTF8.GetString(bytes);
        return new ProjectFileContent(relative, size, IsBinary: false, Truncated: false, content);
    }

    private static bool LooksBinary(byte[] bytes)
    {
        var probe = Math.Min(bytes.Length, BinaryProbeBytes);
        for (var i = 0; i < probe; i++)
        {
            if (bytes[i] == 0)
            {
                return true;
            }
        }
        return false;
    }
}
