using System.ComponentModel.DataAnnotations;

namespace WebFindLove.Models.Services.PhotoService.ViewModels
{
    /// <summary>
    /// ViewModel cho Create Photo
    /// </summary>
    public class PhotoCreateVM
    {
        [Required(ErrorMessage = "User is required")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Photo URL is required")]
        [StringLength(500, ErrorMessage = "Photo URL cannot exceed 500 characters")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? PhotoUrl { get; set; }

        [Display(Name = "Set as Primary Photo")]
        public bool IsPrimary { get; set; } = false;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }
    }
}

