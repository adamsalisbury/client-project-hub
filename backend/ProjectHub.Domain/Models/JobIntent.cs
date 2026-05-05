namespace ProjectHub.Domain.Models;

/// <summary>
/// Why a Claude job was queued. Orthogonal to <see cref="MessageKind"/>,
/// which decides which CLI tools the runner is permitted to use.
/// </summary>
public enum JobIntent
{
    /// <summary>A user-initiated chat or edit message in the project chat.</summary>
    Conversation,

    /// <summary>
    /// A request to verify the current plan. The CLI runs in chat-mode
    /// (read-only); the response is stored on the plan and surfaces in chat
    /// as a "Plan Verification Opinion" speech bubble.
    /// </summary>
    PlanVerification,

    /// <summary>
    /// Execution of a single plan step. The CLI runs in edit-mode; the
    /// worker captures the changed files into a <see cref="StepReview"/>
    /// row when the job finishes.
    /// </summary>
    PlanStep
}
