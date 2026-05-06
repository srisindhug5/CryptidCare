using CryptidCare.Claims.Domain.Entities;

namespace CryptidCare.Claims.Application.Abstractions;

/// <summary>
/// Reads medicine data for adjudication.
/// </summary>
public interface IMedicineRepository
{
    /// <summary>Loads a medicine by id, or null if missing.</summary>
    Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
