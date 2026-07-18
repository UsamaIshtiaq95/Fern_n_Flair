using Microsoft.EntityFrameworkCore;
using UserDomain.Entities;

namespace Infrastructure;

public class AppDbContext : DbContext
{
    public DbSet<Users> Users { get; set; } = null!;
    public DbSet<Admins> Admins { get; set; } = null!;
    public DbSet<Rooms> Rooms { get; set; } = null!;
    public DbSet<Contexts> Contexts { get; set; } = null!;
    public DbSet<Chats> Chats { get; set; } = null!;
    public DbSet<ChatMessages> ChatMessages { get; set; } = null!;
    public DbSet<Logs> Logs { get; set; } = null!;
    public DbSet<Images> Images { get; set; } = null!;
    public DbSet<ApiKeys> ApiKeys { get; set; } = null!;
    public DbSet<AIResults> AIResults { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username);
            entity.HasQueryFilter(e => e.IsActive);
        });

        modelBuilder.Entity<Chats>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }).IsDescending(false, true);
            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.ContextId);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.User).WithMany(u => u.Chats).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Room).WithMany(r => r.Chats).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Context).WithMany(c => c.Chats).HasForeignKey(e => e.ContextId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChatMessages>(entity =>
        {
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => new { e.ChatId, e.CreatedAt });
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.Chat).WithMany(c => c.ChatMessages).HasForeignKey(e => e.ChatId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIResults>(entity =>
        {
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasOne(e => e.Chat).WithMany(c => c.AIResults).HasForeignKey(e => e.ChatId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Message).WithMany().HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Rooms>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => e.RoomName);
        });

        modelBuilder.Entity<Images>(entity =>
        {
            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => e.MessageId);
            entity.HasOne(e => e.Room).WithMany(r => r.Images).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Chat).WithMany().HasForeignKey(e => e.ChatId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Message).WithMany().HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Contexts>(entity =>
        {
            entity.HasIndex(e => e.SourceAI);
        });

        modelBuilder.Entity<Logs>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasOne(e => e.User).WithMany(u => u.Logs).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApiKeys>(entity =>
        {
            entity.HasIndex(e => e.ServiceName).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}


