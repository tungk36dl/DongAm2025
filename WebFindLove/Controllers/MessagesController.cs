using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebFindLove.Models;
using WebFindLove.Models.Services;
using WebFindLove.Models.Services.MessageService;
using WebFindLove.Models.Services.ConversationService;
using WebFindLove.Models.Services.UserService.Dto;
using WebFindLove.Hubs;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class MessagesController : BaseController
    {
        private readonly IMessageService _messageService;
        private readonly IConversationService _conversationService;
        private readonly IUserService _userService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(
            IMessageService messageService,
            IConversationService conversationService,
            IUserService userService,
            IHubContext<ChatHub> hubContext,
            ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _conversationService = conversationService;
            _userService = userService;
            _hubContext = hubContext;
            _logger = logger;
            Logger = logger;
            _logger.LogInformation("MessagesController initialized");
        }

        // GET: Messages - List conversations
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("GET Messages Index - User: {Username}", CurrentUser?.UserName);

            var response = await _conversationService.GetUserConversationsAsync(UserId!.Value);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to get conversations: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }

            // Get unread count
            var unreadResponse = await _messageService.GetUnreadCountAsync(UserId!.Value);
            ViewData["UnreadCount"] = unreadResponse.Success ? unreadResponse.Data : 0;

            return View(response.Data ?? new List<WebFindLove.Models.Entities.Conversation>());
        }

        // GET: Messages/Conversation/5 - View conversation with specific user
        public async Task<IActionResult> Conversation(Guid userId)
        {
            _logger.LogInformation("GET Conversation - CurrentUser: {CurrentUserId}, WithUser: {OtherUserId}", UserId, userId);

            if (userId == UserId)
            {
                TempData["ErrorMessage"] = "Cannot message yourself.";
                return RedirectToAction(nameof(Index));
            }

            // Get the other user's info
            var userResponse = await _userService.GetByIdAsync(userId);
            if (!userResponse.Success || userResponse.Data == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get or create conversation
            var conversationResponse = await _conversationService.GetOrCreatePrivateConversationAsync(UserId!.Value, userId);
            
            if (!conversationResponse.Success || conversationResponse.Data == null)
            {
                _logger.LogWarning("Failed to get conversation: {Message}", conversationResponse.Message);
                TempData["ErrorMessage"] = conversationResponse.Message;
                return RedirectToAction(nameof(Index));
            }

            // Get conversation messages
            var response = await _messageService.GetConversationAsync(UserId!.Value, userId);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to get conversation messages: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }

            // Mark messages as read
            await _messageService.MarkAsReadAsync(UserId!.Value, userId);
            await _conversationService.MarkConversationAsReadAsync(conversationResponse.Data.Id, UserId!.Value);

            ViewData["OtherUser"] = userResponse.Data;
            ViewData["OtherUserId"] = userId;
            ViewData["ConversationId"] = conversationResponse.Data.Id;

            return View(response.Data ?? new List<Message>());
        }

        // POST: Messages/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(Guid receiverId, string content)
        {
            _logger.LogInformation("POST Send Message - From: {SenderId}, To: {ReceiverId}", UserId, receiverId);

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Message content cannot be empty.";
                return RedirectToAction(nameof(Conversation), new { userId = receiverId });
            }

            var response = await _messageService.SendMessageAsync(UserId!.Value, receiverId, content);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to send message: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }
            else
            {
                _logger.LogInformation("Message sent successfully: {MessageId}", response.Data?.Id);
                TempData["SuccessMessage"] = "Message sent successfully!";

                // Send real-time notification via SignalR
                try
                {
                    // Get sender info
                    var senderInfo = await _userService.GetByIdAsync(UserId!.Value);
                    
                    var messageData = new
                    {
                        senderId = UserId.ToString(),
                        senderName = senderInfo.Data?.UserName ?? CurrentUser?.UserName ?? "Unknown",
                        senderAvatar = senderInfo.Data?.Avatar ?? "",
                        message = content,
                        timestamp = DateTime.UtcNow,
                        messageId = response.Data?.Id
                    };
                    
                    _logger.LogInformation("Sending SignalR message to user: {ReceiverId}", receiverId);
                    
                    // Send via ChatHub
                    await _hubContext.Clients.User(receiverId.ToString())
                        .SendAsync("ReceiveMessage", messageData);
                    
                    _logger.LogInformation("SignalR message sent successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SignalR notification");
                    // Don't fail the request if SignalR fails
                }
            }

            return RedirectToAction(nameof(Conversation), new { userId = receiverId });
        }

        // GET: Messages/UnreadCount - API endpoint
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var response = await _messageService.GetUnreadCountAsync(UserId!.Value);
            return Json(new { success = response.Success, count = response.Data });
        }

        // GET: Messages/GetConversationsJson - API endpoint for chat widget
        [HttpGet]
        public async Task<IActionResult> GetConversationsJson()
        {
            _logger.LogInformation("GET Conversations JSON - User: {Username}", CurrentUser?.UserName);

            var response = await _conversationService.GetUserConversationsAsync(UserId!.Value);

            if (!response.Success)
            {
                return Json(new { success = false, message = response.Message });
            }

            var conversations = response.Data?.Select(c =>
            {
                var otherParticipant = c.Participants?.FirstOrDefault(p => p.UserId != UserId!.Value);
                var otherUser = otherParticipant?.User;
                var hasUnread = c.Messages?.Any(m => !m.IsRead && m.ReceiverId == UserId!.Value) ?? false;

                return new
                {
                    conversationId = c.Id,
                    otherUserId = otherUser?.Id,
                    otherUserName = otherUser?.UserName ?? "Unknown User",
                    otherUserAvatar = otherUser?.Avatar,
                    lastMessage = c.LastMessage,
                    lastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                    hasUnread = hasUnread,
                    unreadCount = c.Messages?.Count(m => !m.IsRead && m.ReceiverId == UserId!.Value) ?? 0
                };
            }).OrderByDescending(c => c.lastMessageAt).ToList();

            // Get total unread count
            var unreadResponse = await _messageService.GetUnreadCountAsync(UserId!.Value);
            var unreadCount = unreadResponse.Success ? unreadResponse.Data : 0;

            return Json(new
            {
                success = true,
                conversations = conversations,
                unreadCount = unreadCount
            });
        }

        // GET: Messages/GetMessagesJson - API endpoint for chat widget
        [HttpGet]
        public async Task<IActionResult> GetMessagesJson(Guid userId)
        {
            _logger.LogInformation("GET Messages JSON - CurrentUser: {CurrentUserId}, WithUser: {OtherUserId}", UserId, userId);

            if (userId == UserId)
            {
                return Json(new { success = false, message = "Cannot message yourself." });
            }

            // Get the other user's info
            var userResponse = await _userService.GetByIdAsync(userId);
            if (!userResponse.Success || userResponse.Data == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Get conversation messages
            var response = await _messageService.GetConversationAsync(UserId!.Value, userId);

            if (!response.Success)
            {
                return Json(new { success = false, message = response.Message });
            }

            var messages = response.Data?.Select(m => new
            {
                id = m.Id,
                senderId = m.SenderId,
                receiverId = m.ReceiverId,
                content = m.Content,
                sentAt = m.SentAt,
                isRead = m.IsRead,
                isSentByMe = m.SenderId == UserId!.Value
            }).ToList();

            // Mark messages as read
            await _messageService.MarkAsReadAsync(UserId!.Value, userId);

            return Json(new
            {
                success = true,
                messages = messages,
                otherUser = new
                {
                    id = userResponse.Data.Id,
                    userName = userResponse.Data.UserName,
                    avatar = userResponse.Data.Avatar
                }
            });
        }

        // POST: Messages/SendMessageJson - API endpoint for chat widget
        [HttpPost]
        public async Task<IActionResult> SendMessageJson([FromBody] SendMessageRequest request)
        {
            _logger.LogInformation("POST Send Message JSON - From: {SenderId}, To: {ReceiverId}", UserId, request.ReceiverId);

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Json(new { success = false, message = "Message content cannot be empty." });
            }

            var response = await _messageService.SendMessageAsync(UserId!.Value, request.ReceiverId, request.Content);

            if (!response.Success)
            {
                return Json(new { success = false, message = response.Message });
            }

            // Send real-time notification via SignalR
            try
            {
                var senderInfo = await _userService.GetByIdAsync(UserId!.Value);
                
                var messageData = new
                {
                    senderId = UserId.ToString(),
                    senderName = senderInfo.Data?.UserName ?? CurrentUser?.UserName ?? "Unknown",
                    senderAvatar = senderInfo.Data?.Avatar ?? "",
                    message = request.Content,
                    timestamp = DateTime.UtcNow,
                    messageId = response.Data?.Id
                };
                
                await _hubContext.Clients.User(request.ReceiverId.ToString())
                    .SendAsync("ReceiveMessage", messageData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification");
            }

            return Json(new
            {
                success = true,
                message = new
                {
                    id = response.Data?.Id,
                    senderId = response.Data?.SenderId,
                    receiverId = response.Data?.ReceiverId,
                    content = response.Data?.Content,
                    sentAt = response.Data?.SentAt,
                    isRead = response.Data?.IsRead,
                    isSentByMe = true
                }
            });
        }

        // Request model for SendMessageJson
        public class SendMessageRequest
        {
            public Guid ReceiverId { get; set; }
            public string Content { get; set; } = string.Empty;
        }
    

        // POST: Messages/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid? returnUserId)
        {
            _logger.LogInformation("POST Delete Message - MessageId: {MessageId}, User: {Username}", id, CurrentUser?.UserName);

            var response = await _messageService.DeleteMessageAsync(id);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to delete message: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }
            else
            {
                _logger.LogInformation("Message deleted successfully: {MessageId}", id);
                TempData["SuccessMessage"] = "Message deleted successfully!";
            }

            if (returnUserId.HasValue)
            {
                return RedirectToAction(nameof(Conversation), new { userId = returnUserId.Value });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

