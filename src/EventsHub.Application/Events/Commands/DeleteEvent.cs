using EventsHub.Persistence;
using MediatR;

namespace EventsHub.Application.Events.Commands;

public class DeleteEvent
{
    public class Command : IRequest
    {
        public required string Id { get; set; }
    }

    public class Handler(AppDbContext context) : IRequestHandler<Command>
    {
        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var @event = await context.Events
                .FindAsync([request.Id], cancellationToken)
                ?? throw new Exception("Event not found");

            context.Remove(@event);

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
