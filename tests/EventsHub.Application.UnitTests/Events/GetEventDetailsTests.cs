using EventsHub.Application.Events.Queries;

namespace EventsHub.Application.UnitTests.Events;

[TestFixture]
public class GetEventDetailsTests : HandlerTestBase
{
    [Test]
    public async Task Handle_WhenEventExists_ReturnsMatchingEvent()
    {
        // Arrange
        var existing = await SeedAsync("Detail event");
        var handler = new GetEventDetails.Handler(Context);
        var query = new GetEventDetails.Query { Id = existing.Id };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(existing.Id));
            Assert.That(result.Title, Is.EqualTo("Detail event"));
        });
    }

    [Test]
    public void Handle_WhenEventDoesNotExist_ThrowsActivityNotFound()
    {
        // Arrange
        var handler = new GetEventDetails.Handler(Context);
        var query = new GetEventDetails.Query { Id = Guid.NewGuid().ToString() };

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => handler.Handle(query, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Activity not found"));
    }
}
