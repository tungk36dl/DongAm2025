using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebFindLove.Models.Entity;

namespace WebFindLove.Models.Entities
{
    public class Permission : DomainEntity<Guid>
    {


        [Required]
        [StringLength(100)]
        public string Module { get; set; } = string.Empty;  // Ví dụ: "User"

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;  // Ví dụ: "Create"

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;    // Ví dụ: "User.Create"

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}
