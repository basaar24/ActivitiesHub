using EventsHub.Domain;
using EventsHub.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventsHub.Application.Events.Queries;

public class GetEventList
{
    public class Query : IRequest<List<Event>> {}

    public class Handler(AppDbContext context) : IRequestHandler<Query, List<Event>>
    {
        public async Task<List<Event>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await context.Events.ToListAsync(cancellationToken);
        }
    }
}
