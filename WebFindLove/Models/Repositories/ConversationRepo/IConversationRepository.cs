using WebFindLove.Models.Entities;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.ConversationRepo
{
    /// <summary>
    /// Interface cho Conversation Repository
    /// </summary>
    public interface IConversationRepository : IGenericRepository<Conversation, Guid>
    {
        /// <summary>
        /// Tìm conversation giữa 2 người dùng (private conversation)
        /// </summary>
        Task<Conversation?> FindPrivateConversationAsync(Guid userId1, Guid userId2);

        /// <summary>
        /// Lấy tất cả conversation của một user
        /// </summary>
        Task<List<Conversation>> GetUserConversationsAsync(Guid userId);

        /// <summary>
        /// Lấy conversation theo ID với đầy đủ thông tin participants và messages
        /// </summary>
        Task<Conversation?> GetConversationWithDetailsAsync(Guid conversationId);

        /// <summary>
        /// Cập nhật thông tin tin nhắn cuối cùng của conversation
        /// </summary>
        Task UpdateLastMessageAsync(Guid conversationId, string lastMessage);
    }
}

