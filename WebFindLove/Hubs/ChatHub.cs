using Microsoft.AspNetCore.SignalR;
using WebFindLove.Models.Services.NotificationService;
using WebFindLove.Models.Services.NotificationService.Dto;
using WebFindLove.Helper.HelperServices;

namespace WebFindLove.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly INotificationService _notificationService;
        private readonly IOnlineUserTrackingService _onlineUserTracking;

        public ChatHub(
            ILogger<ChatHub> logger,
            INotificationService notificationService,
            IOnlineUserTrackingService onlineUserTracking)
        {
            _logger = logger;
            _notificationService = notificationService;
            _onlineUserTracking = onlineUserTracking;
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
            var connectionId = Context.ConnectionId;
            
            _logger.LogInformation("User connected - UserId: {UserId}, ConnectionId: {ConnectionId}", 
                userId, connectionId);
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Add to online tracking
                _onlineUserTracking.AddUserConnection(userId, connectionId);
                
                // Broadcast to all clients that this user is now online
                await Clients.All.SendAsync("UserStatusChanged", new
                {
                    userId = userId,
                    isOnline = true,
                    timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation("User {UserId} is now ONLINE", userId);
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;
            
            _logger.LogInformation("User disconnected - UserId: {UserId}, ConnectionId: {ConnectionId}", 
                userId, connectionId);
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Remove from online tracking
                _onlineUserTracking.RemoveUserConnection(connectionId);
                
                // Check if user still has other connections
                var isStillOnline = _onlineUserTracking.IsUserOnline(userId);
                
                if (!isStillOnline)
                {
                    // Broadcast to all clients that this user is now offline
                    await Clients.All.SendAsync("UserStatusChanged", new
                    {
                        userId = userId,
                        isOnline = false,
                        timestamp = DateTime.UtcNow
                    });
                    
                    _logger.LogInformation("User {UserId} is now OFFLINE", userId);
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Check if a specific user is online
        /// </summary>
        public bool IsUserOnline(string userId)
        {
            return _onlineUserTracking.IsUserOnline(userId);
        }

        /// <summary>
        /// Get online status for multiple users
        /// </summary>
        public Dictionary<string, bool> GetUsersOnlineStatus(List<string> userIds)
        {
            var result = new Dictionary<string, bool>();
            
            foreach (var userId in userIds)
            {
                result[userId] = _onlineUserTracking.IsUserOnline(userId);
            }
            
            return result;
        }
    }
}

