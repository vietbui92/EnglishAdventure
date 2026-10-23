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
  - Own identity entities and tables (S0.5, ADR-5): `User`, `Role`, `Permission` in tables `auth_users`, `auth_roles`, `auth_permissions`, `auth_user_roles`, `auth_role_permissions`, `auth_refresh_tokens` (`dbo`). ASP.NET Core Identity tables are no longer used; only Microsoft's PBKDF2 password hasher is reused. Lockout after 5 failures for 15 minutes (rule in `User`). Unique normalized email.
  - JWT HS256 access tokens (15 min default) with `sub`, `jti`, `user_type`, and `role` claims (roles only when assigned). Options are validated at startup, and there is no fallback key.
  - Opaque 512-bit refresh tokens, stored as SHA-256 hashes. Rotation is atomic, reuse revokes the token family, and logout revokes the family.
  - **Web client cookie (B-F1)**: with the header `X-EnglishAdventure-Client: web`, `login` and `refresh` set the refresh token as cookie `ea_refresh` (`HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth`) and return `refreshToken: null`; `refresh` and `logout` read the cookie and take no body; `logout` deletes it. Without the header the cookie is ignored (CSRF guard) and the body-based flow is unchanged. No CORS: the browser app is served from the same origin (Vite proxy in development).
- **User types**: `UserType` (Admin/Parent/Child), stored as int with a database check constraint. It is emitted as the `user_type` claim.
- **Persistence**: migrations `InitialCreate` (identity tables in `dbo`) and `RenameAccountsToUsers` (hand-written renames, data kept). The owner reset the migration history on 2026-10-23 and deleted the old `InitialIdentity`. Design-time factory for `dotnet ef migrations add`.
- **Development migrate endpoint**: `POST /api/v1/database/migrate` creates the database if needed and applies pending migrations, returning their names. Anonymous; 404 outside Development.
- **Module structure (S0.4, ADR-1)**: `Domain/Common/Entity.cs` base class; `UserType` moved to `Domain/Enums/`; reflection-based architecture tests for Domain and Application. No business entities yet.
- **Authorization (S1.1, ADR-8)**:
  - Permission catalog and system roles in `Application/Authorization`.
  - `[Authorize(Policy = Permissions.X.Y)]` checks permissions read from the database on each request (1-minute cache; none for deactivated accounts).
  - `/me` returns `permissions`.
  - First protected endpoint: `GET /api/v1/admin/permissions` (`roles.manage`).
- **First SuperAdmin (S1.2)**:
  - `dotnet run --project src/EnglishAdventure.Api -- bootstrap-superadmin` syncs the permissions and system roles and creates the first SuperAdmin from user secrets or environment variables. It is refused when an active SuperAdmin exists.
  - `sync-roles` re-syncs after catalog changes.
  - Password rule: at least 12 characters with upper, lower, digit and symbol.
  - `ConflictException` maps to 409.
- **User management (S1.3)**:
  - `GET /api/v1/admin/users` (search, paging) and `GET /{id}` need `users.read`.
  - `POST /` (Admin or Parent; Parent gets the Parent role), `POST /{id}/deactivate` and `/activate` (deactivation ends all sessions) need `users.manage`.
  - `PUT /{id}/roles` needs `roles.manage`.
  - Safety rules: no self changes; last SuperAdmin protected; Parent users only hold the Parent role.
  - `UnitOfWork` turns unique-index and concurrency conflicts into 409.

## Build and test results (2026-10-23, SDK 10.0.402)

| Command | Result |
|---|---|
| `dotnet build EnglishAdventure.sln` | ✅ 0 errors, 12 warnings (all NU1902/NU1903 for transitive `System.Security.Cryptography.Pkcs` 9.0.11; pre-existing) |
| `dotnet test` (no SQL variable) | ✅ 102 passed, 31 skipped (SQL-only), 0 failed (Domain 27, Application 51, Api 24 + 31 skipped) |
| Integration tests with `ENGLISH_ADVENTURE_TEST_SQL_CONNECTION` = LocalDB | ✅ 55 passed, 0 failed (throwaway DBs created by `InitialCreate`, then dropped; the Development DB is not touched) |
| CLI smoke test on a throwaway LocalDB DB | ✅ missing settings → exit 1; weak password → exit 1 with reasons; first `bootstrap-superadmin` → exit 0; second → exit 1 ("already exists"); `sync-roles` → exit 0, nothing to add |
| `dotnet run` smoke test | ✅ `/health` 200, `/scalar/v1` 200, OpenAPI lists auth endpoints, anonymous `/me` 401 |

## Not implemented 📋

- Role management API (S1.5): creating roles and choosing their permissions.
- Password reset, email confirmation, external login, logout-all-sessions.
- Rate limiting on `login`/`refresh` (lockout is per account only).
- Cleanup job for expired/revoked refresh-token rows.
- All learning features: lessons, levels, mini-games, child profiles, Parent–Child linking.
- Architecture tests, CI/CD, Azure infrastructure.

## Known issues and risks

1. Vulnerable transitive `System.Security.Cryptography.Pkcs` 9.0.11 (NU1903 high). Pre-existing, needs a package decision.
2. Access tokens cannot be revoked before expiry. Deactivating an account takes effect on `/me` and `refresh` at once, but other future endpoints will trust the token for up to 15 minutes unless they check the account.
3. How Child users sign in is still open (ADR-7). Children currently use email + password like other accounts.
4. Until 2026-10-23 the SQL integration tests ran against the Development database `EnglishAdventure_Dev` on `localhost` instead of a throwaway one (the connection-string override was applied too late). Fixed with `UseSetting` plus a guard. That database still holds 26 test accounts (`@example.test`) and their refresh tokens from those runs.

## Recommended next task

Account provisioning (admin creates Parent users; Parent creates/links Child users), followed by authorization policies (ADR-8).
