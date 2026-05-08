using CryptidCare.Domain.Entities;

namespace CryptidCare.Application.Contracts;

/// <summary>
/// Mutable working state passed through the rule pipeline and quantity adjusters.
/// </summary>
public class ClaimContext
{
    /// <summary>Patient associated with the claim.</summary>
    public required Patient Patient { get; init; }

    /// <summary>Medicine associated with the claim.</summary>
    public required Medicine Medicine { get; init; }

    /// <summary>Original requested quantity from the submission.</summary>
    public required int RequestedQuantity { get; init; }

    /// <summary>Quantity after adjusters; starts equal to <see cref="RequestedQuantity"/>.</summary>
    public int EffectiveQuantity { get; set; }
}
