using CryptidCare.Claims.Application.Contracts;
using CryptidCare.Claims.Application.Rules;
using CryptidCare.Claims.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CryptidCare.Claims.Application.Configuration;

/// <summary>
/// Application-layer host setup: register orchestration, rules, and adjusters.
/// Add overloads or optional configuration binding here as the domain grows (options, feature flags, HttpClient-backed ports, etc.).
/// </summary>
public static class Startup
{
    /// <summary>
    /// Registers application services. Pass <paramref name="configuration"/> when registering options or named Http clients from this layer.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Optional host configuration (e.g. for <c>IOptions&lt;T&gt;</c>).</param>
    public static void ConfigureServices(IServiceCollection services, IConfiguration? configuration = null)
    {
        _ = configuration;

        services.AddScoped<IClaimAdjudicationService, ClaimAdjudicationService>();

        services.AddScoped<IClaimRule, WerewolfSilverAllergyRule>();
        services.AddScoped<IClaimRule, HydraHeadCountRule>();
        services.AddScoped<IQuantityAdjuster, HydraQuantityAdjuster>();
    }
}
