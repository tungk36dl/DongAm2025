using System.ComponentModel.DataAnnotations;
using WebFindLove.Models.Entities;
using WebFindLove.Models.Entity;

namespace WebFindLove.Models
{
    /// <summary>
    /// Role Entity - Đại diện cho vai trò người dùng trong hệ thống
    /// </summary>
    public class Role : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // ============================
        // Navigation Properties
        // ============================
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    }
}

