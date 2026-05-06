using CryptidCare.Claims.Domain.Enums;

namespace CryptidCare.Claims.Application.Contracts;

/// <summary>
/// Outcome of a single claim rule evaluation.
/// </summary>
/// <param name="IsSuccess">True if the rule allows the claim to continue.</param>
/// <param name="Reason">Optional human-readable detail when <paramref name="IsSuccess"/> is false.</param>
/// <param name="RejectionCode">Stable code when the rule fails; <see cref="ClaimRejectionCode.None"/> when successful.</param>
public record RuleResult(bool IsSuccess, string? Reason = null, ClaimRejectionCode RejectionCode = ClaimRejectionCode.None)
{
    /// <summary>Creates a successful rule result.</summary>
    public static RuleResult Success() => new(true, null, ClaimRejectionCode.None);

    /// <summary>Creates a failing rule result with localized-style detail and a stable rejection code.</summary>
    public static RuleResult Failure(string reason, ClaimRejectionCode rejectionCode) =>
        new(false, reason, rejectionCode);
}
