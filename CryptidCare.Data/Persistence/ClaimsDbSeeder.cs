using CryptidCare.Claims.Domain.Entities;
using CryptidCare.Claims.Domain.Enums;

namespace CryptidCare.Claims.Data.Persistence;

/// <summary>
/// Inserts demo patients and medicines when the database is empty (local development convenience).
/// </summary>
public static class ClaimsDbSeeder
{
    /// <summary>
    /// Seeds the database if required tables have no rows.
    /// </summary>
    /// <param name="dbContext">Active EF context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task SeedAsync(ClaimsDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!dbContext.Patients.Any())
        {
            dbContext.Patients.AddRange(
                new Patient
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    TrueName = "Fenrir Jr",
                    Species = Species.Werewolf,
                    HeadCount = 1,
                    IsActive = true
                },
                new Patient
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    TrueName = "Lerna Spawn",
                    Species = Species.Hydra,
                    HeadCount = 5,
                    IsActive = true
                });
        }

        if (!dbContext.Medicines.Any())
        {
            dbContext.Medicines.AddRange(
                new Medicine
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Name = "Moonleaf Tonic",
                    ContainsSilver = false,
                    BaseCost = 12.50m
                },
                new Medicine
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Name = "SilverDust Elixir",
                    ContainsSilver = true,
                    BaseCost = 42m
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
