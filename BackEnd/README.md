# English Adventure — Backend API Foundation

Clean Architecture foundation for the **English Adventure** platform (.NET 10, ASP.NET Core Web API, CQRS with MediatR, EF Core + SQL Server).

This repository contains the project structure, validation pipeline, API baseline, and the **authentication foundation** (ASP.NET Core Identity, JWT access tokens, rotating refresh tokens). Account provisioning, authorization policies, and learning, lesson, game, and child-profile features are not implemented yet.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (verified with `10.0.402`)
- SQL Server or LocalDB (for future migrations and optional integration tests — **not** required for `/health`)

## Solution layout

| Project | Role |
|--------|------|
| `src/EnglishAdventure.Domain` | Domain entities, rules, exceptions (no outward dependencies) |
| `src/EnglishAdventure.Application` | Commands/queries, handlers, validation, application interfaces |
| `src/EnglishAdventure.Infrastructure` | EF Core, SQL Server, persistence implementations |
| `src/EnglishAdventure.Api` | HTTP API, composition root, middleware, Scalar docs |
| `tests/EnglishAdventure.Domain.UnitTests` | Domain rule tests |
| `tests/EnglishAdventure.Application.UnitTests` | Validation pipeline and handler tests |
| `tests/EnglishAdventure.Api.IntegrationTests` | Host startup and HTTP integration tests |

Dependency direction: **Api → Application, Infrastructure**; **Application → Domain**; **Infrastructure → Application, Domain**.

## Commands

From the repository root:

```powershell
dotnet restore EnglishAdventure.sln
dotnet build EnglishAdventure.sln --no-restore
dotnet test EnglishAdventure.sln --no-build
```

Run the API:

```powershell
dotnet run --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj
```

- Health (liveness): `GET https://localhost:<port>/health`
- Foundation example (CQRS smoke test): `GET https://localhost:<port>/api/v1/foundation/status`
- Scalar API reference (Development only): `/scalar/v1` (after OpenAPI is mapped)

## SQL Server configuration

Connection string name: **`EnglishAdventure`** (`ConnectionStrings:EnglishAdventure`).

Configure via:

- `src/EnglishAdventure.Api/appsettings.json` (LocalDB example for local dev)
- `appsettings.Development.json` (localhost example)
- Environment variable: `ConnectionStrings__EnglishAdventure`
- User secrets (recommended for machine-specific dev settings):

```powershell
dotnet user-secrets init --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj
dotnet user-secrets set "ConnectionStrings:EnglishAdventure" "Server=localhost;Database=EnglishAdventure_Dev;Trusted_Connection=True;TrustServerCertificate=True;" --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj
```

Do not commit production credentials. The API does **not** run migrations or destructive schema changes at startup.

## EF Core migrations

Migrations live in `src/EnglishAdventure.Infrastructure/Persistence/Migrations`. The first one is `InitialIdentity` (Identity tables + `RefreshTokens`). The API **never** applies migrations at startup.

The `dotnet ef` tools use Infrastructure as both project and startup project, through `EnglishAdventureDbContextFactory`. It reads `ConnectionStrings__EnglishAdventure` from the environment and otherwise targets the LocalDB database `EnglishAdventure`.

```powershell
# Create a new migration (does not touch any database)
dotnet ef migrations add <Name> `
  --project src/EnglishAdventure.Infrastructure/EnglishAdventure.Infrastructure.csproj `
  --startup-project src/EnglishAdventure.Infrastructure/EnglishAdventure.Infrastructure.csproj `
  --output-dir Persistence/Migrations

# Review the SQL before applying anything
dotnet ef migrations script --idempotent `
  --project src/EnglishAdventure.Infrastructure/EnglishAdventure.Infrastructure.csproj `
  --startup-project src/EnglishAdventure.Infrastructure/EnglishAdventure.Infrastructure.csproj

# Apply to YOUR local development database only
$env:ConnectionStrings__EnglishAdventure = "Server=(localdb)\mssqllocaldb;Database=EnglishAdventure;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet ef database update `
  --project src/EnglishAdventure.Infrastructure/EnglishAdventure.Infrastructure.csproj `
  --startup-project src/EnglishAdventure.Infrastructure/EnglishAdventure.Infrastructure.csproj
```

For shared or production databases, generate an idempotent script and apply it through a reviewed deployment step.

Install the EF CLI tool once if needed:

```powershell
dotnet tool install --global dotnet-ef
```

## Testing strategy

- **Unit tests** — domain and application layers (always run in CI/local).
- **Integration tests** — `WebApplicationFactory` verifies the host starts and `/health` responds without hitting the database.
- **SQL Server integration** — set `ENGLISH_ADVENTURE_TEST_SQL_CONNECTION` to a reachable SQL Server connection string to run the `[SqlServerFact]` tests (connectivity plus the end-to-end auth flows). The auth tests create a uniquely named `EnglishAdventure_AuthTests_<guid>` database, apply the real migrations, and drop only that database afterwards. Without the variable these tests are reported as **Skipped**.

Example:

```powershell
$env:ENGLISH_ADVENTURE_TEST_SQL_CONNECTION = "Server=localhost;Database=EnglishAdventure_Test;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet test tests/EnglishAdventure.Api.IntegrationTests/EnglishAdventure.Api.IntegrationTests.csproj
```

## Authentication

| Endpoint | Auth | Result |
|---|---|---|
| `POST /api/v1/auth/login` `{ email, password }` | anonymous | 200 token pair · 400 validation · 401 generic failure (wrong email/password **or** locked) · 403 inactive account |
| `POST /api/v1/auth/refresh` `{ refreshToken }` | anonymous | 200 new token pair (old refresh token is consumed) · 401 |
| `POST /api/v1/auth/logout` `{ refreshToken }` | anonymous | 204, always (idempotent). Revokes the session; issued access tokens stay valid until expiry. |
| `GET /api/v1/auth/me` | Bearer | 200 `{ userId, email, accountType, roles }` · 401 |

There is no registration endpoint and no seeded administrator. Accounts are created by a separate provisioning workflow (not built yet).

### Required configuration

`Authentication:Jwt:SigningKey` has **no default**. The API refuses to start without a base64 key of at least 256 bits, in every environment. Issuer, audience, and lifetimes are in `appsettings.json` and are not secrets.

Local development (user secrets; `UserSecretsId` is already set on the Api project):

```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "Authentication:Jwt:SigningKey" ([Convert]::ToBase64String($bytes)) --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj
```

Other environments: set the `Authentication__Jwt__SigningKey` environment variable from a secret store (Azure Key Vault reference / App Service setting). Never commit the key. Rotating it invalidates every outstanding access token. Refresh tokens are unaffected.

| Setting | Default |
|---|---|
| `Authentication:Jwt:AccessTokenLifetimeMinutes` | 15 (allowed 1–60) |
| `Authentication:RefreshToken:LifetimeDays` | 14 (allowed 1–90; sliding — each rotation issues a fresh 14-day token) |

Design and security notes: [docs/architecture/decisions.md](docs/architecture/decisions.md) (ADR-5/6/7).

## Key packages

- **MediatR** (Apache-2.0) — CQRS dispatch
- **FluentValidation** — request validation with `ValidationBehavior`
- **EF Core 10 + SQL Server** — persistence
- **Scalar.AspNetCore** — API documentation in Development
