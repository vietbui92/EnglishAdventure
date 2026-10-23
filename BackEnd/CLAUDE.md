# CLAUDE.md — English Adventure Backend

## Product

English Adventure is a web platform where children learn English through structured lessons, levels, and mini-games. Planned user types: **Admin**, **Parent**, **Child**. This repository is the ASP.NET Core Web API backend.

Current state: foundation + a **partial, non-functional** authentication slice. The API host does not start yet (see [docs/project-status.md](docs/project-status.md)).

## Architecture (actual)

.NET 10 (`net10.0`, SDK 10.0.402), Clean Architecture, CQRS via MediatR, FluentValidation, EF Core 10 + SQL Server.

```
src/EnglishAdventure.Domain          // no project/package references; Common/, Entities/<Module>/, Enums/, ValueObjects/, Services/, Exceptions/
src/EnglishAdventure.Application     // -> Domain; MediatR, FluentValidation
src/EnglishAdventure.Infrastructure  // -> Application, Domain; EF Core SqlServer; Microsoft.Extensions.Identity.Core (password hashing only)
src/EnglishAdventure.Api             // -> Application, Infrastructure; composition root, controllers
tests/EnglishAdventure.Domain.UnitTests
tests/EnglishAdventure.Application.UnitTests
tests/EnglishAdventure.Api.IntegrationTests   // WebApplicationFactory<Program>
```

Product decisions: [../docs/product/mvp-product-decisions.md](../docs/product/mvp-product-decisions.md). Step-by-step plan (one approved step at a time): [../docs/roadmap/mvp-implementation-plan.md](../docs/roadmap/mvp-implementation-plan.md). Proposed domain model: [docs/architecture/domain-model-proposal.md](docs/architecture/domain-model-proposal.md). Details: [docs/architecture/overview.md](docs/architecture/overview.md). Decisions: [docs/architecture/decisions.md](docs/architecture/decisions.md). Status: [docs/project-status.md](docs/project-status.md).

## Architectural constraints

