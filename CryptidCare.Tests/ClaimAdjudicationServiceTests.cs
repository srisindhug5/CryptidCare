using CryptidCare.Application.Abstractions;
using CryptidCare.Application.Contracts;
using CryptidCare.Application.Models;
using CryptidCare.Application.Rules;
using CryptidCare.Application.Services;
using CryptidCare.Domain.Entities;
using CryptidCare.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CryptidCare.Tests;

/// <summary>
/// Unit tests for <see cref="ClaimAdjudicationService"/> and default claim rules (mocked repositories).
/// </summary>
public class ClaimAdjudicationServiceTests
{
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IMedicineRepository> _medicineRepository = new();
    private readonly Mock<IClaimRepository> _claimRepository = new();

    [Fact]
    public async Task SubmitAsync_Rejects_WhenWerewolfReceivesSilver()
    {
        ClaimAdjudicationService service = CreateService([new WerewolfSilverAllergyRule()], [new HydraQuantityAdjuster()]);
        Patient patient = CreatePatient(Species.Werewolf);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "SilverDust", ContainsSilver = true, BaseCost = 10m };
        ConfigureData(patient, medicine);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, 2), CancellationToken.None);

        Assert.Equal(ClaimStatus.Rejected, result.Status);
        Assert.Equal(ClaimRejectionCode.WerewolfSilverMedicine, result.RejectionCode);
        Assert.Contains("silver", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAsync_Approves_AndMultipliesHydraQuantity()
    {
        ClaimAdjudicationService service = CreateService([new WerewolfSilverAllergyRule(), new HydraHeadCountRule()], [new HydraQuantityAdjuster()]);
        Patient patient = CreatePatient(Species.Hydra, headCount: 5);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "Moonleaf", ContainsSilver = false, BaseCost = 3m };
        ConfigureData(patient, medicine);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, 2), CancellationToken.None);

        Assert.Equal(ClaimStatus.Approved, result.Status);
        Assert.Equal(10, result.EffectiveQuantity);
        Assert.Equal(30m, result.TotalCost);
    }

    [Fact]
    public async Task SubmitAsync_Rejects_WhenPatientInactive()
    {
        ClaimAdjudicationService service = CreateService([new WerewolfSilverAllergyRule()], [new HydraQuantityAdjuster()]);
        Patient patient = CreatePatient(Species.Phoenix, isActive: false);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "Ash Salve", ContainsSilver = false, BaseCost = 7m };
        ConfigureData(patient, medicine);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, 1), CancellationToken.None);

        Assert.Equal(ClaimStatus.Rejected, result.Status);
        Assert.Equal(ClaimRejectionCode.PatientInactive, result.RejectionCode);
        Assert.Contains("inactive", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAsync_AppliesAllRegisteredRules()
    {
        TrackingRule trackingRule = new TrackingRule();
        ClaimAdjudicationService service = CreateService([trackingRule], []);
        Patient patient = CreatePatient(Species.Phoenix);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "Renewal Brew", ContainsSilver = false, BaseCost = 2m };
        ConfigureData(patient, medicine);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, 3), CancellationToken.None);

        Assert.Equal(ClaimStatus.Approved, result.Status);
        Assert.True(trackingRule.WasExecuted);
    }

    [Fact]
    public async Task SubmitAsync_StoresRuleAuditEntries()
    {
        ClaimAdjudicationService service = CreateService([new WerewolfSilverAllergyRule(), new HydraHeadCountRule()], [new HydraQuantityAdjuster()]);
        Claim? savedClaim = null;
        _claimRepository
            .Setup(x => x.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>()))
            .Callback<Claim, CancellationToken>((claim, _) => savedClaim = claim)
            .Returns(Task.CompletedTask);

        Patient patient = CreatePatient(Species.Phoenix);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "Night Bloom", ContainsSilver = false, BaseCost = 4m };
        ConfigureData(patient, medicine);

        await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, 1), CancellationToken.None);

        Assert.NotNull(savedClaim);
        Assert.NotEmpty(savedClaim!.RuleEvaluations);
    }

    [Fact]
    public async Task SubmitAsync_Rejects_WhenQuantityIsZero()
    {
        ClaimAdjudicationService service = CreateService([], []);
        Patient patient = CreatePatient(Species.Phoenix);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "Test Med", ContainsSilver = false, BaseCost = 1m };
        ConfigureData(patient, medicine);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, 0), CancellationToken.None);

        Assert.Equal(ClaimStatus.Rejected, result.Status);
        Assert.Equal(ClaimRejectionCode.InvalidQuantity, result.RejectionCode);
        Assert.Contains("greater than zero", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAsync_Rejects_WhenQuantityIsNegative()
    {
        ClaimAdjudicationService service = CreateService([], []);
        Patient patient = CreatePatient(Species.Phoenix);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "Test Med", ContainsSilver = false, BaseCost = 1m };
        ConfigureData(patient, medicine);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, -5), CancellationToken.None);

        Assert.Equal(ClaimStatus.Rejected, result.Status);
        Assert.Equal(ClaimRejectionCode.InvalidQuantity, result.RejectionCode);
    }

    [Fact]
    public async Task SubmitAsync_Rejects_WhenPatientNotFound()
    {
        ClaimAdjudicationService service = CreateService([], []);
        Guid patientId = Guid.NewGuid();
        Guid medicineId = Guid.NewGuid();
        
        _patientRepository.Setup(x => x.GetByIdAsync(patientId, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patientId, medicineId, 1), CancellationToken.None);

        Assert.Equal(ClaimStatus.Rejected, result.Status);
        Assert.Equal(ClaimRejectionCode.PatientNotFound, result.RejectionCode);
        Assert.Contains("patient", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Guid.Empty, result.ClaimId);
        _claimRepository.Verify(x => x.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_Rejects_WhenMedicineNotFound()
    {
        ClaimAdjudicationService service = CreateService([], []);
        Patient patient = CreatePatient(Species.Phoenix);
        Guid medicineId = Guid.NewGuid();
        
        _patientRepository.Setup(x => x.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        _medicineRepository.Setup(x => x.GetByIdAsync(medicineId, It.IsAny<CancellationToken>())).ReturnsAsync((Medicine?)null);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicineId, 1), CancellationToken.None);

        Assert.Equal(ClaimStatus.Rejected, result.Status);
        Assert.Equal(ClaimRejectionCode.MedicineNotFound, result.RejectionCode);
        Assert.Contains("medicine", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Guid.Empty, result.ClaimId);
        _claimRepository.Verify(x => x.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_Rejects_WhenHydraHeadCountInvalid()
    {
        ClaimAdjudicationService service = CreateService([new HydraHeadCountRule()], [new HydraQuantityAdjuster()]);
        Patient patient = CreatePatient(Species.Hydra, headCount: 0);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "Hydra Tonic", ContainsSilver = false, BaseCost = 5m };
        ConfigureData(patient, medicine);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, 1), CancellationToken.None);

        Assert.Equal(ClaimStatus.Rejected, result.Status);
        Assert.Equal(ClaimRejectionCode.InvalidHydraHeadCount, result.RejectionCode);
        Assert.Contains("head", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAsync_StopsRulePipeline_OnFirstFailure()
    {
        AlwaysFailRule failFirst = new AlwaysFailRule();
        TrackingRule wouldRunSecond = new TrackingRule();
        ClaimAdjudicationService service = CreateService([failFirst, wouldRunSecond], []);
        Patient patient = CreatePatient(Species.Phoenix);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "Bloom", ContainsSilver = false, BaseCost = 1m };
        ConfigureData(patient, medicine);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, 1), CancellationToken.None);

        Assert.Equal(ClaimStatus.Rejected, result.Status);
        Assert.True(failFirst.WasExecuted);
        Assert.False(wouldRunSecond.WasExecuted);
    }

    [Fact]
    public async Task SubmitAsync_NonHydra_LeavesQuantityUnchanged_WhenHydraAdjusterRegistered()
    {
        ClaimAdjudicationService service = CreateService([new WerewolfSilverAllergyRule()], [new HydraQuantityAdjuster()]);
        Patient patient = CreatePatient(Species.Phoenix, headCount: 99);
        Medicine medicine = new Medicine { Id = Guid.NewGuid(), Name = "Sunpetal", ContainsSilver = false, BaseCost = 2.5m };
        ConfigureData(patient, medicine);

        SubmitClaimResult result = await service.SubmitAsync(new SubmitClaimRequest(patient.Id, medicine.Id, 4), CancellationToken.None);

        Assert.Equal(ClaimStatus.Approved, result.Status);
        Assert.Equal(4, result.EffectiveQuantity);
        Assert.Equal(10m, result.TotalCost);
    }

    private ClaimAdjudicationService CreateService(IEnumerable<IClaimRule> rules, IEnumerable<IQuantityAdjuster> adjusters)
    {
        _claimRepository
            .Setup(x => x.AddAsync(It.IsAny<Claim>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new ClaimAdjudicationService(
            _patientRepository.Object,
            _medicineRepository.Object,
            _claimRepository.Object,
            rules,
            adjusters,
            NullLogger<ClaimAdjudicationService>.Instance);
    }

    private void ConfigureData(Patient patient, Medicine medicine)
    {
        _patientRepository.Setup(x => x.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        _medicineRepository.Setup(x => x.GetByIdAsync(medicine.Id, It.IsAny<CancellationToken>())).ReturnsAsync(medicine);
    }

    private static Patient CreatePatient(Species species, int headCount = 1, bool isActive = true)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            TrueName = "Test",
            Species = species,
            HeadCount = headCount,
            IsActive = isActive
        };
    }

    private class TrackingRule : IClaimRule
    {
        public string Name => nameof(TrackingRule);
        public bool WasExecuted { get; private set; }

        public Task<RuleResult> EvaluateAsync(ClaimContext context, CancellationToken cancellationToken)
        {
            WasExecuted = true;
            return Task.FromResult(RuleResult.Success());
        }
    }

    private class AlwaysFailRule : IClaimRule
    {
        public string Name => nameof(AlwaysFailRule);
        public bool WasExecuted { get; private set; }

        public Task<RuleResult> EvaluateAsync(ClaimContext context, CancellationToken cancellationToken)
        {
            WasExecuted = true;
            return Task.FromResult(RuleResult.Failure("Blocked by test rule.", ClaimRejectionCode.InvalidQuantity));
        }
    }
}
