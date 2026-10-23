# CLAUDE.md — English Adventure Backend

## Product

English Adventure is a web platform where children learn English through structured lessons, levels, and mini-games. Planned account types: **Admin**, **Parent**, **Child**. This repository is the ASP.NET Core Web API backend.

Current state: foundation + a **partial, non-functional** authentication slice. The API host does not start yet (see [docs/project-status.md](docs/project-status.md)).

## Architecture (actual)

.NET 10 (`net10.0`, SDK 10.0.402), Clean Architecture, CQRS via MediatR, FluentValidation, EF Core 10 + SQL Server.

```
src/EnglishAdventure.Domain          // no project/package references
src/EnglishAdventure.Application     // -> Domain; MediatR, FluentValidation
src/EnglishAdventure.Infrastructure  // -> Application, Domain; EF Core SqlServer, Identity EF stores
src/EnglishAdventure.Api             // -> Application, Infrastructure; composition root, controllers
tests/EnglishAdventure.Domain.UnitTests
tests/EnglishAdventure.Application.UnitTests
tests/EnglishAdventure.Api.IntegrationTests   // WebApplicationFactory<Program>
```

Details: [docs/architecture/overview.md](docs/architecture/overview.md). Decisions: [docs/architecture/decisions.md](docs/architecture/decisions.md). Status: [docs/project-status.md](docs/project-status.md).

## Architectural constraints

- Dependencies point inward only. Domain references nothing. Application references only Domain. Never reference Infrastructure or ASP.NET Core types from Application or Domain.
- Business rules belong in Domain entities/value objects. Application handlers orchestrate; controllers only map HTTP to `IMediator.Send`.
- Application defines interfaces (e.g. `IIdentityUserService`); Infrastructure implements them; each layer registers its own services in its `DependencyInjection.cs` (`AddApplication`, `AddInfrastructure`, `AddApi*`).
- One folder per use case: `Application/<Feature>/Commands|Queries/<UseCase>/` holding the request, handler, validator, and response.
- Validation goes in FluentValidation validators, run by `ValidationBehavior`. Do not validate input by hand in handlers.
- Errors: throw the typed exceptions in `Application/Common/Exceptions` or `DomainException`. `GlobalExceptionHandler` maps them to ProblemDetails. When you add an exception type, add its mapping too.
- `Program.cs` sets `ValidateOnBuild = true`. Every MediatR handler's dependencies must be registered, or the host will not start.
- The API never runs migrations at startup. Migrations live in the Infrastructure assembly (`Persistence/Migrations`).

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
```

Optional SQL test: set `ENGLISH_ADVENTURE_TEST_SQL_CONNECTION`. Without it, the test returns early and verifies nothing.

## Security and secrets

- Never commit secrets: passwords, JWT signing keys, SQL credentials. Use `dotnet user-secrets` (not initialised yet; there is no `UserSecretsId`), environment variables (`ConnectionStrings__EnglishAdventure`), or, later, Azure Key Vault.
- Do not print secret values in output, logs, docs, or commits.
- Store refresh tokens only as hashes (`RefreshTokenHasher`, SHA-256). Never persist or log raw tokens.
- Outside Development, never return internal exception details to clients (see `GlobalExceptionHandler`).
- Do not apply migrations or modify a database unless explicitly asked.

## Working rules

- Read the existing code and docs before you change anything. Do not assume a planned feature exists. Check [docs/project-status.md](docs/project-status.md).
- Keep changes small and reviewable, one use case or concern at a time, with tests.
- **Ask before significant architectural changes**: new projects or layers, new packages, a different DbContext base type, changes to dependency direction, module boundaries, or the auth model.
- Do not install or upgrade packages without approval.
- Update `docs/project-status.md` when a feature's status changes.
