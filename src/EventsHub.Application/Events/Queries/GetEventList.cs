using System.Security.Cryptography;
using EventsHub.Domain;
using EventsHub.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventsHub.Application.Events.Queries;

public class GetEventList
{
    public class Query : IRequest<List<Event>> { }

    public class Handler(AppDbContext context, ILogger<GetEventList> logger) : IRequestHandler<Query, List<Event>>
    {
        public async Task<List<Event>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(0, cancellationToken);
                    logger.LogInformation($"Task {i} has completed!");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("Task was CANCELLED!");
            }

            return await context.Events.ToListAsync(cancellationToken);
        }
    }
}
