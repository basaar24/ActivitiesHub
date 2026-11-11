using EventsHub.Application.Core;
using EventsHub.Application.Core.Mapping;
using EventsHub.Application.Events.Commands;
using Microsoft.EntityFrameworkCore;

namespace EventsHub.Application.UnitTests.Events;

[TestFixture]
public class EditEventTests : HandlerTestBase
{
    private IMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        _mapper = new Mapper(new MappingProfiles());
    }

    [Test]
    public async Task Handle_WhenEventExists_UpdatesStoredValues()
    {
        // Arrange
        var existing = await SeedAsync("Original title");
        var edited = NewEvent(existing.Id, "Updated title");
        edited.Venue = "Updated venue";
        edited.IsCancelled = true;
        var handler = new EditEvent.Handler(Context, _mapper);
        var command = new EditEvent.Command { Event = edited };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var stored = await Context.Events.AsNoTracking().FirstAsync(e => e.Id == existing.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.Title, Is.EqualTo("Updated title"));
            Assert.That(stored.Venue, Is.EqualTo("Updated venue"));
            Assert.That(stored.IsCancelled, Is.True);
        });
    }

    [Test]
    public async Task Handle_WhenEventExists_DoesNotCreateANewRow()
    {
        // Arrange
        var existing = await SeedAsync("Original title");
        var countBefore = await Context.Events.CountAsync();
        var handler = new EditEvent.Handler(Context, _mapper);
        var command = new EditEvent.Command { Event = NewEvent(existing.Id, "Updated title") };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(await Context.Events.CountAsync(), Is.EqualTo(countBefore));
    }

    [Test]
    public void Handle_WhenEventDoesNotExist_ThrowsEventNotFound()
    {
        // Arrange
        var handler = new EditEvent.Handler(Context, _mapper);
        var command = new EditEvent.Command { Event = NewEvent(Guid.NewGuid().ToString()) };

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Event not found"));
    }
}
