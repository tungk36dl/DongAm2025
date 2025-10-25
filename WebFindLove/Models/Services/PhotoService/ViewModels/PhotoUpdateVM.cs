using System.ComponentModel.DataAnnotations;

namespace WebFindLove.Models.Services.PhotoService.ViewModels
{
    /// <summary>
    /// ViewModel cho Update Photo
    /// </summary>
    public class PhotoUpdateVM
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Photo URL is required")]
        [StringLength(500, ErrorMessage = "Photo URL cannot exceed 500 characters")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? PhotoUrl { get; set; }

        [Display(Name = "Set as Primary Photo")]
        public bool IsPrimary { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }
    }
}

