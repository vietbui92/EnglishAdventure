# CLAUDE.md — English Adventure Web app

## Product and plan

Front end of English Adventure (children learn English through lessons and mini-games). One app, four areas: the public site (`/`, `/login`, `/register`), `/admin` (SuperAdmin, ContentAdmin), `/parent` (Parent) and `/kids` (child session).
Step-by-step plan, one approved step at a time: [../docs/roadmap/frontend-implementation-plan.md](../docs/roadmap/frontend-implementation-plan.md). Product decisions: [../docs/product/mvp-product-decisions.md](../docs/product/mvp-product-decisions.md). Backend: [../BackEnd/CLAUDE.md](../BackEnd/CLAUDE.md).

## Stack (exact versions in package.json)

Node 24, Vite 8, React 19, React Router 8, i18next, TypeScript 6 (`strict`, `noUncheckedIndexedAccess`), Tailwind CSS 4, shadcn/ui (`radix-nova` style, `radix-ui`, `cn`, `lucide-react`), openapi-fetch + openapi-typescript, TanStack Query 5, ESLint (flat config), Prettier, Vitest + jsdom, Testing Library, MSW 3.
react-hook-form + zod (with the local resolver `src/shared/lib/zod-resolver.ts`; `@hookform/resolvers` is not used).
Planned in later steps: Playwright.

## Source layout: one folder per area

```
src/
  main.tsx, index.css   entry and global styles (shadcn tokens)
  app/routes.tsx        puts the areas' routes together; nothing else lives here
  areas/
    public/             landing, sign-in, registration, email confirmation, legal pages
    admin/              AdminLayout, users/ (list, detail, create, roles), placeholders
    parent/             ParentLayout, placeholders
    kids/               KidsLayout, kids.css (bright theme), KidsHomePage
  shared/
    api/                generated schema, client, ApiError, query client
    auth/               session, sign-in, guards, permissions
    components/         ui/ (shadcn), layouts' building blocks, error pages
    i18n/  lib/  test/  routing.tsx
```

Every area folder has the same shape:

- `routes.tsx`: the area's routes. It imports only the guards and `@/shared/routing`, never the area's pages.
- `index.ts`: exports every page and layout of the area. It is the entry of the area's lazily loaded chunk.
- `i18n/vi.json` and `i18n/en.json`: the area's texts under one top-level key (`public`, `admin`, `parent`, `kids`).
- Pages, layout and feature folders (`admin/users/`), each with its tests next to it.

**Boundaries (enforced by ESLint `no-restricted-imports`):**

- An area imports only its own files and `@/shared/*`. Never another area. Code two areas need moves to `src/shared`.
- `src/shared` imports neither an area nor `src/app`.
- Only `src/main.tsx` and tests import `src/app`. Tests may render the whole app (`routes` from `@/app/routes`).
- Use the `@/` alias, never `../`.

## Rules

- No new packages without the owner's approval. Versions are exact (`.npmrc` `save-exact`).
- **Adding a page:** create it in its area, export it from the area's `index.ts`, and route it in the area's `routes.tsx` with `lazy: lazyPage(area, (admin) => admin.MyPage)`. Put permission checks in `withPermission(...)` (`src/shared/routing.tsx`). Never import a page into a `routes.tsx` directly, or it lands in the main bundle. The landing page is the one exception: it stays eager because most visitors arrive there.
- **Adding an area:** a new folder in `src/areas` with the files above, its routes in `src/app/routes.tsx`, its name in the `areas` list in `eslint.config.js`, and its `vi.json` type in `src/shared/i18n/i18next.d.ts`.
- shadcn components: add with `npx shadcn@latest add <name>` only when a step needs it (they land in `src/shared/components/ui`); they are our code afterwards. Answer "no" if it asks to overwrite `button.tsx`.
- All user-facing text goes through i18n: `const { t } = useTranslation()`. Texts live in the area's `i18n/{vi,en}.json` (always add both; tests check that the keys match and that no two files share a top-level key); shared texts in `src/shared/i18n/locales`. Never write UI text directly in JSX, including `aria-label`. Lesson content stays in English.
- Styling: Admin, Parent and the public site use the shadcn tokens in `src/index.css` (the landing page adds Tailwind palette accents). The kids area uses `.theme-kids` (`src/areas/kids/kids.css`) and touch targets of at least 48px (`min-h-12`).
- API types are generated from the backend OpenAPI document, never written by hand: `npm run api:types` (API running) rewrites `src/shared/api/schema.d.ts`. Call endpoints through `api` in `src/shared/api/client.ts` and `unwrap()`; handle failures by `ApiError.kind`, and show `fieldErrors` on form fields. Server data is read with TanStack Query hooks (`useQuery`/`useMutation`, client from `createQueryClient()`); keep an area's hooks in that area (e.g. `areas/admin/users/api.ts`).
- Tests (Vitest, jsdom): put `*.test.ts(x)` next to the code. Render with `renderWithProviders`, `await renderRoutes(routes, { route })` (async: it waits for lazy routes and redirects) or `renderHookWithProviders` from `src/shared/test/render.tsx`. Mock the API only with `server.use(mockApi(...))` and `problem(...)` from `src/shared/test/api-mocks.ts`; an unmocked request fails the test. Query by role and visible Vietnamese text, and interact through the returned `user`.
- Auth: the session lives in `src/shared/auth/session.ts` (`useSession`, `useSessionState`, `refreshSession`, `signOut`), sign-in in `src/shared/auth/sign-in.ts`. Protect routes with `RequireAuth` / `RequirePermission` (`src/shared/auth/guards.tsx`) and permission codes from `src/shared/auth/permissions.ts`; give menu items a `permission`. A route for the kids area uses `<RequireAuth childSession />`; all others refuse child sessions. In tests use `signInAs(role)` or `signInAsChild()` from `src/shared/test/session.ts`. Access token in memory only; the refresh token is an httpOnly cookie. Send `X-EnglishAdventure-Client: web` on `login`, `refresh` and `logout` (see `BackEnd/src/EnglishAdventure.Api/Authentication/RefreshTokenCookie.cs`). Never store tokens in `localStorage`.
- Passwords for new accounts: validate with `isStrongPassword` (`src/shared/auth/password-rules.ts`, mirrors the backend rule) and show `PasswordChecklist` under the field.
- Child avatars: draw them only with `ChildAvatar` (`src/shared/components/ChildAvatar.tsx`); its keys mirror the backend `ChildProfile.AvatarKeys`. Tests that pass through `/parent/children` need `server.use(...mockFamilyWithoutChildren())`.
- The UI never decides access or grades answers. Hiding a control by permission is convenience; the API is the authority.
- Prettier style: no semicolons, single quotes, 120 columns. Run `npm run lint`, `npm run build` and `npm test` before finishing a step.
