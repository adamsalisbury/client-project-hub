using Microsoft.Extensions.Options;
using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <inheritdoc/>
public sealed class FilesystemService(
    IOptions<FilesystemOptions> options,
    ILogger<FilesystemService> logger) : IFilesystemService
{
    private readonly FilesystemOptions _options = options?.Value ?? new FilesystemOptions();

    /// <inheritdoc/>
    public FilesystemBrowseResponse Browse(string? path)
    {
        var home = ResolveHome();

        string requested;
        if (string.IsNullOrWhiteSpace(path) || path == "~")
        {
            requested = home;
        }
        else if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            requested = Path.Combine(home, path[2..]);
        }
        else
        {
            requested = path;
        }

        string resolved;
        try
        {
            resolved = Path.GetFullPath(requested);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ValidationException($"'{path}' is not a valid path.");
        }

        if (!Directory.Exists(resolved))
        {
            throw new NotFoundException($"Directory '{resolved}' does not exist.");
        }

        IEnumerable<string> directories;
        IEnumerable<string> files;
        try
        {
            directories = Directory.EnumerateDirectories(resolved);
            files = Directory.EnumerateFiles(resolved);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogInformation(ex, "Access denied while listing {Path}", resolved);
            throw new UnprocessableException($"Access denied to '{resolved}'.");
        }

        var entries = new List<FilesystemEntry>();

        foreach (var dir in directories.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }
            entries.Add(new FilesystemEntry(name, dir, IsDirectory: true));
        }

        foreach (var file in files.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(file);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }
            entries.Add(new FilesystemEntry(name, file, IsDirectory: false));
        }

        var parent = Directory.GetParent(resolved)?.FullName;

        return new FilesystemBrowseResponse(
            Path: resolved,
            ParentPath: parent,
            HomePath: home,
            Entries: entries);
    }

    private string ResolveHome()
    {
        if (!string.IsNullOrWhiteSpace(_options.BrowseRoot) && Directory.Exists(_options.BrowseRoot))
        {
            return Path.GetFullPath(_options.BrowseRoot);
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
        {
            home = Path.GetTempPath();
        }
        return home;
    }
}
