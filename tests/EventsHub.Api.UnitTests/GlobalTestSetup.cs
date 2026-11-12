using EventsHub.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventsHub.UnitTests;

[SetUpFixture]
public class GlobalTestSetup
{
    public static AppDbContext AppDbContext { get; private set; }

    [OneTimeSetUp]
    public async Task Setup()
    {
        DbContextOptions<AppDbContext> options = 
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data source=eventshub.db")
                .Options;
        AppDbContext = new AppDbContext(options);
        await AppDbContext.Database.MigrateAsync();
        await DbInitializer.SeedDataAsync(AppDbContext);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await AppDbContext.DisposeAsync();
    }
}
