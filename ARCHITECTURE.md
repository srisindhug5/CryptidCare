# Architecture Notes — CryptidCare Claims API

This document explains *why* the codebase is shaped the way it is. It only describes what is actually implemented in this repository; anything explicitly marked **(roadmap)** is out of scope for the take-home timebox.

---

## 1. High-level shape

A layered solution with one-way dependencies:

```
Domain  ──►  Application  ──►  Data  ──►  Api
                              (Application & Domain referenced by Data)
                              (all three referenced by Api)
```

| Project | Responsibility |
|---|---|
| `CryptidCare.Domain` | Entities (`Patient`, `Medicine`, `Claim`, `ClaimRuleEvaluation`) and enums (`Species`, `ClaimStatus`, `ClaimRejectionCode`). No framework dependencies. |
| `CryptidCare.Application` | Adjudication service, rule contracts (`IClaimRule`, `IQuantityAdjuster`), concrete rules, repository abstractions, request/result DTOs. |
| `CryptidCare.Data` | EF Core `DbContext`, repository implementations, migration & seeding helpers. |
| `CryptidCare.Api` | Controllers, AutoMapper profile, Swagger, correlation-id middleware, global exception handler, health checks, composition root (`Program.cs`). |
| `CryptidCare.Tests` | xUnit + Moq tests for `ClaimAdjudicationService`. |

The API project is the only composition root — it calls `ApiStartup.ConfigureServices`, `ApplicationStartup.ConfigureServices`, and `DataStartup.ConfigureServices` in `Program.cs` and wires the middleware pipeline.

---

## 2. The adjudication flow

`POST /api/claims` →

1. Model binding + DataAnnotations validation produce a `ValidationProblemDetails` 400 if the request body is malformed.
2. `ClaimsController` maps the HTTP request to `SubmitClaimRequest` (Application DTO) and calls `IClaimAdjudicationService.SubmitClaimAsync`.
3. The service:
   - Loads `Patient` and `Medicine` via repositories (returns a structured rejection if either is missing or the patient is inactive).
   - Builds a `ClaimContext` and runs each registered `IClaimRule` in DI order.
   - Records every rule outcome on the `Claim` as a `ClaimRuleEvaluation` row (rule name, pass/fail, reason, rejection code, UTC timestamp).
   - **Short-circuits on the first failing rule** — sets the claim to `Rejected` and persists the audit trail.
   - If all rules pass, runs each registered `IQuantityAdjuster` to compute `EffectiveQuantity`, calculates `TotalCost = EffectiveQuantity * Medicine.BaseCost`, marks the claim `Approved`, persists it.
4. The controller maps the result back to an HTTP response: `200` for approved/rejected with structured payload, `400` only for input/validation problems.

`GET /api/claims/{id}` returns the persisted claim **with its full rule-evaluation audit trail**, so reviewers can see exactly which rules ran, in what order, and why each passed or failed.

---

## 3. Why a rule engine instead of `if`/`else`?

The brief explicitly required: *"adding a new rule must not require modifying the core adjudication service."*

That is achieved with two single-purpose interfaces:

```csharp
public interface IClaimRule
{
    string Name { get; }
    Task<RuleResult> EvaluateAsync(ClaimContext context, CancellationToken ct);
}

public interface IQuantityAdjuster
{
    Task AdjustAsync(ClaimContext context, CancellationToken ct);
}
```

`ClaimAdjudicationService` depends on `IEnumerable<IClaimRule>` and `IEnumerable<IQuantityAdjuster>`. Adding a new rule is a three-step change with **zero edits to the adjudication service**:

1. Implement the interface in `CryptidCare.Application/Rules/`.
2. Register it in `CryptidCare.Application/Configuration/Startup.cs`.
3. Add a unit test.

The two required rules are implemented this way:

- `WerewolfSilverAllergyRule` — rejects with `WerewolfSilverMedicine` when `Species == Werewolf` and `Medicine.ContainsSilver`.
- `HydraHeadCountRule` + `HydraQuantityAdjuster` — `HeadCount` must be ≥ 1 for Hydras (otherwise rejected with `InvalidHydraHeadCount`); the adjuster multiplies `EffectiveQuantity` by `HeadCount`.

Rule ordering is the DI registration order. Cheap/safety-critical rules are registered first so the engine fails fast.

---

## 4. Persistence

