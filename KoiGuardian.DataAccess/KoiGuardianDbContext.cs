﻿using KoiGuardian.DataAccess.Db;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.DataAccess;

public class KoiGuardianDbContext : IdentityDbContext<User>
{
    public KoiGuardianDbContext(DbContextOptions<KoiGuardianDbContext> options)
        : base(options)
    {
    }

    // DbSet properties for collections
    public virtual DbSet<Package> Packages { get; set; } = null!;
    public virtual DbSet<Shop> Shops { get; set; } = null!;
    public virtual DbSet<Product> Products { get; set; } = null!;
    public virtual DbSet<Blog> Blogs { get; set; } = null!;
    public virtual DbSet<BlogProduct> BlogProducts { get; set; } = null!;
    public virtual DbSet<Fish> Fishes { get; set; } = null!;
    public virtual DbSet<Pond> Ponds { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
       

        // Package Configuration (unchanged)
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

        // Shop Configuration
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.ToTable("Shops");
            entity.HasKey(s => s.ShopId);
            entity.Property(s => s.ShopId).IsRequired().HasMaxLength(50);
            entity.Property(s => s.ShopName).IsRequired().HasMaxLength(100);
            entity.Property(s => s.ShopRate).HasColumnType("decimal(18,2)");
            entity.Property(s => s.ShopDescription).HasMaxLength(500);
            entity.Property(s => s.ShopAddress).HasMaxLength(200);
            entity.Property(s => s.IsActivate).IsRequired().HasDefaultValue(false);
            entity.Property(s => s.BizLicences).HasMaxLength(100);
            entity.HasIndex(s => s.ShopName);

            // Relationship with Products (one-to-many)
            entity.HasMany(s => s.Products)
                .WithOne(p => p.Shop)
                .HasForeignKey(p => p.ShopId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Blogs (one-to-many)
            entity.HasMany(s => s.Blogs)
                .WithOne(b => b.Shop)
                .HasForeignKey(b => b.ShopId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Blog Configuration
        modelBuilder.Entity<Blog>(entity =>
        {
            entity.ToTable("Blogs");
            entity.HasKey(b => b.BlogId);
            entity.Property(b => b.BlogId).IsRequired().HasMaxLength(50);
            entity.Property(b => b.Title).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Content).IsRequired();
            entity.Property(b => b.Images).HasMaxLength(1000);
            entity.Property(b => b.Tag).HasMaxLength(100);
            entity.Property(b => b.ReportedDate).HasColumnType("datetime");
            entity.Property(b => b.View).IsRequired();
            entity.Property(b => b.IsApproved).IsRequired();
            entity.Property(b => b.Type).HasMaxLength(50);
            entity.Property(b => b.ShopId).IsRequired().HasMaxLength(50);

            // Relationship with Shop
            entity.HasOne(b => b.Shop)
                .WithMany(s => s.Blogs)
                .HasForeignKey(b => b.ShopId)
                .OnDelete(DeleteBehavior.Cascade);

            
          
        });


        // Product Configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.ProductId);
            entity.Property(p => p.ProductId).IsRequired().HasMaxLength(50);
            entity.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.Property(p => p.StockQuantity).IsRequired();
            entity.Property(p => p.Category).HasMaxLength(100);
            entity.Property(p => p.Brand).HasMaxLength(100);
            entity.Property(p => p.ManufactureDate).HasColumnType("datetime");
            entity.Property(p => p.ExpiryDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<BlogProduct>(entity =>
        {
            entity.ToTable("BlogProducts");
            entity.HasKey(bp => new { bp.BlogId, bp.ProductId });  // Keeping composite key

            entity.Property(bp => bp.BlogId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(bp => bp.ProductId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(bp => bp.BPId)
                .IsRequired();

            entity.HasOne(bp => bp.Blog)
                .WithMany(b => b.BlogProducts)
                .HasForeignKey(bp => bp.BlogId)
                .OnDelete(DeleteBehavior.Cascade);

            
            entity.HasOne(bp => bp.Product)
                .WithMany(p => p.BlogProducts)
                .HasForeignKey(bp => bp.ProductId)
                .OnDelete(DeleteBehavior.NoAction);  
        });

        // Fish Configuration (unchanged)
        modelBuilder.Entity<Fish>(entity =>
        {
            entity.ToTable("Fish");
            entity.HasKey(f => f.KoiID);
            entity.Property(f => f.KoiID).IsRequired().ValueGeneratedOnAdd();
            entity.Property(f => f.PondID).IsRequired();
            entity.Property(f => f.Name).HasMaxLength(100);
            entity.Property(f => f.Image).HasMaxLength(200);
            entity.Property(f => f.Physique).HasMaxLength(50);
            entity.Property(f => f.Length).HasColumnType("decimal(5,2)");
            entity.Property(f => f.Sex).HasMaxLength(10);
            entity.Property(f => f.Breeder).HasMaxLength(100);
            entity.Property(f => f.Age).IsRequired();
            entity.Property(f => f.Weight).HasColumnType("decimal(10,2)");
            entity.Property(f => f.Variety).HasMaxLength(50);
            entity.Property(f => f.InPondSince).HasColumnType("datetime");
            entity.Property(f => f.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(f => f.Pond)
                .WithMany(p => p.Fish)
                .HasForeignKey(f => f.PondID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Pond Configuration (unchanged)
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