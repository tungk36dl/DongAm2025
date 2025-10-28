using WebFindLove.Models.Services.MessageService.Dto;

namespace WebFindLove.Models.Services.MessageService
{
    public interface IMessageService
    {
        Task<DataResponse<List<Message>>> GetConversationAsync(MessageSearch search);
        Task<DataResponse<List<Message>>> GetUserConversationsAsync(Guid userId);
        Task<DataResponse<int>> GetUnreadCountAsync(Guid userId);
        Task<DataResponse<Message>> SendMessageAsync(Guid senderId, Guid receiverId, string content);
        Task<DataResponse<bool>> MarkAsReadAsync(Guid userId, Guid senderId);
        Task<DataResponse<bool>> DeleteMessageAsync(Guid id);
    }
}

