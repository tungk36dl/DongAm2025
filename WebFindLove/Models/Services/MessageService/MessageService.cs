using WebFindLove.Models.Repositories.MessageRepo;
using WebFindLove.Models.Repositories.ConversationRepo;
using WebFindLove.Models.UnitOfWork;
using WebFindLove.Models.Services.ConversationService;

namespace WebFindLove.Models.Services.MessageService
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageRepository _repository;
        private readonly IConversationService _conversationService;
        private readonly IConversationRepository _conversationRepository;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            IUnitOfWork unitOfWork,
            IMessageRepository repository,
            IConversationService conversationService,
            IConversationRepository conversationRepository,
            ILogger<MessageService> logger)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _conversationService = conversationService;
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        public async Task<DataResponse<List<Message>>> GetConversationAsync(Guid userId1, Guid userId2)
        {
            try
            {
                _logger.LogInformation("Getting conversation between users: {UserId1} and {UserId2}", userId1, userId2);
                var messages = await _repository.GetConversationAsync(userId1, userId2);

                return new DataResponse<List<Message>> { Success = true, Data = messages, Message = $"Retrieved {messages.Count} message(s)" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation between users: {UserId1} and {UserId2}", userId1, userId2);
                return new DataResponse<List<Message>> { Success = false, Message = "Failed to get conversation", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<Message>>> GetUserConversationsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting conversations for user: {UserId}", userId);
                var messages = await _repository.GetUserConversationsAsync(userId);

                return new DataResponse<List<Message>> { Success = true, Data = messages, Message = $"Retrieved {messages.Count} conversation(s)" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user: {UserId}", userId);
                return new DataResponse<List<Message>> { Success = false, Message = "Failed to get conversations", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<int>> GetUnreadCountAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting unread count for user: {UserId}", userId);
                var count = await _repository.GetUnreadCountAsync(userId);

                return new DataResponse<int> { Success = true, Data = count, Message = $"{count} unread message(s)" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user: {UserId}", userId);
                return new DataResponse<int> { Success = false, Message = "Failed to get unread count", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Message>> SendMessageAsync(Guid senderId, Guid receiverId, string content)
        {
            try
            {
                _logger.LogInformation("Sending message from {SenderId} to {ReceiverId}", senderId, receiverId);

                if (senderId == receiverId)
                {
                    return new DataResponse<Message> { Success = false, Message = "Cannot send message to yourself" };
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new DataResponse<Message> { Success = false, Message = "Message content cannot be empty" };
                }

                // Tìm hoặc tạo conversation
                var conversationResponse = await _conversationService.GetOrCreatePrivateConversationAsync(senderId, receiverId);
                if (!conversationResponse.Success || conversationResponse.Data == null)
                {
                    return new DataResponse<Message> { Success = false, Message = "Failed to create conversation" };
                }

                var conversation = conversationResponse.Data;

                // Tạo message
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    ConversationId = conversation.Id,
                    Content = content,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _repository.Add(message);

                // Cập nhật last message của conversation
                await _conversationRepository.UpdateLastMessageAsync(conversation.Id, content);

                await _unitOfWork.SaveChangesAsync();

                return new DataResponse<Message> { Success = true, Data = message, Message = "Message sent successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from {SenderId} to {ReceiverId}", senderId, receiverId);
                return new DataResponse<Message> { Success = false, Message = "Failed to send message", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<bool>> MarkAsReadAsync(Guid userId, Guid senderId)
        {
            try
            {
                _logger.LogInformation("Marking messages as read for user {UserId} from sender {SenderId}", userId, senderId);
                await _repository.MarkAsReadAsync(userId, senderId);

                return new DataResponse<bool> { Success = true, Data = true, Message = "Messages marked as read" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read for user {UserId} from sender {SenderId}", userId, senderId);
                return new DataResponse<bool> { Success = false, Message = "Failed to mark messages as read", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<bool>> DeleteMessageAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting message: {Id}", id);
                
                var message = await _repository.FindByIdAsync(id);
                if (message == null)
                {
                    return new DataResponse<bool> { Success = false, Message = "Message not found" };
                }

                // Soft delete
                message.IsActive = false;
                message.UpdatedAt = DateTime.UtcNow;
                _repository.Update(message);
                await _unitOfWork.SaveChangesAsync();

                return new DataResponse<bool> { Success = true, Data = true, Message = "Message deleted successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message: {Id}", id);
                return new DataResponse<bool> { Success = false, Message = "Failed to delete message", ErrorDetails = ex.Message };
            }
        }
    }
}

