using System.Text;
using ProjectHub.Domain.Models;

namespace ProjectHub.Services.Workers;

/// <summary>
/// Pure helper that turns a list of prior turns and a new user message into
/// the single prompt string handed to the Claude Code CLI.
/// </summary>
public static class ProjectPromptBuilder
{
    /// <summary>
    /// Builds a prompt with no project context. Equivalent to
    /// <see cref="Build(IReadOnlyList{ConversationTurn}, string, PromptContext)"/>
    /// with <see cref="PromptContext.Empty"/>.
    /// </summary>
    public static string Build(IReadOnlyList<ConversationTurn> priorTurns, string newMessage)
        => Build(priorTurns, newMessage, PromptContext.Empty, MessageKind.Chat);

    /// <summary>Backwards-compatible overload that defaults to <see cref="MessageKind.Chat"/>.</summary>
    public static string Build(IReadOnlyList<ConversationTurn> priorTurns, string newMessage, PromptContext context)
        => Build(priorTurns, newMessage, context, MessageKind.Chat);

    /// <summary>
    /// Builds the prompt to send to Claude, including any agent persona
    /// prefix, a project preamble, and prior conversation turns.
    /// </summary>
    /// <param name="priorTurns">
    /// Earlier user/assistant exchanges in this project, ordered oldest-first.
    /// May be empty.
    /// </param>
    /// <param name="newMessage">The new user message to respond to.</param>
    /// <param name="context">Project-level context to include before the history.</param>
    /// <param name="kind">
    /// Caller intent. When <see cref="MessageKind.Chat"/>, the prompt is
    /// prefixed with an instruction telling the AI to refuse file edits and
    /// direct the user to plan execution instead.
    /// </param>
    public static string Build(IReadOnlyList<ConversationTurn> priorTurns, string newMessage, PromptContext context, MessageKind kind)
    {
        ArgumentNullException.ThrowIfNull(priorTurns);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(newMessage);

        var hasAgents = context.Agents.Count > 0;
        var hasOtherContext = HasNonAgentContext(context);
        var injectChatInstruction = kind == MessageKind.Chat;
        if (!hasAgents && !hasOtherContext && !injectChatInstruction && priorTurns.Count == 0)
        {
            return newMessage;
        }

        var sb = new StringBuilder();

        if (injectChatInstruction)
        {
            AppendChatInstruction(sb, context.AiName);
        }

        if (hasAgents)
        {
            AppendAgents(sb, context.Agents);
        }

        if (hasOtherContext)
        {
            AppendContext(sb, context);
        }

        if (priorTurns.Count > 0)
        {
            sb.AppendLine("This is a continuing conversation. The prior turns are shown below.");
            sb.AppendLine();
            sb.AppendLine("--- conversation history ---");

            foreach (var turn in priorTurns)
            {
                sb.AppendLine();
                sb.Append("User: ").AppendLine(turn.Message);
                sb.AppendLine();
                sb.Append("Assistant: ").AppendLine(turn.Response);
            }

            sb.AppendLine();
            sb.AppendLine("--- end of conversation history ---");
            sb.AppendLine();
            sb.AppendLine("Now respond to this new user message, taking the prior turns as context:");
            sb.AppendLine();
        }

        sb.Append(newMessage);
        return sb.ToString();
    }

    private static void AppendChatInstruction(StringBuilder sb, string? aiName)
    {
        if (!string.IsNullOrWhiteSpace(aiName))
        {
            sb.Append("Your name is ").Append(aiName).AppendLine(".");
        }
        sb.AppendLine("This message is in chat mode. You must not modify, create, or delete any files in this conversation.");
        sb.AppendLine("If the user asks you to make code or file changes, refuse and explain that all code modification");
        sb.AppendLine("happens through plan execution - they should add a plan step on the Plan tab and run it from there.");
        sb.AppendLine();
    }

    private static bool HasNonAgentContext(PromptContext context)
    {
        return (context.IncludeProjectInfo && (!string.IsNullOrWhiteSpace(context.ProjectName) || !string.IsNullOrWhiteSpace(context.WorkingDirectory)))
            || (context.IncludeClientInfo && !string.IsNullOrWhiteSpace(context.ClientName))
            || context.ClientKnowledge.Count > 0
            || context.ProjectKnowledge.Count > 0
            || context.Tickets.Count > 0;
    }

    private static void AppendAgents(StringBuilder sb, IReadOnlyList<Agent> agents)
    {
        for (var i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            sb.Append("You are a ").Append(agent.Title).Append(", your characteristics are:");
            sb.AppendLine();
            sb.AppendLine(agent.Characteristics);
            sb.AppendLine();
        }
    }

    private static void AppendContext(StringBuilder sb, PromptContext context)
    {
        sb.AppendLine("--- project context ---");
        sb.AppendLine();

        if (context.IncludeProjectInfo)
        {
            if (!string.IsNullOrWhiteSpace(context.ProjectName))
            {
                sb.Append("Project name: ").AppendLine(context.ProjectName);
            }
            if (!string.IsNullOrWhiteSpace(context.WorkingDirectory))
            {
                sb.Append("Working directory: ").AppendLine(context.WorkingDirectory);
            }
        }
        if (context.IncludeClientInfo && !string.IsNullOrWhiteSpace(context.ClientName))
        {
            sb.Append("Client: ").AppendLine(context.ClientName);
        }
        sb.AppendLine();

        if (context.ClientKnowledge.Count > 0)
        {
            sb.AppendLine("--- client knowledge ---");
            foreach (var entry in context.ClientKnowledge)
            {
                sb.AppendLine();
                sb.Append("### ").AppendLine(entry.Title);
                sb.AppendLine(entry.Body);
            }
            sb.AppendLine();
            sb.AppendLine("--- end of client knowledge ---");
            sb.AppendLine();
        }

        if (context.ProjectKnowledge.Count > 0)
        {
            sb.AppendLine("--- project knowledge ---");
            foreach (var entry in context.ProjectKnowledge)
            {
                sb.AppendLine();
                sb.Append("### ").AppendLine(entry.Title);
                sb.AppendLine(entry.Body);
            }
            sb.AppendLine();
            sb.AppendLine("--- end of project knowledge ---");
            sb.AppendLine();
        }

        if (context.Tickets.Count > 0)
        {
            sb.AppendLine("--- tickets ---");
            foreach (var ticket in context.Tickets)
            {
                sb.AppendLine();
                sb.Append("Ticket ").Append(ticket.Code).Append(": ").AppendLine(ticket.Title);
                sb.AppendLine(ticket.Body);
            }
            sb.AppendLine();
            sb.AppendLine("--- end of tickets ---");
            sb.AppendLine();
        }

        sb.AppendLine("--- end of project context ---");
        sb.AppendLine();
    }
}
