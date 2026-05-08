using CryptidCare.Domain.Enums;

namespace CryptidCare.Domain.Entities;

/// <summary>
/// A mythical patient eligible for Cryptid-Care pharmacy claims.
/// </summary>
public class Patient
{
    /// <summary>Stable identifier for the patient.</summary>
    public Guid Id { get; set; }

    /// <summary>Legal or true name used for adjudication and audit.</summary>
    public string TrueName { get; set; } = string.Empty;

    /// <summary>Species-specific behavior (e.g. Hydra multiplier, Werewolf silver rule).</summary>
    public Species Species { get; set; }

    /// <summary>Number of heads; used for Hydra quantity adjustment.</summary>
    public int HeadCount { get; set; } = 1;

    /// <summary>When false, claims for this patient are rejected.</summary>
    public bool IsActive { get; set; } = true;
}
