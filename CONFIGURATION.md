# Configuration Guide — CryptidCare Claims API

The API binds settings through the standard ASP.NET Core configuration chain. **Later sources override earlier ones** (typical host order): `appsettings.json`, then `appsettings.{Environment}.json`, then User Secrets when `ASPNETCORE_ENVIRONMENT` is Development, then environment variables (`__` nests keys, e.g. `ConnectionStrings__ClaimsDatabase`), then command-line arguments.

You usually override JSON defaults with environment variables or User Secrets in Development.

A secret manager such as Azure Key Vault can be added on top via host configuration; this is **not** wired by default.

---

## Required setting

| Key | Purpose |
|---|---|
| `ConnectionStrings:ClaimsDatabase` | SQL Server connection string used by EF Core. |

Checked-in `appsettings.json` and `appsettings.Development.json` default to **SQL Server LocalDB** so you can run on Windows without Docker:

```
Server=(localdb)\MSSQLLocalDB;Database=CryptidCareClaims;Integrated Security=true;TrustServerCertificate=true
```

To run against the container from `docker-compose.yml` instead, override `ConnectionStrings:ClaimsDatabase` (same user/password as `MSSQL_SA_PASSWORD`, default `StrongPassword!123`):

```
Server=localhost,1433;Database=CryptidCareClaims;User Id=sa;Password=StrongPassword!123;TrustServerCertificate=true
```

The **Staging** and **Production** profiles in `launchSettings.json` set the Docker-style string for local checks.

> **Production:** override the connection string from environment variables or a secret manager and use `TrustServerCertificate=false` against a properly issued certificate.

### Override examples

User Secrets (dev):

```bash
cd CryptidCare.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ClaimsDatabase" "Server=...;User Id=...;Password=..."
```

Environment variable (any host):

```bash
# PowerShell
$env:ConnectionStrings__ClaimsDatabase = "Server=...;User Id=...;Password=..."

# bash
export ConnectionStrings__ClaimsDatabase="Server=...;User Id=...;Password=..."
```

Docker Compose for local SQL also reads `MSSQL_SA_PASSWORD` from the environment (defaulting to `StrongPassword!123`), so you can rotate the local password with:

```bash
MSSQL_SA_PASSWORD='AnotherStrong!Pwd1' docker compose up -d
```

…and then point the API at the same password.

---

## Authentication

The `/api/claims/*` endpoints are protected by an API key. Health endpoints (`/health/*`) are anonymous.

| Key | Effect |
|---|---|
| `Authentication:ApiKey` | Expected value of the `X-Api-Key` request header. **Required** for `/api/claims/*` calls; if unset, every request returns 401. |

`appsettings.Development.json` ships a non-secret default (`dev-cryptidcare-local-key-change-me`) so a fresh clone is usable. `appsettings.json` deliberately has no default — production environments must inject the key via environment variable or secret manager:

```bash
# bash
export Authentication__ApiKey="<generated-strong-key>"

# PowerShell
$env:Authentication__ApiKey = "<generated-strong-key>"
```

The handler hashes both the provided and configured keys with SHA-256 and compares them in constant time (`CryptographicOperations.FixedTimeEquals`) to avoid timing side-channels. This is intended as a stub — production should swap it for JWT bearer / OIDC / mTLS as appropriate.

---

## Optional settings

| Key | Default | Effect |
|---|---|---|
| `ApplicationInsights:ConnectionString` | _(unset)_ | When set, Application Insights telemetry is enabled. |
| `Logging:LogLevel:*` | see `appsettings.json` | Standard ASP.NET Core logging levels; `CryptidCare` namespace defaults to `Information`. |

---

## Migrations & seeding

Migrations and demo data are applied automatically at startup by `CryptidCare.Data.Configuration.Startup.ApplyPersistenceAsync()` (called from `Program.cs`):

1. `Database.MigrateAsync()` brings the schema up to date.
2. `ClaimsDbSeeder` inserts demo patients and medicines if the tables are empty.

Manual migration commands (run from the repo root):

```bash
# create a new migration
dotnet ef migrations add <Name> --project CryptidCare.Data --startup-project CryptidCare.Api

# revert to a previous migration
dotnet ef database update <PreviousMigrationName> --project CryptidCare.Data --startup-project CryptidCare.Api
```

---

## Production checklist

- [ ] Connection string injected from a secret manager (not committed).
- [ ] `TrustServerCertificate=false` against a valid certificate.
- [ ] `Authentication:ApiKey` injected from a secret manager (or replaced with JWT/OIDC).
- [ ] `ApplicationInsights:ConnectionString` set if telemetry is required.
- [ ] Database user scoped to the application's required permissions only.
- [ ] No credentials baked into container images or `appsettings.Production.json`.
