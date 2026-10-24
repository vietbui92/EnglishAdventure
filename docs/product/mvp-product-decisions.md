# English Adventure: MVP Product Decisions

| | |
|---|---|
| **Owner** | Quoc Viet Bui (product owner). Only the owner changes a decision. |
| **Status** | Baseline. PD-001 to PD-013 are decided; PD-010 reconciles PD-001 and PD-007 (Section 8, option B). |
| **Last updated** | 2026-10-24 |
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
| PD-011 | Lesson design, stars, levels and rewards (answers OQ-002, OQ-003, OQ-004, OQ-016) | Word bank and learn step per lesson, 0–3 stars per lesson, a boss challenge ends each level, 10 points per new star, four badge types | ✅ Decided 2026-10-24 |
| PD-012 | Content editing, pictures, quiz answers, resume, boss (answers OQ-005, OQ-011, OQ-012, OQ-015, OQ-017) | Structure locked once published; picture URLs; one correct quiz option; resume per activity; boss rules | ✅ Decided 2026-10-24 |
| PD-013 | Discovery walk, review cards and speaking practice (changes PD-011's learn step) | Words met moving on the island; cards only to review; speech recognition, not scored | ✅ Decided 2026-10-24 |

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

### PD-011: Lesson design, stars, levels and rewards (answers OQ-002, OQ-003, OQ-004, OQ-016)

Decided by the owner on 2026-10-24. The learning experience works like a game: the child moves across an adventure map, earns stars and defeats a boss to reach the next level.

**Content structure**

```
Adventure map
└── Level        a topic, for example "Animal Island"; 4–6 lessons, then a boss challenge
    └── Lesson   5–8 words, 2–4 activities, about 5–10 minutes
        ├── Learn step: word cards (picture, English word, Vietnamese meaning), not scored
        └── Activities: Quiz and Matching, built from the lesson's word bank
```

- **Word bank per lesson.** The Admin enters each word once (English word, Vietnamese meaning, picture), then builds Quiz and Matching activities by choosing words from the bank. This is faster, avoids typing errors, and later lets us show which words a child knows. Where pictures come from is still OQ-011. Audio comes later.
- **Learn step.** Every lesson starts with word cards made from its word bank, so the child sees the words before being tested. It is not scored.
- **Quiz forms** (still "pick one option", PD-003): picture → word, word → picture, fill in the missing word. "Listen → pick" waits for audio.
- **Boss challenge.** Each level ends with a boss challenge ("defeat the boss"). The details are OQ-017.

**Stars (OQ-002).** The server computes the score of an attempt as the percentage of correct scored items.

| Score | Stars | Meaning |
|---|---|---|
| below 60% | 0 | Not passed ("Try again!") |
| 60% or more | ⭐ | Passed: the lesson is completed and the next one unlocks |
| 80% or more | ⭐⭐ | |
| 100% | ⭐⭐⭐ | Perfect |

**Replay (OQ-003).** A child can replay any lesson they have unlocked, at any time. The best result is kept. A lesson is completed only once, on the first attempt with at least one star.

**Levels (OQ-016).** The boss challenge of a level unlocks when every regular lesson of that level has at least one star. Defeating the boss unlocks the next level. The **child's level** is the highest level unlocked. It is computed from results and never stored separately, so reordering content cannot corrupt it. Stars are not a gate ("collect N stars"): they only encourage replaying for three stars.

**Points (OQ-004).** 10 points for each newly earned star. Example: a first attempt with ⭐⭐ gives 20 points, a replay with ⭐⭐⭐ gives 10 more, and a replay without a new star gives nothing. Each star of a lesson can be rewarded only once.

**Badges (OQ-004).** Each badge is awarded at most once per child:
- **First lesson:** the first completed lesson.
- **Perfect:** the first lesson with ⭐⭐⭐.
- **Level conqueror** (one per level): the level's boss is defeated.
- **Level superstar** (one per level): ⭐⭐⭐ on every lesson of the level, including the boss (PD-012).

### PD-012: Content editing, pictures, quiz answers, resume and the boss (answers OQ-005, OQ-011, OQ-012, OQ-015, OQ-017)

Decided by the owner on 2026-10-24 (all proposals accepted).

- **Editing published content (OQ-005).** Once a lesson is published its structure is locked: only texts and pictures can be corrected. Adding, removing or reordering words, activities, questions or options needs the lesson to be unpublished, which is allowed only while no child has started it. After that, the lesson can only be archived and replaced by a new one. Children's past results are never changed.
- **Pictures (OQ-011).** The Admin enters a picture URL. File uploads (Blob Storage) come later.
- **Quiz answers (OQ-015).** Every quiz question has exactly one correct option.
- **Resume (OQ-012).** A child who leaves in the middle of a lesson continues from the activity where they stopped. Answers are saved per activity.
- **Boss challenge (OQ-017).**
  - It unlocks when every regular lesson of the level has at least one star.
  - The Admin builds it like a lesson, from Quiz and Matching activities using words from the whole level, and gives it a boss name and picture.
  - A boss health bar drops with each correct answer. It is display only; the server still grades.
  - Same star thresholds as a lesson; it gives stars and points like a lesson. No lives.
  - "Level superstar" includes the boss.

### PD-013: Discovery walk, review cards and speaking practice

Decided by the owner on 2026-10-24. It changes the learn step of PD-011: the word cards are now for review, after a discovery walk.

- **Discovery first.** On the island the child walks along and meets each word in turn: its picture moves the way the word does, it plays its sound if it has one, and shows the English word and its meaning while the device's voice reads the word.
- **How a picture moves.** The Admin picks a movement per word: still (the default), walk, hop, fly, swim, crawl or wiggle. The app animates the word's picture; no animated media is needed.
- **Sounds.** The sound a word makes (a cat's meow) is an optional https link to a sound file, like pictures (PD-012). The word itself is read by the device's English voice, not by recorded files.
- **Review and speaking after.** When the walk ends, the cards come back for review, and the child can practise saying each word. The browser's speech recognition checks what the child said. It is on whenever the browser supports it, without a Parent setting (owner's choice). Practice is never scored: stars still come only from quizzes and matching (PD-012), and nothing about it is saved on the server.
- **Privacy note (accepted by the owner).** With speech recognition, the browser sends the child's voice to its maker's servers (Google for Chrome, Apple for Safari) to turn it into text. Our servers never receive or store audio. This must be covered in the privacy text and reviewed with OQ-008 before the production release.

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
| OQ-002 | What counts as completing a lesson (for example, a minimum score)? | Scoring, unlocking, progress state | ✅ **DECIDED** 2026-10-24 → PD-011: at least 60% (one star) |
| OQ-003 | Can a child replay completed lessons? How are repeat scores handled? | Attempt history, best score, rewards | ✅ **DECIDED** 2026-10-24 → PD-011: replay any time, best result kept, completed once |
| OQ-004 | What are the exact rules for awarding points and badges? | Reward integrity | ✅ **DECIDED** 2026-10-24 → PD-011: 10 points per new star, four badge types |
| OQ-005 | Can an Admin edit, reorder or unpublish a lesson after learners have started it? | Content versioning, historical progress | ✅ **DECIDED** 2026-10-24 → PD-012: structure locked once published |
| OQ-006 | What levels, topics and lessons does the MVP start with? | Seed content, acceptance tests | S3.5 |
| OQ-007 | Which roles and permissions does the first release need? | Authorization policies | ✅ **DECIDED** 2026-10-23 → PD-008. Roles: SuperAdmin, ContentAdmin, Parent. The permission catalog is finalized in S1.1 |
| OQ-008 | Which markets, and what child-consent, retention and deletion rules apply? | Privacy controls, production readiness | S1.4, S6.4 |
| OQ-009 | If independent identity is kept: how does a child start a session? | Credentials, or parent-authorized access | ✅ **DECIDED** 2026-10-24 → PD-010: the Parent starts a child session |
| OQ-010 _(added)_ | How are Parent users created: self-registration, or created by an Admin? Is email confirmation required? | Public endpoints, consent, abuse protection | ✅ **DECIDED** 2026-10-23 → PD-009: self-registration with email confirmation |
| OQ-011 _(added)_ | Where do word pictures come from: a URL entered by the Admin, or uploaded files? | Media storage (uploads need Blob Storage) | ✅ **DECIDED** 2026-10-24 → PD-012: a URL first, uploads later |
| OQ-012 _(added)_ | Does "resume" mean continuing from the next lesson, or from the middle of a lesson? | Saving answers per activity or per lesson | ✅ **DECIDED** 2026-10-24 → PD-012: from the activity where the child stopped |
| OQ-013 _(added)_ | Can a child profile belong to more than one Parent (for example two guardians)? | Family data model | ✅ **DECIDED** 2026-10-24 → PD-010: one Parent per profile in the MVP |
| OQ-014 _(added)_ | Which front-end technology? (`FrontEnd/` is empty) | Front-end track, API contract style, refresh-token transport | ✅ **DECIDED** 2026-10-23: React + Vite + TS, one app with Admin/Parent/Play areas, refresh token in an httpOnly cookie, Vietnamese UI with i18n. See the [front-end plan](../roadmap/frontend-implementation-plan.md) |
| OQ-015 _(added)_ | Does a quiz question always have exactly one correct answer? | Quiz validation and scoring | ✅ **DECIDED** 2026-10-24 → PD-012: exactly one |
| OQ-016 _(added)_ | How do levels unlock: when every lesson of the previous level is complete, or does the lesson order simply continue across levels? | Unlock rule | ✅ **DECIDED** 2026-10-24 → PD-011: defeat the previous level's boss |
| OQ-017 _(added)_ | How does the boss challenge work? | Boss content model, unlock rule, badge rule | ✅ **DECIDED** 2026-10-24 → PD-012 |

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
| 2026-10-24 | OQ-002, OQ-003, OQ-004 and OQ-016 decided by the owner (PD-011): word bank and learn step, 60/80/100% stars, replay with best result, boss challenge per level, 10 points per new star, four badge types. Added OQ-017 (boss details). |
| 2026-10-24 | OQ-005, OQ-011, OQ-012, OQ-015 and OQ-017 decided by the owner (PD-012): all proposals accepted. Remaining open: OQ-006, OQ-008. |
