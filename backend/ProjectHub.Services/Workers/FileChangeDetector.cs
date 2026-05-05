namespace ProjectHub.Services.Workers;

/// <summary>
/// Pure helper that snapshots a directory tree by (relative path → mtime)
/// and computes the set of files added, modified, or deleted between two
/// snapshots. Used by the worker to surface which files Claude touched
/// during a run.
/// </summary>
public sealed class FileChangeDetector
{
    /// <summary>
    /// Directory names that are skipped during snapshotting. These are
    /// either VCS metadata, package manager caches, or build outputs that
    /// produce noisy churn unrelated to user-visible source changes.
    /// </summary>
    private static readonly HashSet<string> s_skipDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".hg",
        ".svn",
        "node_modules",
        "bin",
        "obj",
        "target",
        "dist",
        "build",
        ".next",
        ".cache",
        ".idea",
        ".vs",
        ".vscode",
        "__pycache__"
    };

    /// <summary>
    /// Walks <paramref name="root"/> and returns a map of relative paths
    /// to last-write timestamps. Skipped directories (see
    /// <see cref="s_skipDirectories"/>) are not descended into.
    /// </summary>
    public IReadOnlyDictionary<string, DateTime> Snapshot(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        var snapshot = new Dictionary<string, DateTime>(StringComparer.Ordinal);

        if (!Directory.Exists(root))
        {
            return snapshot;
        }

        var rootFull = Path.GetFullPath(root);
        Walk(rootFull, rootFull, snapshot);
        return snapshot;
    }

    /// <summary>
    /// Returns the relative paths of files that differ between the two
    /// snapshots - added, modified, or deleted - sorted ordinally.
    /// </summary>
    public IReadOnlyList<string> Diff(
        IReadOnlyDictionary<string, DateTime> before,
        IReadOnlyDictionary<string, DateTime> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var changes = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var (path, mtime) in after)
        {
            if (!before.TryGetValue(path, out var prev) || prev != mtime)
            {
                changes.Add(path);
            }
        }

        foreach (var path in before.Keys)
        {
            if (!after.ContainsKey(path))
            {
                changes.Add(path);
            }
        }

        return changes.ToList();
    }

    private static void Walk(string current, string root, Dictionary<string, DateTime> acc)
    {
        IEnumerable<string> files;
        IEnumerable<string> directories;

        try
        {
            files = Directory.EnumerateFiles(current);
            directories = Directory.EnumerateDirectories(current);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        foreach (var file in files)
        {
            try
            {
                var info = new FileInfo(file);
                var relative = Path.GetRelativePath(root, file);
                acc[NormalisePath(relative)] = info.LastWriteTimeUtc;
            }
            catch (IOException)
            {
                // racing with Claude touching a file - skip and move on.
            }
        }

        foreach (var directory in directories)
        {
            var name = Path.GetFileName(directory);
            if (string.IsNullOrEmpty(name) || s_skipDirectories.Contains(name))
            {
                continue;
            }

            Walk(directory, root, acc);
        }
    }

    private static string NormalisePath(string path)
        => path.Replace(Path.DirectorySeparatorChar, '/');
}
