using Microsoft.AspNetCore.SignalR;

namespace WebFindLove.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
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