- **EF Core 10** against SQL Server (LocalDB by default in checked-in settings; SQL Server 2022 in Docker via `docker-compose.yml` when you override the connection string).
- One `DbContext` (`ClaimsDbContext`) with sets for `Patients`, `Medicines`, `Claims`, and `ClaimRuleEvaluations`.
- Per-entity `IEntityTypeConfiguration` classes hold mapping (precision on `decimal`, indexes on `Claim.PatientId`/`MedicineId`/`Status`, cascade from `Claim` → `ClaimRuleEvaluation`).
- Repositories (`PatientRepository`, `MedicineRepository`, `ClaimRepository`) expose just the queries the application layer actually needs.
- On startup, `DataStartup.ApplyPersistenceAsync` calls `Database.MigrateAsync()` and then seeds a small set of demo patients/medicines via `ClaimsDbSeeder` so the API is usable immediately.

---

## 5. Cross-cutting concerns

| Concern | How it's implemented |
|---|---|
| Authentication | `ApiKeyAuthenticationHandler` validates `X-Api-Key` against `Authentication:ApiKey` using SHA-256 hashes compared in constant time. `[Authorize]` on `ClaimsController`; `/health/*` are explicitly `AllowAnonymous`. Stubbed for the timebox; production would swap in JWT/OIDC/mTLS. |
| Correlation IDs | `CorrelationIdMiddleware` reads or generates `X-Correlation-Id`, stores it on `HttpContext.Items`, and attaches it to the response and the logging scope. |
| Error handling | `GlobalExceptionHandler` (`IExceptionHandler`) maps unhandled exceptions to RFC 7807 `ProblemDetails` and logs with the correlation id. Domain rejections never throw — they flow back as a structured `SubmitClaimResult`. |
| Logging | `ILogger<T>` everywhere; structured properties (`ClaimId`, `RuleName`, `EffectiveQuantity`). Application Insights is wired conditionally if `ApplicationInsights:ConnectionString` is configured. |
| API docs | Swashbuckle generates Swagger UI from XML comments at `/swagger`. The Authorize button picks up the API key for "Try it out" calls. |
| Health checks | `/health`, `/health/ready` (DB + business check), `/health/live` (process liveness). All anonymous. |

---

## 6. Testing strategy

`CryptidCare.Tests` covers the core business logic of `ClaimAdjudicationService`:

- Happy path approval with quantity multiplication for Hydras.
- Werewolf + silver rejection produces the right `ClaimRejectionCode` and persists the rejected claim.
- Inactive patient and unknown medicine return structured rejections without throwing.
- Audit-trail entries are recorded for every rule that runs, including the failing one when a rule short-circuits.

Repositories are mocked with Moq, so the tests run without a database. The rules themselves are simple enough that they're exercised through the service rather than tested in isolation, which keeps the test suite focused on the contract reviewers care about.

---

## 7. Configuration

Later configuration sources **override** earlier ones (standard ASP.NET Core host defaults): `appsettings.json`, then `appsettings.{Environment}.json`, then User Secrets when Development, then environment variables, then command-line arguments.

The single critical setting is `ConnectionStrings:ClaimsDatabase`. Checked-in defaults use **SQL Server LocalDB** for a Windows machine without Docker; override to the `docker-compose.yml` SQL Server connection string when running the container locally. Production deployments should inject the connection string and optional `ApplicationInsights:ConnectionString` via environment variables or a secret manager.

See `CONFIGURATION.md` for the full list of settings and how to override them.

---

## 8. What's intentionally out of scope (roadmap)

To keep the take-home within the 3–4 hour timebox, the following are *not* implemented and are noted only so reviewers know they were considered:

- **Stronger** authentication. A stub `X-Api-Key` scheme is wired up to demonstrate the seam (see *Cross-cutting concerns* above) — production should replace it with JWT bearer + OIDC, or mTLS for pharmacy-to-Cryptid-Care service authentication.
- Rate limiting, CORS allow-listing per environment.
- Soft delete + global query filters.
- Audit columns (`CreatedBy`, `ModifiedBy`) populated from an authenticated principal.
- API versioning (`Asp.Versioning`).
- Distributed tracing beyond Application Insights, distributed caching, retry policies on outbound calls.
- A `Dockerfile` for the API itself (only SQL Server is containerized).

The `ClaimRuleEvaluation` audit trail and the rule-engine extensibility are deliberately the parts that *did* get the time, because they're the parts the brief asked us to demonstrate.

---

## 9. References

- [Clean Architecture (Uncle Bob)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Microsoft .NET Application Architecture Guides](https://learn.microsoft.com/en-us/dotnet/architecture/)
- [RFC 7807 — Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc7807)
