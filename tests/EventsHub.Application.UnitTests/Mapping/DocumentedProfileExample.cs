using EventsHub.Application.Core.Mapping;
using EventsHub.Domain;

namespace EventsHub.Application.UnitTests.Mapping;

// This is the example shown in src/EventsHub.Application/README.md ("Adding a mapping profile").
// It lives here as compiled code so the README example is checked by the build and by
// MappingProfilesTests.DocumentedExample_MapsLocationFromCityAndVenue.

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
