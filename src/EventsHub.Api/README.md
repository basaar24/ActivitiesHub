# EventsHub.Api

The ASP.NET Core Web API for EventsHub — the only backend piece that's
actually part of the runtime request path (see
[`docs/Architecture.md`](../../docs/Architecture.md) for how it fits with
the rest of the system).

## What it does

- Exposes CRUD endpoints for `Event` over HTTP, backed by SQLite.
- Controllers are thin: each action builds a MediatR command/query from
  `EventsHub.Application` and sends it with `Mediator.Send(...)`. The
  handlers do the actual `AppDbContext` work.
- On every startup, applies pending EF Core migrations and seeds the
  database if it's empty (`Program.cs`, via `EventsHub.Persistence`) —
  failures here are logged, not fatal, so the app still starts even if
  migration/seeding failed.
- Allows cross-origin requests only from the Vite dev server
  (`localhost:3000`, http and https).

## Wiring (`Program.cs`)

- `AddDbContext<AppDbContext>` with `UseSqlite(ConnectionStrings:SqliteConnection)`
- `AddMediatR` scanning the `EventsHub.Application` assembly
- `AddMapper` scanning the `EventsHub.Application` assembly for mapping
  profiles (`MappingProfiles`), registered as a singleton `IMapper`
- `AddCors` + `UseCors` (any header/method, origins `http(s)://localhost:3000`)
- `MapControllers()`

This project references only `EventsHub.Application`; it reaches
`Persistence` and `Domain` transitively through it.

## Endpoints

All routes are versioned under `api/v1/` via the shared
`EventsHubBaseController` base class (`[Route("api/v1/[controller]")]`,
`[ApiController]`, and a protected `Mediator` property) — new controllers
should derive from it rather than declaring their own route.

| Method | Route | Description |
|---|---|---|
| GET | `/api/v1/Events` | List all events |
| GET | `/api/v1/Events/{id}` | Get one event by id |
| POST | `/api/v1/Events` | Create an event (body: `Event`); returns the new id |
| PUT | `/api/v1/Events` | Edit an event (body: `Event`, matched by its `Id`); `204` on success |
| DELETE | `/api/v1/Events/{id}` | Delete an event |
| GET | `/api/v1/WeatherForecast` | Unmodified ASP.NET template sample endpoint, re-parented onto the same base controller |

Note: the controller declares `400`/`404` responses, but handlers signal
"not found" with a plain `Exception` and there is no exception-handling
middleware, so a missing id currently surfaces as an unhandled server error,
not a `404`. See [`../EventsHub.Application/README.md`](../EventsHub.Application/README.md).

## Running it

From the repo root:

```powershell
dotnet run --project src/EventsHub.Api
```

Serves on `https://localhost:5001` (see `Properties/launchSettings.json`).
The connection string (`ConnectionStrings:SqliteConnection`) points at a
local `eventshub.db` file, created/migrated automatically on startup.

## Testing

Two separate test setups exist for this project:

- **`tests/EventsHub.UnitTests`** (NUnit) — meant to call controllers
  directly in-process against a real (not mocked) SQLite `AppDbContext`
  created and seeded once per run (`[SetUpFixture] GlobalTestSetup`). Tests
  read existing rows (e.g. `Events.FirstAsync()`) instead of asserting
  hardcoded counts/IDs. Run with:

  ```powershell
  dotnet test tests/EventsHub.UnitTests
  ```

  **Currently broken:** `EventsControllerTests` still does
  `new EventsController(AppDbContext)` and calls `GetEventsAsync()` without a
  cancellation token, but `EventsController` now has no constructor arguments
  and gets `IMediator` from the request services. The project fails to
  compile (CS1729) until the tests are updated — for example by setting a
  `ControllerContext` whose `HttpContext.RequestServices` provides an
  `IMediator` wired to the handlers.

- **`tests/EventsHub.IntegrationTests`** — a [Bruno](https://www.usebruno.com/)
  collection (`.yml` requests under `Events/` and `WeatherForecast/`, plus
  a `local.yml` environment) that exercises the API over real HTTP. Run it
  with the Bruno CLI or app against a running `dotnet run` instance — it is
  not part of `dotnet test`. The Create, Edit and Delete requests contain a
  test script asserting a `404` "The event was not found" body, which looks
  copied from the Get-404 request and should be reviewed.

## Regenerating the OpenAPI client

If you change a controller's routes or an entity's shape, the typed RPC
client used elsewhere is now stale. Regeneration is driven from
`EventsHub.OpenApi`, not from this project — see
[`src/EventsHub.OpenApi/README.md`](../EventsHub.OpenApi/README.md).
