# Tasks

## 1. Test project

- [x] 1.1 Create `tests/EventsHub.Application.UnitTests` (NUnit 4.3.2, `Microsoft.NET.Test.Sdk`, `NUnit3TestAdapter`, `NUnit.Analyzers`, net10.0, `NUnit.Framework` global using, references `EventsHub.Application` and `EventsHub.Domain` only), add it to `EventsHub.slnx` under `/tests/`, and verify `dotnet build tests/EventsHub.Application.UnitTests` succeeds
- [x] 1.2 Add a placeholder passing test and verify `dotnet test tests/EventsHub.Application.UnitTests` discovers and runs it (replace it in group 2)

## 2. Mapper core (alongside AutoMapper)

- [x] 2.1 Add `IMapper`, `Profile`, `IMappingExpression<,>` and the `ForMember`/`MapFrom`/`Ignore` option types under `src/EventsHub.Application/Core/Mapping/`, namespace `EventsHub.Application.Core.Mapping`, and verify `dotnet build src/EventsHub.Application` succeeds with no name clash against AutoMapper's types
- [x] 2.2 Implement `Mapper` with the profile-to-map builder: same-name assignable members, `Ignore` and `MapFrom` overrides taking precedence, compiled per-map delegates, and map-onto-existing-destination via `Map<TSource, TDestination>`; verify with tests for the spec scenarios "Edit copies fields onto a tracked entity", "Unmapped destination members are preserved", "Same-name members are copied automatically", "Members without a source or a setter are skipped", "Computed member" and "Ignored member"
- [x] 2.3 Add map-into-new-instance (`Map<TDestination>(source)`) with null-source rejection and a build-time error for a destination without a parameterless constructor; verify with tests for "New instance from source" and "Null source"
- [x] 2.4 Add nested-member and collection-member mapping resolved lazily against the finished map dictionary (so profile order does not matter); verify with tests for "Nested object with its own map" and "Collection of mapped elements", including a case where the nested profile is registered after the outer one
- [x] 2.5 Add the failure modes: `InvalidOperationException` naming both types for an unregistered map at call time, and startup errors for duplicate `(source, destination)` pairs and for a `ForMember` lambda that is not a simple property access; verify with tests for "Missing map" and "Duplicate map"
- [x] 2.6 Add `AddMapper(Assembly)` in `MappingServiceCollectionExtensions` (scan concrete `Profile` subclasses, build once, register `IMapper` as singleton); verify with a test that a profile defined in a test assembly is resolvable and usable from a `ServiceProvider` ("New profile is picked up")

## 3. Cut over and remove AutoMapper

- [x] 3.1 Change `Core/MappingProfiles.cs` to derive from the new `Profile` (keeping `CreateMap<Event, Event>()`) and switch `EditEvent.cs` to the new `IMapper` namespace; verify `dotnet build src/EventsHub.Application` succeeds
- [x] 3.2 Add a test that maps an `Event` onto another `Event` through the real `MappingProfiles` and asserts every member is copied and the destination instance is unchanged, then verify it passes ("Startup with the existing profile")
- [x] 3.3 Replace `AddAutoMapper(typeof(MappingProfiles).Assembly)` with `AddMapper(typeof(MappingProfiles).Assembly)` in `src/EventsHub.Api/Program.cs`, remove the `AutoMapper` `PackageReference` from `EventsHub.Application.csproj`, and verify `dotnet build src/EventsHub.Api` succeeds and its output no longer contains warning NU1903 for AutoMapper
- [x] 3.4 Verify the edit flow end to end: run the API, call `PUT /api/v1/Events` with changed fields for an existing seeded event (Bruno `Events - Edit - 204`, or an equivalent HTTP request), and confirm `GET /api/v1/Events/{id}` returns the new values ("Edit persists mapped fields")

## 4. Documentation

- [x] 4.1 Update the README text that names AutoMapper — `src/EventsHub.Api/README.md` (wiring list), `src/EventsHub.Application/README.md` (packages, `EditEvent` row, `MappingProfiles` row, registration note, and the NU1903 known-issue bullet, now resolved), `src/EventsHub.Domain/README.md`, `src/EventsHub.OpenApi/README.md` — and add a short "Adding a mapping profile" section to the Application README showing a `Profile` with `CreateMap` and `ForMember`; verify with `Grep` that no README mentions `AutoMapper` except in historical or comparison context, and that the documented profile example compiles when pasted into a test profile
- [x] 4.2 Add `dotnet test tests/EventsHub.Application.UnitTests` to the test commands in `CLAUDE.md` and note that `tests/EventsHub.UnitTests` currently does not compile; verify the command runs as written

## 5. Final integration check

- [x] 5.1 Run `dotnet test tests/EventsHub.Application.UnitTests` and build `src/EventsHub.Api`, `src/EventsHub.OpenApi` and `src/EventsHub.Application`; verify all tests pass and none reference `AutoMapper` (`Grep` for `AutoMapper` in `**/*.cs` and `**/*.csproj` returns no matches)
