namespace CryptidCare.Claims.Application.Contracts;

/// <summary>
/// Pluggable adjudication rule. Implementations are registered in DI and run in order.
/// </summary>
public interface IClaimRule
{
    /// <summary>Stable name stored in the rule audit trail.</summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the claim in its current context. Failures stop the pipeline.
    /// </summary>
    /// <param name="context">Patient, medicine, and quantity context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pass or fail with optional reason.</returns>
    Task<RuleResult> EvaluateAsync(ClaimContext context, CancellationToken cancellationToken);
}
