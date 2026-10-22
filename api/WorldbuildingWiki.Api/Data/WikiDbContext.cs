using Microsoft.EntityFrameworkCore;

namespace WorldbuildingWiki.Api.Data;

public sealed class WikiDbContext(DbContextOptions<WikiDbContext> options) : DbContext(options)
{
    public DbSet<FoundationMarker> FoundationMarkers => Set<FoundationMarker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FoundationMarker>(entity =>
        {
            entity.ToTable("foundation_markers");
            entity.HasKey(marker => marker.Id);
            entity.Property(marker => marker.Id).HasColumnName("id");
            entity.Property(marker => marker.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        });
    }
}

public sealed class FoundationMarker
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
