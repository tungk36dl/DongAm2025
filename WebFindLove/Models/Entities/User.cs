using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebFindLove.Models.Entity;

namespace WebFindLove.Models
{
    /// <summary>
    /// User Entity - Đại diện cho người dùng trong hệ thống
    /// </summary>
    public class User : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string? UserName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        public bool IsActive { get; set; } = true;

        // ============================
        // Profile Information
        // ============================
        public string? PhoneNumber { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }  // male | female | other

        public DateTime? DateOfBirth { get; set; }

        public int? Height { get; set; }

        [StringLength(255)]
        public string? Location { get; set; }

        [StringLength(255)]
        public string? Hometown { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }

        public string? Interests { get; set; }  // JSON array

        [StringLength(500)]
        public string? Avatar { get; set; }  // Path to avatar image

        [StringLength(50)]
        public string? PersonalityType { get; set; }  // MBTI hoặc các loại tính cách khác

        [StringLength(1000)]
        public string? PersonalityText { get; set; }  // Mô tả tính cách tự do

        [StringLength(200)]
        public string? Occupation { get; set; }  // Nghề nghiệp hiện tại

        public string? ProfileText { get; set; }  // Text mô tả profile đầy đủ

        public string? ProfileEmbedding { get; set; }  // Vector embedding của profile (lưu dạng JSON string)

        // ============================
        // Role Relationship
        // ============================
        [ForeignKey(nameof(Role))]
        public Guid? RoleId { get; set; }
        public virtual Role? Role { get; set; }

        // Legacy field for backward compatibility
        [StringLength(50)]
        public string? RoleName { get; set; }

        // Số lần cho phép cập nhật profile miễn phí
        public int? FreeProfileUpdatesLeft { get; set; } = 3;

        // Phương thức đăng nhập nhanh
        public string? Provider { get; set; }
        public string? ProviderKey { get; set; }

        // ============================
        // Navigation Properties
        // ============================
        public virtual UserPreference? Preference { get; set; }
        public virtual ICollection<MatchResult>? MatchesAsUser { get; set; }
        public virtual ICollection<MatchResult>? MatchesAsMatchedUser { get; set; }
        public virtual ICollection<Message>? SentMessages { get; set; }
        public virtual ICollection<Message>? ReceivedMessages { get; set; }
        public virtual ICollection<Entities.Notification>? SentNotifications { get; set; }
        public virtual ICollection<Entities.Notification>? ReceivedNotifications { get; set; }
    }
}
