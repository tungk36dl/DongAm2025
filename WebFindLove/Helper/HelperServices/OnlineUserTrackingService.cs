using System.Collections.Concurrent;

namespace WebFindLove.Helper.HelperServices
{
    /// <summary>
    /// Service để track trạng thái online/offline của users
    /// Sử dụng in-memory ConcurrentDictionary để lưu connectionId -> userId mapping
    /// </summary>
    public interface IOnlineUserTrackingService
    {
        /// <summary>
        /// Thêm user connection
        /// </summary>
        void AddUserConnection(string userId, string connectionId);

        /// <summary>
        /// Xóa user connection
        /// </summary>
        void RemoveUserConnection(string connectionId);

        /// <summary>
        /// Kiểm tra user có online không
        /// </summary>
        bool IsUserOnline(string userId);

        /// <summary>
        /// Lấy danh sách connection IDs của user
        /// </summary>
        List<string> GetUserConnections(string userId);

        /// <summary>
        /// Lấy tổng số users online
        /// </summary>
        int GetOnlineUserCount();

        /// <summary>
        /// Lấy danh sách tất cả users online
        /// </summary>
        List<string> GetAllOnlineUserIds();
    }

    public class OnlineUserTrackingService : IOnlineUserTrackingService
    {
        // connectionId -> userId
        private readonly ConcurrentDictionary<string, string> _connectionToUser = new();
        
        // userId -> List of connectionIds (một user có thể có nhiều connections từ các devices khác nhau)
        private readonly ConcurrentDictionary<string, HashSet<string>> _userToConnections = new();
        
        private readonly ILogger<OnlineUserTrackingService> _logger;

        public OnlineUserTrackingService(ILogger<OnlineUserTrackingService> logger)
        {
            _logger = logger;
        }

        public void AddUserConnection(string userId, string connectionId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(connectionId))
                return;

            // Add connection -> user mapping
            _connectionToUser[connectionId] = userId;

            // Add connection to user's connection list
            var connections = _userToConnections.GetOrAdd(userId, _ => new HashSet<string>());
            lock (connections)
            {
                connections.Add(connectionId);
            }

            _logger.LogInformation("User {UserId} connected with ConnectionId {ConnectionId}. Total connections: {Count}", 
                userId, connectionId, connections.Count);
        }

        public void RemoveUserConnection(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
                return;

            // Get userId from connectionId
            if (!_connectionToUser.TryRemove(connectionId, out var userId))
                return;

            // Remove connection from user's list
            if (_userToConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(connectionId);
                    
                    // If no more connections, remove user from tracking
                    if (connections.Count == 0)
                    {
                        _userToConnections.TryRemove(userId, out _);
                        _logger.LogInformation("User {UserId} is now OFFLINE (ConnectionId {ConnectionId} removed)", 
                            userId, connectionId);
                    }
                    else
                    {
                        _logger.LogInformation("User {UserId} disconnected ConnectionId {ConnectionId}. Remaining connections: {Count}", 
                            userId, connectionId, connections.Count);
                    }
                }
            }
        }

        public bool IsUserOnline(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            return _userToConnections.TryGetValue(userId, out var connections) && 
                   connections.Count > 0;
        }

        public List<string> GetUserConnections(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<string>();

            if (_userToConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    return connections.ToList();
                }
            }

            return new List<string>();
        }

        public int GetOnlineUserCount()
        {
            return _userToConnections.Count;
        }

        public List<string> GetAllOnlineUserIds()
        {
            return _userToConnections.Keys.ToList();
        }
    }
}

