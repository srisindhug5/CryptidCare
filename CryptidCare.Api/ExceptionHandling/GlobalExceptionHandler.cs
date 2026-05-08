using CryptidCare.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptidCare.Api.ExceptionHandling;

/// <summary>
/// Maps unhandled exceptions to <see cref="ProblemDetails"/> with safe details outside Development.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException canceled)
        {
            _logger.LogWarning(canceled, "Request was cancelled");
            return await WriteProblemAsync(
                httpContext,
                cancellationToken,
                StatusCodes.Status408RequestTimeout,
                title: "Request timed out or was cancelled",
                detail: _environment.IsDevelopment() ? canceled.ToString() : "The operation was cancelled or the client disconnected.",
                type: "https://httpstatuses.io/408");
        }

        if (exception is DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database update failed");
            return await WriteProblemAsync(
                httpContext,
                cancellationToken,
                StatusCodes.Status409Conflict,
                title: "Could not save data",
                detail: _environment.IsDevelopment()
                    ? dbEx.Message
                    : "A data consistency error occurred. Retry the request or contact support with the correlation id.",
                type: "https://httpstatuses.io/409");
        }

        _logger.LogError(exception, "Unhandled exception");
        return await WriteProblemAsync(
            httpContext,
            cancellationToken,
            StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred",
            detail: _environment.IsDevelopment()
                ? exception.ToString()
                : "An error occurred while processing the request. Please try again later.",
            type: "https://httpstatuses.io/500");
    }

    private async ValueTask<bool> WriteProblemAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken,
        int statusCode,
        string title,
        string detail,
        string type)
    {
        httpContext.Response.StatusCode = statusCode;

        if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out object? correlationObject)
            && correlationObject is string correlationId
            && !httpContext.Response.Headers.ContainsKey(CorrelationIdMiddleware.HeaderName))
        {
            httpContext.Response.Headers.Append(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        ProblemDetailsContext context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = null
        };

        await _problemDetailsService.WriteAsync(context);
        return true;
    }
}
