using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Models;

namespace TemplateJwtProject.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Songs> Songs { get; set; }
    public DbSet<Top2000Entries> Top2000Entries { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tabelnamen configureren
        builder.Entity<Artist>().ToTable("Artist");
        builder.Entity<Songs>().ToTable("Songs");
        builder.Entity<Top2000Entries>().ToTable("Top2000Entries");

        // RefreshToken configuratie
        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        // Top2000Entries composite key configuratie
        builder.Entity<Top2000Entries>()
            .HasKey(e => new { e.SongId, e.Year });

        // Songs configuratie
        builder.Entity<Songs>()
            .HasOne(s => s.Artist)
            .WithMany(a => a.Songs)
            .HasForeignKey(s => s.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        // Top2000Entries relatie met Songs
        builder.Entity<Top2000Entries>()
            .HasOne(t => t.Song)
            .WithMany(s => s.Top2000Entries)
            .HasForeignKey(t => t.SongId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}