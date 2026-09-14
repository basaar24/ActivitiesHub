using System;
using System.Threading.Tasks;
using EventsHub.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsHub.UnitTests.Controllers;

[TestFixture]
public class EventsControllerTests
{
    private EventsController _eventsController;

    [SetUp]
    public void Setup()
    {
        _eventsController = new EventsController(GlobalTestSetup.AppDbContext);
    }

    [Test]
    public async Task GetEventsAsync_WhenEventsExist_ReturnsAllEvents()
    {
        var expectedCount = await GlobalTestSetup.AppDbContext.Events.CountAsync();

        var result = await _eventsController.GetEventsAsync();

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value, Has.Count.EqualTo(expectedCount));
    }

    [Test]
    public async Task GetEventDetailAsync_WhenEventExists_ReturnsMatchingEvent()
    {
        var existing = await GlobalTestSetup.AppDbContext.Events.FirstAsync();

        var result = await _eventsController.GetEventDetailAsync(existing.Id);

        Assert.That(result.Value, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Id, Is.EqualTo(existing.Id));
            Assert.That(result.Value.Title, Is.EqualTo(existing.Title));
        });
    }

    [Test]
    public async Task GetEventDetailAsync_WhenEventDoesNotExist_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid().ToString();

        var result = await _eventsController.GetEventDetailAsync(nonExistentId);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = (NotFoundObjectResult)result.Result;

        Assert.Multiple(() =>
        {
            Assert.That(notFoundResult.Value, Is.EqualTo("The event was not found"));
            Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
        });
    }
}
