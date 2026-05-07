using CryptidCare.Claims.Application.Abstractions;
using CryptidCare.Claims.Domain.Entities;
using CryptidCare.Claims.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CryptidCare.Claims.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IMedicineRepository"/>.
/// </summary>
public class MedicineRepository(ClaimsDbContext dbContext) : IMedicineRepository
{
    /// <inheritdoc />
    public Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Medicines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
