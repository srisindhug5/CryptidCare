using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CryptidCare.Claims.Api.Configuration;

/// <summary>
/// Registers OpenAPI / Swashbuckle for the Claims API.
/// </summary>
public static class SwaggerExtensions
{
    private const string DocumentName = "v1";

    /// <summary>
    /// Adds Swagger generation with API metadata and XML comments from this assembly and referenced layers.
    /// </summary>
    public static IServiceCollection AddCryptidCareSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                DocumentName,
                new OpenApiInfo
                {
                    Title = "CryptidCare Claims API",
                    Version = "v1",
                    Description =
                        "Pharmacy claim submission and adjudication for cryptid patients: rule evaluation, "
                        + "species-specific quantity adjustment, persisted audit trail, and stable rejection codes."
                });

            options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

            IncludeXmlCommentsIfPresent(options, Assembly.GetExecutingAssembly());
            IncludeXmlCommentsIfPresent(options, "CryptidCare.Claims.Application.xml");
            IncludeXmlCommentsIfPresent(options, "CryptidCare.Claims.Domain.xml");
        });

        return services;
    }

    /// <summary>
    /// Serves OpenAPI JSON and the Swagger UI.
    /// </summary>
    public static WebApplication UseCryptidCareSwagger(this WebApplication app)
    {
        app.UseSwagger(c => c.RouteTemplate = "swagger/{documentName}/swagger.json");
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"/swagger/{DocumentName}/swagger.json", "CryptidCare Claims API v1");
            c.DocumentTitle = "CryptidCare Claims API";
            c.DisplayRequestDuration();
            c.EnableTryItOutByDefault();
        });

        return app;
    }

    private static void IncludeXmlCommentsIfPresent(SwaggerGenOptions options, Assembly assembly)
    {
        string xmlFile = $"{assembly.GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }

    private static void IncludeXmlCommentsIfPresent(SwaggerGenOptions options, string fileName)
    {
        string xmlPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }
}
