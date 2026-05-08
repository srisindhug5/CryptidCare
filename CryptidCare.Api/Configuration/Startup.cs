using AutoMapper;
using CryptidCare.Api.Authentication;
using CryptidCare.Api.ExceptionHandling;
using CryptidCare.Api.HealthChecks;
using CryptidCare.Api.Mapping;
using CryptidCare.Api.Middleware;
using CryptidCare.Data.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace CryptidCare.Api.Configuration;

/// <summary>
/// API host setup: cross-cutting services, MVC, OpenAPI, and HTTP pipeline.
/// Use <see cref="ConfigureServices"/> for anything that belongs on <see cref="WebApplicationBuilder"/>
/// (configuration, logging, environment) and <see cref="Configure"/> for middleware order.
/// </summary>
public static class Startup
{
    /// <summary>
    /// Registers controllers, observability, problem details, validation behavior, and Swagger.
    /// </summary>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IServiceCollection services = builder.Services;
        IConfiguration configuration = builder.Configuration;

        // Application Insights 3.x wires Azure Monitor OpenTelemetry exporters; they require a non-empty
        // connection string at startup. Skip registration when unset so local/dev runs without Azure.
        if (HasApplicationInsightsConnectionString(configuration))
        {
            services.AddApplicationInsightsTelemetry(configuration);
        }

        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.Duration;
            options.RequestBodyLogLimit = 0;
            options.ResponseBodyLogLimit = 0;
            options.CombineLogs = true;
        });

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = (ProblemDetailsContext ctx) =>
            {
                HttpContext httpContext = ctx.HttpContext;
                if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out object? correlationObject)
                    && correlationObject is string correlationId)
                {
                    ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
                else
                {
                    ctx.ProblemDetails.Extensions["correlationId"] = httpContext.TraceIdentifier;
                }
            };
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();

        services
            .AddAuthentication(ApiKeyAuthenticationOptions.SchemeName)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.SchemeName,
                _ => { });
        services.AddAuthorization();

        services.AddControllers();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                ValidationProblemDetails problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}"
                };

                if (context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out object? correlationObject)
                    && correlationObject is string correlationId)
                {
                    problemDetails.Extensions["correlationId"] = correlationId;
                }
                else
                {
                    problemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
                }

                return new BadRequestObjectResult(problemDetails);
            };
        });

        services.AddEndpointsApiExplorer();
        services.AddCryptidCareSwagger();

        services.AddHealthChecks()
            .AddCheck<ClaimsApiHealthCheck>("claims_api", tags: ["live"])
            .AddDbContextCheck<ClaimsDbContext>("database", tags: ["ready"]);

        services.AddSingleton(sp =>
        {
            ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            MapperConfiguration mapperConfiguration = new(
                cfg => cfg.AddProfile<ClaimApiMappingProfile>(),
                loggerFactory);
#if DEBUG
            mapperConfiguration.AssertConfigurationIsValid();
#endif
            return mapperConfiguration;
        });
        services.AddSingleton<IMapper>(sp => sp.GetRequiredService<MapperConfiguration>().CreateMapper());
    }

    /// <summary>
    /// Configures the HTTP request pipeline for the Claims API.
    /// </summary>
    public static void Configure(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseCryptidCareSwagger();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseHttpsRedirection();
        app.UseHttpLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        }).AllowAnonymous();
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        }).AllowAnonymous();
    }

    private static bool HasApplicationInsightsConnectionString(IConfiguration configuration)
    {
        string? fromConfig = configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return true;
        }

        string? fromEnv = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        return !string.IsNullOrWhiteSpace(fromEnv);
    }
}
