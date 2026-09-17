# EventsHub — Web

The React frontend for EventsHub. Independent app in the same repo as the
.NET backend (`../src`) — separate build, separate package manager, only
connected to it via HTTP at runtime. See
[`../docs/Architecture.md`](../docs/Architecture.md) for how the two fit
together.

## Stack

Vite 8 + React 19 + TypeScript, MUI 9 (`@mui/material`, `@mui/icons-material`,
`@fontsource/roboto`) for UI, `axios` for HTTP. ESLint (flat config) with
`typescript-eslint`, `eslint-plugin-react-hooks`, `eslint-plugin-react-refresh`.
The React Compiler is enabled via `@rolldown/plugin-babel` in `vite.config.ts`.
`vite-plugin-mkcert` issues a local dev cert so the dev server can run on
`https://localhost:3000` (the origin the backend's CORS policy allows).

## Running it

```powershell
cd web
npm install
npm run dev       # https://localhost:3000
npm run build     # tsc -b && vite build
npm run lint       # eslint .
npm run preview   # preview a production build
```

The dev server expects the backend running at `https://localhost:5001`
(`dotnet run --project ../src/EventsHub.Api`) — there's no proxy config, the
frontend calls that absolute URL directly.

## Current state / known rough edges

This is an early-stage app — worth knowing before extending it:

- **`src/App.tsx`** fetches `https://localhost:5001/api/v1/events` with the
  URL hardcoded inline (`axios.get<Activity[]>(...)`). There's no `.env` /
  `import.meta.env` base-URL config yet — if you add more API calls or need
  this to work against a non-`localhost:5001` backend, that's the first gap
  to close.
- **`src/lib/types/index.d.ts`** declares shared types as *ambient* `type`
  declarations (e.g. `Activity`, which mirrors the backend's `Event`
  entity/DTO — the frontend hasn't been renamed to match yet). Being a
  `.d.ts` ambient declaration file, these types are globally available with
  no `import` statement; add new shared types here the same way rather than
  introducing per-file local type definitions for shapes coming from the API.
- No routing, no state management library, and no test setup yet — `App.tsx`
  is the entire UI (a title + a flat MUI `List` of events).

## Project layout

- `src/App.tsx` — the app's only component right now
- `src/lib/types/` — ambient shared TypeScript types
- `src/assets/` — static assets bundled by Vite
- `public/` — static assets served as-is (e.g. `favicon.svg`)
