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

    // DbSet properties for collections
    public virtual DbSet<Package> Packages { get; set; } = null!;
    public virtual DbSet<PredictSymptoms> PredictSymptoms { get; set; } = null!;
    public virtual DbSet<RelPredictSymptomDisease> RelPredictSymptomDisease { get; set; } = null!;
    public virtual DbSet<Transaction> Transaction { get; set; } = null!;
    public virtual DbSet<Article> Articles { get; set; } = null!;
    public virtual DbSet<PondStandardParam> PondStandardParam { get; set; } = null!;
    public virtual DbSet<AccountPackage> AccountPackage { get; set; } = null!;
    public virtual DbSet<Report> Report { get; set; } = null!;
    public virtual DbSet<Shop> Shops { get; set; } = null!;
    public virtual DbSet<Product> Products { get; set; } = null!;
    public virtual DbSet<Blog> Blogs { get; set; } = null!;
    public virtual DbSet<BlogProduct> BlogProducts { get; set; } = null!;
    public virtual DbSet<Fish> Fishes { get; set; } = null!;
    public virtual DbSet<NormFoodAmount> NormFoodAmount { get; set; } = null!;
    public virtual DbSet<Pond> Ponds { get; set; } = null!;
    //public virtual DbSet<RelSymptomDisease> RelSymptomDisease { get; set; } = null!;

    public virtual DbSet<KoiStandardParam> KoiStandardParams { get; set; } = null!;
    public virtual DbSet<KoiReport> KoiReport { get; set; } = null!;
    public virtual DbSet<Member> Member { get; set; } = null!;
    public virtual DbSet<Wallet> Wallet { get; set; } = null!;
    public virtual DbSet<Food> Food { get; set; } = null!;
    public virtual DbSet<MedicinePondParameter> MedicinePondParameter { get; set; } = null!;
    public virtual DbSet<RelPondParameter> RelPondParameters { get; set; } = null!;
    public virtual DbSet<Variety> Variety { get; set; } = null!;
    public virtual DbSet<Category> Category { get; set; } = null!;
    public virtual DbSet<Disease> Disease { get; set; } = null!;
    public virtual DbSet<Feedback> Feedbacks { get; set; } = null!;
    public virtual DbSet<KoiDiseaseProfile> KoiDiseaseProfile { get; set; } = null!;
    public virtual DbSet<Medicine> Medicine { get; set; } = null!;
    public virtual DbSet<WalletWithdraw> WalletWithdraw { get; set; } = null!;
    public virtual DbSet<Notification> Notifications { get; set; } = null!;
    public virtual DbSet<Order> Orders { get; set; } = null!;
    public virtual DbSet<OrderDetail> OrderDetails { get; set; } = null!;
    public virtual DbSet<PondReminder> PondReminders { get; set; } = null!;
    public virtual DbSet<RelSymptomDisease> RelSymptomDiseases { get; set; } = null!;
    public virtual DbSet<Symptom> Symptoms { get; set; } = null!;
    public virtual DbSet<MedicineDisease> MedicineDisease { get; set; } = null!;


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
            entity.Property(s => s.ShopId).IsRequired().HasMaxLength(50).ValueGeneratedOnAdd();
            entity.Property(s => s.ShopName).IsRequired().HasMaxLength(100);
            entity.Property(s => s.ShopRate).HasColumnType("decimal(18,2)");
            entity.Property(s => s.ShopDescription).HasMaxLength(500);
            entity.Property(s => s.ShopAddress).HasMaxLength(200);
            entity.Property(s => s.IsActivate).IsRequired().HasDefaultValue(false);
            entity.Property(s => s.BizLicences).HasMaxLength(100);
            entity.HasIndex(s => s.ShopName);

            // Relationship with Products (one-to-many)
            entity.HasMany(s => s.Categories)
                .WithOne(p => p.Shop)
                .HasForeignKey(p => p.ShopId);

            // Relationship with Blogs (one-to-many)
            entity.HasMany(s => s.Blogs)
                .WithOne(b => b.Shop)
                .HasForeignKey(b => b.ShopId);
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
            entity.Property(b => b.IsApproved).IsRequired();
            entity.Property(b => b.Type).HasMaxLength(50);
            entity.Property(b => b.ShopId).IsRequired().HasMaxLength(50);

            // Relationship with Shop
            entity.HasOne(b => b.Shop)
                .WithMany(s => s.Blogs)
                .HasForeignKey(b => b.ShopId);

            
          
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
            entity.Property(p => p.CategoryId).HasMaxLength(100);
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
                .HasForeignKey(bp => bp.BlogId);

            
            entity.HasOne(bp => bp.Product)
                .WithMany(p => p.BlogProducts)
                .HasForeignKey(bp => bp.ProductId);  
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
            entity.Property(f => f.VarietyId).HasMaxLength(50);
            entity.Property(f => f.InPondSince).HasColumnType("datetime");
            entity.Property(f => f.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(f => f.Pond)
                .WithMany(p => p.Fish)
                .HasForeignKey(f => f.PondID);
            entity.HasOne(f => f.Variety)
                .WithMany(p => p.Fish)
                .HasForeignKey(f => f.VarietyId);
            entity.HasMany(f => f.RelKoiParameters)
                .WithOne(p => p.Fish)
                .HasForeignKey(f => f.KoiId);
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

        modelBuilder.Entity<KoiReport>(entity =>
        {
            entity.ToTable("RelKoiParameter");
            entity.HasKey( u => u.KoiReportId);
        });
        modelBuilder.Entity<RelPondParameter>(entity =>
        {
            entity.ToTable("RelPondParameter");
            entity.HasKey( u => u.RelPondParameterId);
            entity.HasOne(p => p.Pond)
                      .WithMany(u => u.RelPondParameter)
                      .HasForeignKey(u => u.PondId);
        });
        modelBuilder.Entity<KoiStandardParam>(entity =>
        {
            entity.ToTable("Parameter");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.ToTable("Wallet");
            entity.HasOne(u => u.User).WithOne(u => u.Wallet).HasForeignKey<Wallet>(u => u.UserId);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasOne(u => u.Order).WithMany(u => u.OrderDetail)
            .HasForeignKey(u => u.OrderId);
        });
    }
}