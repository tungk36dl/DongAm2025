using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebFindLove.Models.Entity;
using WebFindLove.Models.Entities;

namespace WebFindLove.Models
{
    /// <summary>
    /// Message Entity - Tin nhắn giữa các người dùng
    /// </summary>
    public class Message : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Sender))]
        public Guid SenderId { get; set; }

        [Required]
        [ForeignKey(nameof(Receiver))]
        public Guid ReceiverId { get; set; }

        public Guid? ConversationId { get; set; }

        [Required]
        [StringLength(5000)]
        public string? Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public bool IsActive { get; set; } = true;

        // ============================
        // Navigation Properties
        // ============================
        public virtual User? Sender { get; set; }
        
        [ForeignKey(nameof(ConversationId))]
        public virtual Conversation? Conversation { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public virtual User? Receiver { get; set; }
    }
}

