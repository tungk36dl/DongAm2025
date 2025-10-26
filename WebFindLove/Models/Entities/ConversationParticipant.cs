using WebFindLove.Models.Entity;

namespace WebFindLove.Models.Entities
{
    public class ConversationParticipant : BaseEntity
    {
        public Guid ConversationId { get; set; }
        public Guid UserId { get; set; }

        public bool IsMuted { get; set; } = false;
        public DateTime? LastReadAt { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Conversation? Conversation { get; set; }
        public virtual User? User { get; set; }
    }
}
