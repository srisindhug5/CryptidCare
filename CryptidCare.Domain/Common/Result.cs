namespace CryptidCare.Claims.Domain.Common;

/// <summary>
/// Represents a result of an operation that can either succeed or fail.
/// This is the core enterprise pattern for explicit error handling without exceptions.
/// </summary>
public abstract class Result
{
    /// <summary>Gets a value indicating whether the result represents success.</summary>
    public abstract bool IsSuccess { get; }

    /// <summary>Gets the errors if the result failed; otherwise empty.</summary>
    public abstract IReadOnlyList<ResultError> Errors { get; }

    /// <summary>Creates a successful result.</summary>
    public static Result<T> Success<T>(T value) => new SuccessResult<T>(value);

    /// <summary>Creates a failed result with a single error.</summary>
    public static Result<T> Failure<T>(string code, string message, string? detail = null) =>
        new FailureResult<T>([new ResultError(code, message, detail)]);

    /// <summary>Creates a failed result with multiple errors.</summary>
    public static Result<T> Failure<T>(params ResultError[] errors) =>
        new FailureResult<T>(errors);

    /// <summary>Creates a successful result with no value.</summary>
    public static Result Success() => new EmptySuccessResult();

    /// <summary>Creates a failed empty result.</summary>
    public static Result Failure(string code, string message, string? detail = null) =>
        new EmptyFailureResult([new ResultError(code, message, detail)]);

    private sealed class SuccessResult<T> : Result<T>
    {
        private readonly T _value;

        public SuccessResult(T value) => _value = value;

        public override bool IsSuccess => true;

        public override IReadOnlyList<ResultError> Errors => [];

        public override T Value => _value;

        public override T? GetValueOrDefault() => _value;
    }

    private sealed class FailureResult<T> : Result<T>
    {
        public FailureResult(IReadOnlyList<ResultError> errors) => Errors = errors;

        public override bool IsSuccess => false;

        public override IReadOnlyList<ResultError> Errors { get; }

        public override T Value =>
            throw new InvalidOperationException("Cannot access the value of a failed result.");

        public override T? GetValueOrDefault() => default;
    }

    private sealed class EmptySuccessResult : Result
    {
        public override bool IsSuccess => true;

        public override IReadOnlyList<ResultError> Errors => [];
    }

    private sealed class EmptyFailureResult : Result
    {
        public EmptyFailureResult(IReadOnlyList<ResultError> errors) => Errors = errors;

        public override bool IsSuccess => false;

        public override IReadOnlyList<ResultError> Errors { get; }
    }
}

/// <summary>
/// Generic result with a value on success.
/// </summary>
public abstract class Result<T> : Result
{
    /// <summary>Gets the value if successful; throws if failed.</summary>
    public abstract T Value { get; }

    /// <summary>Attempts to get the value, returning default on failure.</summary>
    public abstract T? GetValueOrDefault();

    /// <summary>Executes a function if successful, returning this result if failed.</summary>
    public Result<TNext> Then<TNext>(Func<T, Result<TNext>> nextFunc)
    {
        if (!IsSuccess)
        {
            return Result.Failure<TNext>(Errors.ToArray());
        }

        return nextFunc(Value);
    }

    /// <summary>Transforms the value if successful.</summary>
    public Result<TNext> Map<TNext>(Func<T, TNext> mapper)
    {
        if (!IsSuccess)
        {
            return Result.Failure<TNext>(Errors.ToArray());
        }

        return Result.Success(mapper(Value));
    }

    /// <summary>Asynchronously executes a function if successful.</summary>
    public async Task<Result<TNext>> ThenAsync<TNext>(Func<T, Task<Result<TNext>>> nextFunc)
    {
        if (!IsSuccess)
        {
            return Result.Failure<TNext>(Errors.ToArray());
        }

        return await nextFunc(Value);
    }
}

/// <summary>
/// Represents a validation error or business rule violation with structured error information.
/// </summary>
/// <param name="Code">Machine-readable error code for categorization (e.g., "PATIENT_NOT_ACTIVE").</param>
/// <param name="Message">User-friendly error message.</param>
/// <param name="Detail">Optional detailed explanation for debugging.</param>
public record ResultError(string Code, string Message, string? Detail = null)
{
    /// <summary>Error codes for common scenarios.</summary>
    public static class ErrorCodes
    {
        public const string NotFound = "NOT_FOUND";
        public const string InvalidInput = "INVALID_INPUT";
        public const string Conflict = "CONFLICT";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string InternalServerError = "INTERNAL_SERVER_ERROR";
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string DuplicateResource = "DUPLICATE_RESOURCE";
        public const string OperationFailed = "OPERATION_FAILED";
    }
}
