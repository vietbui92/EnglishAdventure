# Architectural Decisions

Each decision below is **confirmed by the project owner**. Its status shows how far the code reflects it as of 2026-10-23.

Status: ✅ Implemented · 🟡 Partially implemented · 📋 Planned · 🔍 Needs validation

| # | Decision | Status |
|---|---|---|
| 1 | Modular monolith, not microservices initially | 🟡 / 🔍 |
| 2 | Clean Architecture with pragmatic DDD | 🟡 |
| 3 | CQRS where it provides value | ✅ |
| 4 | SQL Server and EF Core | 🟡 |
| 5 | ASP.NET Core Identity as the identity foundation | ✅ |
| 6 | JWT access tokens and refresh-token rotation/revocation | ✅ |
| 7 | Admin, Parent, and Child account types | 🟡 |
| 8 | RBAC combined with fine-grained permissions | 📋 |
| 9 | Azure as the deployment platform | 📋 |

---

## ADR-1: Modular monolith first

**Decision.** Ship one deployable API, organised internally by business module. Split into services only when there is a proven need.

**Status: 🟡 Single deployable exists. Module boundaries are not defined.** There is one API host and one `DbContext`. Code is grouped by feature folder (`Application/Authentication`, `Application/Foundation`), but there are no module projects, no per-module schemas, and no rules against cross-module access.

**Needs validation.** Decide how modules are expressed. The options are feature folders within the current four projects, or a set of projects per module. Also decide whether modules share one `DbContext`. Decide this before the first learning-content feature.

## ADR-2: Clean Architecture with pragmatic DDD

**Decision.** Four layers: Domain, Application, Infrastructure, and Api. Dependencies point inward. Use DDD tactical patterns (aggregates, value objects, domain exceptions) where the domain is rich, and avoid ceremony where it is not.

