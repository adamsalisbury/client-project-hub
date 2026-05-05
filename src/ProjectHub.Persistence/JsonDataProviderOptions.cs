namespace ProjectHub.Persistence;

/// <summary>
/// Configuration for <see cref="JsonClaudeJobDataProvider"/>.
/// </summary>
public sealed class JsonDataProviderOptions
{
    public const string SectionName = "JsonDataProvider";

    /// <summary>
    /// Path to the JSON file used to persist jobs. Resolved relative to the
    /// content root if not absolute.
    /// </summary>
    public string FilePath { get; init; } = "data/jobs.json";
}
