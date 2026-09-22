using EventsHub.Application.Events.Queries;
using EventsHub.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventsHub.Api.Controllers;

public class EventsController(IMediator mediator) : EventsHubBaseController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Event>>> GetEventsAsync()
    {
        return await mediator.Send(new GetEventList.Query());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEventDetailAsync(string id)
    {
        return await mediator.Send(new GetEventDetails.Query { Id = id });
    }
}
