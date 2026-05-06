using CryptidCare.Claims.Domain.Entities;

namespace CryptidCare.Claims.Application.Abstractions;

/// <summary>
/// Persists and loads claims and related audit rows.
/// </summary>
public interface IClaimRepository
{
    /// <summary>Persists a new claim (and nested evaluations) to storage.</summary>
    Task AddAsync(Claim claim, CancellationToken cancellationToken);

    /// <summary>Loads a claim by id including rule evaluations when available.</summary>
    Task<Claim?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
