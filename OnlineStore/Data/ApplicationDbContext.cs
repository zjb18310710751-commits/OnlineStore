using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Models;

namespace OnlineStore.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ProductSku> ProductSkus => Set<ProductSku>();
    public DbSet<ProductSpec> ProductSpecs => Set<ProductSpec>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AfterSalesRequest> AfterSalesRequests => Set<AfterSalesRequest>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();
    public DbSet<DeliveryAddress> DeliveryAddresses => Set<DeliveryAddress>();
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>().Property(p => p.Stock).IsConcurrencyToken();
        builder.Entity<Product>().Property(p => p.Price).IsConcurrencyToken();
        builder.Entity<Product>().Property(p => p.IsActive).IsConcurrencyToken();
        builder.Entity<Product>().Property(p => p.ShippingFeeAmount).HasConversion<double>();
        builder.Entity<ProductSku>().Property(s => s.Stock).IsConcurrencyToken();
        builder.Entity<ProductSku>().Property(s => s.Price).IsConcurrencyToken();
        builder.Entity<ProductSku>().Property(s => s.IsActive).IsConcurrencyToken();
        builder.Entity<ProductSku>().Property(s => s.ProductId).IsConcurrencyToken();
        builder.Entity<Order>().Property(o => o.Status).IsConcurrencyToken();
        builder.Entity<Order>().Property(o => o.InventoryRestoredAt).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.UsedCount).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.IsActive).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.DiscountValue).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.DiscountType).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.StartDate).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.EndDate).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.UsageLimit).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.PerUserLimit).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.MaxDiscountAmount).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.MinOrderAmount).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.Scope).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.FirstOrderOnly).IsConcurrencyToken();
        builder.Entity<Coupon>().Property(c => c.ExcludeDiscountedItems).IsConcurrencyToken();

        builder.Entity<HeroSlide>(entity =>
        {
            entity.HasIndex(s => new { s.IsActive, s.DisplayOrder });
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Address).HasMaxLength(500);
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
        });

        builder.Entity<DeliveryAddress>(entity =>
        {
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => new { a.UserId, a.IsDefault });
            entity.Property(a => a.RecipientName).HasMaxLength(100).IsRequired();
            entity.Property(a => a.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(a => a.AddressLine1).HasMaxLength(200).IsRequired();
            entity.Property(a => a.AddressLine2).HasMaxLength(200);
            entity.Property(a => a.City).HasMaxLength(100).IsRequired();
            entity.Property(a => a.PostalCode).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Country).HasMaxLength(100).IsRequired();
            entity.HasOne(a => a.User)
                  .WithMany(u => u.DeliveryAddresses)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Slug).HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(500);
        });

        builder.Entity<Shop>(entity =>
        {
            entity.HasIndex(s => s.Name);
            entity.Property(s => s.Name).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(500);

            entity.Property(s => s.Rating).HasConversion<double>();
        });

        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.Price);
            entity.Property(p => p.Name).HasMaxLength(150).IsRequired();
            entity.Property(p => p.Slug).HasMaxLength(150);
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.Price).HasConversion<double>();
            entity.Property(p => p.OriginalPrice).HasConversion<double?>();
            entity.Property(p => p.ShippingFeeAmount).HasConversion<double>();
            entity.Property(p => p.Sku).HasMaxLength(100);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.Property(p => p.DetailHtml).HasMaxLength(10000);
            entity.Property(p => p.DetailHtmlEn).HasMaxLength(10000);

            entity.HasMany(p => p.Categories)
                  .WithMany(c => c.Products)
                  .UsingEntity<Dictionary<string, object>>(
                      "ProductCategory",
                      j => j.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                      j => j.HasOne<Product>().WithMany().HasForeignKey("ProductId"));

            entity.HasOne(p => p.Shop)
                  .WithMany(s => s.Products)
                  .HasForeignKey(p => p.ShopId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProductSku>(entity =>
        {
            entity.HasIndex(s => s.ProductId);
            entity.Property(s => s.Price).HasConversion<double>();
            entity.HasOne(s => s.Product)
                  .WithMany(p => p.Skus)
                  .HasForeignKey(s => s.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProductSpec>(entity =>
        {
            entity.HasIndex(s => s.ProductId);
            entity.HasOne(s => s.Product)
                  .WithMany(p => p.Specs)
                  .HasForeignKey(s => s.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Review>(entity =>
        {
            entity.HasIndex(r => r.ProductId);
            entity.Property(r => r.ImageUrl).HasMaxLength(500);
            entity.HasOne(r => r.Product)
                  .WithMany(p => p.Reviews)
                  .HasForeignKey(r => r.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(r => new { r.OrderId, r.ProductId, r.UserId })
                  .IsUnique()
                  .HasFilter("[OrderId] IS NOT NULL");
            entity.HasOne(r => r.Order)
                  .WithMany()
                  .HasForeignKey(r => r.OrderId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.OrderNo).IsUnique();
            entity.Property(o => o.Total).HasConversion<double>();
            entity.Property(o => o.DiscountAmount).HasConversion<double>();
            entity.Property(o => o.ShippingAmount).HasConversion<double>();
            entity.Property(o => o.CouponCode).HasMaxLength(50);
            entity.Property(o => o.CouponName).HasMaxLength(100);
            entity.Property(o => o.PaymentMethod).HasMaxLength(50).IsRequired();
            entity.Property(o => o.OrderNotes).HasMaxLength(500);
            entity.Property(o => o.Carrier).HasMaxLength(50);
            entity.Property(o => o.TrackingNumber).HasMaxLength(50);
            entity.Property(o => o.ShippingOrigin).HasMaxLength(200);
            entity.Property(o => o.ShippingOriginEn).HasMaxLength(200);
            entity.Property(o => o.ShippingFeeText).HasMaxLength(100);
            entity.Property(o => o.ShippingFeeTextEn).HasMaxLength(100);
            entity.Property(o => o.DispatchTimeText).HasMaxLength(100);
            entity.Property(o => o.DispatchTimeTextEn).HasMaxLength(100);
            entity.HasMany(o => o.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(o => o.AfterSalesRequests)
                  .WithOne(a => a.Order)
                  .HasForeignKey(a => a.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Order>()
                  .WithMany()
                  .HasForeignKey(o => o.ReplacementForOrderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AfterSalesRequest>(entity =>
        {
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.OrderId)
                  .IsUnique()
                  .HasFilter("[Status] <> N'已拒绝'");
            entity.Property(a => a.Status).IsConcurrencyToken();
            entity.Property(a => a.Type).HasMaxLength(20);
            entity.Property(a => a.Status).HasMaxLength(20);
            entity.Property(a => a.Reason).HasMaxLength(1000);
            entity.Property(a => a.AdminNote).HasMaxLength(500);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(i => i.OrderId);
            entity.Property(i => i.Price).HasConversion<double>();
        });

        builder.Entity<ProductImage>(entity =>
        {
            entity.HasIndex(i => i.ProductId);
            entity.Property(i => i.ImageUrl).HasMaxLength(500);
            entity.HasOne(i => i.Product)
                  .WithMany(p => p.Images)
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Favorite>(entity =>
        {
            entity.HasIndex(f => new { f.UserId, f.ProductId }).IsUnique();
            entity.HasOne(f => f.Product)
                  .WithMany()
                  .HasForeignKey(f => f.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.Code).HasMaxLength(50).IsRequired();
            entity.Property(c => c.DiscountValue).HasConversion<double>();
            entity.Property(c => c.MaxDiscountAmount).HasConversion<double?>();
            entity.Property(c => c.MinOrderAmount).HasConversion<double?>();
            entity.Property(c => c.CampaignName).HasMaxLength(100);
            entity.HasMany(c => c.Categories)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "CouponCategory",
                      j => j.HasOne<Category>().WithMany().HasForeignKey("CategoryId").OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Coupon>().WithMany().HasForeignKey("CouponId").OnDelete(DeleteBehavior.Cascade));
            entity.HasMany(c => c.Products)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "CouponProduct",
                      j => j.HasOne<Product>().WithMany().HasForeignKey("ProductId").OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Coupon>().WithMany().HasForeignKey("CouponId").OnDelete(DeleteBehavior.Cascade));
        });

        builder.Entity<CouponRedemption>(entity =>
        {
            entity.HasIndex(r => new { r.CouponId, r.UserId, r.ReleasedAt });
            entity.HasIndex(r => r.OrderId).IsUnique();
            entity.HasIndex(r => r.UserId);
            entity.Property(r => r.Code).HasMaxLength(50).IsRequired();
            entity.Property(r => r.DiscountAmount).HasConversion<double>();
            entity.HasOne(r => r.Coupon)
                  .WithMany(c => c.Redemptions)
                  .HasForeignKey(r => r.CouponId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Order)
                  .WithMany()
                  .HasForeignKey(r => r.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EmailVerificationCode>(entity =>
        {
            entity.HasIndex(v => v.Email);
            entity.Property(v => v.Email).HasMaxLength(256).IsRequired();
            entity.Property(v => v.Code).HasMaxLength(6).IsRequired();
        });
    }
}
