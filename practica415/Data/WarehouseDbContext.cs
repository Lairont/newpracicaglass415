using Microsoft.EntityFrameworkCore;
using practica415.Models;

namespace practica415.Data;

public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<WarehouseOrder> Orders => Set<WarehouseOrder>();
    public DbSet<WarehouseUser> Users => Set<WarehouseUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Sku).HasMaxLength(24).IsRequired();
            entity.Property(x => x.PurchaseDate).HasColumnType("timestamp without time zone");
            entity.Property(x => x.WarrantyUntil).HasColumnType("timestamp without time zone");
        });
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.ProductName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Sku).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(120);
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        });
        modelBuilder.Entity<WarehouseOrder>(entity =>
        {
            entity.ToTable("warehouse_orders");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.Customer).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        });
        modelBuilder.Entity<WarehouseUser>(entity =>
        {
            entity.ToTable("warehouse_users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.Login).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.AvatarPath).HasMaxLength(300);
            entity.Property(x => x.FullName).HasMaxLength(120);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.MembershipPlan).HasMaxLength(40);
            entity.Property(x => x.MembershipStart).HasColumnType("date");
            entity.Property(x => x.MembershipEnd).HasColumnType("date");
        });
    }
}

public sealed class WarehouseUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Login { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public string? AvatarPath { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? MembershipPlan { get; set; }
    public DateTime? MembershipStart { get; set; }
    public DateTime? MembershipEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
