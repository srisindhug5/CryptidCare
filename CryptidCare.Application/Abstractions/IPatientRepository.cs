using CryptidCare.Claims.Domain.Entities;

namespace CryptidCare.Claims.Application.Abstractions;

/// <summary>
/// Reads patient data for adjudication.
/// </summary>
public interface IPatientRepository
{
    /// <summary>Loads a patient by id, or null if missing.</summary>
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
