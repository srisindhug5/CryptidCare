using CryptidCare.Domain.Enums;

namespace CryptidCare.Domain.Entities;

/// <summary>
/// A pharmacy claim for a patient and medicine, with adjudication outcome and cost.
/// </summary>
public class Claim
{
    /// <summary>Unique claim identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Patient receiving the prescription.</summary>
    public Guid PatientId { get; set; }

    /// <summary>Medicine being claimed.</summary>
    public Guid MedicineId { get; set; }

    /// <summary>Quantity requested by the pharmacy before species adjustments.</summary>
    public int RequestedQuantity { get; set; }

    /// <summary>Quantity after adjustments (e.g. Hydra head multiplier).</summary>
    public int EffectiveQuantity { get; set; }

    /// <summary>Final adjudication state.</summary>
    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

    /// <summary>Total payable amount when approved; zero when rejected.</summary>
    public decimal TotalCost { get; set; }

    /// <summary>Human-readable rejection explanation when <see cref="Status"/> is rejected.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Stable rejection code when <see cref="Status"/> is rejected; null when approved.</summary>
    public ClaimRejectionCode? RejectionCode { get; set; }

    /// <summary>UTC timestamp when the claim was created (set by database default).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Navigation to patient when loaded.</summary>
    public Patient? Patient { get; set; }

    /// <summary>Navigation to medicine when loaded.</summary>
    public Medicine? Medicine { get; set; }

    /// <summary>Per-rule outcomes for explainability and audit.</summary>
    public List<ClaimRuleEvaluation> RuleEvaluations { get; set; } = [];
}
