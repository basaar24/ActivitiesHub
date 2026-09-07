using EventsHub.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventsHub.Persistence;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Event> Events{ get; set; }
}
