# Architectural Decisions

Each decision below is **confirmed by the project owner**. Its status shows how far the code reflects it as of 2026-10-23.

Status: ✅ Implemented · 🟡 Partially implemented · 📋 Planned · 🔍 Needs validation

| # | Decision | Status |
|---|---|---|
| 1 | Modular monolith, not microservices initially | 🟡 (structure decided 2026-10-23) |
| 2 | Clean Architecture with pragmatic DDD | 🟡 |
| 3 | CQRS where it provides value | ✅ |
| 4 | SQL Server and EF Core | 🟡 |
| 5 | Own identity tables (replaced ASP.NET Core Identity on 2026-10-23) | ✅ |
| 6 | JWT access tokens and refresh-token rotation/revocation | ✅ |
| 7 | Admin, Parent, and Child user types | 🟡 |
| 8 | RBAC combined with fine-grained permissions | 🟡 (checks built in S1.1) |
| 9 | Azure as the deployment platform | 📋 |

---

## ADR-1: Modular monolith first

**Decision.** Ship one deployable API, organised internally by business module. Split into services only when there is a proven need.

**Module structure (decided 2026-10-23, plan step S0.4).** All 11 points were approved by the owner. Details are in the [domain model proposal](domain-model-proposal.md) §3, §4 and §10.
1. **Modules:** Family, Content, Learning and Rewards, plus the existing Identity & Access. "Games & Assessment" and "Learning Progress" are merged into **Learning**.
2. **Modules are folders** in the four existing projects, not projects per module.
3. **Domain layout:** `Common/`, `Entities/<Module>/`, `Enums/`, `ValueObjects/`, `Services/` (rules spanning several entities), `Exceptions/`.
4. **One `EnglishAdventureDbContext`** for all modules, so one transaction can cover several modules.
5. **All tables in `dbo`, grouped by a module prefix** in the table name: `auth_`, `family_`, `content_`, `learning_`, `rewards_`. Names are lower-case snake_case and plural (`auth_users`, `content_lessons`); columns stay PascalCase. *Changed 2026-10-23 by the owner; originally one SQL schema per module.*
6. **Between modules, entities store IDs only.** No navigation properties, but database foreign keys are still used.
7. **Every table has a single `Guid Id` primary key.** Business uniqueness uses unique indexes.
8. **Application uses one repository interface per aggregate,** implemented with EF Core in Infrastructure. No generic repository, and no EF Core in Application.
9. **No separate Parent or User business entity.** Other modules store the account ID. Since S0.5, accounts are the `User` entity of the Identity module (`Domain/Entities/Identity`), not Infrastructure framework classes (see ADR-5).
10. **Reflection-based architecture tests** enforce the rules, with no extra package.
11. **`UserType` lives in `Domain/Enums/`.**

**Status: 🟡 Structure in place; no modules built yet.**
- Done: `Domain/Common/Entity.cs` (Guid identity, equality by Id) and `UserType` moved to `Domain/Enums/`.
- Architecture tests:
  - `tests/EnglishAdventure.Domain.UnitTests/Architecture/DomainArchitectureTests.cs`: Domain references only the base class library; classes under `Entities` derive from `Entity`; no entity holds another module's entity.
  - `tests/EnglishAdventure.Application.UnitTests/Architecture/ApplicationArchitectureTests.cs`: Application references no Infrastructure, Api, EF Core or ASP.NET Core assembly.
- `Entities/Identity/` holds the Identity entities (S0.5). `Entities/{Family,Content,Learning,Rewards}/` stay empty until their first approved entity (git does not track empty folders). `ValueObjects/` and `Services/` hold a `README.md`.
- Not yet: no business entity classes until their plan step is approved.

## ADR-2: Clean Architecture with pragmatic DDD

**Decision.** Four layers: Domain, Application, Infrastructure, and Api. Dependencies point inward. Use DDD tactical patterns (aggregates, value objects, domain exceptions) where the domain is rich, and avoid ceremony where it is not.

