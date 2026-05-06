using CryptidCare.Claims.Application.Abstractions;
using CryptidCare.Claims.Application.Contracts;
using CryptidCare.Claims.Application.Models;
using CryptidCare.Claims.Domain.Entities;
using CryptidCare.Claims.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CryptidCare.Claims.Application.Services;

/// <summary>
/// Default implementation: load entities, run rules, apply adjusters, compute cost, persist claim.
/// </summary>
public class ClaimAdjudicationService(
    IPatientRepository patientRepository,
    IMedicineRepository medicineRepository,
    IClaimRepository claimRepository,
    IEnumerable<IClaimRule> rules,
    IEnumerable<IQuantityAdjuster> quantityAdjusters,
    ILogger<ClaimAdjudicationService> logger) : IClaimAdjudicationService
{
    /// <inheritdoc />
    public async Task<SubmitClaimResult> SubmitAsync(SubmitClaimRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return await RejectWithoutContextAsync(
                request,
                ClaimRejectionCode.InvalidQuantity,
                "Quantity must be greater than zero.",
                cancellationToken);
        }

        Patient? patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return RejectWithoutPersistedClaim(
                request,
                ClaimRejectionCode.PatientNotFound,
                "Patient was not found.");
        }

        if (!patient.IsActive)
        {
            return await RejectWithoutContextAsync(
                request,
                ClaimRejectionCode.PatientInactive,
                "Patient is inactive.",
                cancellationToken);
        }

        Medicine? medicine = await medicineRepository.GetByIdAsync(request.MedicineId, cancellationToken);
        if (medicine is null)
        {
            return RejectWithoutPersistedClaim(
                request,
                ClaimRejectionCode.MedicineNotFound,
                "Medicine was not found.");
        }

        ClaimContext context = new ClaimContext
        {
            Patient = patient,
            Medicine = medicine,
            RequestedQuantity = request.Quantity,
            EffectiveQuantity = request.Quantity
        };

        Claim claim = new Claim
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            MedicineId = medicine.Id,
            RequestedQuantity = request.Quantity,
            EffectiveQuantity = request.Quantity,
            Status = ClaimStatus.Pending
        };

        foreach (IClaimRule rule in rules)
        {
            RuleResult ruleResult = await rule.EvaluateAsync(context, cancellationToken);
            claim.RuleEvaluations.Add(new ClaimRuleEvaluation
            {
                Id = Guid.NewGuid(),
                ClaimId = claim.Id,
                RuleName = rule.Name,
                Passed = ruleResult.IsSuccess,
                Reason = ruleResult.Reason,
                RejectionCode = ruleResult.IsSuccess ? null : ruleResult.RejectionCode
            });

            if (!ruleResult.IsSuccess)
            {
                claim.Status = ClaimStatus.Rejected;
                claim.RejectionReason = ruleResult.Reason;
                claim.RejectionCode = ruleResult.RejectionCode;
                claim.EffectiveQuantity = context.EffectiveQuantity;
                claim.TotalCost = 0m;
                await claimRepository.AddAsync(claim, cancellationToken);
                logger.LogInformation(
                    "Claim {ClaimId} rejected by rule {RuleName}: {Reason}",
                    claim.Id,
                    rule.Name,
                    ruleResult.Reason);
                return new SubmitClaimResult(
                    claim.Id,
                    claim.Status,
                    claim.EffectiveQuantity,
                    claim.TotalCost,
                    claim.RejectionReason,
                    claim.RejectionCode);
            }
        }

        foreach (IQuantityAdjuster adjuster in quantityAdjusters)
        {
            await adjuster.AdjustAsync(context, cancellationToken);
        }

        claim.EffectiveQuantity = context.EffectiveQuantity;
        claim.TotalCost = claim.EffectiveQuantity * medicine.BaseCost;
        claim.Status = ClaimStatus.Approved;
        await claimRepository.AddAsync(claim, cancellationToken);

        logger.LogInformation(
            "Claim {ClaimId} approved. EffectiveQuantity={EffectiveQuantity}, TotalCost={TotalCost}",
            claim.Id,
            claim.EffectiveQuantity,
            claim.TotalCost);

        return new SubmitClaimResult(claim.Id, claim.Status, claim.EffectiveQuantity, claim.TotalCost);
    }

    /// <summary>
    /// Persists a rejected claim when patient or medicine context is unavailable or invalid early in the flow.
    /// </summary>
    private async Task<SubmitClaimResult> RejectWithoutContextAsync(
        SubmitClaimRequest request,
        ClaimRejectionCode rejectionCode,
        string reason,
        CancellationToken cancellationToken)
    {
        Claim claim = new Claim
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            MedicineId = request.MedicineId,
            RequestedQuantity = request.Quantity,
            EffectiveQuantity = request.Quantity > 0 ? request.Quantity : 0,
            Status = ClaimStatus.Rejected,
            RejectionReason = reason,
            RejectionCode = rejectionCode,
            TotalCost = 0m
        };

        await claimRepository.AddAsync(claim, cancellationToken);
        logger.LogInformation(
            "Claim {ClaimId} rejected before adjudication: {Reason}",
            claim.Id,
            reason);
        return new SubmitClaimResult(
            claim.Id,
            claim.Status,
            claim.EffectiveQuantity,
            claim.TotalCost,
            claim.RejectionReason,
            claim.RejectionCode);
    }

    /// <summary>
    /// Returns a rejection result without inserting a claim row. Used when FK constraints cannot be satisfied
    /// (unknown patient or medicine id), so persistence would fail at the database.
    /// </summary>
    private static SubmitClaimResult RejectWithoutPersistedClaim(
        SubmitClaimRequest request,
        ClaimRejectionCode rejectionCode,
        string reason)
    {
        int effectiveQty = request.Quantity > 0 ? request.Quantity : 0;
        return new SubmitClaimResult(
            Guid.Empty,
            ClaimStatus.Rejected,
            effectiveQty,
            0m,
            reason,
            rejectionCode);
    }
}
