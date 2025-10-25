using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebFindLove.Models.Entity;

namespace WebFindLove.Models
{
    /// <summary>
    /// MatchResult Entity - Kết quả ghép đôi giữa 2 người dùng
    /// </summary>
    public class MatchResult : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey(nameof(MatchedUser))]
        public Guid MatchedUserId { get; set; }

        [Range(0, 100)]
        public double? MatchScore { get; set; }  // Điểm tương thích 0-100

        [StringLength(2000)]
        public string? AiReasoning { get; set; }  // Lý do AI ghép đôi

        public bool IsActive { get; set; } = true;  // Ghép đôi có còn active không

        // ============================
        // Navigation Properties
        // ============================
        public virtual User? User { get; set; }
        
        [ForeignKey(nameof(MatchedUserId))]
        public virtual User? MatchedUser { get; set; }
    }
}

