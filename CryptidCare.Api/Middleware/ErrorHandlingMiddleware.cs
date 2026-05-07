namespace CryptidCare.Claims.Api.Middleware;

/// <summary>
/// Global exception handling middleware with structured error responses.
/// Translates domain exceptions and validation errors to HTTP responses.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        (int statusCode, string title, object detail) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                (object)ve.Errors),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                "The requested resource does not exist."),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;
        ErrorJsonBody body = new(
            statusCode,
            title,
            detail,
            DateTime.UtcNow,
            context.TraceIdentifier);
        return context.Response.WriteAsJsonAsync(body);
    }

    private sealed record ErrorJsonBody(
        int Status,
        string Title,
        object Detail,
        DateTime Timestamp,
        string TraceId);
}

/// <summary>
/// Custom validation exception for domain validation errors.
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyList<ValidationFailure> Errors { get; }

    public ValidationException(params ValidationFailure[] errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

/// <summary>
/// Represents a single validation failure.
/// </summary>
public record ValidationFailure(string PropertyName, string ErrorMessage, string? ErrorCode = null);

/// <summary>
/// Repository-level exception for data access errors.
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Domain business rule exception.
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR") : base(message)
    {
        ErrorCode = errorCode;
    }
}
