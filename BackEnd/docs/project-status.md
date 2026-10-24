# Project Status

_Last updated: 2026-10-24 (S3.1 levels and lessons implemented)._

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
- **Parent registration (S1.4, PD-009)**:
  - `POST /api/v1/auth/register` (always 202, never reveals whether an email is taken), `POST confirm-email` (single-use token, 24 h, stored as SHA-256), `POST resend-confirmation` (always 202).
  - A self-registered Parent cannot sign in before confirming (403, ProblemDetails `code` `email-not-confirmed`); deactivated users get `user-inactive`. Users created by an administrator count as confirmed.
  - Consent version and time are stored on the user (`Consent.CurrentVersion`).
  - Emails: development writes them as text files to `App_Data/emails` (`Email:PickupDirectory`); the production provider is not chosen yet. Links use `WebApp:BaseUrl` (required outside Development).
  - Rate limiting (built-in ASP.NET Core): `sign-in` 10/min and `registration` 5/min per client address, 429 with `Retry-After`.
  - Migration `AddParentRegistration` (owner applies it with `POST /api/v1/database/migrate`).
- **Child profiles (S2.1, PD-010)**: `api/v1/family/children` (`family.children.manage`, Parent users): list (`includeArchived`), get, create, update, archive, restore, and `options` (12 avatar keys, limit 3). At most 3 active profiles per Parent (422), nickname unique among a family's active profiles regardless of letter case (409), another family's profile answers 404. Table `family_child_profiles` (migration `AddChildProfiles`).
- **Child session (S2.2, PD-010)**: `POST /api/v1/auth/child-session` (`family.children.manage`, own active profile) issues a child session: token claims `session=child` and `child_profile_id`, no roles; only the session-only permission `learning.play`; `/me` returns `session: "child"` and the child profile. The device's Parent session is revoked (the web cookie is replaced), so leaving child mode needs the Parent's password. Refresh keeps the child session until the profile is archived or the Parent deactivated. Migration `AddChildSessions`.
- **Levels and lessons (S3.1, PD-012)**:
  - `api/v1/admin/content/levels`: list (`includeArchived`), get (with its lessons and lesson counts), create, update, `PUT order`, `publish`, `unpublish`, `archive`, `POST {id}/lessons`, `PUT {id}/lessons/order`.
  - `api/v1/admin/content/lessons/{id}`: get, update, `publish`, `unpublish`, `archive`.
  - Permissions: reading `content.read-drafts`, create/edit/order `content.manage`, publish/unpublish/archive `content.publish`.
  - Rules: Draft ⇄ Published → Archived, archived is final (422 on any change) and leaves the order; an archived level freezes its lessons; texts can be corrected while published; unpublish only while no child started (`ILearnerActivityReader`, which answers "nobody" until S4.1).
  - Every response has a `version` (row version, base64); changes send it back and a stale one answers 409. A reorder sends the full list of non-archived IDs (repeated ID 400, list not matching the current items 409) and is saved in one transaction through temporary positions.
  - Tables `content_levels`, `content_lessons` (positions unique among non-archived items), migration `AddContentLevelsAndLessons` (owner applies it with `POST /api/v1/database/migrate`).
- **Word bank (S3.6, PD-011)**: `api/v1/admin/content/lessons/{lessonId}/words` — `GET` (`content.read-drafts`), `POST`, `PUT {wordId}`, `DELETE {wordId}?version=`, `PUT order` (`content.manage`), each returning the word bank with the lesson's status and version. At most 12 words per lesson; English 1–50 characters (unique per lesson, any case), meaning 1–100, optional `https://` picture (≤ 2000). Words are added, removed and reordered only in drafts; corrections are allowed while published. A lesson is published only with at least one word and a picture for every word. 422 responses carry a ProblemDetails `code` (`ContentErrorCodes`). Table `content_lesson_words`, migration `AddLessonWords`.
- **Learn step (S3.7, PD-011)**: each word has `showInLearnStep` (new and existing words: true); `PUT .../words/{wordId}/learn-card` (`content.manage`, drafts only) switches it; a lesson needs at least one card to be published. Migration `AddLearnCards`.
- **Quiz activities (S3.2, PD-011)**: `api/v1/admin/content/lessons/{lessonId}/activities` (`ContentActivitiesController`) — list (`content.read-drafts`); add a quiz, remove and reorder activities, add/change/remove/reorder questions (`content.manage`). Forms `PictureToWord`, `WordToPicture`, `FillInTheBlank`; 2–4 options from the lesson's words with exactly one correct; at most 4 activities per lesson and 10 questions per quiz; a word used by a question cannot be removed. A lesson is published only with at least one activity and no empty quiz. Tables `content_activities`, `content_quiz_questions`, `content_quiz_options`, migration `AddQuizActivities`.
- **Matching activities (S3.3)**: `POST .../activities/matching`, `PUT .../activities/{activityId}/matching` (`content.manage`) with `{ mode: WordToPicture | WordToMeaning, wordIds: 2–6 words, version }`; locked once the lesson is published; words in a matching cannot be removed. Table `content_matching_pairs`, column `content_activities.MatchingMode`, migration `AddMatchingActivities`.
- **Boss challenge (S3.8)**: `POST .../levels/{levelId}/boss`, `PUT .../lessons/{lessonId}/boss` (`content.manage`). The boss is a lesson of kind `Boss` (name, picture) without a word bank or learn step; its quizzes and matchings use the words of the level's lessons; one per level, outside the lessons' order. A level is published only with a published boss and keeps it while published. Columns `content_lessons.Kind`, `BossImageUrl`, migration `AddBossChallenges`.
- **Learner map and play view (S3.4)**: `GET api/v1/play/map` and `GET api/v1/play/lessons/{lessonId}` (`learning.play`, child sessions only). Published content only; answers never sent: options with their own ids, shuffled on every read; matching sides with different ids (`MatchingPair.ChoiceId`, migration `AddMatchingChoiceIds`). Locks and stars come with S4.2.
- **User management (S1.3)**:
  - `GET /api/v1/admin/users` (search, paging) and `GET /{id}` need `users.read`.
  - `POST /` (Admin or Parent; Parent gets the Parent role), `POST /{id}/deactivate` and `/activate` (deactivation ends all sessions) need `users.manage`.
  - `PUT /{id}/roles` needs `roles.manage`.
  - Safety rules: no self changes; last SuperAdmin protected; Parent users only hold the Parent role.
  - `UnitOfWork` turns unique-index and concurrency conflicts into 409.

