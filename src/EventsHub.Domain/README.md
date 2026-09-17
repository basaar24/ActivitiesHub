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

`Event` is used as-is throughout the backend — as the EF Core entity, the
domain model, and the API response DTO. There's no separate DTO type or
mapping step; see
[`docs/Architecture.md`](../../docs/Architecture.md#architectural-pattern)
for the tradeoff that implies.

## Building

Part of the solution; build via the root solution file:

```powershell
dotnet build EventsHub.slnx
```

No standalone run/test target — this project has no executable and no
dedicated test project (it's exercised indirectly through
`tests/EventsHub.UnitTests`, which tests `EventsHub.Api` against real
`Event` data).
