using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CryptidCare.Api.Authentication;

/// <summary>
/// Lightweight API-key authentication for the Claims API.
/// Reads the configured key from <c>Authentication:ApiKey</c> and compares it against the
/// <c>X-Api-Key</c> header in constant time. Intended as a stub demonstrating the seam where a
/// production deployment would plug in JWT bearer / OIDC / mTLS instead.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
    /// </summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out Microsoft.Extensions.Primitives.StringValues headerValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string providedKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string? configuredKey = _configuration[Options.ConfigurationKey];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            Logger.LogWarning(
                "API key authentication is not configured ({ConfigurationKey} is missing). Rejecting request.",
                Options.ConfigurationKey);
            return Task.FromResult(AuthenticateResult.Fail("API key authentication is not configured."));
        }

        if (!FixedTimeEquals(providedKey, configuredKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        Claim[] claims = [new Claim(ClaimTypes.Name, "pharmacy-client")];
        ClaimsIdentity identity = new ClaimsIdentity(claims, Scheme.Name);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        AuthenticationTicket ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Compares two strings in constant time to avoid leaking key content via timing side channels.
    /// Hashing both inputs first ensures the comparison is on fixed-length buffers regardless of input length.
    /// </summary>
    private static bool FixedTimeEquals(string provided, string configured)
    {
        Span<byte> providedHash = stackalloc byte[32];
        Span<byte> configuredHash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(provided), providedHash);
        SHA256.HashData(Encoding.UTF8.GetBytes(configured), configuredHash);
        return CryptographicOperations.FixedTimeEquals(providedHash, configuredHash);
    }
}
