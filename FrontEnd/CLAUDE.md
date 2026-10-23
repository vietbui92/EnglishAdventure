# CLAUDE.md — English Adventure Web app

## Product and plan

Front end of English Adventure (children learn English through lessons and mini-games). One app, three areas: `/admin` (SuperAdmin, ContentAdmin), `/parent` (Parent), `/play` (child session).
Step-by-step plan, one approved step at a time: [../docs/roadmap/frontend-implementation-plan.md](../docs/roadmap/frontend-implementation-plan.md). Product decisions: [../docs/product/mvp-product-decisions.md](../docs/product/mvp-product-decisions.md). Backend: [../BackEnd/CLAUDE.md](../BackEnd/CLAUDE.md).

## Stack (exact versions in package.json)

Node 24, Vite 8, React 19, React Router 8, i18next, TypeScript 6 (`strict`, `noUncheckedIndexedAccess`), Tailwind CSS 4, shadcn/ui (`radix-nova` style, `radix-ui`, `cn`, `lucide-react`), openapi-fetch + openapi-typescript, TanStack Query 5, ESLint (flat config), Prettier, Vitest + jsdom, Testing Library, MSW 3.
react-hook-form + zod (with the local resolver `src/lib/zod-resolver.ts`; `@hookform/resolvers` is not used).
Planned in later steps: Playwright.

## Rules

- No new packages without the owner's approval. Versions are exact (`.npmrc` `save-exact`).
- Import from `src` with the `@/` alias.
- Folder layout (feature-first): `app/` router and shells, `api/` generated types and client, `auth/`, `features/<feature>/`, `components/ui/` (shadcn), `components/` (shared), `lib/`, `i18n/`, `styles/`, `test/`.
- shadcn components: add with `npx shadcn@latest add <name>` only when a step needs it; they are our code afterwards.
- All user-facing text goes through i18n: `const { t } = useTranslation()`, keys in `src/i18n/locales/vi.json` and `en.json` (always add both; a test checks they match). Never write UI text directly in JSX, including `aria-label`. Lesson content stays in English.
- Routes live in `src/app/routes.tsx` (the browser router is created in `main.tsx`); area shells in `src/app/layouts`. Admin and Parent use the neutral shadcn tokens in `src/index.css`; the Play area uses `.theme-play` (`src/styles/play.css`) and touch targets of at least 48px (`min-h-12`).
- API types are generated from the backend OpenAPI document, never written by hand: `npm run api:types` (API running) rewrites `src/api/schema.d.ts`. Call endpoints through `api` in `src/api/client.ts` and `unwrap()`; handle failures by `ApiError.kind`, and show `fieldErrors` on form fields. Server data is read with TanStack Query hooks (`useQuery`/`useMutation`, client from `createQueryClient()`).
- Tests (Vitest, jsdom): put `*.test.ts(x)` next to the code. Render with `renderWithProviders`, `renderRoutes` or `renderHookWithProviders` from `src/test/render.tsx`. Mock the API only with `server.use(mockApi(...))` and `problem(...)` from `src/test/api-mocks.ts`; an unmocked request fails the test. Query by role and visible Vietnamese text, and interact through the returned `user`.
- Auth: the session lives in `src/auth/session.ts` (`useSession`, `useSessionState`, `refreshSession`, `signOut`), sign-in in `src/auth/sign-in.ts`. Protect routes with `RequireAuth` / `RequirePermission` (`src/auth/guards.tsx`) and permission codes from `src/auth/permissions.ts`; give menu items a `permission`. In tests use `signInAs(role)` from `src/test/session.ts`. Access token in memory only; the refresh token is an httpOnly cookie. Send `X-EnglishAdventure-Client: web` on `login`, `refresh` and `logout` (see `BackEnd/src/EnglishAdventure.Api/Authentication/RefreshTokenCookie.cs`). Never store tokens in `localStorage`.
- The UI never decides access or grades answers. Hiding a control by permission is convenience; the API is the authority.
- Prettier style: no semicolons, single quotes, 120 columns. Run `npm run lint`, `npm run build` and `npm test` before finishing a step.
