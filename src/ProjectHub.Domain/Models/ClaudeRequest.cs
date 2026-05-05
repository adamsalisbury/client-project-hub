namespace ProjectHub.Domain.Models;

/// <summary>
/// A new message dispatched to Claude Code as part of a project. The full
/// history of the project is replayed before each request, so each message is
/// processed in the context of the prior conversation.
/// </summary>
/// <param name="ProjectId">Identifier of the project this message belongs to.</param>
/// <param name="Message">The user message to run through Claude Code.</param>
/// <param name="Kind">
/// Intent of the message - <see cref="MessageKind.Chat"/> by default. Chat
/// messages are run with mutating tools disallowed; Edit messages are run
/// with full tool access.
/// </param>
public sealed record ClaudeRequest(Guid ProjectId, string Message, MessageKind Kind = MessageKind.Chat);
