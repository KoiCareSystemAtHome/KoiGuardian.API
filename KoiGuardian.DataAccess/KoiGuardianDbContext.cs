using KoiGuardian.DataAccess.Db;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.DataAccess;

public class KoiGuardianDbContext : IdentityDbContext<User>
{
    public KoiGuardianDbContext(DbContextOptions<KoiGuardianDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> User { get; set; } = null!;
    public virtual DbSet<Package> Package { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the Package entity
        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(p => p.PackageId); // Primary Key
            entity.Property(p => p.PackageId).IsRequired().HasMaxLength(50);

            entity.Property(p => p.PackageTitle).IsRequired().HasMaxLength(200);
            entity.Property(p => p.PackageDescription).HasMaxLength(1000);
            entity.Property(p => p.PackagePrice).HasColumnType("decimal(18,2)");

            entity.Property(p => p.Type).IsRequired().HasMaxLength(50);
            entity.Property(p => p.StartDate).HasColumnType("datetime");
            entity.Property(p => p.EndDate).HasColumnType("datetime");
        });
    }
}
