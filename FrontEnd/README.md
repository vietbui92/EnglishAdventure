# English Adventure — Web app

React + Vite + TypeScript single-page app for the Admin, Parent and child (Play) areas.
Plan: [../docs/roadmap/frontend-implementation-plan.md](../docs/roadmap/frontend-implementation-plan.md).

## Requirements

- Node 24 LTS (at least 22.12; see `.nvmrc`). npm refuses to install on older versions (`engine-strict`).
- The backend API on `http://localhost:5063` for anything that calls the API (see [../BackEnd/README.md](../BackEnd/README.md)).
- Chrome, Edge or Firefox for development. Safari does not accept the `Secure` session cookie on `http://localhost`.

## Commands

```powershell
npm install          # install the exact versions from package-lock.json
npm run dev          # dev server on http://localhost:5173
npm run build        # type check and production build into dist/
npm run preview      # serve dist/ locally
npm run lint         # ESLint
npm run format       # Prettier (write); format:check only checks
npm test             # Vitest, single run; test:watch to watch
```

## Adding a UI component

Components come from shadcn/ui and are copied into `src/components/ui`:

```powershell
npx shadcn@latest add dialog
```

Add a component only when a step needs it. After adding it, the code is ours: edit it freely.
