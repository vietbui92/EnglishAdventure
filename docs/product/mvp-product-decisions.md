# English Adventure: MVP Product Decisions

| | |
|---|---|
| **Owner** | Quoc Viet Bui (product owner). Only the owner changes a decision. |
| **Status** | Baseline. PD-001 to PD-010 are decided; PD-010 reconciles PD-001 and PD-007 (Section 8, option B). |
| **Last updated** | 2026-10-23 |
| **Related** | [Implementation plan](../roadmap/mvp-implementation-plan.md) · [Domain model proposal](../../BackEnd/docs/architecture/domain-model-proposal.md) · [Architecture decisions](../../BackEnd/docs/architecture/decisions.md) |

This document does **not** authorize code, migrations or schema changes. Each implementation step is reviewed and approved separately (see the implementation plan).

Labels used in this document:

- ✅ **Decided**: chosen by the owner.
- 💡 **Proposed**: a suggestion that needs the owner's review.
- ❓ **Open**: not decided yet. Do not guess an answer.

---

## 1. Vision

English Adventure is a web platform where **children learn English through an adventure**: ordered lessons, levels that unlock as they progress, short mini-games, points and badges. Parents follow their children's progress. Admins create and publish the learning content.

The MVP should give children a usable learning experience and give Admins a working content tool, without building a complex learning engine or a distributed system. The system should be able to grow later without a rewrite.

## 2. Users

| User | What they do in the MVP |
|---|---|
| **Admin** | Signs in. Creates, orders and publishes levels, lessons, quiz activities and matching activities. Manages accounts within their permissions. |
| **Parent** | Signs in. Manages the learning profiles of their own children. Sees each child's progress, results, points and badges. |
| **Child** | Uses the learning experience (how a child gets access is open: PD-007 / OQ-009). Sees available lessons, plays quiz and matching activities, gets results and rewards, and continues where they stopped. |

User type, role and permission are three different things:

- **User type** (Admin, Parent, Child) describes who the user is in the product.
- **Role** groups responsibilities.
- **Permission** allows one specific operation.

## 3. Decisions

| ID | Topic | Choice | Status |
|---|---|---|---|
| PD-001 | Child user model | `parent-profile`: a Parent manages child learning profiles under the Parent's account | ✅ Decided; conflict with PD-007 resolved by PD-010 |
| PD-002 | Learning progression | `sequential-unlock`: lessons are ordered; completing one unlocks the next | ✅ Decided |
| PD-003 | First mini-games | `quiz-match`: Quiz and Matching | ✅ Decided |
| PD-004 | Rewards | `points-badges`: points and badges | ✅ Decided |
| PD-005 | MVP scope | `admin-parent-child`: Admin content, Parent monitoring, Child learning | ✅ Decided |
| PD-006 | Content source | `admin-configured`: Admins manage content; seed data is for development and demos only | ✅ Decided |
| PD-007 | Child privacy and identity | `independent-identity`: each child has a distinct identity | ✅ Reconciled with PD-001 by PD-010 (Section 8, option B) |
| PD-008 | Initial roles and permissions (answers OQ-007) | Three roles: SuperAdmin, ContentAdmin, Parent | ✅ Decided 2026-10-23 |
| PD-010 | Child identity, session and guardians (answers OQ-001, OQ-009, OQ-013) | Section 8 option B: parent-owned child profiles, child session started by the Parent, one Parent per profile, several profiles per Parent | ✅ Decided 2026-10-24 |
| PD-009 | Parent registration (answers OQ-010) | Parents register themselves on the public site and must confirm their email before signing in | ✅ Decided 2026-10-23 (details in S1.4) |

### PD-001: Parent-managed child profiles
- A Parent can manage several child profiles.
- Each child profile has its own progress, game history, points and badges.
- Learning records always belong to the correct child profile.
- Only authorized users can access a child profile.
- A child profile is a **learning identity**. It is not automatically a login account.

