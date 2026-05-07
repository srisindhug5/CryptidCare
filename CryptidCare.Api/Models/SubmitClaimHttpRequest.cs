using System.ComponentModel.DataAnnotations;

namespace CryptidCare.Claims.Api.Models;

/// <summary>
/// Request body for claim submission. Invalid payloads fail at the API layer (400) before adjudication or persistence.
/// </summary>
/// <param name="PatientId">Patient receiving the medicine.</param>
/// <param name="MedicineId">Medicine being claimed.</param>
/// <param name="Quantity">Requested quantity (must be positive).</param>
public record SubmitClaimHttpRequest(
    Guid PatientId,
    Guid MedicineId,
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    int Quantity)
    : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PatientId == Guid.Empty)
        {
            yield return new ValidationResult(
                "PatientId must not be empty.",
                [nameof(PatientId)]);
        }

        if (MedicineId == Guid.Empty)
        {
            yield return new ValidationResult(
                "MedicineId must not be empty.",
                [nameof(MedicineId)]);
        }
    }
}
