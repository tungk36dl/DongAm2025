using WebFindLove.Models.Entity;

namespace WebFindLove.Models.Entities
{
    public class Conversation : BaseEntity
    {
        public string Type { get; set; } = "private"; // "private" hoặc "group"

        public DateTime? LastMessageAt { get; set; }

        public string? LastMessage { get; set; }

        // Navigation
        public virtual ICollection<ConversationParticipant>? Participants { get; set; }
        public virtual ICollection<Message>? Messages { get; set; }
    }
}
