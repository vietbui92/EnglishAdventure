# MVP Implementation Plan

| | |
|---|---|
| **Status** | Draft for review. No step is approved yet. |
| **Last updated** | 2026-10-24 |
| **Related** | [Product decisions](../product/mvp-product-decisions.md) · [Domain model proposal](../../BackEnd/docs/architecture/domain-model-proposal.md) · [Backend status](../../BackEnd/docs/project-status.md) |

## How we work: one step = one review case

1. **Review:** the owner reads the step and answers the open questions that block it.
2. **Plan:** Claude inspects the current code and posts the exact scope, the affected files and the tests.
3. **Approve:** the owner approves or changes the scope.
4. **Build:** Claude implements only that scope. If the model changes, Claude also creates the migration (`dotnet ef migrations add`) and the owner reviews it.
5. **Verify:** build and tests pass. The owner applies migrations in Development with `POST /api/v1/database/migrate`.
6. **Record:** Claude updates the step's status here, `project-status.md` and any affected decision. The owner commits.

Rules for every step:
- Small and reviewable.
- Never drop or recreate the database.
- No new packages without approval.
- Keep the Clean Architecture boundaries.
- Business rules have unit tests. Security and access rules have integration tests.

**Status values:**

| Status | Meaning |
|---|---|
| ✅ Done | Built and verified |
| 🟢 Ready | Can start now |
| 🟡 Needs decision | Waiting for an open question to be answered |
| ⚪ Waiting | Waiting for another step |
| 💤 Later | Not needed for the MVP's first release |

## Overview

| Step | Title | Blocked by | Status |
|---|---|---|---|
| **Phase 0: Baseline** | | | |
| S0.0 | Authentication foundation (login, JWT, refresh rotation, logout, `/me`) | | ✅ Done |
| S0.1 | Commit the current work (owner) | | 🟢 Ready |
| S0.2 | Fix stale lines in `CLAUDE.md` and `overview.md` | | 🟢 Ready |
| S0.3 | Remove test accounts from `EnglishAdventure_Dev` (optional) | owner approval | 🟢 Ready |
| S0.4 | Approve the module structure (ADR-1) and add an architecture test | | ✅ Done 2026-10-23 |
| S0.5 | Replace ASP.NET Core Identity tables with our own tables in `dbo` (ADR-5) | | ✅ Done 2026-10-23 (owner applies the migration) |
| **Phase 1: Identity & Access** | | | |
| S1.1 | Permission catalog and policy-based authorization | | ✅ Done 2026-10-23 |
| S1.2 | Secure SuperAdmin bootstrap and role/permission sync | | ✅ Done 2026-10-23 |
| S1.3 | Admin user management (create users, assign roles) | | ✅ Done 2026-10-23 |
| S1.4 | Parent self-registration with email confirmation (PD-009) | | ✅ Done 2026-10-23 |
| S1.5 | Role management (create roles, choose their permissions) | | 🟢 Ready |
| **Phase 2: Family** | | | |
| S2.1 | Child profiles for Parents (up to 3 active per Parent) | | ✅ Done 2026-10-24 |
| S2.2 | Child session started by the Parent | | ✅ Done 2026-10-24 |
| **Phase 3: Content** | | | |
| S3.1 | Levels and lessons: manage, order, publish | | ✅ Done 2026-10-24 |
| S3.6 | Word bank per lesson (PD-011) | | ✅ Done 2026-10-24 |
| S3.7 | Learn step: word cards from the word bank (PD-011) | | ✅ Done 2026-10-24 |
| S3.2 | Quiz activities built from the word bank | | ✅ Done 2026-10-24 |
| S3.3 | Matching activities built from the word bank | | ✅ Done 2026-10-24 |
| S3.8 | Boss challenge per level (PD-011, PD-012) | | ✅ Done 2026-10-24 |
| S3.4 | Learner lesson map (read published content) | | ✅ Done 2026-10-24 |
| S3.5 | Development seed content | OQ-006, S3.8 | 🟡 Needs decision |
| **Phase 4: Learning** | | | |
| S4.1 | Lesson attempts, server-side grading and stars | | ✅ Done 2026-10-24 |
| S4.2 | Completion, best result, lesson and level unlocking | | 🟢 Ready |
| S4.3 | Parent progress view | S4.2 | ⚪ Waiting |
| **Phase 5: Rewards** | | | |
| S5.1 | Points ledger (10 points per new star) | S4.2 | ⚪ Waiting |
| S5.2 | Badges (four types) | S5.1 | ⚪ Waiting |
| S5.3 | Rewards views for child and Parent | S5.2 | ⚪ Waiting |
| **Phase 6: Release hardening** | | | |
| S6.1 | Rate limiting on `login` and `refresh` | | ⚪ Waiting |
| S6.2 | Cleanup job: expired tokens, unconfirmed accounts after 7 days | | ⚪ Waiting |
| S6.3 | Fix the vulnerable `System.Security.Cryptography.Pkcs` package | owner approval | 🟢 Ready |
| S6.4 | Privacy: retention, deletion, log review | OQ-008 | 🟡 Needs decision |
| S6.5 | CI pipeline: build and test on every push | | ⚪ Waiting |
| **Later** | | | |
| S7.x | Azure hosting, Key Vault, Application Insights | stable MVP | 💤 Later |
| F.x | Front-end track, see the [front-end plan](frontend-implementation-plan.md) | | 🟢 Ready (F0.1, B-F1) |

## Suggested order

Admin content does **not** depend on the child-identity decision. So we can build content while OQ-001 is being decided.

```mermaid
flowchart LR
    S0[S0.1–S0.4<br/>Baseline] --> S11[S1.1<br/>Permissions]
    S11 --> S12[S1.2<br/>SuperAdmin]
    S12 --> S13[S1.3<br/>Admin users]
    S11 --> S31[S3.1<br/>Levels & lessons]
    S31 --> S36[S3.6<br/>Word bank]
    S36 --> S37[S3.7 Learn step] & S32[S3.2 Quiz] & S33[S3.3 Matching]
    S32 & S33 --> S38[S3.8<br/>Boss]
    S37 & S38 --> S34[S3.4<br/>Lesson map]
    S14[S1.4<br/>Parent users] --> S21[S2.1<br/>Child profiles] --> S22[S2.2<br/>Child session]
    S22 & S34 --> S41[S4.1<br/>Attempts] --> S42[S4.2<br/>Completion & unlock]
    S42 --> S51[S5.1 Points] --> S52[S5.2 Badges] --> S53[S5.3 Reward views]
    S42 --> S43[S4.3<br/>Parent progress]
```

