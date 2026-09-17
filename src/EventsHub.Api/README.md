# EventsHub.Api

The ASP.NET Core Web API for EventsHub — the only backend piece that's
actually part of the runtime request path (see
[`docs/Architecture.md`](../../docs/Architecture.md) for how it fits with
the rest of the system).

## What it does

- Serves `Event` data over HTTP, read-only, from a SQLite database.
- On every startup, applies pending EF Core migrations and seeds the
  database if it's empty (`Program.cs`, via `EventsHub.Persistence`) —
  failures here are logged, not fatal, so the app still starts even if
  migration/seeding failed.
- Allows cross-origin requests only from the Vite dev server
  (`localhost:3000`, http and https).

Controllers inject `AppDbContext` directly and query it with EF Core — there
is no service/repository layer, despite `EventsHub.Application` existing in
the project's reference graph. See
[`docs/Architecture.md`](../../docs/Architecture.md#architectural-pattern)
for why.

## Endpoints

All routes are versioned under `api/v1/` via the shared
`EventsHubBaseController` base class (`[Route("api/v1/[controller]")]`,
`[ApiController]`) — new controllers should derive from it rather than
declaring their own route.

| Method | Route | Description |
|---|---|---|
| GET | `/api/v1/Events` | List all events |
| GET | `/api/v1/Events/{id}` | Get one event by id; `404` with a plain-string body if not found |
| GET | `/api/v1/WeatherForecast` | Unmodified ASP.NET template sample endpoint, re-parented onto the same base controller |

## Running it

From the repo root:

```powershell
dotnet run --project src/EventsHub.Api
```

Serves on `https://localhost:5001` (see `Properties/launchSettings.json`).
The connection string (`ConnectionStrings:SqliteConnection`) points at a
local `eventshub.db` file, created/migrated automatically on startup.

## Testing

Two separate, non-overlapping test setups exist for this project:

- **`tests/EventsHub.UnitTests`** (NUnit) — calls controllers directly
  in-process, no HTTP involved, against a real (not mocked) SQLite
  `AppDbContext` that's created and seeded once per test run
  (`[SetUpFixture] GlobalTestSetup`, not per test). Because the DB and seed
  data are shared across the whole run, tests read existing rows (e.g.
  `Events.FirstAsync()`) instead of asserting hardcoded counts/IDs. Run with:

  ```powershell
  dotnet test tests/EventsHub.UnitTests
  ```

- **`tests/EventsHub.IntegrationTests`** — a [Bruno](https://www.usebruno.com/)
  collection (`.yml` requests under `Events/` and `WeatherForecast/`, plus
  a `local.yml` environment) that exercises the API over real HTTP. Run it
  with the Bruno CLI or app against a running `dotnet run` instance — it is
  not part of `dotnet test`.

## Regenerating the OpenAPI client

If you change a controller's routes or an entity's shape, the typed RPC
client used elsewhere is now stale. Regeneration is driven from
`EventsHub.OpenApi`, not from this project — see
[`src/EventsHub.OpenApi/README.md`](../EventsHub.OpenApi/README.md).
