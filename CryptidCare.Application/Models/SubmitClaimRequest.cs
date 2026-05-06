namespace CryptidCare.Claims.Application.Models;

/// <summary>
/// Input for submitting a prescription claim from a pharmacy.
/// </summary>
/// <param name="PatientId">Patient receiving the medicine.</param>
/// <param name="MedicineId">Medicine being claimed.</param>
/// <param name="Quantity">Requested quantity (positive).</param>
public record SubmitClaimRequest(Guid PatientId, Guid MedicineId, int Quantity);
