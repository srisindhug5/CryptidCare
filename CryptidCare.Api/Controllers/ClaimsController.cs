using AutoMapper;
using CryptidCare.Api.Models;
using CryptidCare.Application.Abstractions;
using CryptidCare.Application.Contracts;
using CryptidCare.Application.Models;
using CryptidCare.Domain.Entities;
using CryptidCare.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CryptidCare.Api.Controllers;

/// <summary>
/// HTTP API for submitting claims and retrieving adjudication details including rule audit rows.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClaimsController(
    IClaimAdjudicationService claimAdjudicationService,
    IClaimRepository claimRepository,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Submits a claim for adjudication. Returns 200 when approved and 400 when rejected.
    /// </summary>
    /// <param name="request">Patient, medicine, and quantity.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Claim id, status, effective quantity, total cost, and rejection reason if any.</returns>
    /// <remarks>
    /// A 400 response is either a validation error (<see cref="ValidationProblemDetails"/>) when the request body is invalid,
    /// or a rejected adjudication result (<see cref="SubmitClaimResponse"/>) with <c>reason</c> and <c>reasonCode</c>.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(SubmitClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SubmitClaimResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SubmitClaimResponse>> SubmitClaimAsync(
        [FromBody] SubmitClaimHttpRequest request,
        CancellationToken cancellationToken)
    {
        SubmitClaimResult result = await claimAdjudicationService.SubmitAsync(
            new SubmitClaimRequest(request.PatientId, request.MedicineId, request.Quantity),
            cancellationToken);

        SubmitClaimResponse response = mapper.Map<SubmitClaimResponse>(result);
        return result.Status == ClaimStatus.Rejected ? BadRequest(response) : Ok(response);
    }

    /// <summary>
    /// Gets a persisted claim by id, including ordered rule evaluation audit entries.
    /// </summary>
    /// <param name="claimId">Claim identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Full claim details or 404 when not found.</returns>
    [HttpGet("{claimId:guid}")]
    [ProducesResponseType(typeof(ClaimDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimDetailsResponse>> GetClaimAsync(Guid claimId, CancellationToken cancellationToken)
    {
        Claim? claim = await claimRepository.GetByIdAsync(claimId, cancellationToken);
        if (claim is null)
        {
            return NotFound();
        }

        ClaimDetailsResponse response = mapper.Map<ClaimDetailsResponse>(claim);
        return Ok(response);
    }
}

/// <summary>
/// Response returned from claim submission.
/// </summary>
/// <param name="ClaimId">Persisted claim identifier.</param>
/// <param name="Status">Approved or Rejected.</param>
/// <param name="EffectiveQuantity">Quantity after adjustments.</param>
/// <param name="TotalCost">Payable amount when approved.</param>
/// <param name="Reason">Rejection reason when status is rejected.</param>
/// <param name="ReasonCode">Stable rejection code when status is rejected (enum name).</param>
public record SubmitClaimResponse(
    Guid ClaimId,
    string Status,
    int EffectiveQuantity,
    decimal TotalCost,
    string? Reason,
    string? ReasonCode);

/// <summary>
/// Full claim read model including rule audit trail.
/// </summary>
/// <param name="ClaimId">Claim identifier.</param>
/// <param name="Status">Approved or Rejected.</param>
/// <param name="RequestedQuantity">Original requested quantity.</param>
/// <param name="EffectiveQuantity">Quantity after adjustments.</param>
/// <param name="TotalCost">Stored total cost.</param>
/// <param name="Reason">Rejection reason if rejected.</param>
/// <param name="ReasonCode">Stable rejection code if rejected.</param>
/// <param name="RuleEvaluations">Ordered rule outcomes.</param>
public record ClaimDetailsResponse(
    Guid ClaimId,
    string Status,
    int RequestedQuantity,
    int EffectiveQuantity,
    decimal TotalCost,
    string? Reason,
    string? ReasonCode,
    IReadOnlyCollection<RuleEvaluationResponse> RuleEvaluations);

/// <summary>
/// One row in the rule audit trail.
/// </summary>
/// <param name="RuleName">Rule that ran.</param>
/// <param name="Passed">Whether the rule passed.</param>
/// <param name="Reason">Optional message from the rule.</param>
/// <param name="ReasonCode">Stable code when the rule failed.</param>
public record RuleEvaluationResponse(string RuleName, bool Passed, string? Reason, string? ReasonCode);
