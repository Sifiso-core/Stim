using System;
using Microsoft.EntityFrameworkCore;
using Stim.Api.Entities;

namespace Stim.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Application);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
    public DbSet<Game> Games { get; set; }
    public DbSet<Developer> Developers { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<GameTag> GameTags { get; set; }
    public DbSet<Genre> Genres { get; set; }
}
