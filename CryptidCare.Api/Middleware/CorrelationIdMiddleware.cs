using System.Diagnostics;

namespace CryptidCare.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation id for tracing (header <see cref="HeaderName"/> or generated)
/// and adds it to logger scope for downstream providers (including Application Insights).
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    public CorrelationIdMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _loggerFactory = loggerFactory;
    }

    /// <summary>Request / response header name for the correlation identifier.</summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary><see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> key storing the active correlation id.</summary>
    public const string ItemKey = "CorrelationId";

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[ItemKey] = correlationId;
        Activity.Current?.SetTag("CorrelationId", correlationId);

        context.Response.OnStarting(() =>
        {
            IHeaderDictionary headers = context.Response.Headers;
            if (!headers.ContainsKey(HeaderName))
            {
                headers.Append(HeaderName, correlationId);
            }

            return Task.CompletedTask;
        });

        ILogger logger = _loggerFactory.CreateLogger("CryptidCare.Request");
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestId"] = context.TraceIdentifier
        }))
        {
            await _next(context);
        }
    }
}
