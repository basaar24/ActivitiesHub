# Proposal

## Why

`EventsHub.Application` uses AutoMapper 13.0.1 for a single job: `EditEvent.Handler` copies an incoming `Event` onto the tracked entity through `IMapper`, using one `Event` → `Event` map in `MappingProfiles`. That package version carries a known high-severity advisory (build warning NU1903 on every project that references it), and the project owns a small, well-understood mapping need. A first-party mapper removes the vulnerable dependency and lets the team shape the mapping API for the profiles it plans to add.

## What Changes

- Add a first-party mapper to `EventsHub.Application` (under `Core/Mapping`): an `IMapper` abstraction, a `Profile` base class with `CreateMap<TSource, TDestination>()`, and a mapper implementation that builds and caches its maps once at startup.
- Convert `MappingProfiles` to the new `Profile` base class, keeping the existing `Event` → `Event` map behavior.
- Keep the call site shape: `EditEvent.Handler` still injects `IMapper` and calls `Map(source, destination)`; only the `using` changes from `AutoMapper` to the new namespace.
- Replace `AddAutoMapper(typeof(MappingProfiles).Assembly)` in `EventsHub.Api/Program.cs` with an equivalent registration that scans an assembly for `Profile` subclasses, so profiles added later are picked up without further wiring.
- Support what future profiles need beyond same-name copying: per-member overrides (`ForMember` with `MapFrom` or `Ignore`), mapping into a new destination instance, and nested/collection members that have their own registered map.
- **BREAKING (internal only):** remove the `AutoMapper` package reference from `EventsHub.Application.csproj`. No HTTP API or database behavior changes.
- Update the affected README text (Api, Application, Domain, OpenApi) that names AutoMapper.

Non-goals: query projection (`ProjectTo`), automatic flattening/unflattening, automatic reverse maps, and value-converter/resolver plugin systems. These can be added when a real profile needs them.

## Capabilities

### New Capabilities
- `object-mapping`: How application code maps one object onto another (existing or new instance) using registered profiles, including convention-based member matching, per-member overrides, and clear failures for unregistered maps.

### Modified Capabilities
<!-- None: openspec/specs/ is empty, and no existing spec-level behavior changes. -->

## Impact

- **Code:** `src/EventsHub.Application/Core/MappingProfiles.cs`, new files under `src/EventsHub.Application/Core/Mapping/`, `src/EventsHub.Application/Events/Commands/EditEvent.cs`, `src/EventsHub.Api/Program.cs`.
- **Dependencies:** removes `AutoMapper` 13.0.1 (clears warning NU1903); no new packages (reflection plus compiled expression delegates from the BCL).
- **Behavior:** `PUT /api/v1/Events` must behave as before — the tracked entity is updated from the request body.
- **Tests:** there is currently no test coverage of mapping, and the unit-test project does not compile (`EventsControllerTests` still constructs `EventsController(AppDbContext)`). This change adds mapper tests in a new project, `tests/EventsHub.Application.UnitTests`, so they do not depend on repairing that fixture.
- **Docs:** READMEs listed above; `CLAUDE.md` and `docs/` do not currently name AutoMapper beyond a comparison table and are left as they are.
