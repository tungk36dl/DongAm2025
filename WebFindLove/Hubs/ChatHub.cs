using Microsoft.AspNetCore.SignalR;
using WebFindLove.Models.Services.NotificationService;
using WebFindLove.Models.Services.NotificationService.Dto;

namespace WebFindLove.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly INotificationService _notificationService;

        public ChatHub(
            ILogger<ChatHub> logger,
            INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Send message to a specific user
        /// </summary>
        public async Task SendMessageToUser(string receiverUserId, object messageData)
        {
            _logger.LogInformation("SendMessageToUser called - To: {ReceiverId}", receiverUserId);
            
            // Send to all connections of the receiver
            await Clients.User(receiverUserId).SendAsync("ReceiveMessage", messageData);
            
            _logger.LogInformation("Message sent to user: {ReceiverId}", receiverUserId);
        }

        /// <summary>
        /// Send notification to a specific user (realtime)
        /// </summary>
        public async Task SendNotificationToUser(string receiverUserId, object notificationData)
        {
            _logger.LogInformation("SendNotificationToUser called - To: {ReceiverId}", receiverUserId);
            
            try
            {
                // Send notification via SignalR
                await Clients.User(receiverUserId).SendAsync("ReceiveNotification", notificationData);
                
                _logger.LogInformation("Notification sent to user: {ReceiverId}", receiverUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user: {ReceiverId}", receiverUserId);
            }
        }

        /// <summary>
        /// Create and send notification when new message arrives
        /// </summary>
        public async Task NotifyNewMessage(string senderUserId, string receiverUserId, string senderName, string messagePreview)
        {
            try
            {
                _logger.LogInformation("Creating notification for new message - From: {SenderId} To: {ReceiverId}", 
                    senderUserId, receiverUserId);
                
                // Parse GUIDs
                if (!Guid.TryParse(senderUserId, out var senderGuid) || !Guid.TryParse(receiverUserId, out var receiverGuid))
                {
                    _logger.LogWarning("Invalid GUID format for sender or receiver");
                    return;
                }

                // Create notification in database
                var notificationDto = new NotificationCreateDto
                {
                    Title = "Tin nhắn mới",
                    Message = $"{senderName} đã gửi cho bạn: {messagePreview}",
                    SenderId = senderGuid,
                    ReceiverId = receiverGuid,
                    Link = $"/Messages/Index",
                    Type = "Message"
                };

                var result = await _notificationService.CreateNotificationAsync(notificationDto);

                if (result.Success && result.Data != null)
                {
                    // Send real-time notification via SignalR
                    await Clients.User(receiverUserId).SendAsync("ReceiveNotification", new
                    {
                        id = result.Data.Id,
                        title = result.Data.Title,
                        message = result.Data.Message,
                        senderName = result.Data.SenderName,
                        senderAvatar = result.Data.SenderAvatar,
                        link = result.Data.Link,
                        type = result.Data.Type,
                        timeAgo = result.Data.TimeAgo,
                        createdAt = result.Data.CreatedAt
                    });

                    _logger.LogInformation("Notification created and sent successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to create notification: {Message}", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotifyNewMessage");
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation("User connected - UserId: {UserId}, ConnectionId: {ConnectionId}", 
                userId, Context.ConnectionId);
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation("User disconnected - UserId: {UserId}", userId);
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}

