# Design

## Context

Current state (observed): the only consumer is `EditEvent.Handler(AppDbContext, IMapper)`, which calls `mapper.Map(request.Event, trackedEvent)` to copy fields onto an EF-tracked entity. `Core/MappingProfiles.cs` holds one profile with `CreateMap<Event, Event>()`. `Program.cs` registers it with `AddAutoMapper(typeof(MappingProfiles).Assembly)`. `Event` has `required` members and a string `Id`. The `EventsHub.Application` project already references `EventsHub.Domain` and `EventsHub.Persistence`; adding a mapper there keeps the layering as is. See proposal.md for motivation.

Constraints: no new NuGet packages; the copy onto the tracked entity must keep working with EF change tracking (values assigned through normal property setters); and the mapper is built at startup, then used concurrently by many requests.

## Goals / Non-Goals

**Goals:**
- Same call shape for handlers: inject `IMapper`, call `Map(source, destination)`.
- Profiles are declared in code the same way as today (subclass a `Profile`, call `CreateMap`), so existing and future profiles look familiar.
- Maps are built once, then executed as cached delegates (no per-call reflection).
- Errors are detected as early as possible: duplicates and invalid overrides at startup.

**Non-Goals:**
- `ProjectTo`/IQueryable projection, flattening, automatic reverse maps, converter/resolver extension points, mapping validation of unmapped destination members.
- Publishing this as a reusable library; it lives in `EventsHub.Application`.

## Decisions

### 1. Namespace and layout
New files under `src/EventsHub.Application/Core/Mapping/`, namespace `EventsHub.Application.Core.Mapping`:

- `IMapper` — `TDestination Map<TDestination>(object source)` and `TDestination Map<TSource, TDestination>(TSource source, TDestination destination)`.
- `Profile` — abstract base with protected `CreateMap<TSource, TDestination>()` returning an `IMappingExpression<TSource, TDestination>`.
- `IMappingExpression<TSource, TDestination>` — `ForMember(dest => dest.X, opt => opt.MapFrom(src => ...))` and `ForMember(dest => dest.X, opt => opt.Ignore())`.
- `Mapper` — the `IMapper` implementation, holding a read-only dictionary keyed by `(Type source, Type destination)`.
- `MappingServiceCollectionExtensions.AddMapper(Assembly)` — scans the assembly for concrete `Profile` subclasses, instantiates each, builds the mapper, and registers `IMapper` as a singleton.

`MappingProfiles` stays where it is and now derives from the new `Profile`. The `Map(source, destination)` shape matches AutoMapper's, so `EditEvent` only changes its `using`.

*Alternative considered:* place the mapper in a new `EventsHub.Mapping` project. Rejected — one consumer today; a project boundary adds solution and reference churn without a second user.

### 2. Compiled delegates per map
When a profile calls `CreateMap`, the map records member rules. On `Build()`, for each destination property that is writable, the mapper resolves a rule in this order: explicit `Ignore` → explicit `MapFrom` → same-name (ordinal, case-sensitive) readable source property with an assignable type → nested/collection map lookup when types differ → unmapped (skipped). It then compiles one `Action<TSource, TDestination>` (via `System.Linq.Expressions`) that assigns members in sequence, and a factory `Func<TDestination>` for the new-instance path.

*Alternatives considered:* (a) plain reflection at each call — simpler, but slower and repeated for every request; (b) source generators — best performance, disproportionate for this project; (c) hand-written mapping methods — no profile abstraction, and the user asked for profile support.

### 3. Members from the source are matched case-sensitively
Same-name matching is ordinal and case-sensitive, so a typo in a future profile shows up as an unmapped member rather than an unintended match. This matches how the C# types in this solution are already named. Can be relaxed later behind an option if a real mapping needs it.

### 4. Nested and collection members
When source and destination member types are not assignable, the builder looks up a map for those types. Collections are recognized as `IEnumerable<T>` sources mapped into `List<T>`-compatible destinations, element by element using the element-type map. Lookups are resolved lazily at map execution against the finished dictionary so profile registration order does not matter.

### 5. New-instance creation
`Map<TDestination>(source)` creates the destination with `Activator.CreateInstance` (or a compiled `new()` where available). `Event`'s `required` members are a compile-time rule only, so runtime creation followed by member copy is valid. A destination type without a parameterless constructor fails at build time with a message naming the type.

### 6. Errors and lifetime
- Unregistered `(source, destination)` at call time → `InvalidOperationException` naming both types.
- Duplicate pair across profiles, invalid `ForMember` lambda (not a simple property access), or missing parameterless constructor → thrown from `AddMapper` during startup, so the app never serves requests with a broken mapper.
- `IMapper` is a singleton; the map dictionary is immutable after build and the delegates hold no per-request state, so it is thread-safe.

*Alternative considered:* scoped lifetime. Rejected — no per-request state, and singleton avoids rebuilding maps.

### 7. Startup wiring
In `Program.cs`, replace `AddAutoMapper(typeof(MappingProfiles).Assembly)` with `AddMapper(typeof(MappingProfiles).Assembly)`. MediatR handler registration is untouched.

### 8. Tests
Add mapper unit tests that exercise the spec scenarios without a database or controller: a small set of test-local profiles and types, plus one test that maps `Event` onto `Event` with the real `MappingProfiles`. `tests/EventsHub.UnitTests` cannot host them because it does not compile today (`EventsControllerTests` constructs `EventsController(AppDbContext)`, CS1729), so the tests go in a new NUnit project, `tests/EventsHub.Application.UnitTests`, added to `EventsHub.slnx`. It follows the existing project's conventions (NUnit 4.3.2, `NUnit.Framework` global using, net10.0) and references only `EventsHub.Application` and `EventsHub.Domain`. Repairing the old fixture is a separate change.

## Risks / Trade-offs

- **Reimplementing solved problems** (AutoMapper covers far more) → Mitigation: the non-goals are explicit and the spec limits the contract to what the project needs; features are added when a profile needs them.
- **Subtle behavioral differences from AutoMapper** (e.g. null handling, case-insensitive matching, collection replacement) → Mitigation: the spec defines the behavior; the `Event` → `Event` edit path has a test that asserts every member is copied.
- **Expression-tree code is harder to debug** → Mitigation: keep the builder small, throw errors that name the type pair and member, and cover each rule with a test.
- **Silent under-mapping** (an unmapped destination member is skipped without an error) → Mitigation: recorded as a deliberate default in the spec; a strict validation mode is a possible follow-up.
- **`Event` → `Event` overwrites `Id` too**: `EditEvent` finds the entity by `Id`, so the assignment is a no-op today. This is unchanged from current behavior and is not addressed here.

## Migration Plan

1. Add the mapper and its tests alongside AutoMapper (both compile; nothing uses the new one yet).
2. Switch `MappingProfiles`, `EditEvent` and `Program.cs` together, then remove the `AutoMapper` package reference.
3. Build the solution and confirm warning NU1903 no longer appears; run the new mapper tests and exercise `PUT /api/v1/Events` (Bruno `Events - Edit - 204`) against a running API.
4. Update the READMEs that mention AutoMapper.

Rollback: revert the commit; no data or API contract changes are involved.

## Open Questions

- Whether to add an opt-in strict validation pass (fail startup when a destination member is unmapped) once there are profiles with different shapes. Safe to defer; the default matches today's behavior.
