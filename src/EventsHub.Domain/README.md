# EventsHub.Domain

The innermost layer: plain entity classes, no framework dependencies (no EF
Core, no ASP.NET references — just the `net10.0` BCL).

## Contents

- `Event.cs` — the one entity in the system. String `Id` (client-generated
  via `Guid.NewGuid().ToString()`, not database-generated), a mix of
  `required` fields (`Title`, `Description`, `Category`, `City`, `Venue`,
  `Latitude`, `Longitude` — the latter two stored as `string`, not
  numeric/decimal, matching the SQLite schema) and plain fields (`Date`,
  `IsCancelled`).

## Who uses it

- **`EventsHub.Persistence`** maps it as `DbSet<Event> Events` on `AppDbContext`.
- **`EventsHub.Application`** handlers take and return `Event`, and
  `MappingProfiles` maps `Event` → `Event` for edits.
- **`EventsHub.Api`** uses `Event` as the request body and response type of
  `EventsController`.
- **`web/`** mirrors it by hand as the ambient `Activity` type, in
  `web/src/lib/types/index.d.ts`. That type declares `latitude`/`longitude` as
  `number`, whereas the entity stores them as `string`.

`Event` is used as-is throughout the backend — as the EF Core entity, the
domain model, and the API response DTO. There's no separate DTO type; see
[`docs/Architecture.md`](../../docs/Architecture.md) for the tradeoff that
implies.

## Building

Part of the solution; build via the root solution file:

```powershell
dotnet build EventsHub.slnx
```

No standalone run/test target — this project has no executable and no
dedicated test project.
