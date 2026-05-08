using CryptidCare.Application.Abstractions;
using CryptidCare.Domain.Entities;
using CryptidCare.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CryptidCare.Data.Repositories;

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
