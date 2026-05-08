using CryptidCare.Application.Contracts;
using CryptidCare.Application.Rules;
using CryptidCare.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CryptidCare.Application.Configuration;

/// <summary>
/// Application-layer host setup: registers the adjudication service, claim rules, and quantity adjusters.
/// </summary>
public static class Startup
{
    /// <summary>
    /// Registers application services in the host service collection.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IClaimAdjudicationService, ClaimAdjudicationService>();

        services.AddScoped<IClaimRule, WerewolfSilverAllergyRule>();
        services.AddScoped<IClaimRule, HydraHeadCountRule>();
        services.AddScoped<IQuantityAdjuster, HydraQuantityAdjuster>();
    }
}
