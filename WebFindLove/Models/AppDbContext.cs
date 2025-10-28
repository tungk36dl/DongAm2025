using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.Entities;

namespace WebFindLove.Models
{
    /// <summary>
    /// AppDbContext - Database context cho WebFindLove application
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ============================
        // DbSets
        // ============================
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<MatchResult> MatchResults { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================
            // User Configuration
            // ============================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();

                // User -> Role relationship
                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.SetNull);

                // User -> UserPreference (1:1)
                entity.HasOne(e => e.Preference)
                    .WithOne(p => p.User)
                    .HasForeignKey<UserPreference>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> MatchResults as User (1:many)
                entity.HasMany(e => e.MatchesAsUser)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // User -> MatchResults as MatchedUser (1:many)
                entity.HasMany(e => e.MatchesAsMatchedUser)
                    .WithOne(m => m.MatchedUser)
                    .HasForeignKey(m => m.MatchedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // User -> Messages as Sender (1:many)
                entity.HasMany(e => e.SentMessages)
                    .WithOne(m => m.Sender)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // User -> Messages as Receiver (1:many)
                entity.HasMany(e => e.ReceivedMessages)
                    .WithOne(m => m.Receiver)
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                // User -> Notifications as Sender (1:many)
                entity.HasMany(e => e.SentNotifications)
                    .WithOne(n => n.Sender)
                    .HasForeignKey(n => n.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // User -> Notifications as Receiver (1:many)
                entity.HasMany(e => e.ReceivedNotifications)
                    .WithOne(n => n.Receiver)
                    .HasForeignKey(n => n.ReceiverId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================
            // Role Configuration
            // ============================
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // ============================
            // Permission Configuration
            // ============================
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Module).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Name).IsUnique();

                // Optional: bạn có thể thêm mô tả mặc định
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            // ============================
            // RolePermission Configuration (Many-to-Many)
            // ============================
            modelBuilder.Entity<RolePermission>(entity =>
            {
                // Composite Primary Key
                entity.HasKey(e => new { e.RoleId, e.PermissionId });

                // Role relationship
                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Permission relationship
                entity.HasOne(rp => rp.Permission)
                    .WithMany()
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Tạo index để tìm kiếm nhanh
                entity.HasIndex(e => e.PermissionId);
            });

            // ============================
            // UserPreference Configuration
            // ============================
            modelBuilder.Entity<UserPreference>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
            });

            // ============================
            // MatchResult Configuration
            // ============================
            modelBuilder.Entity<MatchResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.MatchedUserId });
                
                // Prevent self-matching
                entity.ToTable(t => t.HasCheckConstraint("CK_MatchResult_NoSelfMatch", "[UserId] <> [MatchedUserId]"));
            });

            // ============================
            // Message Configuration
            // ============================
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SenderId);
                entity.HasIndex(e => e.ReceiverId);
                entity.HasIndex(e => e.SentAt);
                
                // Prevent self-messaging
                entity.ToTable(t => t.HasCheckConstraint("CK_Message_NoSelfMessage", "[SenderId] <> [ReceiverId]"));
            });

            // ============================
            // Conversation Configuration
            // ============================
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.LastMessageAt);
                entity.HasIndex(e => e.Type);

                // Conversation -> Messages (1:many)
                entity.HasMany(e => e.Messages)
                    .WithOne(m => m.Conversation)
                    .HasForeignKey("ConversationId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================
            // ConversationParticipant Configuration
            // ============================
            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ConversationId, e.UserId }).IsUnique();
                entity.HasIndex(e => e.UserId);

                // ConversationParticipant -> Conversation (many:1)
                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Participants)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ConversationParticipant -> User (many:1)
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================
            // Notification Configuration
            // ============================
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ReceiverId);
                entity.HasIndex(e => e.SenderId);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ============================
            // Seed Data (Optional)
            // ============================
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default roles
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Fixed date for seeding

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    Description = "Administrator role with full permissions",
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new Role
                {
                    Id = userRoleId,
                    Name = "User",
                    Description = "Regular user role",
                    IsActive = true,
                    CreatedAt = seedDate
                }
            );
            // Seed default permissions
            var permissions = new List<Permission>
{
            new Permission
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Module = "User",
                Action = "View",
                Name = "User.View",
                Description = "Xem danh sách người dùng"
            },
            new Permission
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Module = "User",
                Action = "Create",
                Name = "User.Create",
                Description = "Tạo người dùng mới"
            },
            new Permission
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Module = "User",
                Action = "Edit",
                Name = "User.Edit",
                Description = "Chỉnh sửa người dùng"
            },
            new Permission
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                Module = "User",
                Action = "Delete",
                Name = "User.Delete",
                Description = "Xóa người dùng"
            }
        };
            modelBuilder.Entity<Permission>().HasData(permissions);

        }
    }
}
