using EventsHub.Application.Core.Mapping;
using EventsHub.Domain;

namespace EventsHub.Application.Core;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Event, Event>();
    }
}
