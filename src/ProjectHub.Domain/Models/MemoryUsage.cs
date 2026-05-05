namespace ProjectHub.Domain.Models;

/// <summary>
/// Approximate breakdown of how many bytes the project would contribute to a
/// Claude prompt: conversation history + tickets + project knowledge +
/// client knowledge + the project preamble. Compared against a soft
/// <see cref="MaxBytes"/> ceiling so the UI can show a "memory" bar.
/// </summary>
public sealed record MemoryUsageResponse(
    long ConversationBytes,
    long TicketBytes,
    long ProjectKnowledgeBytes,
    long ClientKnowledgeBytes,
    long AgentBytes,
    long ProjectInfoBytes,
    long TotalBytes,
    long MaxBytes);
