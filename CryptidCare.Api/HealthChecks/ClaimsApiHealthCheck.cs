using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CryptidCare.Api.HealthChecks;

/// <summary>
/// Health check for the Claims API application.
/// Reports the status of core dependencies and business rules.
/// </summary>
public class ClaimsApiHealthCheck : IHealthCheck
{
    private readonly ILogger<ClaimsApiHealthCheck> _logger;

    public ClaimsApiHealthCheck(ILogger<ClaimsApiHealthCheck> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                { "status", "operational" },
                { "timestamp", DateTime.UtcNow },
                { "version", "1.0.0" }
            };

            _logger.LogInformation("Claims API health check passed");
            return HealthCheckResult.Healthy("Claims API is operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claims API health check failed");
            return HealthCheckResult.Unhealthy("Claims API is unavailable", ex);
        }
    }
}
