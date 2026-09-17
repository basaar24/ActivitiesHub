# EventsHub.Persistence

The data-access layer: EF Core `DbContext`, migrations, and seed data.
Depends only on `EventsHub.Domain`.

## Contents

- **`AppDbContext.cs`** — `DbSet<Event> Events`. Configured for SQLite
  (`UseSqlite`) by whichever host wires it up (`EventsHub.Api`'s
  `Program.cs`, or `GlobalTestSetup` in the unit tests) — this project
  itself only declares the `Microsoft.EntityFrameworkCore.Sqlite` package
  reference, it doesn't hardcode a connection string.
- **`DbInitializer.cs`** — `SeedDataAsync(AppDbContext)`. Idempotent: returns
  immediately if `context.Events.Any()`. Otherwise inserts 10 fixed events
  at Mexican venues, dated across roughly nine months in the past to eight
  months in the future (`DateTime.Now.AddMonths(...)`), then
  `SaveChangesAsync()`.
- **`Migrations/`** — EF Core migration history. Currently a single
  `InitialCreate` migration describing one `Events` table (string `Id`
  primary key, all other columns `NOT NULL`, `Date` stored as `TEXT`,
  `IsCancelled` as `INTEGER`). **Generated — never hand-edit.**

## Working with migrations

Run from the repo root, with `Persistence` as `-p` (the project containing
the `DbContext`) and `Api` as `-s` (the project with the connection string
and `Microsoft.EntityFrameworkCore.Design` package):

```powershell
dotnet ef migrations add <Name> -p src/EventsHub.Persistence -s src/EventsHub.Api
dotnet ef database update -p src/EventsHub.Persistence -s src/EventsHub.Api
```

Both `EventsHub.Api` (on every startup) and
`tests/EventsHub.UnitTests` (once, in `GlobalTestSetup`) call
`context.Database.MigrateAsync()` themselves — you generally don't need to
run `database update` manually against those; it's mainly useful for
inspecting the schema directly.

## Seed data caveat

Because `SeedDataAsync` only checks "is the table non-empty," it will not
re-seed or reconcile if the schema changes shape after data already exists
— it's a first-run bootstrap, not a general fixture/sync mechanism.
