using EventsHub.Application.Core;
using EventsHub.Application.Core.Mapping;
using EventsHub.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EventsHub.Application.UnitTests.Mapping;

[TestFixture]
public class MappingProfilesTests
{
    private static Event NewEvent(string id, string label) => new()
    {
        Id = id,
        Title = $"{label} title",
        Date = new DateTime(2026, 5, 17, 20, 30, 0),
        Description = $"{label} description",
        Category = $"{label} category",
        IsCancelled = label == "source",
        City = $"{label} city",
        Venue = $"{label} venue",
        Latitude = "19.4326",
        Longitude = "-99.1332",
    };

    [Test]
    public void Map_EventOntoEvent_CopiesEveryMemberAndKeepsTheInstance()
    {
        var mapper = new Mapper(new MappingProfiles());
        var source = NewEvent("id-1", "source");
        var destination = NewEvent("id-1", "destination");

        var result = mapper.Map(source, destination);

        Assert.That(result, Is.SameAs(destination));
        var properties = typeof(Event).GetProperties();
        Assert.That(properties, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            foreach (var property in properties)
            {
                Assert.That(property.GetValue(destination), Is.EqualTo(property.GetValue(source)), property.Name);
            }
        });
    }

    [Test]
    public void DocumentedExample_MapsLocationFromCityAndVenue()
    {
        var mapper = new Mapper(new EventSummaryProfile());

        var summary = mapper.Map<EventSummary>(NewEvent("id-3", "source"));

        Assert.Multiple(() =>
        {
            Assert.That(summary.Title, Is.EqualTo("source title"));
            Assert.That(summary.Location, Is.EqualTo("source city - source venue"));
        });
    }

    [Test]
    public void AddMapper_OnApplicationAssembly_RegistersMapperWithEventMap()
    {
        var services = new ServiceCollection();

        services.AddMapper(typeof(MappingProfiles).Assembly);

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        var destination = NewEvent("id-2", "destination");
        mapper.Map(NewEvent("id-2", "source"), destination);
        Assert.That(destination.Title, Is.EqualTo("source title"));
    }
}
