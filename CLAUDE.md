# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

EventsHub is a school project (ICI 2026 01): a .NET 10 backend (Clean Architecture skeleton) with a React/Vite/MUI frontend. Backend and frontend are two independent apps living in the same repo (`src`/`tests` for .NET, `web` for the SPA) — there is no shared build.

## Commands

### Backend (.NET, run from repo root)

```powershell
dotnet build EventsHub.slnx                 # build everything
dotnet run --project src/EventsHub.Api      # run the API (https://localhost:5001) — needs PostgreSQL reachable at the DefaultConnection string (localhost:5432)
dotnet test EventsHub.slnx                  # all tests (40: 32 Application + 8 Api)
dotnet test tests/EventsHub.Application.UnitTests  # 20 mapper tests (pure) + 12 handler tests (database via HandlerTestBase)
dotnet test tests/EventsHub.Application.UnitTests --filter "FullyQualifiedName~EventsHub.Application.UnitTests.Mapping"  # mapper tests only
dotnet test tests/EventsHub.Api.UnitTests   # 8 controller tests
dotnet test tests/EventsHub.Api.UnitTests --filter "FullyQualifiedName~EventsControllerTests"  # single fixture
dotnet test tests/EventsHub.Api.UnitTests --filter "Name=GetEventDetailAsync_WhenEventDoesntExist_ThrowsActivityNotFound"  # single test
```

The database is PostgreSQL (Npgsql). The container work (Dockerfiles, Docker Compose, Testcontainers) is described step by step in `docs/guides/ContainersImplementationGuide.md`, which also marks which steps are already applied — check that guide and the actual files before assuming the containers or Testcontainers exist yet.

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

The typed RPC client is generated, not hand-written. Regenerate it after changing any controller/DTO shape (see `docs/guides/OpenApiSetup.md` for full detail):

```powershell
dotnet run --project src/EventsHub.OpenApi/EventsHub.OpenApi.csproj --no-launch-profile --urls http://127.0.0.1:5011
Invoke-WebRequest -Uri http://127.0.0.1:5011/swagger/EventsHub/swagger.json -OutFile src/openapi/EventsHub.v1.json
# stop the host, then:
dotnet tool run nswag run src/nswag/EventsHub.nswag
```

### Integration tests

`tests/EventsHub.IntegrationTests` is a Bruno collection (`.yml` requests under `Events/` and `WeatherForecast/`, environment `environments/local.yml`), run with the Bruno CLI/app — not `dotnet test`.

## Architecture

Clean-Architecture-flavored layering with CQRS: controllers in the Api project send MediatR requests (via `EventsHubBaseController.Mediator`) to handlers in `EventsHub.Application`. Handlers live in `Events/Commands` (`CreateEvent`, `EditEvent`, `DeleteEvent`) and `Events/Queries` (`GetEventList`, `GetEventDetails`), one static-class-per-use-case with nested `Command`/`Query` and `Handler`. `Core/` holds a custom `IMapper` + `MappingProfiles` (registered with `AddMapper`). Handlers use `AppDbContext` directly and signal "not found" by throwing `Exception` (there is no exception-to-404 middleware).

