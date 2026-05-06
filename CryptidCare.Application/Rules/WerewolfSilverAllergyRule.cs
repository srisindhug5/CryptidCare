using CryptidCare.Claims.Application.Contracts;
using CryptidCare.Claims.Domain.Enums;

namespace CryptidCare.Claims.Application.Rules;

/// <summary>
/// Rejects claims where a werewolf patient would receive silver-containing medicine.
/// </summary>
public class WerewolfSilverAllergyRule : IClaimRule
{
    /// <inheritdoc />
    public string Name => nameof(WerewolfSilverAllergyRule);

    /// <inheritdoc />
    public Task<RuleResult> EvaluateAsync(ClaimContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Patient);
        ArgumentNullException.ThrowIfNull(context.Medicine);

        if (context.Patient.Species == Species.Werewolf && context.Medicine.ContainsSilver)
        {
            return Task.FromResult(RuleResult.Failure(
                "Werewolves cannot receive medicine containing silver.",
                ClaimRejectionCode.WerewolfSilverMedicine));
        }

        return Task.FromResult(RuleResult.Success());
    }
}
