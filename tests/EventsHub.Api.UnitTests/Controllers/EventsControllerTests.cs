using EventsHub.Api.Controllers;
using EventsHub.Application.Core;
using EventsHub.Application.Core.Mapping;
using EventsHub.Application.Events.Queries;
using EventsHub.Domain;
using EventsHub.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventsHub.Api.UnitTests.Controllers;

[TestFixture]
public class EventsControllerTests
{
    private ServiceProvider _serviceProvider = null!;
    private EventsController _eventsController = null!;

    // Events created by these tests. They are removed in TearDown so the shared seeded rows stay untouched.
    private readonly List<string> _ownedEventIds = [];

    private static AppDbContext Db => GlobalTestSetup.AppDbContext;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddMediatR(opt => opt.RegisterServicesFromAssemblyContaining<GetEventList.Handler>());
        services.AddSingleton(GlobalTestSetup.AppDbContext);
        services.AddSingleton<IMapper>(new Mapper(new MappingProfiles()));
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();

        _eventsController = new EventsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = _serviceProvider }
            }
        };
    }

    [TearDown]
    public async Task TearDown()
    {
        foreach (var id in _ownedEventIds)
        {
            var leftover = await Db.Events.FindAsync(id);
            if (leftover is not null)
            {
                Db.Events.Remove(leftover);
                await Db.SaveChangesAsync();
            }
        }
        _ownedEventIds.Clear();

        await _serviceProvider.DisposeAsync();
    }

    [Test]
    public async Task GetEventsAsync_WhenEventsExists_ReturnsAllEvents()
    {
        // Arrange
        var expectedCount = await Db.Events.CountAsync();

        // Act
        var result = await _eventsController.GetEventsAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value, Has.Count.EqualTo(expectedCount));
    }

    [Test]
    public async Task GetEventDetailAsync_WhenEventExists_ReturnsMatchingEvent()
    {
        // Arrange
        var existing = await Db.Events.FirstAsync();

        // Act
        var result = await _eventsController.GetEventDetailAsync(existing.Id);

        // Assert
        Assert.That(result.Value, Is.Not.Null);
        var detail = result.Value!;
        Assert.Multiple(() =>
        {
            Assert.That(detail.Id, Is.EqualTo(existing.Id));
            Assert.That(detail.Title, Is.EqualTo(existing.Title));
        });
    }

    [Test]
    public void GetEventDetailAsync_WhenEventDoesntExist_ThrowsActivityNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _eventsController.GetEventDetailAsync(nonExistentId));
        Assert.That(ex!.Message, Is.EqualTo("Activity not found"));
    }

    [Test]
    public async Task CreateEventAsync_WhenEventIsValid_ReturnsIdAndPersistsEvent()
    {
        // Arrange
        var newEvent = NewEvent("Controller create test");
        _ownedEventIds.Add(newEvent.Id);

        // Act
        var result = await _eventsController.CreateEventAsync(newEvent);

        // Assert
        Assert.That(result.Value, Is.EqualTo(newEvent.Id));
        var stored = await Db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == newEvent.Id);
        Assert.That(stored, Is.Not.Null);
    }

    [Test]
    public async Task EditEventAsync_WhenEventExists_ReturnsNoContentAndUpdatesTitle()
    {
        // Arrange
        var owned = await AddOwnedEventAsync("Before edit");
        var edited = NewEvent(owned.Id, "After edit");

        // Act
        var result = await _eventsController.EditEventAsync(edited);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        var stored = await Db.Events.AsNoTracking().FirstAsync(e => e.Id == owned.Id);
        Assert.That(stored.Title, Is.EqualTo("After edit"));
    }

    [Test]
    public void EditEventAsync_WhenEventDoesntExist_ThrowsEventNotFound()
    {
        // Arrange
        var edited = NewEvent(Guid.NewGuid().ToString(), "Ghost");

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _eventsController.EditEventAsync(edited));
        Assert.That(ex!.Message, Is.EqualTo("Event not found"));
    }

    [Test]
    public async Task DeleteEventAsync_WhenEventExists_ReturnsOkAndRemovesEvent()
    {
        // Arrange
        var owned = await AddOwnedEventAsync("To delete");

        // Act
        var result = await _eventsController.DeleteEventAsync(owned.Id);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        var remaining = await Db.Events.AsNoTracking().AnyAsync(e => e.Id == owned.Id);
        Assert.That(remaining, Is.False);
    }

    [Test]
    public void DeleteEventAsync_WhenEventDoesntExist_ThrowsEventNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _eventsController.DeleteEventAsync(nonExistentId));
        Assert.That(ex!.Message, Is.EqualTo("Event not found"));
    }

    private async Task<Event> AddOwnedEventAsync(string title)
    {
        var @event = NewEvent(title: title);
        Db.Events.Add(@event);
        await Db.SaveChangesAsync();
        _ownedEventIds.Add(@event.Id);
        return @event;
    }

    private static Event NewEvent(string? id = null, string title = "Test event") => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Title = title,
        Description = "Test description",
        Category = "music",
        City = "Merida",
        Venue = "Test venue",
        Date = DateTime.UtcNow.AddDays(7),
    };
}