### PD-002: Sequential unlocking
- Lessons have a defined order.
- The system records lesson completion.
- Whether a lesson is unlocked depends on its prerequisite being completed (and any other progression rules).
- Repeating a lesson must not create duplicate completions or duplicate rewards.
- When an Admin reorders lessons, existing progress must not be silently corrupted.
- Still open: minimum score (OQ-002), retries and replays (OQ-003), content changes during progress (OQ-005), and how levels unlock (OQ-016).

### PD-003: Quiz and Matching
- **Quiz:** the learner picks an answer from options.
- **Matching:** the learner matches related items, for example English words and pictures.
- Share common concepts (activity, attempt, score) where it helps, but do not build an over-generic game engine.

### PD-004: Points and badges
- In scope: points awarded by defined rules; badges awarded when their conditions are met; showing points and badges; keeping enough history to explain every reward.
- Out of scope: a reward store, redeeming points, a virtual economy, advanced streaks, seasonal competitions.
- Repeated requests, retries or duplicate processing must never create duplicate rewards.

### PD-005: MVP scope
See Section 2. The MVP does not need every future administration, reporting, subscription or social feature.

### PD-006: Admin-configured content
- Admins create and edit levels and lessons, set lesson order, create quiz questions and answer options, set up matching pairs, and control publication.
- Content tools are built step by step.
- Seed data is only for development and demos.
- AI-generated content is not part of the MVP.

### PD-007: Independent child identity (conflicts with PD-001)
A distinct child identity does not have to mean a separate email or password. It could be a separate application identity with a controlled way to start a session. That has not been decided.

**Until this is resolved:** do not finalize child authentication or the account-to-profile database relationships.

### PD-008: Initial roles and permissions (answers OQ-007)

**Roles.** The MVP starts with exactly three roles:

1. **SuperAdmin:** full system administration, including user management and role/permission management.
2. **ContentAdmin:** manages learning content: creates, edits, orders and publishes levels, lessons, quiz activities and matching activities.
3. **Parent:** manages their own children's learning profiles and views their children's progress, activity results, points and badges.

**Principles:**
- Roles and permissions are separate. A role is a collection of permissions; a permission is one specific action.
- **Least privilege.** ContentAdmin never automatically gets user-management, role-management or permission-management privileges.
- **Server-side ownership.** A Parent can access only the child profiles and learning records they are authorized for. A `ChildProfileId` sent by the client is never trusted on its own; ownership and access are checked on the server.
- **Secure SuperAdmin provisioning.** No default SuperAdmin with a hard-coded password, and no public SuperAdmin registration endpoint.
- **No extra roles** in the MVP unless an existing requirement makes one necessary.
  - *Changed by the owner on 2026-10-23:* holders of `roles.manage` may create roles at runtime and choose their permissions from the catalog (plan step S1.5). The three roles above remain the built-in system roles. Permissions themselves are still defined only in code.
- **Extensible, but simple.** The permission catalog can grow without adding a complex authorization framework.

### PD-010: Child identity, session and guardians (answers OQ-001, OQ-009, OQ-013)

Decided by the owner on 2026-10-24, adopting option B of Section 8:
- **OQ-001:** a child is a **learning profile** (nickname and avatar) owned by a Parent account. The child has no email and no password. All progress belongs to the profile's stable ID.
- **Several profiles per Parent** (owner addition): a Parent creates one profile per child, for example three profiles for three children.
- **OQ-009:** the Parent picks a profile on the device and starts a **child session**. That session can only learn as that profile; it cannot reach Parent or Admin features. Leaving child mode requires the Parent to sign in again.
- **OQ-013:** in the MVP each profile belongs to exactly **one** Parent. A second guardian can be added later (the model allows a guardian list without moving data).

### PD-009: Parent registration (answers OQ-010)

- Parents create their own account on the public website (landing page → register).
- The email address must be confirmed through a link before the account can sign in.
- Admins can still create Parent users (S1.3).
- **Still open:** the email provider; the consent and privacy text parents accept (part of OQ-008); link lifetime and resend limits; abuse protection (rate limiting, S6.1). These are settled in plan step S1.4.

