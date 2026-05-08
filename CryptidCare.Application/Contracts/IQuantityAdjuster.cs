namespace CryptidCare.Application.Contracts;

/// <summary>
/// Applies species-specific quantity adjustments after all reject rules succeed.
/// </summary>
public interface IQuantityAdjuster
{
    /// <summary>
    /// Updates <see cref="ClaimContext.EffectiveQuantity"/> based on business rules.
    /// </summary>
    /// <param name="context">Working claim context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AdjustAsync(ClaimContext context, CancellationToken cancellationToken);
}
