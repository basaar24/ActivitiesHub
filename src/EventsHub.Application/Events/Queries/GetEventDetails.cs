using EventsHub.Domain;
using EventsHub.Persistence;
using MediatR;

namespace EventsHub.Application.Events.Queries;

public class GetEventDetails
{
    public class Query : IRequest<Event>
    {
        public required string Id { get; set; }
    }

    public class Handler(AppDbContext context) : IRequestHandler<Query, Event>
    {
        public async Task<Event> Handle(Query request, CancellationToken cancellationToken)
        {
            var result = await context.Events.FindAsync([request.Id], cancellationToken)
                            ?? throw new Exception("Activity not found");
            return result;
        }
    }
}
