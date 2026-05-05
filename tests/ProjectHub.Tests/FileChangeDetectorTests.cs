using ProjectHub.Services.Workers;

namespace ProjectHub.Tests;

public sealed class FileChangeDetectorTests : IDisposable
{
    private readonly string _root;
    private readonly FileChangeDetector _detector = new();

    public FileChangeDetectorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "FileChangeDetectorTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Snapshot_OfEmptyDirectory_IsEmpty()
    {
        var snapshot = _detector.Snapshot(_root);

        Assert.Empty(snapshot);
    }

    [Fact]
    public void Snapshot_ReturnsRelativePathsWithForwardSlashes()
    {
        var subdir = Path.Combine(_root, "src", "lib");
        Directory.CreateDirectory(subdir);
        File.WriteAllText(Path.Combine(subdir, "thing.cs"), "x");

        var snapshot = _detector.Snapshot(_root);

        Assert.Single(snapshot);
        Assert.Contains("src/lib/thing.cs", snapshot.Keys);
    }

    [Fact]
    public void Snapshot_SkipsNoiseDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".git"));
        File.WriteAllText(Path.Combine(_root, ".git", "HEAD"), "ref: refs/heads/main");

        Directory.CreateDirectory(Path.Combine(_root, "node_modules", "react"));
        File.WriteAllText(Path.Combine(_root, "node_modules", "react", "package.json"), "{}");

        Directory.CreateDirectory(Path.Combine(_root, "bin"));
        File.WriteAllText(Path.Combine(_root, "bin", "out.dll"), "binary");

        File.WriteAllText(Path.Combine(_root, "real-source.cs"), "code");

        var snapshot = _detector.Snapshot(_root);

        Assert.Single(snapshot);
        Assert.Contains("real-source.cs", snapshot.Keys);
    }

    [Fact]
    public void Snapshot_OfMissingDirectory_IsEmpty()
    {
        var snapshot = _detector.Snapshot(Path.Combine(_root, "nope"));

        Assert.Empty(snapshot);
    }

    [Fact]
    public void Diff_NoChanges_ReturnsEmpty()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "one");
        var before = _detector.Snapshot(_root);
        var after = _detector.Snapshot(_root);

        var diff = _detector.Diff(before, after);

        Assert.Empty(diff);
    }

    [Fact]
    public void Diff_DetectsAddedFile()
    {
        var before = _detector.Snapshot(_root);
        File.WriteAllText(Path.Combine(_root, "added.txt"), "x");
        var after = _detector.Snapshot(_root);

        var diff = _detector.Diff(before, after);

        Assert.Single(diff);
        Assert.Equal("added.txt", diff[0]);
    }

    [Fact]
    public void Diff_DetectsModifiedFile()
    {
        var path = Path.Combine(_root, "modify.txt");
        File.WriteAllText(path, "first");
        var before = _detector.Snapshot(_root);

        // bump mtime explicitly so the test is deterministic regardless of FS resolution.
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(2));
        File.WriteAllText(path, "second");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(2));

        var after = _detector.Snapshot(_root);

        var diff = _detector.Diff(before, after);

        Assert.Single(diff);
        Assert.Equal("modify.txt", diff[0]);
    }

    [Fact]
    public void Diff_DetectsDeletedFile()
    {
        var path = Path.Combine(_root, "delete-me.txt");
        File.WriteAllText(path, "x");
        var before = _detector.Snapshot(_root);

        File.Delete(path);
        var after = _detector.Snapshot(_root);

        var diff = _detector.Diff(before, after);

        Assert.Single(diff);
        Assert.Equal("delete-me.txt", diff[0]);
    }

    [Fact]
    public void Diff_DetectsCombinationOfChanges()
    {
        File.WriteAllText(Path.Combine(_root, "stays.txt"), "same");
        File.WriteAllText(Path.Combine(_root, "to-delete.txt"), "bye");
        var before = _detector.Snapshot(_root);

        File.WriteAllText(Path.Combine(_root, "added.txt"), "hi");
        File.Delete(Path.Combine(_root, "to-delete.txt"));
        var after = _detector.Snapshot(_root);

        var diff = _detector.Diff(before, after);

        Assert.Equal(2, diff.Count);
        Assert.Contains("added.txt", diff);
        Assert.Contains("to-delete.txt", diff);
        Assert.DoesNotContain("stays.txt", diff);
    }

    [Fact]
    public void Diff_ResultIsOrdinallySorted()
    {
        File.WriteAllText(Path.Combine(_root, "z.txt"), "1");
        File.WriteAllText(Path.Combine(_root, "a.txt"), "1");
        File.WriteAllText(Path.Combine(_root, "m.txt"), "1");

        var before = _detector.Snapshot(_root);
        // Modify all three to count as changes
        var now = DateTime.UtcNow.AddSeconds(2);
        File.SetLastWriteTimeUtc(Path.Combine(_root, "z.txt"), now);
        File.SetLastWriteTimeUtc(Path.Combine(_root, "a.txt"), now);
        File.SetLastWriteTimeUtc(Path.Combine(_root, "m.txt"), now);
        var after = _detector.Snapshot(_root);

        var diff = _detector.Diff(before, after);

        Assert.Equal(new[] { "a.txt", "m.txt", "z.txt" }, diff);
    }
}
