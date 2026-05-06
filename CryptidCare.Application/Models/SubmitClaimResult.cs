using CryptidCare.Claims.Domain.Enums;

namespace CryptidCare.Claims.Application.Models;

/// <summary>
/// Outcome returned after adjudication and persistence.
/// </summary>
/// <param name="ClaimId">Identifier of the stored claim.</param>
/// <param name="Status">Approved or rejected.</param>
/// <param name="EffectiveQuantity">Quantity after adjustments.</param>
/// <param name="TotalCost">Payable amount when approved; zero when rejected.</param>
/// <param name="Reason">Rejection explanation when <paramref name="Status"/> is rejected.</param>
/// <param name="RejectionCode">Stable machine-readable code when rejected; null when approved.</param>
public record SubmitClaimResult(
    Guid ClaimId,
    ClaimStatus Status,
    int EffectiveQuantity,
    decimal TotalCost,
    string? Reason = null,
    ClaimRejectionCode? RejectionCode = null);
