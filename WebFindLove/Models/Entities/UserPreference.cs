using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebFindLove.Models.Entity;

namespace WebFindLove.Models
{
    /// <summary>
    /// UserPreference Entity - Sở thích tìm kiếm đối tượng của người dùng
    /// </summary>
    public class UserPreference : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [StringLength(20)]
        public string? PreferredGender { get; set; }  // male | female | all

        public int? AgeMin { get; set; }

        public int? AgeMax { get; set; }

        public int? MinHeight { get; set; }

        public int? MaxHeight { get; set; }

        [StringLength(255)]
        public string? LocationPreference { get; set; }

        public string? PersonalityPreference { get; set; }  // JSON

        public string? InterestPreference { get; set; }     // JSON

        public string? PreferenceText { get; set; }  // Text mô tả sở thích tìm kiếm đầy đủ

        public string? PreferenceEmbedding { get; set; }  // Vector embedding của preference (lưu dạng JSON string)

        // ============================
        // Navigation Properties
        // ============================
        public virtual User? User { get; set; }
    }
}

