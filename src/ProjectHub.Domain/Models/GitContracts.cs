namespace ProjectHub.Domain.Models;

/// <summary>
/// Response from <c>GET /api/projects/{id}/file/diff</c>.
/// </summary>
/// <param name="RelativePath">Path the diff was generated for.</param>
/// <param name="HasChanges">True when there are uncommitted changes for the file.</param>
/// <param name="IsUntracked">True when the file is untracked (no diff vs HEAD; whole content is "added").</param>
/// <param name="Diff">Unified diff text (may be empty when <see cref="HasChanges"/> is false).</param>
public sealed record FileDiffResponse(
    string RelativePath,
    bool HasChanges,
    bool IsUntracked,
    string Diff);

/// <summary>One commit entry returned for a file's history.</summary>
public sealed record FileCommitEntry(
    string Sha,
    string ShortSha,
    string Author,
    string Email,
    DateTimeOffset Date,
    string Subject);

/// <summary>
/// Response from <c>GET /api/projects/{id}/file/history</c>.
/// </summary>
public sealed record FileHistoryResponse(
    string RelativePath,
    IReadOnlyList<FileCommitEntry> Commits);