## Build and test results (2026-10-23, SDK 10.0.402)

| Command | Result |
|---|---|
| 2026-10-24, S3.4: `dotnet test -c Release` with the SQL variable | ✅ 344 passed, 0 skipped, 0 failed (Domain 113, Application 112, Api 119) |
| 2026-10-24, S3.8: `dotnet test -c Release` with the SQL variable | ✅ 339 passed, 0 skipped, 0 failed (Domain 113, Application 112, Api 114) |
| 2026-10-24, S3.3: `dotnet test -c Release` with the SQL variable | ✅ 328 passed, 0 skipped, 0 failed (Domain 106, Application 112, Api 110) |
| 2026-10-24, S3.2: `dotnet test -c Release` with the SQL variable | ✅ 308 passed, 0 skipped, 0 failed (Domain 99, Application 104, Api 105) |
| 2026-10-24, S3.7: `dotnet test -c Release` with the SQL variable | ✅ 277 passed, 0 skipped, 0 failed (Domain 83, Application 95, Api 99) |
| 2026-10-24, S3.6: `dotnet test -c Release` with `ENGLISH_ADVENTURE_TEST_SQL_CONNECTION` = `localhost` | ✅ 273 passed, 0 skipped, 0 failed (Domain 80, Application 95, Api 98) |
| 2026-10-24, S3.1: `dotnet test -c Release` with `ENGLISH_ADVENTURE_TEST_SQL_CONNECTION` = `localhost` | ✅ 241 passed, 0 skipped, 0 failed (Domain 63, Application 86, Api 92) |
| `dotnet build EnglishAdventure.sln` | ✅ 0 errors, 12 warnings (all NU1902/NU1903 for transitive `System.Security.Cryptography.Pkcs` 9.0.11; pre-existing) |
| `dotnet test` (no SQL variable) | ✅ 102 passed, 31 skipped (SQL-only), 0 failed (Domain 27, Application 51, Api 24 + 31 skipped) |
| Integration tests with `ENGLISH_ADVENTURE_TEST_SQL_CONNECTION` = LocalDB | ✅ 55 passed, 0 failed (throwaway DBs created by `InitialCreate`, then dropped; the Development DB is not touched) |
| CLI smoke test on a throwaway LocalDB DB | ✅ missing settings → exit 1; weak password → exit 1 with reasons; first `bootstrap-superadmin` → exit 0; second → exit 1 ("already exists"); `sync-roles` → exit 0, nothing to add |
| `dotnet run` smoke test | ✅ `/health` 200, `/scalar/v1` 200, OpenAPI lists auth endpoints, anonymous `/me` 401 |

## Not implemented 📋

- Role management API (S1.5): creating roles and choosing their permissions.
- Password reset, email confirmation, external login, logout-all-sessions.
- Production email provider (S1.4 writes development emails to files).
- Cleanup of expired tokens and of Parent accounts never confirmed within 7 days (S6.2).
- Cleanup job for expired/revoked refresh-token rows.
 the learner lesson map (S3.4); attempts, progress and rewards (Phases 4–5).
- Architecture tests, CI/CD, Azure infrastructure.

## Known issues and risks

1. Vulnerable transitive `System.Security.Cryptography.Pkcs` 9.0.11 (NU1903 high). Pre-existing, needs a package decision.
2. Access tokens cannot be revoked before expiry. Deactivating an account takes effect on `/me` and `refresh` at once, but other future endpoints will trust the token for up to 15 minutes unless they check the account.
3. How Child users sign in is still open (ADR-7). Children currently use email + password like other accounts.
4. Until 2026-10-23 the SQL integration tests ran against the Development database `EnglishAdventure_Dev` on `localhost` instead of a throwaway one (the connection-string override was applied too late). Fixed with `UseSetting` plus a guard. That database still holds 26 test accounts (`@example.test`) and their refresh tokens from those runs.

## Recommended next task

S4.1 lesson attempts with server-side grading and stars; the kids' learn step (F4.6) can start in parallel.
