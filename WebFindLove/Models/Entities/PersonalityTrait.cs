using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebFindLove.Models.Entity;

namespace WebFindLove.Models
{
    /// <summary>
    /// PersonalityTrait Entity - Đặc điểm tính cách người dùng
    /// </summary>
    public class PersonalityTrait : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [StringLength(10)]
        public string? MbtiType { get; set; }  // INTJ, ENFP, etc.

        public string? TraitsJson { get; set; }  // JSON: {openness, agreeableness, extraversion, etc.}

        [StringLength(2000)]
        public string? AiSummary { get; set; }  // AI-generated personality summary

        public string? CompatibilityWeight { get; set; }  // JSON: Trọng số các tiêu chí tương thích

        // ============================
        // Navigation Properties
        // ============================
        public virtual User? User { get; set; }
    }
}

