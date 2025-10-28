using WebFindLove.Models.Services.MessageService.Dto;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.MessageRepo
{
    /// <summary>
    /// Interface cho Message Repository
    /// </summary>
    public interface IMessageRepository : IGenericRepository<Message, Guid>
    {
        /// <summary>
        /// Get conversation between two users
        /// </summary>
        Task<List<Message>> GetConversationAsync(MessageSearch search);

        /// <summary>
        /// Get user's conversations (latest message from each contact)
        /// </summary>
        Task<List<Message>> GetUserConversationsAsync(Guid userId);

        /// <summary>
        /// Get unread messages count for user
        /// </summary>
        Task<int> GetUnreadCountAsync(Guid userId);

        /// <summary>
        /// Mark messages as read
        /// </summary>
        Task MarkAsReadAsync(Guid userId, Guid senderId);
    }
}

