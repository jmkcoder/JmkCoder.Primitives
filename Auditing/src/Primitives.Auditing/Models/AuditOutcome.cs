namespace Primitives.Auditing.Models;

/// <summary>Outcome of an audited action.</summary>
public enum AuditOutcome
{
    /// <summary>The action completed successfully.</summary>
    Success,

    /// <summary>The action was rejected due to insufficient permissions.</summary>
    Denied,

    /// <summary>The action failed due to an error.</summary>
    Failure,
}