**Not decided by PD-008:**
- The exact permission catalog. The proposal is in the [domain model proposal §9](../../BackEnd/docs/architecture/domain-model-proposal.md) and is finalized in plan step S1.1.
- How a child accesses the app (OQ-001, OQ-009).
- Every other open question.

## 4. Privacy principles (they apply now)

- Collect only the data that learning and safe operation need.
- Do not require a child's email address or other direct identifiers without a clear need.
- Allow access to child records only through verified relationships and permissions.
- One family must never see another family's child profiles.
- Keep login credentials separate from learning-profile data.
- Define retention and deletion before the production release (OQ-008).
- Do not put unnecessary personal data in logs or analytics.

## 5. Scope

**In the MVP:**
- The three user experiences from Section 2.
- Quiz and Matching.
- Sequential unlocking.
- Points and badges.
- Admin-managed content.

**Not in the MVP** (unless the owner approves it later):
- Microservices, Kubernetes, or distributed messaging without a concrete need.
- A points store or redemption.
- Leaderboards or social features.
- AI-generated content.
- Several independent authentication systems.
- Complex analytics or recommendation engines.
- Subscriptions and payments.

Architecture: a modular monolith with Clean Architecture, pragmatic DDD, and CQRS where it helps. Azure is the planned platform, used once the application is stable and tested.

## 6. MVP acceptance criteria

The MVP is complete only when all of these are implemented **and tested**.

**Administration**
- Authorized Admins can create, edit, order and publish content.
- Quiz and Matching activities can be configured.
- Invalid content is rejected with clear validation errors.

**Learning**
- Learners can open only the lessons they are eligible for.
- Learners can complete Quiz and Matching activities.
- Results and progress are kept across sessions.
- Sequential unlocking is enforced by the backend, not only by the front end.

**Rewards**
- Points follow documented rules.
- A badge is awarded only when its conditions are met.
- Duplicate submissions never create duplicate rewards.

**Parent and child data**
- A Parent can access only their own children's records.
- Learning data stays linked to the correct learner.
- Access across families is prevented, and tests prove it.

**Engineering quality**
- Clean Architecture boundaries are kept.
- Business rules can be tested without HTTP or a database where practical.
- Important authorization and progression rules have automated tests.
- Every database change is a reviewed migration.
- Secrets are managed safely.
- The build and all tests pass.

## 7. Open questions

Rows marked _(added)_ were raised while preparing the plan. They are not in the owner's original list and need the owner's confirmation.

| ID | Question | Why it matters | Blocks plan step |
|---|---|---|---|
| OQ-001 | Are children profiles under a Parent, or independent application identities? | Account relationships, sign-in flow, privacy | ✅ **DECIDED** 2026-10-24 → PD-010: parent-owned learning profiles, several per Parent |
| OQ-002 | What counts as completing a lesson (for example, a minimum score)? | Scoring, unlocking, progress state | S4.1, S4.2 |
| OQ-003 | Can a child replay completed lessons? How are repeat scores handled? | Attempt history, best score, rewards | S4.2, S5.1 |
| OQ-004 | What are the exact rules for awarding points and badges? | Reward integrity | S5.1, S5.2 |
| OQ-005 | Can an Admin edit, reorder or unpublish a lesson after learners have started it? | Content versioning, historical progress | S3.1 |
| OQ-006 | What levels, topics and lessons does the MVP start with? | Seed content, acceptance tests | S3.5 |
| OQ-007 | Which roles and permissions does the first release need? | Authorization policies | ✅ **DECIDED** 2026-10-23 → PD-008. Roles: SuperAdmin, ContentAdmin, Parent. The permission catalog is finalized in S1.1 |
| OQ-008 | Which markets, and what child-consent, retention and deletion rules apply? | Privacy controls, production readiness | S1.4, S6.4 |
| OQ-009 | If independent identity is kept: how does a child start a session? | Credentials, or parent-authorized access | ✅ **DECIDED** 2026-10-24 → PD-010: the Parent starts a child session |
| OQ-010 _(added)_ | How are Parent users created: self-registration, or created by an Admin? Is email confirmation required? | Public endpoints, consent, abuse protection | ✅ **DECIDED** 2026-10-23 → PD-009: self-registration with email confirmation |
| OQ-011 _(added)_ | Where do Matching pictures come from: a URL entered by the Admin, or uploaded files? | Media storage (uploads need Blob Storage) | S3.3 |
| OQ-012 _(added)_ | Does "resume" mean continuing from the next lesson, or from the middle of a lesson? | Saving answers per activity or per lesson | S4.1 |
| OQ-013 _(added)_ | Can a child profile belong to more than one Parent (for example two guardians)? | Family data model | ✅ **DECIDED** 2026-10-24 → PD-010: one Parent per profile in the MVP |
| OQ-014 _(added)_ | Which front-end technology? (`FrontEnd/` is empty) | Front-end track, API contract style, refresh-token transport | ✅ **DECIDED** 2026-10-23: React + Vite + TS, one app with Admin/Parent/Play areas, refresh token in an httpOnly cookie, Vietnamese UI with i18n. See the [front-end plan](../roadmap/frontend-implementation-plan.md) |
| OQ-015 _(added)_ | Does a quiz question always have exactly one correct answer? | Quiz validation and scoring | S3.2 |
| OQ-016 _(added)_ | How do levels unlock: when every lesson of the previous level is complete, or does the lesson order simply continue across levels? | Unlock rule | S4.2 |

