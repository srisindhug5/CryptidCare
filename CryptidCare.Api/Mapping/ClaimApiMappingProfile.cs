using AutoMapper;
using CryptidCare.Api.Controllers;
using CryptidCare.Application.Models;
using CryptidCare.Domain.Entities;

namespace CryptidCare.Api.Mapping;

/// <summary>
/// AutoMapper profile for API response DTOs. Keeps mapping rules out of controllers.
/// Positional record DTOs use explicit conversions so constructor arguments align when names differ
/// from sources (e.g. <c>RejectionCode</c> vs <c>ReasonCode</c>, <c>Id</c> vs <c>ClaimId</c>).
/// </summary>
public class ClaimApiMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimApiMappingProfile"/> class.
    /// </summary>
    public ClaimApiMappingProfile()
    {
        CreateMap<ClaimRuleEvaluation, RuleEvaluationResponse>()
            .ConvertUsing(s => new RuleEvaluationResponse(
                s.RuleName,
                s.Passed,
                s.Reason,
                s.RejectionCode.HasValue ? s.RejectionCode.Value.ToString() : null));

        CreateMap<SubmitClaimResult, SubmitClaimResponse>()
            .ConvertUsing(s => new SubmitClaimResponse(
                s.ClaimId,
                s.Status.ToString(),
                s.EffectiveQuantity,
                s.TotalCost,
                s.Reason,
                s.RejectionCode.HasValue ? s.RejectionCode.Value.ToString() : null));

        CreateMap<Claim, ClaimDetailsResponse>()
            .ConvertUsing(s => MapClaimToDetails(s));
    }

    private static ClaimDetailsResponse MapClaimToDetails(Claim s)
    {
        IReadOnlyCollection<RuleEvaluationResponse> evaluations = s.RuleEvaluations
            .OrderBy(e => e.EvaluatedAtUtc)
            .Select(e => new RuleEvaluationResponse(
                e.RuleName,
                e.Passed,
                e.Reason,
                e.RejectionCode.HasValue ? e.RejectionCode.Value.ToString() : null))
            .ToList();

        return new ClaimDetailsResponse(
            s.Id,
            s.Status.ToString(),
            s.RequestedQuantity,
            s.EffectiveQuantity,
            s.TotalCost,
            s.RejectionReason,
            s.RejectionCode.HasValue ? s.RejectionCode.Value.ToString() : null,
            evaluations);
    }
}
