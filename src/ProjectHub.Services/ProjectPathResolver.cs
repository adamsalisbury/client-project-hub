namespace ProjectHub.Services;

/// <summary>
/// Helpers for safely resolving relative paths supplied by API callers
/// against a project's working directory. Rejects rooted paths and any path
/// that would escape the project root.
/// </summary>
public static class ProjectPathResolver
{
    public static bool TryResolve(string projectRoot, string? requested, out string resolved, out string error)
    {
        resolved = string.Empty;
        error = string.Empty;

        var rootFull = Path.GetFullPath(projectRoot);
        var rootWithSep = rootFull.EndsWith(Path.DirectorySeparatorChar) ? rootFull : rootFull + Path.DirectorySeparatorChar;

        var requestedNormalised = (requested ?? string.Empty).Trim();
        if (Path.IsPathRooted(requestedNormalised))
        {
            error = "Path must be relative to the project root.";
            return false;
        }

        var combined = string.IsNullOrEmpty(requestedNormalised)
            ? rootFull
            : Path.Combine(rootFull, requestedNormalised);

        string full;
        try
        {
            full = Path.GetFullPath(combined);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = $"Invalid path: {ex.Message}";
            return false;
        }

        if (full != rootFull && !full.StartsWith(rootWithSep, StringComparison.Ordinal))
        {
            error = "Path escapes the project root.";
            return false;
        }

        resolved = full;
        return true;
    }

    public static string ToRelative(string root, string absolute)
    {
        var relative = Path.GetRelativePath(root, absolute);
        if (relative == ".")
        {
            return string.Empty;
        }
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }
}
