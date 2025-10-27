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

        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự")]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        [StringLength(20, ErrorMessage = "Giới tính không được vượt quá 20 ký tự")]
        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Chiều cao là bắt buộc")]
        [Range(100, 250, ErrorMessage = "Chiều cao phải từ 100 đến 250 cm")]
        [Display(Name = "Height (cm)")]
        public int? Height { get; set; }

        [Required(ErrorMessage = "Địa chỉ hiện tại là bắt buộc")]
        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [StringLength(255, ErrorMessage = "Quê quán không được vượt quá 255 ký tự")]
        [Display(Name = "Hometown")]
        public string? Hometown { get; set; }

        [StringLength(1000, ErrorMessage = "Tiểu sử không được vượt quá 1000 ký tự")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Biography")]
        public string? Bio { get; set; }

        [Required(ErrorMessage = "Sở thích là bắt buộc")]
        [Display(Name = "Interests (comma-separated)")]
        public string? Interests { get; set; }

        [Required(ErrorMessage = "Nhóm tính cách là bắt buộc")]
        [StringLength(50, ErrorMessage = "Nhóm tính cách không được vượt quá 50 ký tự")]
        [Display(Name = "Personality Type")]
        public string? PersonalityType { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tính cách không được vượt quá 1000 ký tự")]
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

