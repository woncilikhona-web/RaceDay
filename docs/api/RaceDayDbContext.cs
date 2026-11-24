using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<RaceResult> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(30);
            e.Property(x => x.Bio).HasMaxLength(500);
            e.Property(x => x.ProfileImageUrl).HasMaxLength(500);
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(300);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Event>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(8, 2);
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.HasOne(x => x.Category).WithMany(c => c.Events).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents).HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Enrolment>(e =>
        {
            e.HasOne(x => x.Event).WithMany(ev => ev.Enrolments).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Enrolments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
        });

        b.Entity<RaceResult>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result).HasForeignKey<RaceResult>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EnrolmentId).IsUnique();
        });
    }
}