**Status: 🟡** Project references follow the rule (see [overview.md](overview.md#2-project-references-and-dependency-rules)). Domain contains only `DomainException` and `AccountType`, so there are no aggregates yet. Nothing enforces the rule automatically.

**Pragmatic choice already in code:** `ApplicationUser` and `RefreshToken` live in Infrastructure as persistence/identity types, not as domain entities. Application reaches them only through ports (`IIdentityUserService`, `IRefreshTokenService`).

## ADR-3: CQRS where it provides value

**Decision.** Model use cases as MediatR commands and queries with FluentValidation validators. Do not require separate read and write stores.

**Status: ✅ Mechanism implemented and tested.** MediatR 14.2.0 and `ValidationBehavior<,>` work. They are verified by unit tests and the `GetFoundationStatus` query. Commands and queries share one `DbContext`. There is no read model separation, which is consistent with "where it provides value".

## ADR-4: SQL Server and EF Core

**Decision.** SQL Server, accessed through EF Core 10. Migrations live in the Infrastructure assembly and are applied explicitly, never at app startup.

**Status: 🟡** One `DbContext` (`EnglishAdventureDbContext`, an `IdentityDbContext`) with SQL Server. First migration `InitialIdentity` exists (Identity + `RefreshTokens`). It is created but not applied to any persistent database. `dotnet ef` runs through the design-time factory in Infrastructure, with Infrastructure as the startup project. That avoids adding the EF Design package to Api. It has been verified on SQL Server LocalDB by the opt-in integration tests (each run migrates a throwaway database).

## ADR-5: ASP.NET Core Identity as the identity foundation

**Decision.** User accounts, password hashing, and lockout use ASP.NET Core Identity with `Guid` keys.

**Status: ✅ (foundation).** `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` and `AddIdentityCore` (API-only, no cookies, no token providers), plus `AddRoles<ApplicationRole>` and EF stores. Settings:
- Lockout is on for new users: 5 failures lead to a 15-minute lockout.
- `RequireUniqueEmail`, backed by a unique filtered index on `NormalizedEmail`.
- Password policy is the Identity defaults.

`IdentityUserService` implements `IIdentityUserService`. Its login policy:
- **Unknown email:** a dummy hash is verified (timing equalisation), then the generic failure is returned.
- **Locked out:** the generic failure is returned and the password is **not** checked, so a locked account cannot be used as a password oracle.
- **Wrong password:** the failure is counted and the generic failure is returned.
- **Correct password on an inactive account:** 403. This is only revealed to someone who knows the password.
- **Success:** the failure count is reset.

**Still open.** Password policy for children. Whether Child accounts sign in with email and password at all (see ADR-7). Email confirmation is not required, because no confirmation workflow exists.

## ADR-6: JWT access tokens + refresh-token rotation and revocation

**Decision.** Short-lived JWT access tokens. Opaque refresh tokens stored as hashes. Each refresh issues a new token and revokes the old one. Reuse of a revoked token is detected per token family.

**Status: ✅ (foundation).**
- **Access token:** HS256, issued by `JwtAccessTokenIssuer` in Infrastructure (`Microsoft.IdentityModel.JsonWebTokens`).
  - Claims: `sub` (user id), `jti`, `iat`/`nbf`/`exp`, `iss`, `aud`, `account_type`, and `role` (only roles actually assigned).
  - No email or other personal data, and no permission claims.
  - Lifetime is 15 min by default (1–60 allowed).
- **Validation:** JwtBearer with `MapInboundClaims = false`. It checks issuer, audience, signature (HS256 only), lifetime (30 s clock skew), and requires `exp`. Claim names are shared through `AuthClaimTypes`.
- **Keys:** `JwtOptions` are bound from `Authentication:Jwt` and validated on start in **every** environment. There is no default or fallback key. The key comes from user secrets in development and from an env var or Key Vault elsewhere.
- **Refresh token:**
  - 64 random bytes (base64url) in the JSON body. Only the SHA-256 hash is stored, under a unique index.
  - Lifetime is 14 days and slides on each rotation. There is no absolute session cap yet.
- **Rotation** (`RefreshTokenService.RotateAsync`):
  1. Look up the token by hash.
  2. Unknown or expired returns 401.
  3. Already revoked counts as **reuse**: the whole family is revoked and 401 is returned.
  4. Otherwise, inside a transaction, `UPDATE … SET RevokedAtUtc, Reason = Rotated, ReplacedByTokenId WHERE Id = @id AND RevokedAtUtc IS NULL`. Then insert the replacement and commit.
  5. If the update matches 0 rows, a concurrent request won. That is treated as reuse, so the family is revoked, the winner's new token included.

  **Policy:** concurrent use of one refresh token ends the session. Clients must serialise refreshes. This is covered by an 8-way concurrency test.
- **Logout:** revokes every active token in the presented token's family and always returns 204, including for unknown tokens. Access tokens are **not** revoked and live until `exp`.
- **Ineligible account:** if the account became inactive or locked, refresh revokes the new token's family.
- **Transport:** response body (decided 2026-10-23). An HttpOnly cookie variant for the browser client can be added later.

**Trade-offs and remaining risks.**
- Access tokens cannot be revoked early. Mitigations are a short lifetime and DB checks in `me` and `refresh`.
- Expired and revoked rows accumulate. A cleanup job is needed; the `ExpiresAtUtc` index exists.
- The symmetric key is shared by issuer and validator in the same process. Move to asymmetric keys if other services ever validate tokens.
- No rate limiting on `login` or `refresh`.
- Refresh tokens in the response body are readable by JavaScript in a browser client.

## ADR-7: Admin, Parent, and Child account types

**Decision.** There are three account types.

**Status: 🟡** `Domain.Users.AccountType` (`Admin=1, Parent=2, Child=3`) is stored on `ApplicationUser` as an int. A database check constraint (`CK_AspNetUsers_AccountType`) rejects other values. It is emitted as the `account_type` claim (enum name) and returned by `/me`.
- The account type is **not** a role and grants nothing by itself. Authorization will use roles and permissions (ADR-8).
- Clients cannot choose or change it: there is no registration endpoint and no seeded admin.
- There are no Parent–Child relationships, no child profiles, and no type-specific rules.

**Needs validation.** How Child accounts authenticate, given children's privacy requirements (for example, sign-in managed by a parent rather than by email). How a Parent is linked to its Children.

## ADR-8: RBAC + fine-grained permissions

**Decision.** Authorize through roles plus granular permissions, enforced by policies.

**Status: 📋 Not implemented.** Only role-name retrieval exists (`IIdentityUserService.GetRolesAsync`) and the token issuer contract takes roles. There is no permission model, no permission claims, and no authorization policies or handlers. `AddAuthorization()` is called with no configuration; only `[Authorize]` (authenticated user) is used, on `/me`.

**Needs validation.** Where permissions are stored (role claims or a custom table). Whether permissions go into the JWT or are resolved on each request. Policy naming conventions.

## ADR-9: Azure hosting

**Decision.** Deploy to Azure.

**Status: 📋 Not implemented.** There are no Dockerfiles, IaC (Bicep/Terraform), pipelines, Key Vault integration, or Application Insights. The `Azure.Identity` package appears only as a transitive dependency of the SQL Server provider. It is not used.

**Needs validation.** Hosting target (App Service or Container Apps), Azure SQL tier, secrets via Key Vault and managed identity, and the CI/CD platform.
