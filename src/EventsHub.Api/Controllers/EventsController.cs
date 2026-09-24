using EventsHub.Application.Events.Commands;
using EventsHub.Application.Events.Queries;
using EventsHub.Domain;
using Microsoft.AspNetCore.Mvc;

namespace EventsHub.Api.Controllers;

public class EventsController : EventsHubBaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Event>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Event>>> GetEventsAsync()
    {
        return await Mediator.Send(new GetEventList.Query());
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Event), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Event>> GetEventDetailAsync(string id)
    {
        return await Mediator.Send(new GetEventDetails.Query { Id = id });
    }

    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<string>> CreateEventAsync(Event @event)
    {
        return await Mediator.Send(new CreateEvent.Command { Event = @event });
    }

    [HttpPut]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EditEventAsync(Event @event)
    {
        await Mediator.Send(new EditEvent.Command { Event = @event });
        return NoContent();
    }
}
