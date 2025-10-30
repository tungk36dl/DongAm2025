using System.ComponentModel.DataAnnotations;

namespace WebFindLove.Models.Services.UserService.Dto
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }

        public string? FullName { get; set; }

        public bool? IsActive { get; set; }

        public string? PasswordHash { get; set; }

        public string? Role { get; set; }
        public string? Avatar { get; set; }

        // ============================
        // Profile Information
        // ============================
        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }  // male | female | other

        public DateTime? DateOfBirth { get; set; }

        public int? Height { get; set; }

        public string? Location { get; set; }

        public string? Hometown { get; set; }

        public string? Bio { get; set; }
        public string? Occupation { get; set; }  // Nghề nghiệp hiện tại

    }
}
