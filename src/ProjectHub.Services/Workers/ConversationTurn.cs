namespace ProjectHub.Services.Workers;

/// <summary>
/// One completed user/assistant exchange used as context for a follow-up
/// message in the same project.
/// </summary>
/// <param name="Message">The user message.</param>
/// <param name="Response">The assistant response that followed it.</param>
public sealed record ConversationTurn(string Message, string Response);