- Dependencies point inward only. Domain references nothing. Application references only Domain. Never reference Infrastructure or ASP.NET Core types from Application or Domain.
- Business rules belong in Domain entities/value objects. Application handlers orchestrate; controllers only map HTTP to `IMediator.Send`.
- Application defines interfaces (e.g. `IIdentityUserService`); Infrastructure implements them; each layer registers its own services in its `DependencyInjection.cs` (`AddApplication`, `AddInfrastructure`, `AddApi*`).
- One folder per use case: `Application/<Feature>/Commands|Queries/<UseCase>/` holding the request, handler, validator, and response.
- Validation goes in FluentValidation validators, run by `ValidationBehavior`. Do not validate input by hand in handlers.
- Errors: throw the typed exceptions in `Application/Common/Exceptions` or `DomainException`. `GlobalExceptionHandler` maps them to ProblemDetails. When you add an exception type, add its mapping too.
- `Program.cs` sets `ValidateOnBuild = true`. Every MediatR handler's dependencies must be registered, or the host will not start.
- The API never runs migrations at startup. Migrations live in the Infrastructure assembly (`Persistence/Migrations`).
- In Development, migrations are applied by calling `POST /api/v1/database/migrate` (`DatabaseController`, `[DevelopmentOnly]`: 404 in every other environment). Shared and production databases get a reviewed idempotent script.
- **Whenever you change an entity or its EF configuration, run `dotnet ef migrations add <Name>` yourself** (command in README) and review the generated migration. The owner does not run `dotnet ef` by hand. Do not run `dotnet ef database update`; the owner applies migrations through the endpoint.
- **Module structure (ADR-1, decided):**
  - Modules: Family, Content, Learning, Rewards, plus Identity. They are folders in each project, not separate projects.
  - Domain layout: `Common/Entity.cs`, `Entities/<Module>/`, `Enums/`, `ValueObjects/`, `Services/`, `Exceptions/`.
  - Every entity derives from `Entity` (single `Guid Id`); business uniqueness uses unique indexes.
  - Between modules, store IDs only (no navigation properties), but keep database foreign keys.
  - One `DbContext`. No SQL schemas: every table is in `dbo` and named `<module>_<plural_snake_case>` (`auth_users`, `content_lessons`); prefixes `auth_`, `family_`, `content_`, `learning_`, `rewards_`. Columns stay PascalCase. Set the name explicitly with `ToTable(...)` in each configuration.
  - Application uses one repository interface per aggregate (no generic repository, no EF Core in Application).
  - Accounts, roles and permissions are our own Identity-module entities (`Domain/Entities/Identity`, ADR-5). ASP.NET Core Identity tables, stores and `UserManager` are not used. Other modules store the account ID; there is no separate Parent or User entity.
  - Passwords are hashed only through `IPasswordHasher` (Microsoft's PBKDF2 implementation). Never write custom password cryptography.
- **Authorization (ADR-8):**
  - Protect endpoints with `[Authorize(Policy = Permissions.X.Y)]` (`Application/Authorization/Permissions.cs`). Never authorize by role name.
  - A new permission must be added to `Permissions.All`, and to `SystemRoles` if a system role should hold it.
  - Permissions are read per request through `IUserPermissionReader` (1-minute cache), never from the token.
  - `DomainArchitectureTests` and `ApplicationArchitectureTests` enforce these rules.
  - Add entities only from approved plan steps ([domain model proposal](docs/architecture/domain-model-proposal.md)).
- Integration tests override the connection string with `UseSetting`, never `ConfigureAppConfiguration`: `AddInfrastructure` reads it while `Program.cs` builds the host, before `ConfigureAppConfiguration` overrides apply.

## Coding conventions (observed)

- File-scoped namespaces, `sealed` classes and `sealed record` for requests/responses/DTOs.
- Constructor injection into `private readonly _camelCase` fields.
- Nullable enabled, implicit usings enabled.
- Async methods take a `CancellationToken` and pass it on.
- Test names use snake_case sentences (`Handle_throws_validation_exception_when_request_is_invalid`). xUnit only, with no mocking library.

## Commands (verified 2026-10-23)

```powershell
dotnet restore EnglishAdventure.sln
dotnet build EnglishAdventure.sln --no-restore
dotnet test EnglishAdventure.sln --no-build
dotnet run --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj
dotnet run --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj -- sync-roles            # sync permissions + system roles
dotnet run --project src/EnglishAdventure.Api/EnglishAdventure.Api.csproj -- bootstrap-superadmin  # first SuperAdmin (Bootstrap:SuperAdmin:* settings)
```

Administration tasks are command-line verbs (`Api/CommandLine/CommandLineTasks.cs`), never HTTP endpoints.

Optional SQL test: set `ENGLISH_ADVENTURE_TEST_SQL_CONNECTION`. Without it, the test returns early and verifies nothing.

## Security and secrets

- Never commit secrets: passwords, JWT signing keys, SQL credentials. Use `dotnet user-secrets` (not initialised yet; there is no `UserSecretsId`), environment variables (`ConnectionStrings__EnglishAdventure`), or, later, Azure Key Vault.
- Do not print secret values in output, logs, docs, or commits.
- Store refresh tokens and email confirmation tokens only as hashes (`RefreshTokenHasher`, SHA-256). Never persist or log raw tokens, or links that contain them.
- Anonymous auth endpoints carry `[EnableRateLimiting(...)]` (`Api/RateLimiting`). Public endpoints must not reveal whether an email is registered (same status and body for every outcome).
- Emails go through `IEmailSender`; in development they are files in `App_Data/emails` (git-ignored). Links come from `IAppLinks` (`WebApp:BaseUrl`).
- Outside Development, never return internal exception details to clients (see `GlobalExceptionHandler`).
- Do not apply migrations or modify a database unless explicitly asked.

## Working rules

- Read the existing code and docs before you change anything. Do not assume a planned feature exists. Check [docs/project-status.md](docs/project-status.md).
- Keep changes small and reviewable, one use case or concern at a time, with tests.
- **Ask before significant architectural changes**: new projects or layers, new packages, a different DbContext base type, changes to dependency direction, module boundaries, or the auth model.
- Do not install or upgrade packages without approval.
- Update `docs/project-status.md` when a feature's status changes.