- **EventsHub.Domain** — POCO entities only (`Event`). No EF or ASP.NET references.
- **EventsHub.Persistence** — `AppDbContext` (PostgreSQL via Npgsql), EF Core `Migrations/`, and `DbInitializer.SeedDataAsync` (idempotent — no-ops if `Events` table is non-empty; seeds 10 Mexican-venue events spanning past/future dates). Npgsql maps `DateTime` to `timestamp with time zone` and rejects non-UTC values, so: seed/test data must use `DateTime.UtcNow`, and `AppDbContext.OnModelCreating` has a value converter forcing `Event.Date` to `Kind=Utc` (the SPA's date input sends dates without a time zone).
- **EventsHub.Api** — ASP.NET Core Web API, controllers only. Connection string key is `ConnectionStrings:DefaultConnection`. `Program.cs` runs `Database.MigrateAsync()` + `DbInitializer.SeedDataAsync` on every startup (wrapped in try/catch, logged, never rethrown — a DB error is silently logged and the app still starts serving with whatever schema exists). CORS is locked to `localhost:3000` (the Vite dev server) via `AllowAnyHeader().AllowAnyMethod().WithOrigins(...)`.
  - All controllers derive from `EventsHubBaseController` (`[Route("api/v1/[controller]")]`, `[ApiController]`), including `WeatherForecastController` — so routes are `api/v1/Events`, `api/v1/WeatherForecast`, etc. New controllers should follow the same base-class pattern rather than declaring their own route/attributes.
- **EventsHub.OpenApi** — a _separate_, standalone host (own `Program.cs`, own launch profile on port 5011) whose only job is to load the Api project's controllers as an MVC "application part" via reflection and serve the NSwag-generated OpenAPI document/UI. It is not part of the runtime request path of the real API — only used to (re)generate `src/openapi/EventsHub.v1.json` and the typed client. Note: it references `typeof(WeatherForecastController)` to get the Api assembly handle, since `Program` there is an internal top-level type.
- **Generated, never hand-edit**: `src/openapi/EventsHub.v1.json`, `src/EventsHub.OpenApi/Generated/EventsHubRpcClient.generated.cs`, and everything under `src/EventsHub.Persistence/Migrations/` (regenerate via `dotnet ef migrations add`, don't hand-edit migration files).

### Frontend (`web/`)

Vite + React 19 + MUI 9 + axios + TanStack Query + react-router, TypeScript. Feature-based layout under `web/src` (`app/` for layout/router/shared, `features/activities` with `dashboard`/`details`/`form`, `features/home`, `lib/` for api/hooks/types). The axios instance in `web/src/lib/api/agent.ts` uses `import.meta.env.VITE_API_URL` as its base URL (`web/.env.development` sets `https://localhost:5001/api/v1`; it is gitignored). `VITE_*` values are baked in at build time, not read at runtime. Shared frontend types live in `web/src/lib/types/index.d.ts` as ambient `type` declarations (e.g. `Activity`, mirroring the backend `Event` entity/DTO) — no import statement needed, these are global ambient types.

### Testing conventions

Two NUnit test projects (no `WebApplicationFactory`/HTTP layer):

- **`tests/EventsHub.Api.UnitTests`** — controller tests. `EventsControllerTests` builds its own `ServiceCollection` (MediatR + the shared `AppDbContext` + mapper) and gives the controller a `DefaultHttpContext` as `ControllerContext`. They share one `AppDbContext` seeded once per test run via `[SetUpFixture] GlobalTestSetup` (`OneTimeSetUp`/`OneTimeTearDown`), not per-test. Because the DB and seed data are shared, tests read existing seeded rows (e.g. `Events.FirstAsync()`) rather than asserting on hardcoded data, and any test that writes registers its rows in `_ownedEventIds` so `TearDown` removes them. Keep new tests in that style so they don't depend on ordering or count assumptions that seeding could invalidate.
- **`tests/EventsHub.Application.UnitTests`** — `Mapping/` (pure mapper tests, no database) and `Events/` (handler tests). Handler tests inherit `HandlerTestBase`, which gives **every test its own empty database** (`Context`, plus `NewEvent` and `SeedAsync`, which detaches the entity so the handler reads from the database) and construct handlers directly with `new`, not through MediatR.

**Current state:** both test projects still create their database with SQLite (`UseSqlite` in `GlobalTestSetup.cs` and `HandlerTestBase.cs`), which no longer matches the PostgreSQL provider used by the app. The planned migration to Testcontainers (one PostgreSQL container per test project; a fresh database per test in `HandlerTestBase`; handler test files unchanged) is Part E of `docs/guides/ContainersImplementationGuide.md`. Once Part E is applied, update this section.

## Reference docs

`docs/` contains course-material write-ups worth checking before assuming context:

- `docs/Architecture.md` — architecture overview.
- `docs/fundamentals/` — concept docs: `CleanArchitectureFundamentals.md`, `CqrsFundamentals.md`, `DbContextFundamentals.md`, `SoftwareTestingFundamentals.md`.
- `docs/guides/` — how-tos: `ContainersImplementationGuide.md` (PostgreSQL, Docker, Compose, Testcontainers, optional HTTPS proxy), `OpenApiSetup.md` (OpenAPI/NSwag pipeline), `GitInPracticeGuide.md`, `husky-setup.md`, `install-graphify-openspec.md`.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:

- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
