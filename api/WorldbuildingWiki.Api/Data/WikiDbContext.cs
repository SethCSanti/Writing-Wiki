using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WorldbuildingWiki.Api.Security;
using Microsoft.AspNetCore.DataProtection;

namespace WorldbuildingWiki.Api.Data;

public sealed class WikiDbContext(DbContextOptions<WikiDbContext> options, IDataProtectionProvider dataProtection) : IdentityDbContext<WikiUser>(options)
{
    private readonly IDataProtector _tokenProtector = dataProtection.CreateProtector("WorldbuildingWiki.IdentityToken.v1");
    public DbSet<FoundationMarker> FoundationMarkers => Set<FoundationMarker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<IdentityUserToken<string>>()
            .Property(token => token.Value)
            .HasConversion(value => value == null ? null : _tokenProtector.Protect(value),
                protectedValue => protectedValue == null ? null : _tokenProtector.Unprotect(protectedValue));
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
