namespace ProjectHub.Domain.Models;

/// <summary>Lifecycle of a single plan step.</summary>
public enum PlanStepStatus
{
    /// <summary>Authored but never executed.</summary>
    Pending,

    /// <summary>Currently being executed by Claude.</summary>
    Running,

    /// <summary>Execution finished; the user has not yet decided commit / rollback.</summary>
    AwaitingReview,

    /// <summary>Changed files were committed to git.</summary>
    Committed,

    /// <summary>Changed files were rolled back to their last committed state.</summary>
    RolledBack,

    /// <summary>Execution failed (non-zero exit, error, or worker exception).</summary>
    Failed
}
