using CryptidCare.Claims.Domain.Common;

namespace CryptidCare.Claims.Application.Common;

/// <summary>
/// Pagination request parameters following enterprise conventions.
/// Used for all list endpoints to support large datasets efficiently.
/// </summary>
/// <param name="PageNumber">1-indexed page number (default 1).</param>
/// <param name="PageSize">Items per page (default 25, max 100).</param>
/// <param name="SortBy">Property to sort by (default "CreatedAtUtc").</param>
/// <param name="SortOrder">ASC or DESC (default ASC).</param>
public record PaginationRequest(
    int PageNumber = 1,
    int PageSize = 25,
    string SortBy = "CreatedAtUtc",
    string SortOrder = "ASC")
{
    /// <summary>Validates pagination parameters.</summary>
    public Result<PaginationRequest> Validate()
    {
        var errors = new List<ResultError>();

        if (PageNumber < 1)
        {
            errors.Add(new ResultError(
                ResultError.ErrorCodes.ValidationFailed,
                "PageNumber must be at least 1.",
                $"Received: {PageNumber}"));
        }

        if (PageSize < 1 || PageSize > 100)
        {
            errors.Add(new ResultError(
                ResultError.ErrorCodes.ValidationFailed,
                "PageSize must be between 1 and 100.",
                $"Received: {PageSize}"));
        }

        if (!string.IsNullOrWhiteSpace(SortOrder) && !SortOrder.Equals("ASC", StringComparison.OrdinalIgnoreCase) && !SortOrder.Equals("DESC", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new ResultError(
                ResultError.ErrorCodes.ValidationFailed,
                "SortOrder must be 'ASC' or 'DESC'.",
                $"Received: {SortOrder}"));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<PaginationRequest>(errors.ToArray());
        }

        return Result.Success(this);
    }

    /// <summary>Calculates the skip count for database queries.</summary>
    public int GetSkip() => (PageNumber - 1) * PageSize;
}

/// <summary>
/// Generic paginated response envelope following HATEOAS principles (when applicable).
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    string SortBy,
    string SortOrder)
{
    /// <summary>Total pages based on page size.</summary>
    public int TotalPages => (int)Math.Ceiling((decimal)TotalCount / PageSize);

    /// <summary>Whether there's a next page.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Whether there's a previous page.</summary>
    public bool HasPreviousPage => PageNumber > 1;
}