1. **S0.1 → S0.4.** Clean baseline and an agreed module structure.
2. **S1.1 → S1.2.** Admins can sign in, and endpoints are protected by permissions.
3. **S3.1 → S3.6 → S3.7 / S3.2 / S3.3 → S3.8 → S3.4.** Admins can build real content (PD-011). Step numbers are kept stable, so the order is not numeric.
4. **S1.4 → S2.2.** Parents, child profiles, child sessions.
5. **S4.1 → S4.2 → S5.x → S4.3 / S5.3.** The learning loop and rewards.
6. **S1.3, S3.5, Phase 6.** Fit around the above. Phase 6 must be done before any public release.

---

## Step details

### Phase 0: Baseline

**S0.1 Commit the current work (owner).**
- The migrate endpoint, the test database fix and these documents are uncommitted.
- *Done when:* the working tree is clean.

**S0.2 Fix stale documentation.**
- `CLAUDE.md` says the API "does not start yet", calls auth "non-functional" and says user secrets aren't set up. All three are wrong.
- `overview.md` says "no migrations".
- *Done when:* the docs match the code. No code changes.

**S0.3 Remove test accounts from the dev database (optional).**
- Delete the 26 `@example.test` accounts, their refresh tokens and the test-created roles from `EnglishAdventure_Dev`.
- Only with explicit approval; this changes your database.
- *Done when:* `AspNetUsers` contains no `@example.test` rows.
- **Superseded by S0.5:** the `AspNet*` tables and their test accounts no longer exist.

**S0.4 Module structure (ADR-1).** ✅ Done 2026-10-23. The owner approved all 11 decisions (see ADR-1). Added `Domain/Common/Entity.cs`, moved `UserType` to `Domain/Enums/`, added `DomainArchitectureTests` and `ApplicationArchitectureTests`. No entities, tables or migrations.
- Approve or change the structure in the [domain model proposal](../../BackEnd/docs/architecture/domain-model-proposal.md):
  - The layer trees (§3) and the `Domain/Entities/<Module>` layout (§4).
  - The module rules (§10): one `DbContext`, one table prefix per module, references by ID only.
- If approved, move `Domain/Users/UserType.cs` to `Domain/Enums/` (a namespace change only; the database is unaffected).
- Add a reflection-based architecture test for the layer rules and the module rules (no new package).
- *Done when:* ADR-1 is marked decided and the test passes.

