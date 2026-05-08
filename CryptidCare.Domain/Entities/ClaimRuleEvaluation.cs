using CryptidCare.Domain.Enums;

namespace CryptidCare.Domain.Entities;

/// <summary>
/// One evaluation record for a rule run during claim adjudication (audit trail).
/// </summary>
public class ClaimRuleEvaluation
{
    /// <summary>Primary key for this evaluation row.</summary>
    public Guid Id { get; set; }

    /// <summary>Claim this evaluation belongs to.</summary>
    public Guid ClaimId { get; set; }

    /// <summary>Rule identifier, typically the rule type name.</summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>Whether the rule passed for this claim.</summary>
    public bool Passed { get; set; }

    /// <summary>Optional detail when the rule fails or adds context.</summary>
    public string? Reason { get; set; }

    /// <summary>Stable rejection code when <see cref="Passed"/> is false; null when the rule passed.</summary>
    public ClaimRejectionCode? RejectionCode { get; set; }

    /// <summary>UTC time the rule was evaluated (set by database default).</summary>
    public DateTime EvaluatedAtUtc { get; set; }

    /// <summary>Navigation to parent claim when loaded.</summary>
    public Claim? Claim { get; set; }
}
