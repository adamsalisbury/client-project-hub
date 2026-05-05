using ProjectHub.Domain.Models;

namespace ProjectHub.Services;

/// <summary>Read-only filesystem browser for picking working directories.</summary>
public interface IFilesystemService
{
    FilesystemBrowseResponse Browse(string? path);
}
