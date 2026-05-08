# Developer Guidelines — CryptidCare Claims API

Practical guidance for working in this codebase. The goal is to keep contributions consistent with the patterns that are already wired up; we deliberately avoid prescribing patterns the project doesn't use today.

---

## Getting started

```bash
dotnet --version              # 10.x
docker compose up -d          # local SQL Server
dotnet build CryptidCare.slnx
dotnet test CryptidCare.slnx
dotnet run --project CryptidCare.Api
```

Swagger UI is at `https://localhost:<port>/swagger`. Health endpoints are `/health`, `/health/ready`, `/health/live`.

---

## Project layout

```
CryptidCare.Domain/         Entities (Patient, Medicine, Claim, ClaimRuleEvaluation), enums.
CryptidCare.Application/    Adjudication service, rule contracts (IClaimRule, IQuantityAdjuster),
                            concrete rules, repository abstractions, request/result DTOs,
                            DI wiring (Configuration/Startup.cs).
CryptidCare.Data/           ClaimsDbContext, EF mappings, repository implementations,
                            ClaimsDbSeeder, DI wiring (Configuration/Startup.cs) including
                            ApplyPersistenceAsync (migrations + seed).
CryptidCare.Api/            Composition root (Program.cs), ClaimsController, AutoMapper profile,
                            CorrelationIdMiddleware, GlobalExceptionHandler, ClaimsApiHealthCheck,
                            Swagger setup.
CryptidCare.Tests/          xUnit + Moq tests for ClaimAdjudicationService.
```

Dependencies flow one way: `Domain ← Application ← Data ← Api`. Don't introduce a back-reference.

---

## How to add a new rule

The brief explicitly required: *"adding a new rule must not require modifying the core adjudication service."* That extension point is `IClaimRule` (reject/approve) and `IQuantityAdjuster` (mutate quantity after rules pass). Pick whichever fits.

1. **Implement the contract** in `CryptidCare.Application/Rules/`.

   ```csharp
   public sealed class VampireSunscreenDiscountRule : IClaimRule
   {
       public string Name => nameof(VampireSunscreenDiscountRule);

       public Task<RuleResult> EvaluateAsync(
           ClaimContext context,
           CancellationToken cancellationToken)
       {
           if (context.Patient.Species == Species.Vampire &&
               context.Medicine.Name.Contains("Sunscreen", StringComparison.OrdinalIgnoreCase))
           {
               // Approve but flag — the audit trail will record this rule passed.
               return Task.FromResult(RuleResult.Success("Vampire sunscreen discount applied."));
           }

           return Task.FromResult(RuleResult.Success());
       }
   }
   ```

2. **Register it** in `CryptidCare.Application/Configuration/Startup.cs`. Order matters — cheap/safety-critical rules first so the engine fails fast:

   ```csharp
   services.AddScoped<IClaimRule, WerewolfSilverAllergyRule>();
   services.AddScoped<IClaimRule, HydraHeadCountRule>();
   services.AddScoped<IClaimRule, VampireSunscreenDiscountRule>(); // new
   ```

3. **Test it** in `CryptidCare.Tests/ClaimAdjudicationServiceTests.cs` (or a new file). Existing tests show the pattern: mock the three repositories with Moq, exercise the service, assert the `SubmitClaimResult` and the persisted `ClaimRuleEvaluation` entries.

You should not need to touch `ClaimAdjudicationService` itself.

For a quantity-adjustment rule (e.g. "Phoenix gets +1 dose because it'll burn up some"), implement `IQuantityAdjuster` instead. Adjusters run in DI order *after* all rules have passed, mutating `context.EffectiveQuantity`.

---

## Conventions used in this codebase

- **Async I/O.** Every repository, rule, and service method takes a `CancellationToken` and is `async`.
- **Constructor injection only.** No service-locator, no `IServiceProvider.GetService` at runtime.
- **Plural collection injection for plugins.** The adjudication service takes `IEnumerable<IClaimRule>` and `IEnumerable<IQuantityAdjuster>`. Add new ones by registering them; don't change consumers.
- **Domain rejections do not throw.** Failed rules return a `RuleResult` with a `ClaimRejectionCode`; the service maps that to `SubmitClaimResult`. Exceptions are reserved for genuinely unexpected failures and are caught by `GlobalExceptionHandler`.
- **Structured logs.** Use named placeholders (`logger.LogInformation("Claim {ClaimId} approved", claim.Id)`), not string interpolation. Don't log credentials or PII.
- **No code comments restating the code.** Only add a comment when intent or trade-offs aren't obvious from the code itself.

---

## Pre-merge checklist

- [ ] `dotnet build CryptidCare.slnx` is clean (no new warnings).
- [ ] `dotnet test CryptidCare.slnx` passes.
- [ ] Any new rule has a unit test covering both pass and fail paths.
- [ ] Any new public API has XML documentation (Swagger reads it).
- [ ] No connection strings, keys, or other secrets added to source.
- [ ] If you added an EF entity or property, you ran `dotnet ef migrations add <Name> --project CryptidCare.Data --startup-project CryptidCare.Api`.

---

## Where things live (quick reference)

| If you want to… | Look at… |
|---|---|
| Add a rule | `CryptidCare.Application/Rules/` and `Configuration/Startup.cs` |
| Change adjudication orchestration | `CryptidCare.Application/Services/ClaimAdjudicationService.cs` |
| Add an entity or column | `CryptidCare.Domain/Entities/` + EF mapping in `CryptidCare.Data/Persistence/` |
| Change an HTTP contract | `CryptidCare.Api/Controllers/ClaimsController.cs` and `Models/` + `Mapping/ClaimApiMappingProfile.cs` |
| Tweak request/response error shape | `CryptidCare.Api/ExceptionHandling/GlobalExceptionHandler.cs` |
| Adjust health checks | `CryptidCare.Api/HealthChecks/ClaimsApiHealthCheck.cs` and `Configuration/Startup.cs` |

For the why behind these choices, see [`ARCHITECTURE.md`](ARCHITECTURE.md).
