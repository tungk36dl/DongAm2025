using WebFindLove.Models.Entities;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.ConversationParticipantRepo
{
    /// <summary>
    /// Interface cho ConversationParticipant Repository
    /// </summary>
    public interface IConversationParticipantRepository : IGenericRepository<ConversationParticipant, Guid>
    {
        /// <summary>
        /// Lấy tất cả participants của một conversation
        /// </summary>
        Task<List<ConversationParticipant>> GetConversationParticipantsAsync(Guid conversationId);

        /// <summary>
        /// Kiểm tra user có phải là participant của conversation không
        /// </summary>
        Task<bool> IsParticipantAsync(Guid conversationId, Guid userId);

        /// <summary>
        /// Lấy participant record của user trong conversation
        /// </summary>
        Task<ConversationParticipant?> GetParticipantAsync(Guid conversationId, Guid userId);

        /// <summary>
        /// Cập nhật thời gian đọc tin nhắn cuối cùng
        /// </summary>
        Task UpdateLastReadAsync(Guid conversationId, Guid userId);
    }
}

