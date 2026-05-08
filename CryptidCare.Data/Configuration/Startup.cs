using CryptidCare.Application.Abstractions;
using CryptidCare.Data.Persistence;
using CryptidCare.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CryptidCare.Data.Configuration;

/// <summary>
/// Data layer host setup: EF Core, repositories, and post-build persistence actions (migrations, seeding).
/// Extend with <c>AddOptions</c>, resilience, or health checks as needed without touching <c>Program.cs</c>.
/// </summary>
public static class Startup
{
    /// <summary>
    /// Registers SQL Server persistence and repository implementations.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Application configuration (expects <c>ConnectionStrings:ClaimsDatabase</c>).</param>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("ClaimsDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ClaimsDatabase' is not configured. "
                + "Configure via appsettings.json, environment variables (ConnectionStrings__ClaimsDatabase), "
                + "or Azure Key Vault for secure credential management.");
        }

        services.AddDbContext<ClaimsDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null));
        });

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<IClaimRepository, ClaimRepository>();
    }

    /// <summary>
    /// Applies pending EF Core migrations and runs <see cref="ClaimsDbSeeder"/>.
    /// </summary>
    /// <param name="services">Root service provider (typically <c>app.Services</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ApplyPersistenceAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        ClaimsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        await ClaimsDbSeeder.SeedAsync(dbContext, cancellationToken).ConfigureAwait(false);
    }
}
