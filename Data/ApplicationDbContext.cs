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
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();


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

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.Property(x => x.AccessToken).HasMaxLength(64).IsRequired();
            entity.Property(x => x.GuestId).HasMaxLength(64);
            entity.Property(x => x.CustomerName).HasMaxLength(100);
            entity.Property(x => x.CustomerEmail).HasMaxLength(120);
            entity.Property(x => x.CustomerPhone).HasMaxLength(20);
            entity.Property(x => x.AssignedStaffName).HasMaxLength(100);
            entity.Property(x => x.Topic).HasMaxLength(50);
            entity.Property(x => x.AutomationContext).HasMaxLength(2000);
            entity.HasIndex(x => x.AccessToken).IsUnique();
            entity.HasIndex(x => new { x.Status, x.LastMessageAt });
            entity.HasIndex(x => new { x.CustomerId, x.Status });
            entity.HasIndex(x => new { x.GuestId, x.Status });
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.SenderName).HasMaxLength(100);
            entity.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.ConversationId, x.CreatedAt });
            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.Property(x => x.Comment).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.AdminReply).HasMaxLength(1000);
            entity.Property(x => x.HandledByStaffName).HasMaxLength(100);
            entity.Property(x => x.Status).HasDefaultValue(ReviewStatus.Approved);
            entity.Property(x => x.HelpfulCount).HasDefaultValue(0);
            entity.HasIndex(x => x.ProductId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.Rating);
            entity.HasIndex(x => new { x.ProductId, x.UserId, x.OrderId }).IsUnique();
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OrderDetail).WithMany().HasForeignKey(x => x.OrderDetailId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarrantyRequest>(entity =>
        {
            entity.HasIndex(x => x.RequestCode).IsUnique();
            entity.HasIndex(x => x.WarrantyCode);
            entity.HasIndex(x => new { x.Phone, x.CreatedAt });
            entity.HasIndex(x => new { x.Status, x.UpdatedAt });
            entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OrderDetail).WithMany().HasForeignKey(x => x.OrderDetailId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

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
