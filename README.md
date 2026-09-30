# EventsHub

School project (ICI 2026 01): a .NET 10 backend with a React frontend, kept as
two independent apps in one repo — there is no shared build.

| Path | What it is | README |
|---|---|---|
| `src/EventsHub.Api` | ASP.NET Core Web API — the only backend piece in the runtime request path | [README](src/EventsHub.Api/README.md) |
| `src/EventsHub.Application` | MediatR commands/queries and handlers (the use cases) | [README](src/EventsHub.Application/README.md) |
| `src/EventsHub.Domain` | The `Event` entity | [README](src/EventsHub.Domain/README.md) |
| `src/EventsHub.Persistence` | EF Core `AppDbContext`, migrations, seed data (SQLite) | [README](src/EventsHub.Persistence/README.md) |
| `src/EventsHub.OpenApi` | Standalone host that generates the OpenAPI document and typed client | [README](src/EventsHub.OpenApi/README.md) |
| `web/` | React 19 + Vite + MUI single-page app | [README](web/README.md) |
| `tests/` | NUnit unit tests and a Bruno HTTP collection | see the Api README |
| `docs/` | Architecture write-ups and course-material guides | [Architecture](docs/Architecture.md) |

## How the pieces connect

```
web (React)  --HTTP-->  EventsHub.Api  --MediatR-->  EventsHub.Application  -->  EventsHub.Persistence  -->  SQLite
                         controllers                   handlers                    AppDbContext               eventshub.db
                                                             \__ EventsHub.Domain (Event) used by every layer
EventsHub.OpenApi --loads Api assembly--> OpenAPI JSON --NSwag--> typed client   (build-time only)
```

- A controller in `EventsHub.Api` builds a MediatR command or query and sends
  it through `Mediator` (from `EventsHubBaseController`).
- The matching handler in `EventsHub.Application` uses `AppDbContext`
  directly (no repository layer) and returns `Event` objects, which are also
  the API response shape.
- `EventsHub.Api` references only `EventsHub.Application`; it reaches
  `Persistence` and `Domain` through that project reference.

## Quick start

```powershell
# backend (https://localhost:5001)
dotnet run --project src/EventsHub.Api

# frontend (https://localhost:3000)
cd web
npm install
npm run dev
```

Other commands (build, tests, EF migrations, OpenAPI regeneration) are listed
in the READMEs above and in [`CLAUDE.md`](CLAUDE.md).
