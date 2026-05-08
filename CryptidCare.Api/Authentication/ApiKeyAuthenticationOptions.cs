using Microsoft.AspNetCore.Authentication;

namespace CryptidCare.Api.Authentication;

/// <summary>
/// Options for <see cref="ApiKeyAuthenticationHandler"/>.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>The authentication scheme name registered with ASP.NET Core.</summary>
    public const string SchemeName = "ApiKey";

    /// <summary>HTTP header carrying the API key.</summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>Configuration key holding the expected API key value.</summary>
    public string ConfigurationKey { get; set; } = "Authentication:ApiKey";
}
