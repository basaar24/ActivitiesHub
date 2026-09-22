using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventsHub.Api.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class EventsHubBaseController : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetService<IMediator>()
            ?? throw new InvalidOperationException("IMediator service in unavailabler");
}
