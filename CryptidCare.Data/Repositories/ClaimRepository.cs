using CryptidCare.Claims.Application.Abstractions;
using CryptidCare.Claims.Domain.Entities;
using CryptidCare.Claims.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CryptidCare.Claims.Data.Repositories;

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
