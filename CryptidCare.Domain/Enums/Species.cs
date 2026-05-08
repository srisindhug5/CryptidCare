namespace CryptidCare.Domain.Enums;

/// <summary>
/// Mythical species; drives eligibility and pricing adjustments.
/// </summary>
public enum Species
{
    /// <summary>Unspecified or unknown species.</summary>
    Unknown = 0,

    /// <summary>Subject to silver-containing medicine restrictions.</summary>
    Werewolf = 1,

    /// <summary>Quantity scales with head count.</summary>
    Hydra = 2,

    /// <summary>Example additional species for extensibility demos.</summary>
    Phoenix = 3,

    /// <summary>Example additional species for extensibility demos.</summary>
    Vampire = 4
}
