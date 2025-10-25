using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebFindLove.Models.Entity;

namespace WebFindLove.Models
{
    /// <summary>
    /// Photo Entity - Ảnh của người dùng
    /// </summary>
    public class Photo : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string? PhotoUrl { get; set; }

        public bool IsPrimary { get; set; } = false;  // Ảnh đại diện chính

        public bool IsActive { get; set; } = true;

        [StringLength(255)]
        public string? Description { get; set; }

        // ============================
        // Navigation Properties
        // ============================
        public virtual User? User { get; set; }
    }
}

