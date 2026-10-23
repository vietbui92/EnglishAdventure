# English Adventure — Backend API Foundation

Clean Architecture foundation for the **English Adventure** platform (.NET 10, ASP.NET Core Web API, CQRS with MediatR, EF Core + SQL Server).

This repository contains the project structure, validation pipeline, API baseline, and the **authentication foundation** (own tables for accounts, roles and permissions; PBKDF2 password hashing; JWT access tokens; rotating refresh tokens). Account provisioning, authorization policies, and learning, lesson, game, and child-profile features are not implemented yet.

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

Migrations live in `src/EnglishAdventure.Infrastructure/Persistence/Migrations`. The API **never** applies migrations at startup.

- `InitialCreate` creates the identity tables in `dbo`.
- `RenameAccountsToUsers` renames "account" to "user" without losing data: tables `auth_users` and `auth_user_roles`, columns `UserId` and `UserType`, permissions `users.*`.

The result is `auth_users`, `auth_roles`, `auth_permissions`, `auth_user_roles`, `auth_role_permissions` and `auth_refresh_tokens`. Table names use a module prefix (`auth_`, `family_`, `content_`, `learning_`, `rewards_`). `MigrationSchemaSqlServerTests` checks that the migrations always produce exactly the schema of the current model.

**Applying migrations in Development:** run the API and call `POST /api/v1/database/migrate` (in `EnglishAdventure.Api.http` or Scalar). It creates the database from `ConnectionStrings:EnglishAdventure` if needed, applies every pending migration, and returns the names it applied (an empty list when already up to date). The endpoint answers 404 in every other environment.

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

# Alternative to the migrate endpoint: apply to YOUR local development database only
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
| `GET /api/v1/auth/me` | Bearer | 200 `{ userId, email, userType, roles, permissions }` · 401 |
| `GET /api/v1/admin/permissions` | Bearer + `roles.manage` | 200 permission catalog · 401 · 403 |
| `GET /api/v1/admin/users?page=1&pageSize=20&search=` | Bearer + `users.read` | 200 paged accounts |
| `GET /api/v1/admin/users/{id}` | Bearer + `users.read` | 200 · 404 |
| `POST /api/v1/admin/users` `{ email, password, userType }` | Bearer + `users.manage` | 201 (`Admin` or `Parent`; Parent gets the Parent role) · 400 · 409 duplicate email |
| `POST /api/v1/admin/users/{id}/deactivate` · `/activate` | Bearer + `users.manage` | 204 (deactivation ends all sessions) · 404 · 422 self / last SuperAdmin |
| `PUT /api/v1/admin/users/{id}/roles` `{ roles: [...] }` | Bearer + `roles.manage` | 200 · 404 unknown role · 422 own roles / last SuperAdmin / role not allowed for the user type |

There is no registration endpoint. The first SuperAdmin is created once from the command line (below); after that, a SuperAdmin creates accounts through `POST /api/v1/admin/users`.

### Create the first SuperAdmin

Run this once per environment, after the migrations are applied (`POST /api/v1/database/migrate` in Development). The command also creates the permissions and the three system roles (SuperAdmin, ContentAdmin, Parent) with their default permissions.

```powershell
dotnet user-secrets set "Bootstrap:SuperAdmin:Email" "you@example.com" --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj
dotnet user-secrets set "Bootstrap:SuperAdmin:Password" "<at least 12 chars: upper, lower, digit, symbol>" --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj

dotnet run --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj -- bootstrap-superadmin

# Afterwards, remove the password from user secrets
dotnet user-secrets remove "Bootstrap:SuperAdmin:Password" --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj
```

- **Exit code 0** means the account was created. **Exit code 1** means it was refused: missing settings, weak password, existing email, or a SuperAdmin already exists. The reason is logged; the password never is.
- **Other environments:** set `Bootstrap__SuperAdmin__Email` and `Bootstrap__SuperAdmin__Password` as environment variables instead.
- **When the permission catalog changes in code,** run `dotnet run --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj -- sync-roles`. It only adds what is missing and never removes anything.

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
