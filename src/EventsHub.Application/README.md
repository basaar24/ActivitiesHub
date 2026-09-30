# EventsHub.Application

The use-case layer. Each operation on `Event` is a MediatR request with its
handler, written as one class per use case with nested `Command`/`Query` and
`Handler` types. References `EventsHub.Domain` and `EventsHub.Persistence`;
package: `MediatR` 14.2.0. Object mapping is done by a small in-house mapper
(`Core/Mapping`), not a third-party library.

## Contents

| File | Request | Handler behavior |
|---|---|---|
| `Events/Queries/GetEventList.cs` | `Query` → `List<Event>` | Returns `context.Events.ToListAsync(...)`. Also runs a 10-iteration loop that logs progress and checks the cancellation token (a leftover cancellation-token demo). |
| `Events/Queries/GetEventDetails.cs` | `Query { Id }` → `Event` | `FindAsync(Id)`; throws `Exception("Activity not found")` if missing. |
| `Events/Commands/CreateEvent.cs` | `Command { Event }` → `string` | Adds the event, saves, returns its `Id`. No validation. |
| `Events/Commands/EditEvent.cs` | `Command { Event }` | Finds the event by `Event.Id` (throws `Exception("Event not found")` if missing), copies fields onto it with `IMapper`, saves. |
| `Events/Commands/DeleteEvent.cs` | `Command { Id }` | Finds the event (throws `Exception("Event not found")` if missing), removes it, saves. |
| `Core/MappingProfiles.cs` | — | Mapping profile with one map, `Event` → `Event`, used by `EditEvent`. |
| `Core/Mapping/` | — | The in-house mapper: `IMapper`, `Mapper`, `Profile`, and the `AddMapper` registration extension. |

## How it connects to the rest of the solution

- **Api → Application.** `EventsController` sends these requests through
  `Mediator` (`IMediator`, exposed by `EventsHubBaseController`). In
  `EventsHub.Api/Program.cs`, `AddMediatR` scans this assembly for handlers
  (anchored on `GetEventList.Handler`) and `AddMapper` scans it for `Profile`
  classes (currently `MappingProfiles`) and registers a singleton `IMapper`.
- **Application → Persistence.** Every handler takes `AppDbContext` in its
  constructor and queries it directly. There is no repository or unit-of-work
  abstraction, so this layer is coupled to EF Core.
- **Application → Domain.** Handlers accept and return `Event` itself; there
  are no separate DTOs.

## Adding a use case

1. Add a static-style class under `Events/Commands/` or `Events/Queries/`
   with nested `Command`/`Query` (`IRequest` / `IRequest<T>`) and `Handler`
   (`IRequestHandler<,>`) types, following the existing files.
2. Send it from a controller action with `Mediator.Send(...)`. Handlers are
   discovered automatically; no registration needed.
3. If you change a controller route or response shape, regenerate the OpenAPI
   client (see [`../EventsHub.OpenApi/README.md`](../EventsHub.OpenApi/README.md)).

## Adding a mapping profile

Maps are declared in a `Profile` subclass; `AddMapper` finds every concrete
profile in this assembly at startup, so there is nothing to register.

```csharp
public class EventSummary
{
    public string Title { get; set; } = "";
    public string Location { get; set; } = "";
}

public class EventSummaryProfile : Profile
{
    public EventSummaryProfile()
    {
        CreateMap<Event, EventSummary>()
            .ForMember(d => d.Location, o => o.MapFrom(s => s.City + " - " + s.Venue));
    }
}
```

Then inject `IMapper` into a handler:

```csharp
mapper.Map(request.Event, trackedEvent);        // copy onto an existing object
var summary = mapper.Map<EventSummary>(@event); // create a new object
```

How maps behave:

- Public writable destination properties are filled from same-named public
  readable source properties (ordinal, case-sensitive) with an assignable
  type. Anything else is skipped silently, so check the result of a new map.
- `ForMember` overrides that name: `MapFrom(expression)` computes it,
  `Ignore()` leaves it alone. The selector must be a public writable property.
- When the types differ, a member is mapped through the registered map for
  those types; a collection is mapped element by element into a `List<T>`.
  Same-type members are assigned by reference (a shallow copy).
- Startup fails on a duplicate source/destination pair, an invalid `ForMember`
  selector, or a destination type without a public parameterless constructor.
  Mapping between types with no registered map throws `InvalidOperationException`
  naming both types.
- Not supported: query projection, flattening, automatic reverse maps
  (declare both directions), converters.

The example above is compiled and tested in
`tests/EventsHub.Application.UnitTests/Mapping/DocumentedProfileExample.cs`.

## Known rough edges

- "Not found" is signalled with a plain `Exception`, and no exception-handling
  middleware is registered in `Program.cs`. Missing ids therefore surface as
  unhandled server errors rather than the `404` the controller's
  `ProducesResponseType` attributes declare.
