using BookShoppingCart.Models.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCart.Data.Data;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<CartDetail> CartDetails => Set<CartDetail>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<Stock> Stocks => Set<Stock>();

    public DbSet<ApiSession> ApiSessions => Set<ApiSession>();
    public DbSet<ApiRefreshToken> ApiRefreshTokens => Set<ApiRefreshToken>();

    public DbSet<CommerceAuditEvent> CommerceAuditEvents => Set<CommerceAuditEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<CommerceAuditEvent>().Property(e => e.Kind).HasMaxLength(30);
        model.Entity<CommerceAuditEvent>().Property(e => e.ActorId).HasMaxLength(450);
        model.Entity<CommerceAuditEvent>().Property(e => e.RequestHash).HasMaxLength(64).IsUnicode(false);
        model.Entity<CommerceAuditEvent>().Property(e => e.Before).HasMaxLength(64);
        model.Entity<CommerceAuditEvent>().Property(e => e.After).HasMaxLength(64);
        model.Entity<CommerceAuditEvent>().Property(e => e.Reason).HasMaxLength(300);
        model.Entity<CommerceAuditEvent>().HasIndex(e => new { e.Kind, e.EntityId, e.CreatedAtUtc });
        model.Entity<ApiSession>().Property(s => s.UserId).HasMaxLength(450);
        model.Entity<ApiSession>().Property(s => s.SecurityStampHash).HasMaxLength(64).IsUnicode(false);
        model.Entity<ApiSession>().HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<ApiSession>().HasIndex(s => new { s.UserId, s.ExpiresAtUtc });
        model.Entity<ApiSession>().HasIndex(s => s.ExpiresAtUtc);
        model.Entity<ApiRefreshToken>().Property(t => t.TokenHash).HasMaxLength(64).IsUnicode(false);
        model.Entity<ApiRefreshToken>().HasIndex(t => t.TokenHash).IsUnique();
        model.Entity<ApiRefreshToken>().HasOne(t => t.Session).WithMany().HasForeignKey(t => t.SessionId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<IdentityUser>().HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("EmailIndex").IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
        model.Entity<Genre>().Property(g => g.GenreName).UseCollation("SQL_Latin1_General_CP1_CI_AS");
        model.Entity<Genre>().HasIndex(g => g.GenreName).IsUnique();
        model.Entity<ShoppingCart>().Property(c => c.UserId).HasMaxLength(450).IsRequired();
        model.Entity<ShoppingCart>().HasIndex(c => c.UserId).IsUnique();
        model.Entity<CartDetail>().HasIndex(c => new { c.ShoppingCartId, c.BookId }).IsUnique();
        model.Entity<Book>().Property(b => b.IsArchived).IsConcurrencyToken();
        model.Entity<Book>().Property(b => b.Price).HasPrecision(18, 2);
        model.Entity<CartDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);
        model.Entity<OrderDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);
        model.Entity<Order>().Property(o => o.UserId).HasMaxLength(450);
        model.Entity<Order>().HasIndex(o => new { o.UserId, o.CreateDate });
        model.Entity<Order>().HasIndex(o => new { o.UserId, o.CheckoutKey }).IsUnique()
            .HasFilter("[CheckoutKey] IS NOT NULL");
        foreach (var name in new[] { "Subtotal", "Discount", "Shipping", "Total" })
            model.Entity<Order>().Property<decimal>(name).HasPrecision(18, 2);
        model.Entity<OrderDetail>().HasOne(d => d.Book).WithMany(b => b.OrderDetail)
            .HasForeignKey(d => d.BookId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Book>().HasOne(b => b.Genre).WithMany(g => g.Books)
            .HasForeignKey(b => b.GenreId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Stock>().ToTable("Stock", t => t.HasCheckConstraint("CK_Stock_Quantity", "[Quantity] >= 0"));
        model.Entity<CartDetail>().ToTable("CartDetail", t => t.HasCheckConstraint("CK_CartDetail_Quantity", "[Quantity] BETWEEN 1 AND 1000"));
        model.Entity<OrderDetail>().ToTable("OrderDetail", t => t.HasCheckConstraint("CK_OrderDetail_Quantity", "[Quantity] BETWEEN 1 AND 1000"));
        model.Entity<Book>().ToTable("Book", t => t.HasCheckConstraint("CK_Book_Price", "[Price] > 0"));
    }
}
