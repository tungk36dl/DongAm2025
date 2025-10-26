using WebFindLove.Models.Entities;

namespace WebFindLove.Models.Services.ConversationService
{
    public interface IConversationService
    {
        /// <summary>
        /// Tìm hoặc tạo conversation giữa 2 người dùng
        /// </summary>
        Task<DataResponse<Conversation>> GetOrCreatePrivateConversationAsync(Guid userId1, Guid userId2);

        /// <summary>
        /// Lấy tất cả conversation của user
        /// </summary>
        Task<DataResponse<List<Conversation>>> GetUserConversationsAsync(Guid userId);

        /// <summary>
        /// Lấy chi tiết conversation
        /// </summary>
        Task<DataResponse<Conversation>> GetConversationDetailsAsync(Guid conversationId);

        /// <summary>
        /// Kiểm tra user có quyền truy cập conversation không
        /// </summary>
        Task<DataResponse<bool>> CanAccessConversationAsync(Guid conversationId, Guid userId);

        /// <summary>
        /// Đánh dấu conversation đã đọc
        /// </summary>
        Task<DataResponse<bool>> MarkConversationAsReadAsync(Guid conversationId, Guid userId);
    }
}