## 8. Proposed resolution of PD-001 versus PD-007

✅ **Decided 2026-10-24: option B (PD-010).**

The conflict goes away if we separate three things:

- **Learning identity:** who owns the progress.
- **Credentials:** how someone proves who they are.
- **Session:** what a signed-in device may do.

| | A. Profile only | **B. Parent-owned profile + child session** (recommended) | C. Child login account |
|---|---|---|---|
| Child has own credentials | No | No | Yes (username + password or PIN, no email) |
| Child has a distinct identity in the app | Only a profile ID inside the parent's session | **Yes**: a child session limited to one profile | Yes |
| How a child starts playing | Parent signs in and picks a profile; the device keeps the parent's session | Parent signs in, picks a profile, and the API issues a **child session**. Leaving child mode needs the parent again | Child signs in alone |
| Child can reach Parent features on a shared device | **Yes** | No | No |
| Child data collected | Minimal | Minimal | More (stored credentials) |
| Fits PD-001 | ✅ | ✅ | ⚠️ the profile becomes an account |
| Fits PD-007 | ⚠️ weak | ✅ | ✅ |
| Complexity | Lowest | Medium | Highest |

**Why B:**
- It meets both decisions: the child has a distinct identity (PD-007) without credentials of their own (PD-001 and the privacy principles).
- All progress belongs to a stable `ChildProfile` ID.
- Parent and child sessions are clearly separated, so blocking access across families is easier to enforce and test.
- Option C can be added later on top of B without moving data.

## 9. Change log

| Date | Change |
|---|---|
| 2026-10-23 | Baseline recorded from the owner's "MVP Product Decisions" document. Added OQ-010 to OQ-016 and the proposed resolution in Section 8, both waiting for review. |
| 2026-10-23 | OQ-007 decided by the owner: recorded as PD-008 (three roles and the authorization principles). |
| 2026-10-23 | Naming: "account" renamed to "user" (permissions `users.read` / `users.manage`, "user type"). |
| 2026-10-23 | OQ-014 decided by the owner: front-end technology and session transport (see the front-end plan). |
| 2026-10-23 | PD-008 amended by the owner: roles can be created at runtime (role management, S1.5). Password rule for Admin/Parent users: at least 12 characters with upper case, lower case, a digit and a symbol. |
| 2026-10-23 | OQ-010 decided by the owner: Parents self-register with email confirmation (PD-009). Front end restructured by area with a public landing page. |
| 2026-10-24 | OQ-001, OQ-009 and OQ-013 decided by the owner (PD-010, Section 8 option B): parent-owned child profiles, several per Parent, child session started by the Parent, one Parent per profile. PD-001 and PD-007 are no longer in conflict. |
