using KoiGuardian.DataAccess.Db;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KoiGuardian.DataAccess;

public class KoiGuardianDbContext : IdentityDbContext<User>
{
    public KoiGuardianDbContext(DbContextOptions<KoiGuardianDbContext> options) : base(options)
    {

    }
    public DbSet<User> ApplicationUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}