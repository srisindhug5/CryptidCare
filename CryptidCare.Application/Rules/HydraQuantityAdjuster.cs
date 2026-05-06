using CryptidCare.Claims.Application.Contracts;
using CryptidCare.Claims.Domain.Enums;

namespace CryptidCare.Claims.Application.Rules;

/// <summary>
/// Multiplies effective quantity by head count for Hydra patients.
/// </summary>
public class HydraQuantityAdjuster : IQuantityAdjuster
{
    /// <inheritdoc />
    public Task AdjustAsync(ClaimContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Patient);

        if (context.Patient.Species == Species.Hydra)
        {
            context.EffectiveQuantity = context.RequestedQuantity * context.Patient.HeadCount;
        }

        return Task.CompletedTask;
    }
}
