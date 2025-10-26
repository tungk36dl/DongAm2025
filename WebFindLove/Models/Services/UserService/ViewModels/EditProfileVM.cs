using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebFindLove.Models.Services.UserService.ViewModels
{
    /// <summary>
    /// ViewModel for editing user profile information
    /// </summary>
    public class EditProfileVM
    {
        public Guid Id { get; set; }

        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(20, ErrorMessage = "Gender cannot exceed 20 characters")]
        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Range(100, 250, ErrorMessage = "Height must be between 100 and 250 cm")]
        [Display(Name = "Height (cm)")]
        public int? Height { get; set; }

        [StringLength(255, ErrorMessage = "Location cannot exceed 255 characters")]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [StringLength(255, ErrorMessage = "Hometown cannot exceed 255 characters")]
        [Display(Name = "Hometown")]
        public string? Hometown { get; set; }

        [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Biography")]
        public string? Bio { get; set; }

        [Display(Name = "Interests (comma-separated)")]
        public string? Interests { get; set; }

        [StringLength(50, ErrorMessage = "Personality type cannot exceed 50 characters")]
        [Display(Name = "Personality Type")]
        public string? PersonalityType { get; set; }

        [StringLength(1000, ErrorMessage = "Personality description cannot exceed 1000 characters")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Personality Description")]
        public string? PersonalityText { get; set; }

        [Display(Name = "Current Avatar")]
        public string? Avatar { get; set; }

        [Display(Name = "Upload New Avatar")]
        [DataType(DataType.Upload)]
        public IFormFile? AvatarFile { get; set; }
    }
}

