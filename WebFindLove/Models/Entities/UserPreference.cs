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

        [Required(ErrorMessage = "Giới tính mong muốn là bắt buộc")]
        [StringLength(20)]
        public string? PreferredGender { get; set; }  // male | female | all

        [Required(ErrorMessage = "Tuổi tối thiểu là bắt buộc")]
        [Range(18, 100, ErrorMessage = "Tuổi tối thiểu phải từ 18 đến 100")]
        public int? AgeMin { get; set; }

        [Required(ErrorMessage = "Tuổi tối đa là bắt buộc")]
        [Range(18, 100, ErrorMessage = "Tuổi tối đa phải từ 18 đến 100")]
        public int? AgeMax { get; set; }

        public int? MinHeight { get; set; }

        public int? MaxHeight { get; set; }

        [StringLength(255)]
        public string? LocationPreference { get; set; }

        public string? PersonalityPreference { get; set; }  // JSON

        public string? InterestPreference { get; set; }     // JSON

        public string? PreferenceText { get; set; }  // Text mô tả sở thích tìm kiếm đầy đủ

        public string? PreferenceEmbedding { get; set; }  // Vector embedding của preference (lưu dạng JSON string)

        // Số lần cho phép cập nhật sở thích miễn phí
        public int? FreeUpdateCount { get; set; } = 3;

        // ============================
        // Navigation Properties
        // ============================
        public virtual User? User { get; set; }
    }
}

