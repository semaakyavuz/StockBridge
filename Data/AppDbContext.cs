using Microsoft.EntityFrameworkCore;
using StockBridge.API.Models;

namespace StockBridge.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.CreatedAt)
                      .HasDefaultValueSql("(now() at time zone 'utc')");

                entity.Property(p => p.UnitPrice)
                      .HasPrecision(18, 2);
            });

            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.Property(s => s.CreatedAt)
                      .HasDefaultValueSql("(now() at time zone 'utc')");
            });
        }
    }
}