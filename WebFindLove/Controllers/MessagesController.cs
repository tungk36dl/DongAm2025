using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebFindLove.Models;
using WebFindLove.Models.Services;
using WebFindLove.Models.Services.MessageService;
using WebFindLove.Models.Services.ConversationService;
using WebFindLove.Models.Services.UserService.Dto;
using WebFindLove.Models.Services.NotificationService;
using WebFindLove.Models.Services.NotificationService.Dto;
using WebFindLove.Hubs;
using WebFindLove.Models.Services.MessageService.Dto;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using WebFindLove.Helper.HelperServices.Mapper;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class MessagesController : BaseController
    {
        private readonly IMessageService _messageService;
        private readonly IConversationService _conversationService;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IMapper _mapper;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(
            IMessageService messageService,
            IConversationService conversationService,
            IUserService userService,
            INotificationService notificationService,
            IHubContext<ChatHub> hubContext,
            IMapper mapper,
            ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _conversationService = conversationService;
            _userService = userService;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _mapper = mapper;
            _logger = logger;
            Logger = logger;
        }

        // ==============================
        // GET: Messages - List conversations
        // ==============================
        public async Task<IActionResult> Index()
        {
            try
            {
                if (UserId == null)
                {
                    TempData["ErrorMessage"] = "User not authenticated.";
                    return RedirectToAction("Login", "Auth");
                }

                var response = await _conversationService.GetUserConversationsAsync(UserId.Value);

                if (!response.Success)
                    TempData["ErrorMessage"] = response.Message;

                var unreadResponse = await _messageService.GetUnreadCountAsync(UserId.Value);
                ViewData["UnreadCount"] = unreadResponse.Success ? unreadResponse.Data : 0;

                return View(response.Data ?? new List<WebFindLove.Models.Entities.Conversation>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Messages Index");
                TempData["ErrorMessage"] = "An error occurred while loading conversations.";
                return View(new List<WebFindLove.Models.Entities.Conversation>());
            }
        }

        // ==============================
        // GET Conversation
        // ==============================
        public async Task<IActionResult> Conversation(Guid userId)
        {
            try
            {
                if (UserId == null)
                    return RedirectToAction("Login", "Auth");

                if (userId == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid user.";
                    return RedirectToAction(nameof(Index));
                }

                if (userId == UserId)
                {
                    TempData["ErrorMessage"] = "Cannot chat with yourself.";
                    return RedirectToAction(nameof(Index));
                }

                // User info
                var userResponse = await _userService.GetByIdAsync(userId);
                if (!userResponse.Success || userResponse.Data == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Conversation
                var conversationResponse = await _conversationService
                    .GetOrCreatePrivateConversationAsync(UserId.Value, userId);

                if (!conversationResponse.Success || conversationResponse.Data == null)
                {
                    TempData["ErrorMessage"] = conversationResponse.Message;
                    return RedirectToAction(nameof(Index));
                }

                var search = new MessageSearch
                {
                    UserId1 = UserId.Value,
                    UserId2 = userId,
                    PageSize = 1000
                };

                var response = await _messageService.GetConversationAsync(search);

                if (!response.Success)
                    TempData["ErrorMessage"] = response.Message;

                // Mark as read
                await _messageService.MarkAsReadAsync(UserId.Value, userId);
                await _conversationService.MarkConversationAsReadAsync(conversationResponse.Data.Id, UserId.Value);
                var dataResopnse = _mapper.Map<User, UserDto>(userResponse.Data);
                ViewData["OtherUser"] = dataResopnse;
                ViewData["OtherUserId"] = userId;
                ViewData["ConversationId"] = conversationResponse.Data.Id;

                return View(response.Data ?? new List<Message>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation");
                TempData["ErrorMessage"] = "An error occurred loading the conversation.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ==============================
        // POST Send
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(Guid receiverId, string content)
        {
            try
            {
                if (UserId == null)
                    return RedirectToAction("Login", "Auth");

                if (receiverId == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid receiver.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    TempData["ErrorMessage"] = "Message cannot be empty.";
                    return RedirectToAction(nameof(Conversation), new { userId = receiverId });
                }

                var response = await _messageService.SendMessageAsync(UserId.Value, receiverId, content);

                if (!response.Success)
                {
                    TempData["ErrorMessage"] = response.Message;
                }
                else
                {
                    // SIGNALR + notification
                    try
                    {
                        var senderInfo = await _userService.GetByIdAsync(UserId.Value);

                        var senderName = senderInfo.Data?.UserName ?? "Unknown";
                        var senderAvatar = senderInfo.Data?.Avatar ?? "";

                        var messageData = new
                        {
                            senderId = UserId.ToString(),
                            senderName = senderName,
                            senderAvatar = senderAvatar,
                            message = content,
                            timestamp = DateTime.UtcNow,
                            messageId = response.Data?.Id
                        };

                        await _hubContext.Clients.User(receiverId.ToString())
                            .SendAsync("ReceiveMessage", messageData);

                        var notificationDto = new NotificationCreateDto
                        {
                            Title = "Tin nhắn mới",
                            Message = $"{senderName}: {content}",
                            SenderId = UserId.Value,
                            ReceiverId = receiverId,
                            Link = "/Messages/Index",
                            Type = "Message"
                        };

                        await _notificationService.CreateNotificationAsync(notificationDto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SignalR sending failed.");
                    }
                }

                return RedirectToAction(nameof(Conversation), new { userId = receiverId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message.");
                TempData["ErrorMessage"] = "An error occurred while sending message.";
                return RedirectToAction(nameof(Conversation), new { userId = receiverId });
            }
        }

        // ==============================
        // GET UnreadCount
        // ==============================
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                if (UserId == null)
                    return Json(new { success = false, count = 0 });

                var response = await _messageService.GetUnreadCountAsync(UserId.Value);
                return Json(new { success = response.Success, count = response.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return Json(new { success = false, count = 0 });
            }
        }

        // ==============================
        // GET Conversations JSON
        // ==============================
        [HttpGet]
        public async Task<IActionResult> GetConversationsJson()
        {
            try
            {
                if (UserId == null)
                    return RedirectToAction("Login", "Auth");

                var response = await _conversationService.GetUserConversationsAsync(UserId.Value);

                if (!response.Success)
                    return Json(new { success = false, message = response.Message });

                var conversations = response.Data?.Select(c =>
                {
                    var other = c.Participants?.FirstOrDefault(p => p.UserId != UserId.Value)?.User;

                    return new
                    {
                        conversationId = c.Id,
                        otherUserId = other?.Id,
                        otherUserName = other?.UserName ?? "Unknown",
                        otherUserAvatar = other?.Avatar,
                        lastMessage = c.LastMessage,
                        lastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                        unreadCount = c.Messages?.Count(m => !m.IsRead && m.ReceiverId == UserId.Value) ?? 0
                    };
                }).OrderByDescending(c => c.lastMessageAt);

                var unreadResponse = await _messageService.GetUnreadCountAsync(UserId.Value);

                return Json(new
                {
                    success = true,
                    conversations,
                    unreadCount = unreadResponse.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation list JSON");
                return Json(new { success = false, message = "Failed to load conversations." });
            }
        }

        // ==============================
        // GET Messages JSON
        // ==============================
        [HttpGet]
        public async Task<IActionResult> GetMessagesJson(Guid userId)
        {
            try
            {
                if (UserId == null)
                    return Json(new { success = false, message = "Not logged in." });

                if (userId == Guid.Empty)
                    return Json(new { success = false, message = "Invalid user id." });

                var userResponse = await _userService.GetByIdAsync(userId);
                if (!userResponse.Success || userResponse.Data == null)
                    return Json(new { success = false, message = "User not found." });

                var search = new MessageSearch
                {
                    UserId1 = UserId.Value,
                    UserId2 = userId,
                    PageSize = 20
                };

                var response = await _messageService.GetConversationAsync(search);

                if (!response.Success)
                    return Json(new { success = false, message = response.Message });

                var messages = response.Data?.Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.ReceiverId,
                    m.Content,
                    m.SentAt,
                    m.IsRead,
                    isSentByMe = m.SenderId == UserId.Value
                });

                await _messageService.MarkAsReadAsync(UserId.Value, userId);

                return Json(new
                {
                    success = true,
                    messages,
                    otherUser = new
                    {
                        userResponse.Data.Id,
                        userName = userResponse.Data.UserName,
                        avatar = userResponse.Data.Avatar
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading messages JSON");
                return Json(new { success = false, message = "Failed to load messages." });
            }
        }

        // ==============================
        // POST SendMessageJson
        // ==============================
        [HttpPost]
        public async Task<IActionResult> SendMessageJson([FromBody] SendMessageRequest request)
        {
            try
            {
                if (UserId == null)
                    return Json(new { success = false, message = "Not logged in." });

                if (string.IsNullOrWhiteSpace(request.Content))
                    return Json(new { success = false, message = "Message empty." });

                var response = await _messageService
                    .SendMessageAsync(UserId.Value, request.ReceiverId, request.Content);

                if (!response.Success)
                    return Json(new { success = false, message = response.Message });

                // SIGNALR
                try
                {
                    // Lấy thông tin đầy đủ của sender (bao gồm avatar)
                    var senderInfo = await _userService.GetByIdAsync(UserId.Value);
                    var senderName = senderInfo.Data?.UserName ?? "Unknown";
                    var senderAvatar = senderInfo.Data?.Avatar ?? "";

                    await _hubContext.Clients.User(request.ReceiverId.ToString())
                        .SendAsync("ReceiveMessage", new
                        {
                            senderId = UserId.ToString(),
                            senderName = senderName,
                            senderAvatar = senderAvatar,
                            messageId = response.Data?.Id,
                            message = request.Content,
                            timestamp = DateTime.UtcNow
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SignalR error.");
                }

                // Trả về chỉ dữ liệu cần thiết, tránh object cycle
                return Json(new { 
                    success = true, 
                    message = new
                    {
                        id = response.Data?.Id,
                        senderId = response.Data?.SenderId,
                        receiverId = response.Data?.ReceiverId,
                        content = response.Data?.Content,
                        sentAt = response.Data?.SentAt,
                        isRead = response.Data?.IsRead
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message JSON");
                return Json(new { success = false, message = "Error sending message." });
            }
        }

        // ==============================
        // POST Delete
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid? returnUserId)
        {
            try
            {
                var response = await _messageService.DeleteMessageAsync(id);

                if (!response.Success)
                    TempData["ErrorMessage"] = response.Message;
                else
                    TempData["SuccessMessage"] = "Message deleted.";

                if (returnUserId.HasValue)
                    return RedirectToAction(nameof(Conversation), new { userId = returnUserId.Value });

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                TempData["ErrorMessage"] = "An error occurred deleting message.";
                return RedirectToAction(nameof(Index));
            }
        }

        public class SendMessageRequest
        {
            public Guid ReceiverId { get; set; }
            public string Content { get; set; } = "";
        }
    }
}
