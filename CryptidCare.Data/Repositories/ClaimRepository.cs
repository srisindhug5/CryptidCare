using CryptidCare.Application.Abstractions;
using CryptidCare.Domain.Entities;
using CryptidCare.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CryptidCare.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IClaimRepository"/>.
/// </summary>
public class ClaimRepository(ClaimsDbContext dbContext) : IClaimRepository
{
    /// <inheritdoc />
    public async Task AddAsync(Claim claim, CancellationToken cancellationToken)
    {
        await dbContext.Claims.AddAsync(claim, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Claim?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Claims
            .Include(x => x.RuleEvaluations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
