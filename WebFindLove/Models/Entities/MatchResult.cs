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
        public double? MatchScore { get; set; }

        /// <summary>
        /// Dạng ghép đôi:
        ///  - OneWay: người A thấy người B phù hợp (A → B)
        ///  - Mutual: cả hai thấy nhau phù hợp (A ↔ B)
        /// </summary>
        [StringLength(50)]
        public string MatchType { get; set; } = "OneWay"; // Enum-like: "OneWay" | "Mutual"

        /// <summary>
        /// Vector embedding dùng để cache việc tính toán (nếu cần)
        /// </summary>
        public string? EmbeddingSnapshot { get; set; }

        /// <summary>
        /// Lý do AI đề xuất ghép đôi (tóm tắt bằng ngôn ngữ tự nhiên)
        /// </summary>
        [StringLength(2000)]
        public string? AiReasoning { get; set; }

        /// <summary>
        /// Có đang active hay không (để vô hiệu hoá các match cũ)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Ngày tính toán gần nhất
        /// </summary>
        public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;

        // ============================
        // Navigation Properties
        // ============================
        public virtual User? User { get; set; }

        [ForeignKey(nameof(MatchedUserId))]
        public virtual User? MatchedUser { get; set; }
    }
}
