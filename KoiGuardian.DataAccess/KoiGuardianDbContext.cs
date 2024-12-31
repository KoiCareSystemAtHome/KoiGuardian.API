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
    public virtual DbSet<Shop> Shop { get; set; } = null!;
    public virtual DbSet<Fish> Fish { get; set; } = null!;
    public virtual DbSet<Pond> Pond { get; set; } = null!;

    public virtual DbSet<Parameter> Parameters { get; set; } = null!;
    public virtual DbSet<ParameterUnit> PoParameterUnitsnd { get; set; } = null!;
    public virtual DbSet<RelKoiParameter> RelKoiParameters { get; set; } = null!;
    public virtual DbSet<RelPondParameter> RelPondParameters { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(p => p.PackageId);
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
            entity.Property(s => s.ShopId).IsRequired().HasMaxLength(50).ValueGeneratedOnAdd();
            entity.Property(s => s.ShopName).IsRequired().HasMaxLength(100);
            entity.Property(s => s.ShopRate).HasColumnType("decimal(18,2)").HasMaxLength(20);
            entity.Property(s => s.ShopDescription).HasMaxLength(500);
            entity.Property(s => s.ShopAddress).HasMaxLength(200);
            entity.Property(s => s.IsActivate).IsRequired().HasDefaultValue(false);
            entity.Property(s => s.BizLicences).HasMaxLength(100);
            entity.HasIndex(s => s.ShopName);
        });

        
        modelBuilder.Entity<Fish>(entity =>
        {
            entity.ToTable("Fish");
            entity.HasKey(f => f.KoiID);            
            entity.Property(f => f.KoiID).IsRequired().ValueGeneratedOnAdd();
            entity.Property(f => f.PondID).IsRequired();
            entity.Property(f => f.Name).HasMaxLength(100);
            entity.Property(f => f.Image).HasMaxLength(200);
            entity.Property(f => f.Variety).HasMaxLength(50);
            entity.Property(f => f.InPondSince).HasColumnType("datetime");
            entity.Property(f => f.Price).HasColumnType("decimal(18,2)");

           
            entity.HasOne(f => f.Pond)
                .WithMany(p => p.Fish)
                .HasForeignKey(f => f.PondID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        
        modelBuilder.Entity<Pond>(entity =>
        {
            entity.ToTable("Pond");
            entity.HasKey(p => p.PondID);

            
            entity.Property(p => p.PondID).IsRequired().ValueGeneratedOnAdd();
            entity.Property(p => p.OwnerId).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(50);
            entity.Property(p => p.CreateDate).HasColumnType("datetime");

            
            entity.HasIndex(p => p.OwnerId);
        });
    }
}