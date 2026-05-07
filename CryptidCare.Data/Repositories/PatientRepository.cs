using CryptidCare.Claims.Application.Abstractions;
using CryptidCare.Claims.Domain.Entities;
using CryptidCare.Claims.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CryptidCare.Claims.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPatientRepository"/>.
/// </summary>
public class PatientRepository(ClaimsDbContext dbContext) : IPatientRepository
{
    /// <inheritdoc />
    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Patients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
