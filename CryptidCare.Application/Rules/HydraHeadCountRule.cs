using CryptidCare.Claims.Application.Contracts;
using CryptidCare.Claims.Domain.Enums;

namespace CryptidCare.Claims.Application.Rules;

/// <summary>
/// Ensures Hydra patients have a valid head count before quantity adjustment.
/// </summary>
public class HydraHeadCountRule : IClaimRule
{
    /// <inheritdoc />
    public string Name => nameof(HydraHeadCountRule);

    /// <inheritdoc />
    public Task<RuleResult> EvaluateAsync(ClaimContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Patient);

        if (context.Patient.Species == Species.Hydra && context.Patient.HeadCount < 1)
        {
            return Task.FromResult(RuleResult.Failure(
                "Hydra patients must have at least one head.",
                ClaimRejectionCode.InvalidHydraHeadCount));
        }

        return Task.FromResult(RuleResult.Success());
    }
}
