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

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.ToTable("Shops");
            entity.HasKey(s => s.ShopId);

            entity.Property(s => s.ShopId).IsRequired().HasMaxLength(50);
            entity.Property(s => s.ShopName).IsRequired().HasMaxLength(100);
            entity.Property(s => s.ShopRate).HasColumnType("decimal(18,2)").HasMaxLength(20);
            entity.Property(s => s.ShopDescription).HasMaxLength(500);
            entity.Property(s => s.ShopAddress).HasMaxLength(200);
            entity.Property(s => s.IsActivate).IsRequired().HasDefaultValue(false);
            entity.Property(s => s.BizLicences).HasMaxLength(100);

           

            entity.HasIndex(s => s.ShopName);

        });
    }
}
