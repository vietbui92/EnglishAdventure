# Project Status

_Last updated: 2026-10-23 (authentication foundation implemented)._

## Summary

The API host starts again. The **authentication foundation** is implemented and tested end-to-end against SQL Server (LocalDB): login, JWT access tokens, rotating refresh tokens with reuse detection, logout, and the current-user endpoint. Authorization (policies, permissions) and account provisioning are not implemented.

## Implemented and verified ✅

- Four-layer Clean Architecture solution on .NET 10, with correct project reference direction.
- MediatR CQRS pipeline with FluentValidation `ValidationBehavior`.
- `GET /api/v1/foundation/status`, `/health`, OpenAPI and Scalar (Development only, with a Bearer security scheme).
- `GlobalExceptionHandler`: validation 400, auth failures 401, ineligible account 403, not found 404, domain 422, other 500 (detail hidden outside Development). `UseStatusCodePages` gives empty-body errors (e.g. the bearer challenge) a ProblemDetails body.
- **Authentication** (`api/v1/auth`):
  - `POST login`, `POST refresh`, `POST logout`, `GET me`.
  - ASP.NET Core Identity Core (`IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`). Lockout after 5 failures for 15 minutes. Unique normalized email.
  - JWT HS256 access tokens (15 min default) with `sub`, `jti`, `account_type`, and `role` claims (roles only when assigned). Options are validated at startup, and there is no fallback key.
  - Opaque 512-bit refresh tokens, stored as SHA-256 hashes. Rotation is atomic, reuse revokes the token family, and logout revokes the family.
- **Account types**: `AccountType` (Admin/Parent/Child), stored as int with a database check constraint. It is emitted as the `account_type` claim.
- **Persistence**: migration `InitialIdentity` (created, **not applied** to any persistent database). Design-time factory for `dotnet ef`.

## Build and test results (2026-10-23, SDK 10.0.402)

| Command | Result |
|---|---|
| `dotnet build EnglishAdventure.sln` | ✅ 0 errors, 12 warnings (all NU1902/NU1903 for transitive `System.Security.Cryptography.Pkcs` 9.0.11; pre-existing) |
| `dotnet test` (no SQL variable) | ✅ 41 passed, 15 skipped (SQL-only), 0 failed |
| Integration tests with `ENGLISH_ADVENTURE_TEST_SQL_CONNECTION` = LocalDB | ✅ 33 passed, 0 failed (throwaway DB created by migration, then dropped) |
| `dotnet run` smoke test | ✅ `/health` 200, `/scalar/v1` 200, OpenAPI lists auth endpoints, anonymous `/me` 401 |

## Not implemented 📋

- Account provisioning / user administration (no way to create users through the API yet). No seeded admin.
- Authorization: role seeding, permission model, policies (ADR-8).
- Password reset, email confirmation, external login, logout-all-sessions.
- Rate limiting on `login`/`refresh` (lockout is per account only).
- Cleanup job for expired/revoked refresh-token rows.
- All learning features: lessons, levels, mini-games, child profiles, Parent–Child linking.
- Architecture tests, CI/CD, Azure infrastructure.

## Known issues and risks

1. Vulnerable transitive `System.Security.Cryptography.Pkcs` 9.0.11 (NU1903 high). Pre-existing, needs a package decision.
2. Access tokens cannot be revoked before expiry. Deactivating an account takes effect on `/me` and `refresh` at once, but other future endpoints will trust the token for up to 15 minutes unless they check the account.
3. How Child accounts sign in is still open (ADR-7). Children currently use email + password like other accounts.
4. Repository is not under version control (no `.git`).

## Recommended next task

Account provisioning (admin creates Parent accounts; Parent creates/links Child accounts), followed by authorization policies (ADR-8).