**Status: 🟡** Project references follow the rule (see [overview.md](overview.md#2-project-references-and-dependency-rules)). Domain contains `Common/Entity`, `Enums/UserType`, `Exceptions/DomainException` and the Identity module's entities (`User`, `UserRole`, `Role`, `RolePermission`, `Permission`; see ADR-5). Since 2026-10-23, architecture tests enforce the Domain and Application dependency rules (see ADR-1).

**Pragmatic choice already in code:** `RefreshToken` stays in Infrastructure as security machinery, not a domain entity. Application reaches login and sessions only through ports (`IIdentityUserService`, `IRefreshTokenService`, `IPasswordHasher`).

## ADR-3: CQRS where it provides value

**Decision.** Model use cases as MediatR commands and queries with FluentValidation validators. Do not require separate read and write stores.

**Status: ✅ Mechanism implemented and tested.** MediatR 14.2.0 and `ValidationBehavior<,>` work. They are verified by unit tests and the `GetFoundationStatus` query. Commands and queries share one `DbContext`. There is no read model separation, which is consistent with "where it provides value".

## ADR-4: SQL Server and EF Core

**Decision.** SQL Server, accessed through EF Core 10. Migrations live in the Infrastructure assembly and are applied explicitly, never at app startup. In Development they are applied by calling `POST /api/v1/database/migrate` (decided 2026-10-23); the endpoint returns 404 in every other environment.

**Status: 🟡** One `DbContext` (`EnglishAdventureDbContext`, a plain `DbContext`) with SQL Server. Migrations: `InitialCreate` creates the identity tables in `dbo`; `RenameAccountsToUsers` renames "account" to "user" (hand-written renames, data kept). `MigrationSchemaSqlServerTests` checks that the migrations produce exactly the model's schema. The owner reset the migration history on 2026-10-23: the old `InitialIdentity` (ASP.NET Identity tables) was deleted, and there is no drop-and-replace migration. `dotnet ef` is used only to create migrations; the migrate endpoint (`ApplyMigrationsCommand` → `IDatabaseMigrator` → `Database.MigrateAsync`) applies them. `dotnet ef` runs through the design-time factory in Infrastructure, with Infrastructure as the startup project. That avoids adding the EF Design package to Api. It has been verified on SQL Server LocalDB by the opt-in integration tests (each run migrates a throwaway database). Until 2026-10-23 those tests wrongly used the Development database `EnglishAdventure_Dev`; see project-status.

## ADR-5: Own identity tables (replaces ASP.NET Core Identity)

**Decision (changed 2026-10-23, plan step S0.5).** Accounts, roles and permissions are our own entities and tables, in `dbo` with the `auth_` prefix (owner's choice). ASP.NET Core Identity's tables, stores and `UserManager` are **not** used. Only Microsoft's password hashing algorithm is reused (`PasswordHasher` from `Microsoft.Extensions.Identity.Core`, PBKDF2), because writing our own password cryptography would be an unnecessary security risk.

*Originally:* ASP.NET Core Identity (`IdentityDbContext`, `AspNet*` tables). The owner changed this so that the database and the entities are designed by the project and easy to read.

**Status: ✅ (foundation).**
- **Entities** (`Domain/Entities/Identity`):
  - `User`, which owns its `UserRole`s.
  - `Role`, which owns its `RolePermission`s.
  - `Permission`.
- **Tables:**
  - `auth_users`: unique `NormalizedEmail`, check constraint `CK_auth_users_UserType`, row version.
  - `auth_roles`: unique `Name`.
  - `auth_permissions`: unique `Code`.
  - `auth_user_roles`: unique (`UserId`, `RoleId`).
  - `auth_role_permissions`: unique (`RoleId`, `PermissionId`).
  - `auth_refresh_tokens`.
- **Lockout** is a rule of `User`: the 5th failed login locks it for 15 minutes.
- **Password hashing** sits behind `IPasswordHasher` (Application), implemented by `IdentityPasswordHasher` (Infrastructure).
- **No password policy is enforced yet**, because no endpoint creates accounts. It is added with account creation (S1.2/S1.3).
- **Dropped:** `AspNetUserClaims`, `AspNetUserLogins` and `AspNetUserTokens` (unused). Password reset and email confirmation will get their own tables when needed (OQ-010).

`IdentityUserService` implements `IIdentityUserService`. Its login policy (unchanged by S0.5):
- **Unknown email:** a dummy hash is verified (timing equalisation), then the generic failure is returned.
- **Locked out:** the generic failure is returned and the password is **not** checked, so a locked account cannot be used as a password oracle.
- **Wrong password:** the failure is counted and the generic failure is returned.
- **Correct password on an inactive account:** 403. This is only revealed to someone who knows the password.
- **Success:** the failure count is reset. If the stored hash uses outdated settings, it is replaced.
- **Concurrent logins** to the same account: a row-version conflict re-applies the change to the reloaded account, so no failed attempt is lost.

**Still open.** Password policy for children. Whether Child users sign in with email and password at all (see ADR-7). Email confirmation is not required, because no confirmation workflow exists.

## ADR-6: JWT access tokens + refresh-token rotation and revocation

**Decision.** Short-lived JWT access tokens. Opaque refresh tokens stored as hashes. Each refresh issues a new token and revokes the old one. Reuse of a revoked token is detected per token family.

**Status: ✅ (foundation).**
- **Access token:** HS256, issued by `JwtAccessTokenIssuer` in Infrastructure (`Microsoft.IdentityModel.JsonWebTokens`).
  - Claims: `sub` (user id), `jti`, `iat`/`nbf`/`exp`, `iss`, `aud`, `user_type`, and `role` (only roles actually assigned).
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

## ADR-7: Admin, Parent, and Child user types

**Decision.** There are three user types.

**Status: 🟡** `Domain.Enums.UserType` (`Admin=1, Parent=2, Child=3`) is stored on `User` as an int. A database check constraint (`CK_auth_users_UserType`) rejects other values. It is emitted as the `user_type` claim (enum name) and returned by `/me`.
- The user type is **not** a role and grants nothing by itself. Authorization will use roles and permissions (ADR-8).
- Clients cannot choose or change it: there is no registration endpoint and no seeded admin.
- There are no Parent–Child relationships, no child profiles, and no type-specific rules.

**Needs validation.** How Child users authenticate, given children's privacy requirements (for example, sign-in managed by a parent rather than by email). How a Parent is linked to its Children.

## ADR-8: RBAC + fine-grained permissions

**Decision.** Authorize through roles plus granular permissions, enforced by policies.

**Status: 🟡 Foundation built (S1.1, 2026-10-23).**
- **Catalog in code:** `Application/Authorization/Permissions.cs` (9 permissions) and `SystemRoles.cs` (SuperAdmin, ContentAdmin, Parent with default permissions).
- **Storage:** `auth_permissions`, `auth_roles`, `auth_role_permissions`, `auth_user_roles`.
- **Policies:** the policy name is the permission code (`[Authorize(Policy = Permissions.Roles.Manage)]`). `PermissionPolicyProvider` builds it, so nothing is registered by hand.
- **Resolution:** `PermissionAuthorizationHandler` → `IUserPermissionReader`. Permissions are read from the database on each request with a 1-minute cache; they are **not** in the JWT. A deactivated account has no permissions. Role names in the token grant nothing.
- **Endpoints:** `/me` returns the permissions. First protected endpoint: `GET /api/v1/admin/permissions`.
- **Roles at runtime:** roles can be created and edited at runtime (owner decision, S1.5). Permissions cannot; they exist only in code.

- **Seeding (S1.2):** `sync-roles` and `bootstrap-superadmin` command-line verbs create the permissions, the system roles and their defaults, and the first SuperAdmin.

- **User management (S1.3):** `api/v1/admin/users`, with `users.read`, `users.manage` and `roles.manage`.
  - Nobody can change their own roles or deactivate themselves.
  - The last active SuperAdmin is protected.
  - Deactivation revokes all refresh tokens.
  - A Parent user can only hold the Parent role, and an Admin user cannot hold it (`UserRoleRules`).

**Not yet:** role management (S1.5).

## ADR-9: Azure hosting

**Decision.** Deploy to Azure.

**Status: 📋 Not implemented.** There are no Dockerfiles, IaC (Bicep/Terraform), pipelines, Key Vault integration, or Application Insights. The `Azure.Identity` package appears only as a transitive dependency of the SQL Server provider. It is not used.

**Needs validation.** Hosting target (App Service or Container Apps), Azure SQL tier, secrets via Key Vault and managed identity, and the CI/CD platform.
