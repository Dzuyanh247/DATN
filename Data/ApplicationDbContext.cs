using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<WarrantyRequest> WarrantyRequests => Set<WarrantyRequest>();
    public DbSet<BuildPcConfig> BuildPcConfigs => Set<BuildPcConfig>();
    public DbSet<BuildPcItem> BuildPcItems => Set<BuildPcItem>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<ShippingConfig> ShippingConfigs => Set<ShippingConfig>();
    public DbSet<ShopLocation> ShopLocations => Set<ShopLocation>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();


    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = utcNow;
                }

                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(BaseEntity.CreatedAt))
                    .HasDefaultValueSql("GETUTCDATE()");

                modelBuilder.Entity(entityType.ClrType).Property(nameof(BaseEntity.UpdatedAt))
                    .HasDefaultValueSql("GETUTCDATE()");
            }
        }


        modelBuilder.Entity<PasswordResetOtp>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(x => x.User)
                .WithMany(x => x.PasswordResetOtps)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UserId, x.IsUsed, x.ExpiresAt });
            entity.HasIndex(x => new { x.Email, x.CodeHash });
        });

        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.ProductCode).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.SourceUrl);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasMany(p => p.ProductImages)
            .WithOne(pi => pi.Product)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductImage>()
            .HasIndex(x => new { x.ProductId, x.SortOrder });

        modelBuilder.Entity<Product>()
            .Property(x => x.Price)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Product>()
            .Property(x => x.SalePrice)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Product>()
            .Property(x => x.DiscountPrice)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(x => x.SubtotalAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(x => x.DiscountAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(x => x.TotalAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(x => x.ShippingFee)
            .HasPrecision(18, 2);
        modelBuilder.Entity<ShippingConfig>()
            .Property(x => x.BaseDistanceKm)
            .HasPrecision(8, 2);
        modelBuilder.Entity<ShippingConfig>()
            .Property(x => x.MaxDistanceKm)
            .HasPrecision(8, 2);
        modelBuilder.Entity<ShippingConfig>()
            .Property(x => x.FreeShippingDistanceKm)
            .HasPrecision(8, 2);
        modelBuilder.Entity<ShippingConfig>()
            .Property(x => x.BaseFee)
            .HasPrecision(18, 2);
        modelBuilder.Entity<ShippingConfig>()
            .Property(x => x.ExtraFeePerKm)
            .HasPrecision(18, 2);
        modelBuilder.Entity<OrderDetail>()
            .Property(x => x.UnitPrice)
            .HasPrecision(18, 2);
        modelBuilder.Entity<OrderDetail>()
            .Property(x => x.TotalPrice)
            .HasPrecision(18, 2);
        modelBuilder.Entity<BuildPcConfig>()
            .Property(x => x.TotalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.Property(e => e.DealSectionBackgroundUrl)
                .HasMaxLength(1000);

            entity.Property(e => e.HotPromotionBackgroundUrl)
                .HasMaxLength(1000);
        });
    }
}