**S0.5 Own identity tables (ADR-5).** ✅ Done 2026-10-23. All six decisions approved by the owner.
- **Domain entities:** `User`, `UserRole`, `Role`, `RolePermission`, `Permission` in `Domain/Entities/Identity`.
- **Database:** tables in `dbo` with the `auth_` prefix (owner's choice). The migration history was reset by the owner: one standalone migration, `InitialCreate`, creates everything. The old `InitialIdentity` migration was deleted.
- **Package:** `Microsoft.AspNetCore.Identity.EntityFrameworkCore` replaced by `Microsoft.Extensions.Identity.Core` (password hashing only).
- **Behavior:** login, refresh, logout and `/me` are unchanged and covered by the existing tests.
- **Owner action:** apply the migration with `POST /api/v1/database/migrate` after restarting the API.

### Phase 1: Identity & Access

> **Owner decision 2026-10-23:** build user and role management in four steps, each reviewed separately:
> S1.1 permission checks → S1.2 first SuperAdmin → S1.3 user management → S1.5 role management.
> Approved choices:
> - Passwords need at least 12 characters with upper case, lower case, a digit and a symbol.
> - The first SuperAdmin comes from user secrets or environment variables.
> - Permissions are looked up per request, with a 1-minute cache.

**S1.1 Permission catalog and policy authorization.** ✅ Done 2026-10-23.
- **Catalog:** `Application/Authorization/Permissions.cs` holds the 9 permissions and `SystemRoles.cs` the 3 PD-008 roles with their defaults:
  - SuperAdmin gets every permission except `family.*`.
  - ContentAdmin gets `content.*`.
  - Parent gets `family.*`.
- **Checking:**
  - `[Authorize(Policy = Permissions.X.Y)]`. `PermissionPolicyProvider` builds each policy from the permission code, so nothing is registered by hand.
  - `PermissionAuthorizationHandler` reads the account's permissions through `IUserPermissionReader`: database lookup on each request, 1-minute cache, and empty for a deactivated account.
  - Role names in the token grant nothing.
- **Responses:** `/me` now returns `permissions`. The first protected endpoint is `GET /api/v1/admin/permissions` (`roles.manage`). 401 and 403 are ProblemDetails.
- **Migration:** none.
- **Tests:** catalog and system-role rules (unit); 401/403/200 through the real middleware (no database); real role → permission lookup and deactivation on SQL Server.

**S1.2 Secure SuperAdmin bootstrap.** ✅ Done 2026-10-23. Built as planned below. The command-line verbs are in `Api/CommandLine/CommandLineTasks.cs`, and the commands are `SyncSystemRolesCommand` and `BootstrapSuperAdminCommand` (`Application/Authorization/Commands`). Repositories (`IUserRepository`, `IRoleRepository`, `IPermissionRepository`) and `IUnitOfWork` were introduced. Verified by unit tests, a SQL integration test, and a CLI smoke test.
- **Includes:**
  - Command-line verbs on the API, not HTTP endpoints:
    - `sync-roles`: creates the catalog permissions and the 3 system roles, and gives the system roles their default permissions.
    - `bootstrap-superadmin`: runs `sync-roles`, then creates the first SuperAdmin.
  - Credentials from `Bootstrap:SuperAdmin:Email` / `Bootstrap:SuperAdmin:Password` (user secrets locally, environment variables elsewhere). Never in code, config files or logs.
  - Refuses (non-zero exit) when a SuperAdmin already exists.
  - Password rule: at least 12 characters with upper, lower, digit and symbol. It is reused by S1.3.
- **Replaces** the hand-written seed SQL for roles and permissions.
- *Done when:*
  - The first run creates the SuperAdmin, and a second run refuses.
  - That account can sign in and call `GET /api/v1/admin/permissions`.
  - Tests use a throwaway database.

**S1.3 Admin user management.** ✅ Done 2026-10-23. Built as listed below.
- `AdminUsersController` sends commands and queries from `Application/Users`.
- **Added rule** (`UserRoleRules`): a Parent user can only hold the Parent role, and an Admin user cannot hold it. A family account can never become an administrator.
- Rule violations return 422, unknown accounts or roles 404, duplicates and concurrent edits 409 (via `UnitOfWork`).
- Tests: validators (unit); 8 SQL end-to-end tests including last-SuperAdmin protection and session revocation.
- **Endpoints** (`api/v1/admin/users`):
  - `GET /` and `GET /{id}` (`users.read`).
  - `POST /` creates an Admin or Parent user (`users.manage`). A new Parent user gets the Parent role. No Child users until OQ-001 is decided.
  - `POST /{id}/deactivate` and `/activate` (`users.manage`).
  - `PUT /{id}/roles` (`roles.manage`).
- **Rules:**
  - Nobody can deactivate themselves or change their own roles.
  - The last active SuperAdmin cannot be deactivated or demoted.
  - Deactivating an account revokes its refresh tokens.
  - ContentAdmin can reach none of these endpoints.
- *Done when:* each rule has a test, and an unauthorized call returns 403.

**S1.5 Role management.** 🟢 Ready
- **Endpoints** (`api/v1/admin/roles`), all `roles.manage`:
  - `GET /` lists roles with their permissions.
  - `POST /` creates a role with a chosen set of catalog permissions.
  - `PUT /{id}` renames a role and changes its description and permissions.
  - `DELETE /{id}` deletes a role, only when no account has it.
- **Rules:**
  - Permissions come only from the catalog; they are never created through the API.
  - The 3 system roles cannot be deleted or renamed.
  - SuperAdmin always keeps every non-family permission.
  - Changes apply within the 1-minute permission cache.

**S1.4 Parent self-registration with email confirmation.** ✅ Done 2026-10-23. OQ-010 decided 2026-10-23 (PD-009). The owner approved the scope below and the four answers: production email provider later (development writes files); consent checkbox now, with provisional links to the terms and privacy pages until OQ-008; links valid 24 h; unconfirmed accounts deleted after 7 days by the S6.2 cleanup job.

*As built:* commands `RegisterParent`, `ConfirmEmail`, `ResendEmailConfirmation` (Application/Authentication); `User.RegisterParent` / `ConfirmEmail`; `User.Create` (administrators, bootstrap) counts as confirmed; `Consent.CurrentVersion` = `2026-10-23`; `IEmailSender` → `PickupDirectoryEmailSender` (`Email:PickupDirectory`, default `App_Data/emails`, git-ignored); `IAppLinks` → `WebApp:BaseUrl` (required, validated at start; `http://localhost:5173` in Development); unconfirmed or inactive sign-in → 403 with ProblemDetails `code` `email-not-confirmed` / `user-inactive`; rate limiter policies `sign-in` (10/min per address) and `registration` (5/min) with 429 ProblemDetails and `Retry-After` (`RateLimiting` settings). Migration `AddParentRegistration`: three `auth_users` columns, table `auth_email_confirmation_tokens`, existing users backfilled as confirmed (checked with the idempotent script on a scratch database that had a user). Tests: 6 domain, 9 validator, 10 SQL integration (all listed below); 165 backend tests pass.

**Scope as approved:**
- **Endpoints** (anonymous, `api/v1/auth`):
  - `POST register` with email, password (the S1.2 rule) and consent. Always answers `202 Accepted` with the same body, also when the email already has an account, so the endpoint never reveals who is registered. A taken email gets a "you already have an account" email instead.
  - `POST confirm-email` with the token from the link: confirms the account (`204`). An expired or used token gives `400`.
  - `POST resend-confirmation` with an email: always `202`; sends a new link only to an unconfirmed account.
- **Sign-in:** an unconfirmed Parent gets `403` with a distinct problem type (only after the correct password, like a deactivated account), so the web app can offer "resend the link". Users created by an Admin count as confirmed.
- **Domain and database** (migration): `User.EmailConfirmedAtUtc` (existing users backfilled as confirmed); `User.ConsentAcceptedAtUtc` and `ConsentVersion`; table `auth_email_confirmation_tokens` (SHA-256 hash only, like refresh tokens; 24 h lifetime; a new link voids the older unused ones; opening a used link again answers 204 without change).
- **Email:** `IEmailSender` in Application. Development implementation writes each email as a file to a local folder and logs the link; no package. The production provider is a separate decision (SMTP, SendGrid, Azure Communication Services, ...).
- **Abuse protection:** ASP.NET Core's built-in rate limiter on `register`, `resend-confirmation` and `login` (moves part of S6.1 forward).
- **Tests:** no account enumeration (same answer for new and taken emails), token hashing, expiry, single use, unconfirmed sign-in refused, Admin-created users unaffected, rate limit.
- **Questions for the owner:** (1) production email provider now or later; (2) the consent text and its version (links to terms and privacy pages that do not exist yet, part of OQ-008); (3) link lifetime (proposed 24 h); (4) delete accounts never confirmed after N days?
- *Done when:* the tests above pass and the front end (F-R1, F-R2) can register, confirm and sign in end to end.

### Phase 2: Family

**S2.1 Child profiles.** OQ-001 and OQ-013 decided 2026-10-24 (PD-010). **Owner answers 2026-10-24:** at most **3** active profiles per Parent (archived ones do not count); nicknames unique within a family; about 12 drawn animal avatars, never photos; archive and restore instead of delete.

*As built (✅ 2026-10-24):* `ChildProfile` with `MaxActivePerParent = 3` and the 12 `AvatarKeys` in the entity; `Nickname` value object; commands `CreateChildProfile`, `UpdateChildProfile`, `ArchiveChildProfile`, `RestoreChildProfile` and queries `ListChildProfiles`, `GetChildProfile` (`Application/Family`); `ChildProfilesController` on `api/v1/family/children` plus `GET options` (avatars and limit) for the web app. Limit → 422, nickname taken → 409 (checked in the handler, backed by the filtered unique index), another family's profile → 404 everywhere. The Nickname column has collation `Latin1_General_100_CI_AS` ("An" = "an", "An" ≠ "Ân"). Migration `AddChildProfiles`. Tests: 13 domain, 7 validator, 8 SQL integration (several profiles, the limit on create and restore, unique nicknames per family, reuse after archiving, update and restore, cross-family 404, 401/403, invalid input and options); 194 backend tests pass. Checked end to end on a scratch database (register → confirm → sign in → three profiles → fourth refused → duplicate nickname 409 → archive). The SuperAdmin role does not hold `family.*`, so admins cannot see families (PD-008).

Scope:
- **Domain** (`Domain/Entities/Family`): `ChildProfile` (aggregate root) with `ParentUserId`, `Nickname` (value object: 1–50 characters, trimmed), `AvatarKey` (one of a fixed set of drawn avatars, no photos), `Status` (Active, Archived), `CreatedAtUtc`, `ArchivedAtUtc?`. Rules: rename and change the avatar only while active; `IsOwnedBy(userId)`. No real name, birth date, photo or email (privacy principles).
- **Several profiles per Parent** (owner addition), with a cap on active profiles and a unique nickname within the family (questions below).
- **Endpoints** (`api/v1/family/children`, permission `family.children.manage`, Parent users only):
  - `GET /` (own profiles; archived ones on request) and `GET /{id}`.
  - `POST /` (nickname, avatar) and `PUT /{id}` (nickname, avatar).
  - `POST /{id}/archive` and `POST /{id}/restore`.
- **Ownership on the server:** every query filters by the signed-in Parent. Another family's profile ID answers 404, never 403, so IDs reveal nothing. A client-sent ID is never trusted on its own (PD-008).
- **Archive, not delete:** archiving hides the profile and stops learning but keeps the progress, and it can be restored. Permanent deletion of child data belongs to S6.4 (OQ-008).
- **Migration:** table `family_child_profiles` (FK to `auth_users`, restrict delete), index on `ParentUserId`, filtered unique index on (`ParentUserId`, normalized nickname) for active profiles, row version for concurrent edits (409).
- **Tests:** nickname and avatar rules (unit); the cap; unique nickname; archive and restore; cross-family 404 on every endpoint; an Admin, or a Parent without the permission, gets 403.
- **Questions for the owner:** (1) cap on active profiles per Parent (proposed 10); (2) nickname unique within a family (proposed yes, so the child picker is unambiguous); (3) avatars: a fixed set of about 12 drawn animal avatars (proposed), never photos; (4) archive and restore instead of delete (proposed).
- *Done when:* the tests above pass and the Parent area (F3.1) can create, edit, archive and restore profiles end to end.

**S2.2 Child session.** ✅ Done 2026-10-24. OQ-009 decided 2026-10-24 (PD-010, option B). **Owner answers:** a child session lasts like a Parent's (14 days, renewed with use); the exit button asks for confirmation only.

*As built:* the endpoint is `POST api/v1/auth/child-session` (body `childProfileId`), not under `family/children`: the web app's refresh cookie is only sent to `api/v1/auth`, and it is needed to end the Parent's session on the device. `learning.play` is in `Permissions.SessionOnly`, outside the role catalog, so no role (not even SuperAdmin, nor a role created in S1.5) can hold it. `PermissionAuthorizationHandler` asks `IUserPermissionReader.GetChildSessionPermissionsAsync` for child sessions. `IUserContext.ChildProfileId` comes from the token for the learning endpoints. `RefreshTokenCookie.Respond` is the one place that answers with tokens (cookie for the web app). Migration `AddChildSessions` (nullable `ChildProfileId` with a foreign key on `auth_refresh_tokens`). Tests: 1 domain, 1 catalog, 6 SQL integration (claims and `/me`, 403 on family, admin and starting another child session; only this device's Parent session ends; refresh keeps child mode until the profile is archived; own active profile only; administrators refused; web cookie switched); 202 backend tests pass. Checked end to end through the Vite proxy on a scratch database.

Scope as approved:
- **Start** `POST api/v1/family/children/{id}/session` (`family.children.manage`, the Parent's own **active** profile; another family's → 404, archived → 422). The answer is a new token pair for a **child session**; with the web header the refresh token replaces the cookie, as on sign-in.
- **One session per device:** starting child mode **ends the Parent's session on this device** (its refresh token is revoked). Getting back to the Parent area therefore needs the Parent's password: this is how "leaving child mode needs the Parent" is enforced. The Parent's other devices are not affected.
- **Child access token:** `sub` = the Parent's user ID (who is responsible), plus `session` = `child` and `child_profile_id`. No roles.
- **Permissions:** new catalog permission `learning.play`, held by **no role**. `IUserPermissionReader` gives a child session exactly `{ learning.play }` (and nothing once the Parent is deactivated); Parent, Admin and family endpoints therefore answer 403. A helper `IUserContext.ChildProfileId` gives learning endpoints (S4.x) the profile, so a child can never name another profile.
- **Refresh:** `auth_refresh_tokens` gets a nullable `ChildProfileId` (migration). Rotation keeps it and issues child tokens again. If the profile was archived, or the Parent deactivated, refresh fails (401) and the session ends.
- **`/me`** for a child session returns the child: `session: "child"`, `childProfile { id, nickname, avatarKey }`, `permissions: ["learning.play"]`. Parent and admin sessions get `session: "user"`.
- **Leave** with the normal `logout`: the device is signed out and shows the sign-in page.
- **Front end (F3.2):** a "Cho con học" button on each active profile card (with a short confirmation that the Parent will need to sign in to come back); a child session lands on `/kids` (greeting with the avatar and nickname); `/kids` accepts only child sessions and `/parent`, `/admin` refuse them; the exit button in the kids area signs out after a confirmation and opens the sign-in page.
- **Tests:** start only for an own active profile; child token → 403 on family, admin and `users` endpoints; refresh keeps the child session; archiving the profile ends it at the next refresh; Parent's session on the device is revoked; `/me` shapes; front-end routing for both session kinds.
- **Questions for the owner:** (1) how long a child session lasts without use (proposed: like a Parent's, 14 days sliding, so a family tablet stays in child mode); (2) the exit button: a confirmation only (proposed), or a parental check (for example a simple sum) so small children do not leave by accident.
- *Done when:* the tests above pass, and on one browser a Parent can start child mode, the child sees their own space, and leaving asks for the Parent's sign-in.

### Phase 3: Content

**S3.1 Levels and lessons.** ✅ Done 2026-10-24. OQ-005 decided 2026-10-24 (PD-012). The owner approved the scope below and its three points (archived is final, published content can be reordered, lessons do not move between levels).

*As built:* as listed below, with two details: a reorder list that does not name exactly the current items answers **409** (someone changed the list meanwhile; a repeated or empty ID is still 400); and the "children started" refusal is unit-tested with a fake `ILearnerActivityReader` instead of an integration test, because the real reader has no data before S4.1. Shared rules sit in an abstract `OrderedContent` base class (`Domain/Entities/Content`). `IUnitOfWork.ExecuteInTransactionAsync` was added for the reorder. Tests: 16 domain cases, 17 application cases (handlers with fakes, validators), 6 SQL integration; 241 backend tests pass.
- **Domain** (`Domain/Entities/Content`):
  - `Level` (aggregate root): `Title`, `Description?` (up to 500 characters), `SortOrder`, `Status`, `PublishedAtUtc?`, `ArchivedAtUtc?`.
  - `Lesson` (aggregate root): `LevelId`, `Title`, `SortOrder`, `Status`, `PublishedAtUtc?`, `ArchivedAtUtc?`. Words, activities and the boss are added to it in S3.6–S3.8.
  - `ContentStatus` enum (Draft, Published, Archived) and a `ContentTitle` value object (trimmed, 1–100 characters), in the style of `Nickname`.
  - **Rules:**
    - Draft → Published; Published → Draft (unpublish); Draft or Published → Archived. **Archived is final** (PD-012: replace it with a new lesson).
    - Title and description can be changed while Draft or Published (fixing texts is allowed by PD-012), never when Archived.
    - A lesson can be created, published or changed only while its level is not archived.
    - **Unpublish** only while no child has started the lesson (any lesson of the level, for a level). Learning data does not exist yet: the handler asks `ILearnerActivityReader`, whose implementation answers "nobody" until S4.1 creates attempts and makes it real.
    - Publish-readiness checks (word bank not empty, activities valid, boss present) are added by S3.6, S3.2, S3.3 and S3.8.
- **Order:**
  - A new level or lesson goes to the end.
  - `PUT …/order` takes the full list of the non-archived IDs in their new order; a missing, extra or repeated ID → 400.
  - Allowed for published content too (PD-002): S4.2's unlock rule keeps lessons a child already completed available, so reordering never takes progress away.
  - `SortOrder` is unique among non-archived items (filtered unique index); a reorder is saved in one transaction.
- **Concurrency:** each level and lesson returns a `version` (its row version). Update, publish, unpublish and archive send it back; a stale version → 409 ("someone else changed this, reload").
- **Application** (`Application/Content`):
  - Commands: `CreateLevel`, `UpdateLevel`, `PublishLevel`, `UnpublishLevel`, `ArchiveLevel`, `ReorderLevels`, `CreateLesson`, `UpdateLesson`, `PublishLesson`, `UnpublishLesson`, `ArchiveLesson`, `ReorderLessons`.
  - Queries: `ListLevels` (with lesson counts per status), `GetLevel` (with its lessons), `GetLesson`.
  - `ILevelRepository`, `ILessonRepository`, `IContentQueries`, `ILearnerActivityReader`.
- **Api:** `api/v1/admin/content/levels` and `api/v1/admin/content/lessons`. Reading needs `content.read-drafts`; create, edit and reorder need `content.manage`; publish, unpublish and archive need `content.publish` (ContentAdmin and SuperAdmin hold all three; a Parent gets 403).
- **Migration:** `AddContentLevelsAndLessons` (`content_levels`, `content_lessons`; `Lesson.LevelId` foreign key with restricted delete).
- **Not included:** moving a lesson to another level; words, activities and the boss (S3.6–S3.8); the learner view (S3.4); the admin screens (F2.4).
- **Tests:**
  - Domain: every status change, archived is final, texts, publish/unpublish rules.
  - Validators: titles, description, reorder lists.
  - SQL integration: create and list in order, reorder (and a bad list → 400), stale version → 409, archived items leave the order, unpublish refused when learners started (with a fake reader), Parent → 403, anonymous → 401.
- *Done when:* the tests above pass, and a ContentAdmin can create, order, publish, unpublish and archive levels and lessons through the API.

**S3.6 Word bank per lesson (PD-011).** ✅ Done 2026-10-24.

*As built:* `LessonWord` is a child entity of the `Lesson` aggregate (table `content_lesson_words`, unique English word per lesson with a case-insensitive collation, cascade with the lesson). Every word change sets `Lesson.ContentChangedAtUtc`, so the **lesson's version** covers its words: each word request sends it and gets the new one back. Endpoints under `api/v1/admin/content/lessons/{lessonId}/words`: `GET` (`content.read-drafts`), `POST`, `PUT {wordId}`, `DELETE {wordId}?version=`, `PUT order` (`content.manage`); each returns the word bank (`lessonStatus`, `lessonVersion`, `maxWords`, `words`). Publishing a lesson now checks it is ready. **422 responses carry a `code`** (`DomainException.Code`, `ContentErrorCodes`): `lesson-not-ready`, `lesson-structure-locked`, `word-limit`, `word-duplicate`, `content-archived`, `content-started` (the last two also on the S3.1 rules). A word order that does not match the current words answers 409, like S3.1. Migration `AddLessonWords`. Tests: 17 domain cases, 9 application cases, 6 SQL integration (and the S3.1 tests that publish a lesson now add a word first); 273 backend tests pass.

**Owner answers 2026-10-24:** a picture is optional when saving a word but required for every word before the lesson is published; at most **12** words per lesson (at least 1 to publish); picture URLs must be `https://` (up to 2000 characters).
- **Includes:** add, edit, order and remove the lesson's words: English word, Vietnamese meaning, picture URL (PD-012; uploads later).
- **Rules:** English word 1–50 characters, meaning 1–100, no duplicate English word in a lesson (any letter case). Adding, removing and reordering words only while the lesson is a draft (PD-012: structure locked once published); fixing a word's texts and picture is allowed while published. A word used by an activity can't be removed (enforced from S3.2, when activities exist).
- **Migration:** `content_lesson_words`.
- *Done when:* the rules are unit-tested and only `content.*` permissions can change words.

**S3.7 Learn step (PD-011).** ✅ Done 2026-10-24. *As built:* `LessonWord.ShowInLearnStep` (new words: true) and `Lesson.LearnCards` (the shown words in word-bank order); `Lesson.SetLearnCard` (drafts only, 422 `lesson-structure-locked`); publishing needs a card (422 `lesson-not-ready`). Endpoint `PUT api/v1/admin/content/lessons/{lessonId}/words/{wordId}/learn-card` `{ show, version }` (`content.manage`); the word bank returns `showInLearnStep` per word. Migration `AddLearnCards` adds the column with default **true**, so existing words become cards. Tests: 3 domain, validator checks, 1 SQL integration; 277 backend tests pass.

**Owner answers 2026-10-24:** words are switched in or out of the learn step only while the lesson is a draft (a structural change, PD-012); a lesson needs at least one learn card to be published.
- **Includes:** every lesson shows word cards built from its word bank before its activities. Not scored. The Admin chooses which words appear (default: all), in the word bank's order.
- *Done when:* the cards are in order and the publishing rule is tested. The play view that sends the cards to children comes with S3.4, which tests them there.

**S3.2 Quiz activities.** ✅ Done 2026-10-24.

*As built:* the `Lesson` aggregate owns `LessonActivity` (abstract; one table `content_activities` with a `Type` column, EF table-per-hierarchy) → `QuizActivity` → `QuizQuestion` → `QuizOption`. A question points to words of the word bank (`CorrectWordId`, options by `WordId`), never copies them; "correct" is not stored on the option but follows from `CorrectWordId`, so a question cannot have two right answers. `QuizQuestionSpec` holds the rules (2–4 different options, a fill-in sentence of 1–200 characters with exactly one `___`, no sentence for the other forms). Endpoints in `ContentActivitiesController` under `api/v1/admin/content/lessons/{lessonId}/activities`: `GET`, `POST quiz`, `DELETE {activityId}`, `PUT order`, `POST/PUT/DELETE {activityId}/questions[/{questionId}]`, `PUT {activityId}/questions/order`; every change sends the lesson version and returns all activities. Badly shaped questions answer **400**; a word of another lesson 422 `question-invalid`. New 422 codes: `activity-limit`, `question-limit`, `question-invalid`, `word-in-use`. In a published lesson only a fill-in sentence can be corrected. Publishing now also needs at least one activity and no empty quiz. Migration `AddQuizActivities` (new tables only). Tests: 16 domain cases, 9 application cases, 6 SQL integration (and the earlier tests that publish a lesson now add a quiz through `QuizTestData`); 308 backend tests pass.

**Owner answers 2026-10-24:** the Admin chooses the wrong options, with suggestions from the system (the editor, F2.5, suggests other words of the lesson; the choice is stored, so a question is the same every time); **2–4 options per question, 3 by default**; at most **4 activities per lesson** and **10 questions per quiz**; a lesson needs **at least one activity** to be published.
- **Includes:** add, edit and order quiz activities and questions, built from the word bank. Forms: picture → word, word → picture, fill in the missing word.
- **Rules:** at least 2 options and exactly 1 correct option (PD-012). Options come from the lesson's words. A lesson can't be published with an invalid activity.
- **Migration:** `content_activities`, `content_quiz_questions`, `content_quiz_options`.
- *Done when:* every invalid configuration returns a clear 400.

**S3.3 Matching activities.** ✅ Done 2026-10-24. *As built:* `MatchingActivity` (second kind of `LessonActivity`, `Type` 2, column `MatchingMode`) owns `MatchingPair`s, each just a word of the word bank: its English text is one side and its picture or meaning the other, so every pair has exactly one partner and nothing is typed twice. 2–6 different words (`matching-invalid` for a word of another lesson). Endpoints `POST .../activities/matching` and `PUT .../activities/{activityId}/matching` `{ mode, wordIds, version }`; the activity list returns `matchingMode` and `pairWordIds`. A matching has no texts of its own, so it is fully locked once the lesson is published. A lesson can be published with only a matching. Migration `AddMatchingActivities` (new column with a check constraint, new table). Tests: 7 domain, 8 application cases, 5 SQL integration; 328 backend tests pass.

**Owner answers 2026-10-24:** the Admin chooses per activity whether children match each English word with its **picture** (default) or its **Vietnamese meaning**; **2–6 pairs** per activity.
- **Includes:** a matching activity chooses 2–6 words from the word bank and pairs each word with its picture (or meaning).
- **Rules:** at least 2 pairs, no duplicate values, exactly one right-hand value per pair.
- **Migration:** `content_matching_pairs`.
- *Done when:* the rules are tested and the activity can be published.

**S3.8 Boss challenge (PD-011, PD-012).** ✅ Done 2026-10-24.

*As built:* the boss is a `Lesson` of `Kind = Boss` (title = boss name, `BossImageUrl`), so quizzes, matchings, publishing and, later, attempts work the same for it. It has no word bank and no learn step (422 `boss-no-word-bank`); its activities point to words of the level's non-archived regular lessons, which the handlers load as `LevelWords`. It stays out of the lessons' order (filtered unique index on regular lessons only; another filtered unique index allows one non-archived boss per level, 422 `boss-exists`). Publishing a boss needs its picture, an activity ready to play and a picture for every word it uses. A level is published only with a published boss (422 `level-needs-boss`), and a published level keeps its boss (unpublish or archive the boss: 422 `boss-required`). A word the boss uses cannot be removed from its lesson (`word-in-use`). Endpoints `POST api/v1/admin/content/levels/{levelId}/boss` `{ title, imageUrl? }` and `PUT api/v1/admin/content/lessons/{lessonId}/boss` `{ title, imageUrl?, version }`; `GET .../lessons/{bossId}/words` returns the level's words (read-only, `maxWords` 0). Lessons now return `kind` and `bossImageUrl`, levels `bossStatus`; the lesson counts are regular lessons only. Migration `AddBossChallenges` (`Kind` defaults to 1 for existing lessons). Tests: 7 domain, 4 SQL integration (the S3.1 archive test no longer publishes its level); 339 backend tests pass.

**Owner answers 2026-10-24:** the boss's quizzes and matchings **point to words of the level's lessons** (no word bank of its own; a word used by the boss cannot be removed from its lesson); a **level is published only with a published boss**; a boss has a **name and a picture**, the picture required to publish; the boss has **no learn step**.
- **Includes:** each level has at most one boss challenge, placed after its lessons. It is built like a lesson (Quiz and Matching) from the words of the whole level, with a boss name and picture for display.
- **Rules:** a level can't be published without a valid boss challenge.
- *Done when:* the rules are unit-tested.

**S3.4 Learner lesson map.** ✅ Done 2026-10-24. *As built:* `PlayController` on `api/v1/play` for child sessions only (`learning.play`; Parents and Admins get 403): `GET map` (published levels in order, each with its published lessons in order and its published boss; a level without a published lesson is left out) and `GET lessons/{lessonId}` (a published lesson or boss of a published level, else 404): learn cards, then activities. **No answer is sent or can be derived:** quiz options use the options' own ids (never word ids) and are shuffled on every read (the stored order puts the correct one first); picture → word options carry text only, word → picture options pictures only; a matching's right side uses a separate `MatchingPair.ChoiceId` and its own shuffle. A boss is played with the words of the level's lessons and has no learn cards. Read model `PlayQueries` (`IPlayQueries`). Migration `AddMatchingChoiceIds` (each existing pair gets `NEWID()`). Tests: 5 SQL integration (map, no answers or word ids in the payload, new orders, boss and drafts, child session only); 344 backend tests pass.
- **Includes:**
  - A read model of published levels, lessons and boss challenges for learners.
  - Lesson content for play (learn cards and activities), **without the correct answers**.
  - Lock state and stars are added in S4.2.
- *Done when:* drafts never appear, and the play view contains no answer keys (tested).

**S3.5 Development seed content.** *Needs OQ-006.*
- Seed content is loaded only in Development, by an explicit command or endpoint, never automatically in production.

### Phase 4: Learning

**S4.1 Lesson attempts.** ✅ Done 2026-10-24.

*As built:* `LessonAttempt` (Learning module, `learning_lesson_attempts`) owns `AttemptAnswer`s (`learning_attempt_answers`, one row per try). The server builds an `AnswerKey` from the lesson (quiz option ids, matching pair id → `ChoiceId`) and grades every answer; no score is accepted from a client. Every question and every pair is one item; an attempt finishes when every question is answered and every pair matched, then `LessonScoring` gives the percent (rounded down) and the stars. One unfinished attempt per child and lesson (filtered unique index): starting again resumes it, after finishing a new one starts. A repeated answer returns the stored result; two answers at the same moment cannot both pass the attempt's row version (409, the client sends again), so the last item always finishes the attempt. Endpoints on `PlayController` (child session): `POST play/lessons/{lessonId}/attempts` (start or resume), `GET play/attempts/{attemptId}`, `POST play/attempts/{attemptId}/questions/{questionId}` `{ optionId }`, `POST play/attempts/{attemptId}/matches` `{ leftId, rightId }`; answers return right/wrong (and the right option for a quiz) with the attempt (done items, answered questions, matched pairs, result). 422 codes `answer-invalid`, `attempt-finished`; another child's attempt is 404. `ILearnerActivityReader` now reads attempts, so a started lesson can no longer be unpublished. Migration `AddLessonAttempts`. Tests: 15 domain cases, 5 SQL integration; 364 backend tests pass.

**Owner answers 2026-10-24:** a quiz is answered **one question at a time** with immediate right/wrong feedback (and the right answer shown after a wrong one); only the first answer counts. A matching is checked **one pair at a time**; a wrong pair comes apart and can be tried again until all are matched; only pairs right on the first try count. Answers are saved one by one, so a child resumes even within an activity. *Scope note:* the "unlocked" check needs completions, so it is added with S4.2; S4.1 checks that the lesson and its level are published.
- **Includes:**
  - Start an attempt (checks published; unlocked from S4.2). Works the same for lessons and boss challenges.
  - Submit answers one question or pair at a time (see above).
  - Server-side grading: score = percentage of correct scored items; stars 0–3 at 60 / 80 / 100% (PD-011).
  - Submitting twice is safe.
- **Migration:** `learning_lesson_attempts` and `learning_attempt_answers`.
- *Done when:*
  - A client-sent score is ignored.
  - Submitting twice returns the same result.
  - A locked lesson cannot be started (from S4.2).
  - The grading rules have unit tests.

**S4.2 Completion, best result and unlocking.**
- **Includes:**
  - The first attempt with at least one star creates a `LessonCompletion`; later attempts only raise its best stars (PD-011, replay allowed).
  - The computed unlock rules: next lesson after a completed one; the boss after every lesson of the level has a star; the next level after the boss is defeated.
  - The child's level, computed (never stored).
  - The lesson map shows Locked, Available or Completed, with the best stars.
- **Migration:** `learning_lesson_completions` (unique index on child and lesson, best stars).
- *Done when:*
  - The unlock rules are unit-tested, including reordering.
  - Concurrent submits create only one completion (integration test).
  - A replay with a lower score never lowers the best stars.

**S4.3 Parent progress view.**
- **Includes:** a Parent sees each child's completions, scores and history.
- *Done when:* a cross-family request returns 404 (tested).

### Phase 5: Rewards

**S5.1 Points ledger.**
- **Includes:** 10 points for each newly earned star (PD-011), awarded in the same transaction as the attempt result, using the ledger and its unique source key (child, lesson, star number).
- **Migration:** `rewards_points_transactions`.
- *Done when:* retries, replays and concurrent submits never add points for the same star twice (tested).

**S5.2 Badges.** "Level superstar" includes the boss (PD-012).
- **Includes:** the four badge types of PD-011 (First lesson, Perfect, Level conqueror, Level superstar), awarding them, and management of definitions (`rewards.manage`).
- **Migration:** `rewards_badges` and `rewards_badge_awards`.
- *Done when:* each criterion is unit-tested and no badge is awarded twice.

**S5.3 Rewards views.**
- **Includes:** the child sees their points and badges; the Parent sees each child's.
- *Done when:* access rules are tested.

### Phase 6: Release hardening

Every Phase 6 step must be done before any public release.

- **S6.1 Rate limiting:** ASP.NET Core's built-in rate limiter on `login` and `refresh`.
- **S6.2 Cleanup job:** deletes old expired and revoked refresh tokens, used or expired email confirmation tokens, and Parent accounts never confirmed within 7 days (owner decision 2026-10-23, S1.4).
- **S6.3 Vulnerable package:** fix NU1903 for `System.Security.Cryptography.Pkcs` 9.0.11. Needs package approval.
- **S6.4 Privacy:** retention and deletion of child data, log review, decided by OQ-008.
- **S6.5 CI:** build and test on every push, including the SQL integration tests against a throwaway database.

### Later

- **S7.x Azure:** App Service or Container Apps, Azure SQL, Key Vault, Application Insights, and migration bundles in the deploy pipeline.
- **F.x Front end:** OQ-014 decided 2026-10-23. Planned step by step in the [front-end plan](frontend-implementation-plan.md); each screen starts when its backend step is Done.

## Change log

| Date | Change |
|---|---|
| 2026-10-23 | First draft. |
| 2026-10-24 | PD-011 (lesson design): added S3.6 word bank, S3.7 learn step, S3.8 boss challenge; S3.2/S3.3 build from the word bank; S4.1 computes stars; S4.2 adds best result, boss and level unlocking; S5.1/S5.2 no longer wait for OQ-004. |
| 2026-10-24 | PD-012 (OQ-005, 011, 012, 015, 017 decided): no Phase 3–5 step waits for a decision any more except S3.5 (OQ-006). S3.1 scope drafted for approval. |
| 2026-10-24 | S3.1 done: levels and lessons API (order, publish, unpublish, final archive, versions), migration `AddContentLevelsAndLessons`. S3.6 is Ready; F2.4 can start. |
| 2026-10-24 | S3.6 done: word bank per lesson (max 12 words, pictures required to publish, https only, structure locked once published), rule codes on 422, migration `AddLessonWords`. S3.7, S3.2, S3.3 and F2.7 are Ready. |
| 2026-10-24 | S3.7 done: learn cards chosen per word in drafts, at least one card to publish, migration `AddLearnCards`. While making it, `AddLessonWords` was removed by mistake and recreated with its original ID `20261024022022` and identical schema (checked against the Development database, which had applied it). |
| 2026-10-24 | S3.2 done: quiz activities (3 question forms, 2–4 options from the word bank, max 4 activities and 10 questions, at least one activity to publish), migration `AddQuizActivities`. F2.5 (quiz editor) is Ready. |
| 2026-10-24 | S3.3 done: matching activities (word ↔ picture or word ↔ meaning, 2–6 pairs), migration `AddMatchingActivities`. S3.8 boss challenge is Ready; F2.5 matching editor is Ready. |
| 2026-10-24 | S3.8 done: boss challenge per level (a boss lesson using the level's words, name and picture, level published only with its boss), migration `AddBossChallenges`. S3.4 learner lesson map and F2.8 boss editor are Ready. |
| 2026-10-24 | S3.4 done: learner map and lessons to play (`api/v1/play`), answers never sent, migration `AddMatchingChoiceIds`. Phase 3 is done except S3.5 (OQ-006). S4.1 attempts and F4.6 learn step are Ready. |
| 2026-10-24 | S4.1 done: lesson attempts graded by the server one question or pair at a time, stars, resume, migration `AddLessonAttempts`. S4.2 and F4.3/F4.4 (quiz and matching players) are Ready. |
| 2026-10-24 | F4.3/F4.4 done on the frontend (quiz and matching players, result with stars, boss health bar). |
| 2026-10-24 | S2.2 done: child sessions (`POST api/v1/auth/child-session`, session-only `learning.play`, child profile in the token and on `/me`, Parent's session on the device ended, refresh ends when the profile is archived), migration `AddChildSessions`. |
| 2026-10-24 | S2.1 done: child profiles (at most 3 active per Parent, unique nicknames per family, 12 drawn avatars, archive and restore), migration `AddChildProfiles`. S2.2 next. |
| 2026-10-24 | OQ-001, OQ-009, OQ-013 decided (PD-010): S2.1 drafted for approval (several profiles per Parent); S2.2 waits only for S2.1. |
| 2026-10-23 | S1.4 done: Parent self-registration with email confirmation (PD-009), development email files, rate limiting on sign-in and registration. S6.2 now also removes unconfirmed accounts after 7 days. |
| 2026-10-23 | OQ-014 decided. Front-end track moved to its own plan (`frontend-implementation-plan.md`), including backend step B-F1 (refresh-token cookie). |
| 2026-10-23 | OQ-007 decided (PD-008). S1.1 no longer blocked by a decision; it waits for S0.4. Permission names updated in S1.1, S1.3 and S5.2. |
| 2026-10-23 | S0.4 done (structure approved, recorded as ADR-1). S1.1 is now Ready. |
| 2026-10-23 | Owner request: "account" renamed to "user" in code, database (migration `RenameAccountsToUsers`, data kept) and API (`api/v1/admin/users`, permissions `users.*`). |
| 2026-10-23 | S1.3 done: user management API (list, get, create Admin/Parent, activate/deactivate, set roles) with the safety rules. S1.5 is Ready. |
| 2026-10-23 | S1.2 done: `sync-roles` and `bootstrap-superadmin` command-line verbs, password rule, repositories and unit of work. S1.3 is Ready. |
| 2026-10-23 | Owner approved user and role management in four steps (S1.1 → S1.2 → S1.3 → new S1.5). S1.1 done: permission catalog, per-request permission checks with a 1-minute cache, `/me` permissions, first protected endpoint. |
| 2026-10-23 | S0.5 added and done: own `identity` schema replaces ASP.NET Core Identity tables (ADR-5). S0.3 superseded. |
| 2026-10-23 | S0.5 follow-up: the owner reset the migration history (single `InitialCreate`) and moved the identity tables to `dbo`. |
| 2026-10-23 | Owner decision: no SQL schemas; tables use module prefixes (`auth_`, `family_`, `content_`, `learning_`, `rewards_`). Migration lines in S2–S5 renamed. |
| 2026-10-23 | Aligned with the rewritten domain model proposal: S0.4 scope (layer trees, `Entities` folder, `UserType` move), section references, per-activity submission (S4.1), unique index instead of composite key (S4.2), `PointsTransactions` (S5.1). |
