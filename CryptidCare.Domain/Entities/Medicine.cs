namespace CryptidCare.Claims.Domain.Entities;

/// <summary>
/// A formulary item that can appear on a prescription claim.
/// </summary>
public class Medicine
{
    /// <summary>Stable identifier for the medicine.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name of the medicine.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the formulation contains silver (safety rules for some species).</summary>
    public bool ContainsSilver { get; set; }

    /// <summary>Unit price before quantity multipliers.</summary>
    public decimal BaseCost { get; set; }
}
