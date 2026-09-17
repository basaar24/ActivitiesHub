# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

EventsHub is a school project (ICI 2026 01): a .NET 10 backend (Clean Architecture skeleton) with a React/Vite/MUI frontend. Backend and frontend are two independent apps living in the same repo (`src`/`tests` for .NET, `web` for the SPA) — there is no shared build.

## Commands

### Backend (.NET, run from repo root)

```powershell
dotnet build EventsHub.slnx                 # build everything
dotnet run --project src/EventsHub.Api      # run the API (https://localhost:5001)
dotnet test tests/EventsHub.UnitTests       # run all unit tests
dotnet test tests/EventsHub.UnitTests --filter "FullyQualifiedName~EventsControllerTests"  # single fixture
dotnet test tests/EventsHub.UnitTests --filter "Name=GetEventDetailAsync_WhenEventDoesntExist_ReturnsNotFound"  # single test
```

EF Core migrations (project = Persistence, startup = Api):

```powershell
dotnet ef migrations add <Name> -p src/EventsHub.Persistence -s src/EventsHub.Api
dotnet ef database update -p src/EventsHub.Persistence -s src/EventsHub.Api
```

### Frontend (`web/`)

```powershell
cd web
npm run dev       # Vite dev server, https://localhost:3000 (mkcert-issued cert)
npm run build     # tsc -b && vite build
npm run lint      # eslint .
```

### OpenAPI client regeneration

The typed RPC client is generated, not hand-written. Regenerate it after changing any controller/DTO shape (see `docs/BackendBuildSteps.md` step 8 for full detail):

```powershell
dotnet run --project src/EventsHub.OpenApi/EventsHub.OpenApi.csproj --no-launch-profile --urls http://127.0.0.1:5011
Invoke-WebRequest -Uri http://127.0.0.1:5011/swagger/EventsHub/swagger.json -OutFile src/openapi/EventsHub.v1.json
# stop the host, then:
dotnet tool run nswag run src/nswag/EventsHub.nswag
```

### Integration tests

`tests/EventsHub.IntegrationTests` is a Bruno collection (`.yml` requests under `Events/` and `WeatherForecast/`, environment `environments/local.yml`), run with the Bruno CLI/app — not `dotnet test`.

## Architecture

Clean-Architecture-flavored layering, but `EventsHub.Application` is currently an unused placeholder project (no files) — controllers in the Api project call `AppDbContext` directly instead of going through an application/service layer.

- **EventsHub.Domain** — POCO entities only (`Event`). No EF or ASP.NET references.
- **EventsHub.Persistence** — `AppDbContext` (SQLite via `UseSqlite`), EF Core `Migrations/`, and `DbInitializer.SeedDataAsync` (idempotent — no-ops if `Events` table is non-empty; seeds 10 Mexican-venue events spanning past/future dates).
- **EventsHub.Api** — ASP.NET Core Web API, controllers only. `Program.cs` runs `Database.MigrateAsync()` + `DbInitializer.SeedDataAsync` on every startup (wrapped in try/catch, logged, never rethrown — a DB error is silently logged and the app still starts serving with whatever schema exists). CORS is locked to `localhost:3000` (the Vite dev server) via `AllowAnyHeader().AllowAnyMethod().WithOrigins(...)`.
  - All controllers derive from `EventsHubBaseController` (`[Route("api/v1/[controller]")]`, `[ApiController]`), including `WeatherForecastController` — so routes are `api/v1/Events`, `api/v1/WeatherForecast`, etc. New controllers should follow the same base-class pattern rather than declaring their own route/attributes.
- **EventsHub.OpenApi** — a *separate*, standalone host (own `Program.cs`, own launch profile on port 5011) whose only job is to load the Api project's controllers as an MVC "application part" via reflection and serve the NSwag-generated OpenAPI document/UI. It is not part of the runtime request path of the real API — only used to (re)generate `src/openapi/EventsHub.v1.json` and the typed client. Note: it references `typeof(WeatherForecastController)` to get the Api assembly handle, since `Program` there is an internal top-level type.
- **Generated, never hand-edit**: `src/openapi/EventsHub.v1.json`, `src/EventsHub.OpenApi/Generated/EventsHubRpcClient.generated.cs`, and everything under `src/EventsHub.Persistence/Migrations/` (regenerate via `dotnet ef migrations add`, don't hand-edit migration files).

### Frontend (`web/`)

Vite + React 19 + MUI 9 + axios, TypeScript. Very early state — `App.tsx` fetches `https://localhost:5001/api/v1/events` directly with a hardcoded absolute URL (no env-based API base URL yet) and renders a flat list. Shared frontend types live in `web/src/lib/types/index.d.ts` as ambient `type` declarations (e.g. `Activity`, mirroring the backend `Event` entity/DTO) — no import statement needed, these are global ambient types.

### Testing conventions

Unit tests (`tests/EventsHub.UnitTests`) use NUnit, target the Api project's controllers directly (no `WebApplicationFactory`/HTTP layer), and share one SQLite `AppDbContext` seeded once per test run via `[SetUpFixture] GlobalTestSetup` (`OneTimeSetUp`/`OneTimeTearDown`), not per-test. Because the DB and seed data are shared across the whole fixture run, tests read existing seeded rows (e.g. `Events.FirstAsync()`) rather than asserting on hardcoded fixture data — keep new tests in that style so they don't depend on ordering or count assumptions that seeding could invalidate.

## Reference docs

`docs/` contains course-material write-ups worth checking before assuming context:
- `BackendBuildSteps.md` — full commit-by-commit reconstruction of how `src/` was built, including exact commands used for scaffolding, EF migrations, and the OpenAPI/NSwag pipeline.
- `DbContextFundamentals.md`, `OpenApiSetup.md`, `SoftwareTestingFundamentals.md`, `GitInPracticeGuide.md` — supporting concept docs for this project's stack choices.
