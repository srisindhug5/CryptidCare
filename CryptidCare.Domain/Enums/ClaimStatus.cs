namespace CryptidCare.Claims.Domain.Enums;

/// <summary>
/// Lifecycle state of a claim after adjudication.
/// </summary>
public enum ClaimStatus
{
    /// <summary>Transient state before persistence finalizes (not typically returned to clients).</summary>
    Pending = 0,

    /// <summary>All rules passed and cost was calculated.</summary>
    Approved = 1,

    /// <summary>Validation or a rule failed; see rejection reason on the claim.</summary>
    Rejected = 2
}
