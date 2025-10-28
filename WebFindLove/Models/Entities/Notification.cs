using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebFindLove.Models.Entity;

namespace WebFindLove.Models.Entities
{
    public class Notification : DomainEntity<Guid>
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        // Người gửi thông báo (có thể là admin hoặc user)
        public Guid? SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        // Người nhận thông báo
        [Required]
        public Guid ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public User Receiver { get; set; }

        // Liên kết đến hành động cụ thể (VD: /Message/Detail/5)
        [StringLength(255)]
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        // Kiểu thông báo (tùy chọn)
        [StringLength(50)]
        public string? Type { get; set; } // "Message", "System", "Post", "Comment"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
