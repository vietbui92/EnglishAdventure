# CLAUDE.md — English Adventure Web app

## Product and plan

Front end of English Adventure (children learn English through lessons and mini-games). One app, three areas: `/admin` (SuperAdmin, ContentAdmin), `/parent` (Parent), `/play` (child session).
Step-by-step plan, one approved step at a time: [../docs/roadmap/frontend-implementation-plan.md](../docs/roadmap/frontend-implementation-plan.md). Product decisions: [../docs/product/mvp-product-decisions.md](../docs/product/mvp-product-decisions.md). Backend: [../BackEnd/CLAUDE.md](../BackEnd/CLAUDE.md).

## Stack (exact versions in package.json)

Node 24, Vite 8, React 19, TypeScript 6 (`strict`, `noUncheckedIndexedAccess`), Tailwind CSS 4, shadcn/ui (`radix-nova` style, `radix-ui`, `cn`, `lucide-react`), ESLint (flat config), Prettier, Vitest.
Planned in later steps: React Router, TanStack Query, react-hook-form + zod, i18next, openapi-typescript + openapi-fetch, Testing Library, MSW, Playwright.

## Rules

- No new packages without the owner's approval. Versions are exact (`.npmrc` `save-exact`).
- Import from `src` with the `@/` alias.
- Folder layout (feature-first): `app/` router and shells, `api/` generated types and client, `auth/`, `features/<feature>/`, `components/ui/` (shadcn), `components/` (shared), `lib/`, `i18n/`, `styles/`, `test/`.
- shadcn components: add with `npx shadcn@latest add <name>` only when a step needs it; they are our code afterwards.
- All user-facing text goes through i18n (`vi` default, `en` fallback) once F0.4 is done. Lesson content stays in English.
- API types are generated from the backend OpenAPI document (F0.3), never written by hand.
- Auth: access token in memory only; the refresh token is an httpOnly cookie. Send `X-EnglishAdventure-Client: web` on `login`, `refresh` and `logout` (see `BackEnd/src/EnglishAdventure.Api/Authentication/RefreshTokenCookie.cs`). Never store tokens in `localStorage`.
- The UI never decides access or grades answers. Hiding a control by permission is convenience; the API is the authority.
- Prettier style: no semicolons, single quotes, 120 columns. Run `npm run lint`, `npm run build` and `npm test` before finishing a step.
