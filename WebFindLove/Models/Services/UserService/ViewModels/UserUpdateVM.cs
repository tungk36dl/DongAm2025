using System.ComponentModel.DataAnnotations;

namespace WebFindLove.Models.Services.UserService.ViewModels
{
    public class UserUpdateVM
    {
        public Guid Id { get; set; }
        
        [Display(Name = "Free Profile Updates Left")]
        public int? FreeProfileUpdatesLeft { get; set; }
        [Required]
        [StringLength(100)]
        public string? UserName { get; set; }
        [Required]
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Role { get; set; }
        public string? Avatar { get; set; }
    }
}
