using System.Linq.Expressions;
using CryptidCare.Claims.Domain.Entities;
using CryptidCare.Claims.Domain.Enums;

namespace CryptidCare.Claims.Application.Common;

/// <summary>
/// Specification pattern for building reusable, testable query filters.
/// Encapsulates domain logic for common queries without mixing concerns.
/// </summary>
public abstract class Specification<T>
{
    /// <summary>Gets the filter expression to be compiled into queries.</summary>
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    /// <summary>Included related entities (EF Core Include).</summary>
    public List<Expression<Func<T, object>>> Includes { get; } = [];

    /// <summary>String-based includes for navigation properties.</summary>
    public List<string> IncludeStrings { get; } = [];

    /// <summary>Primary sort order.</summary>
    public Expression<Func<T, object>>? OrderBy { get; protected set; }

    /// <summary>Descending sort order.</summary>
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

    /// <summary>Pagination starting point.</summary>
    public int? Take { get; protected set; }

    /// <summary>Skip count for offset-based pagination.</summary>
    public int? Skip { get; protected set; }

    /// <summary>Whether to include deleted entities (soft delete support).</summary>
    public bool IsPagingEnabled { get; protected set; }

    /// <summary>Adds an include for eager-loading related data.</summary>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression) =>
        Includes.Add(includeExpression);

    /// <summary>Adds a string-based include.</summary>
    protected virtual void AddInclude(string includeString) =>
        IncludeStrings.Add(includeString);

    /// <summary>Enables pagination with skip/take.</summary>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>Enables ordering in ascending order.</summary>
    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) =>
        OrderBy = orderByExpression;

    /// <summary>Enables ordering in descending order.</summary>
    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression) =>
        OrderByDescending = orderByDescExpression;
}

/// <summary>
/// Example specification demonstrating the pattern for querying claims.
/// </summary>
public sealed class ApprovedClaimsSpecification : Specification<Claim>
{
    public ApprovedClaimsSpecification(int pageNumber, int pageSize)
    {
        Criteria = claim => claim.Status == ClaimStatus.Approved;
        AddInclude(c => c.Patient);
        AddInclude(c => c.Medicine);
        AddInclude(c => c.RuleEvaluations);
        
        ApplyOrderByDescending(c => c.CreatedAtUtc);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}

/// <summary>
/// Example specification for rejected claims within a date range.
/// </summary>
public sealed class RejectedClaimsInPeriodSpecification : Specification<Claim>
{
    public RejectedClaimsInPeriodSpecification(DateTime startDate, DateTime endDate)
    {
        Criteria = claim =>
            claim.Status == ClaimStatus.Rejected &&
            claim.CreatedAtUtc >= startDate &&
            claim.CreatedAtUtc <= endDate;

        AddInclude(c => c.Patient);
        AddInclude(c => c.RuleEvaluations);
        ApplyOrderByDescending(c => c.CreatedAtUtc);
    }
}

/// <summary>
/// Example specification for claims by patient.
/// </summary>
public sealed class ClaimsByPatientSpecification : Specification<Claim>
{
    public ClaimsByPatientSpecification(Guid patientId, int pageNumber, int pageSize)
    {
        Criteria = claim => claim.PatientId == patientId;
        AddInclude(c => c.Medicine);
        AddInclude(c => c.RuleEvaluations);
        
        ApplyOrderByDescending(c => c.CreatedAtUtc);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}
