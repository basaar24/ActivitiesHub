using EventsHub.Application.Events.Commands;
using Microsoft.EntityFrameworkCore;

namespace EventsHub.Application.UnitTests.Events;

[TestFixture]
public class DeleteEventTests : HandlerTestBase
{
    [Test]
    public async Task Handle_WhenEventExists_RemovesItFromDatabase()
    {
        // Arrange
        var existing = await SeedAsync("To delete");
        var handler = new DeleteEvent.Handler(Context);
        var command = new DeleteEvent.Command { Id = existing.Id };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var remaining = await Context.Events.AsNoTracking().AnyAsync(e => e.Id == existing.Id);
        Assert.That(remaining, Is.False);
    }

    [Test]
    public void Handle_WhenEventDoesNotExist_ThrowsEventNotFound()
    {
        // Arrange
        var handler = new DeleteEvent.Handler(Context);
        var command = new DeleteEvent.Command { Id = Guid.NewGuid().ToString() };

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Event not found"));
    }
}
