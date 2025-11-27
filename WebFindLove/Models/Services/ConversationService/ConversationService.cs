using WebFindLove.Models.Entities;
using WebFindLove.Models.Repositories.ConversationRepo;
using WebFindLove.Models.Repositories.ConversationParticipantRepo;
using WebFindLove.Models.UnitOfWork;
using WebFindLove.Helper.HelperServices;

namespace WebFindLove.Models.Services.ConversationService
{
    public class ConversationService : IConversationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConversationRepository _conversationRepository;
        private readonly IConversationParticipantRepository _participantRepository;
        private readonly IUrlHelperService _urlHelperService;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            IUnitOfWork unitOfWork,
            IConversationRepository conversationRepository,
            IConversationParticipantRepository participantRepository,
            IUrlHelperService urlHelperService,
            ILogger<ConversationService> logger)
        {
            _unitOfWork = unitOfWork;
            _conversationRepository = conversationRepository;
            _participantRepository = participantRepository;
            _urlHelperService = urlHelperService;
            _logger = logger;
        }

        public async Task<DataResponse<Conversation>> GetOrCreatePrivateConversationAsync(Guid userId1, Guid userId2)
        {
            try
            {
                _logger.LogInformation("Finding or creating conversation between {UserId1} and {UserId2}", userId1, userId2);

                // Tìm conversation hiện có
                var existingConversation = await _conversationRepository.FindPrivateConversationAsync(userId1, userId2);
                
                if (existingConversation != null && existingConversation.Participants != null)
                {
                    foreach(var participant in existingConversation.Participants)
                    {
                        if(participant != null && participant.User != null)
                        {
                            participant.User.Avatar = _urlHelperService.GetUrl(participant.User.Avatar);
                        }
                    }
                    _logger.LogInformation("Found existing conversation: {ConversationId}", existingConversation.Id);
                    return new DataResponse<Conversation> 
                    { 
                        Success = true, 
                        Data = existingConversation, 
                        Message = "Conversation found" 
                    };
                }

                // Tạo conversation mới
                var newConversation = new Conversation
                {
                    Type = "private",
                    CreatedAt = DateTime.UtcNow,
                    Participants = new List<ConversationParticipant>
                    {
                        new ConversationParticipant
                        {
                            UserId = userId1,
                            JoinedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        },
                        new ConversationParticipant
                        {
                            UserId = userId2,
                            JoinedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        }
                    }
                };

                _conversationRepository.Add(newConversation);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created new conversation: {ConversationId}", newConversation.Id);

                return new DataResponse<Conversation> 
                { 
                    Success = true, 
                    Data = newConversation, 
                    Message = "Conversation created" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating conversation between {UserId1} and {UserId2}", userId1, userId2);
                return new DataResponse<Conversation> 
                { 
                    Success = false, 
                    Message = "Failed to get or create conversation", 
                    ErrorDetails = ex.Message 
                };
            }
        }

        public async Task<DataResponse<List<Conversation>>> GetUserConversationsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting conversations for user: {UserId}", userId);
                
                var conversations = await _conversationRepository.GetUserConversationsAsync(userId);

                foreach (var conversation in conversations)
                {
                    if (conversation != null && conversation.Participants != null)
                    {
                        foreach (var u in conversation.Participants)
                        {
                            if (u != null && u.User != null && u.User.Avatar != null)
                            {
                                u.User.Avatar = _urlHelperService.GetUrl(u.User.Avatar);
                            }
                        }
                    }

                }
                return new DataResponse<List<Conversation>> 
                { 
                    Success = true, 
                    Data = conversations, 
                    Message = $"Retrieved {conversations.Count} conversation(s)" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user: {UserId}", userId);
                return new DataResponse<List<Conversation>> 
                { 
                    Success = false, 
                    Message = "Failed to get conversations", 
                    ErrorDetails = ex.Message 
                };
            }
        }

        public async Task<DataResponse<Conversation>> GetConversationDetailsAsync(Guid conversationId)
        {
            try
            {
                _logger.LogInformation("Getting conversation details: {ConversationId}", conversationId);
                
                var conversation = await _conversationRepository.GetConversationWithDetailsAsync(conversationId);

                if (conversation == null)
                {
                    return new DataResponse<Conversation> 
                    { 
                        Success = false, 
                        Message = "Conversation not found" 
                    };
                }

                return new DataResponse<Conversation> 
                { 
                    Success = true, 
                    Data = conversation, 
                    Message = "Conversation details retrieved" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation details: {ConversationId}", conversationId);
                return new DataResponse<Conversation> 
                { 
                    Success = false, 
                    Message = "Failed to get conversation details", 
                    ErrorDetails = ex.Message 
                };
            }
        }

        public async Task<DataResponse<bool>> CanAccessConversationAsync(Guid conversationId, Guid userId)
        {
            try
            {
                _logger.LogInformation("Checking access for user {UserId} to conversation {ConversationId}", userId, conversationId);
                
                var isParticipant = await _participantRepository.IsParticipantAsync(conversationId, userId);

                return new DataResponse<bool> 
                { 
                    Success = true, 
                    Data = isParticipant, 
                    Message = isParticipant ? "User has access" : "User does not have access" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking access for user {UserId} to conversation {ConversationId}", userId, conversationId);
                return new DataResponse<bool> 
                { 
                    Success = false, 
                    Message = "Failed to check access", 
                    ErrorDetails = ex.Message 
                };
            }
        }

        public async Task<DataResponse<bool>> MarkConversationAsReadAsync(Guid conversationId, Guid userId)
        {
            try
            {
                _logger.LogInformation("Marking conversation {ConversationId} as read for user {UserId}", conversationId, userId);
                
                await _participantRepository.UpdateLastReadAsync(conversationId, userId);
                await _unitOfWork.SaveChangesAsync();

                return new DataResponse<bool> 
                { 
                    Success = true, 
                    Data = true, 
                    Message = "Conversation marked as read" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking conversation {ConversationId} as read for user {UserId}", conversationId, userId);
                return new DataResponse<bool> 
                { 
                    Success = false, 
                    Message = "Failed to mark conversation as read", 
                    ErrorDetails = ex.Message 
                };
            }
        }
    }
}

