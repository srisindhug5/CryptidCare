namespace CryptidCare.Claims.Application.Behaviors;

using CryptidCare.Claims.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Pipeline behavior for validation using specifications across all request handlers.
/// Follows the pipeline pattern for cross-cutting concerns.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        _logger.LogInformation("Validating request {RequestName}", requestName);

        // If TRequest is IRequest<Result<T>>, validate before proceeding
        if (request is IValidateableRequest validatable)
        {
            var validationResult = validatable.Validate();
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Validation failed for {RequestName}: {Errors}", 
                    requestName, 
                    string.Join(", ", validationResult.Errors.Select(e => e.Code)));

                // Return validation failure response (implementation depends on response type)
                // This is simplified - actual implementation needs adapter based on TResponse
            }
        }

        return await next();
    }
}

/// <summary>
/// Marker interface for requests that support validation.
/// </summary>
public interface IValidateableRequest
{
    /// <summary>Validates the request and returns result with errors if any.</summary>
    Result Validate();
}

/// <summary>
/// Pipeline behavior for logging request/response lifecycle.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling request {RequestName} at {Timestamp}", 
            requestName, 
            DateTime.UtcNow);

        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            TResponse response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Request {RequestName} completed successfully in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Request {RequestName} failed after {ElapsedMilliseconds}ms: {ExceptionMessage}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }
}

/// <summary>
/// Pipeline behavior for performance monitoring.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowRequestThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        TResponse response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "Long running request {RequestName} completed in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
