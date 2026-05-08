using CryptidCare.Application.Models;

namespace CryptidCare.Application.Contracts;

/// <summary>
/// Orchestrates validation, rules, adjustments, pricing, and persistence for a claim submission.
/// </summary>
public interface IClaimAdjudicationService
{
    /// <summary>
    /// Submits a claim, persists the outcome, and returns the adjudication result.
    /// </summary>
    /// <param name="request">Patient, medicine, and quantity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Final status, quantities, cost, and rejection reason if applicable.</returns>
    Task<SubmitClaimResult> SubmitAsync(SubmitClaimRequest request, CancellationToken cancellationToken);
}
