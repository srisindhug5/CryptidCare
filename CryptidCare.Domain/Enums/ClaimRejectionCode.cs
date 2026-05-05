namespace CryptidCare.Claims.Domain.Enums;

/// <summary>
/// Stable machine-readable reason for a rejected claim. Clients should branch on this value, not on free-form rejection text.
/// </summary>
public enum ClaimRejectionCode
{
    /// <summary>No rejection (approved claims).</summary>
    None = 0,

    /// <summary>Requested quantity is not positive.</summary>
    InvalidQuantity,

    /// <summary>Patient id does not exist.</summary>
    PatientNotFound,

    /// <summary>Patient record exists but is not active.</summary>
    PatientInactive,

    /// <summary>Medicine id does not exist.</summary>
    MedicineNotFound,

    /// <summary>Werewolf patient cannot receive silver-containing medicine.</summary>
    WerewolfSilverMedicine,

    /// <summary>Hydra patient has an invalid head count for quantity rules.</summary>
    InvalidHydraHeadCount
}
