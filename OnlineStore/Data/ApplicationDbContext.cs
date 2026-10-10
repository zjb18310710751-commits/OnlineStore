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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
            // SQLite 无原生 decimal 类型，转 double(REAL) 存储，保证排序/比较正确
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
            entity.Property(p => p.Sku).HasMaxLength(100);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);

            // 多分类（多对多，自动生成 ProductCategory 联结表）
            entity.HasMany(p => p.Categories)
                  .WithMany(c => c.Products)
                  .UsingEntity<Dictionary<string, object>>(
                      "ProductCategory",
                      j => j.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                      j => j.HasOne<Product>().WithMany().HasForeignKey("ProductId"));

            // 店铺：店铺下仍有商品时禁止删除（Restrict）
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
            entity.HasOne(r => r.Product)
                  .WithMany(p => p.Reviews)
                  .HasForeignKey(r => r.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
