using CryptidCare.Application.Abstractions;
using CryptidCare.Domain.Entities;
using CryptidCare.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CryptidCare.Data.Repositories;

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
