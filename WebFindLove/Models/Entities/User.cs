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

        [Required]
        [StringLength(255)]
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

        // ============================
        // Role Relationship
        // ============================
        [ForeignKey(nameof(Role))]
        public Guid? RoleId { get; set; }
        public virtual Role? Role { get; set; }

        // Legacy field for backward compatibility
        [StringLength(50)]
        public string? RoleName { get; set; }

        // ============================
        // Navigation Properties
        // ============================
        public virtual UserPreference? Preference { get; set; }
        public virtual PersonalityTrait? PersonalityTrait { get; set; }
        public virtual ICollection<Photo>? Photos { get; set; }
        public virtual ICollection<MatchResult>? MatchesAsUser { get; set; }
        public virtual ICollection<MatchResult>? MatchesAsMatchedUser { get; set; }
        public virtual ICollection<Message>? SentMessages { get; set; }
        public virtual ICollection<Message>? ReceivedMessages { get; set; }
    }
}
