using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.Entities;
using WebFindLove.Models.Repositories.NotificationRepo;
using WebFindLove.Models.Services.NotificationService.Dto;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Services.NotificationService
{
    /// <summary>
    /// Service implementation cho Notification business logic
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork unitOfWork,
            INotificationRepository notificationRepository,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<DataResponse<List<NotificationDto>>> GetUserNotificationsAsync(Guid userId, int pageSize = 10, int pageNumber = 1)
        {
            try
            {
                _logger.LogInformation("Getting notifications for user: {UserId}", userId);

                var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, pageSize, pageNumber);

                var notificationDtos = notifications.Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    SenderId = n.SenderId,
                    SenderName = n.Sender?.UserName ?? "Hệ thống",
                    SenderAvatar = n.Sender?.Avatar,
                    ReceiverId = n.ReceiverId,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt,
                    TimeAgo = GetTimeAgo(n.CreatedAt)
                }).ToList();

                return new DataResponse<List<NotificationDto>>
                {
                    Success = true,
                    Data = notificationDtos,
                    Message = $"Đã tải {notificationDtos.Count} thông báo"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user: {UserId}", userId);
                return new DataResponse<List<NotificationDto>>
                {
                    Success = false,
                    Message = "Lỗi khi tải thông báo",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<int>> GetUnreadCountAsync(Guid userId)
        {
            try
            {
                var count = await _notificationRepository.GetUnreadCountAsync(userId);

                return new DataResponse<int>
                {
                    Success = true,
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user: {UserId}", userId);
                return new DataResponse<int>
                {
                    Success = false,
                    Message = "Lỗi khi đếm thông báo chưa đọc",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<NotificationDto>> CreateNotificationAsync(NotificationCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Creating notification for user: {ReceiverId}", dto.ReceiverId);

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
                    Message = dto.Message,
                    SenderId = dto.SenderId,
                    ReceiverId = dto.ReceiverId,
                    Link = dto.Link,
                    Type = dto.Type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _notificationRepository.Add(notification);
                await _unitOfWork.SaveChangesAsync();

                // Load Sender info
                var createdNotification = await _notificationRepository
                    .FindAll(n => n.Id == notification.Id)
                    .Include(n => n.Sender)
                    .FirstOrDefaultAsync();

                var notificationDto = new NotificationDto
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    SenderId = notification.SenderId,
                    SenderName = createdNotification?.Sender?.UserName ?? "Hệ thống",
                    SenderAvatar = createdNotification?.Sender?.Avatar,
                    ReceiverId = notification.ReceiverId,
                    Link = notification.Link,
                    IsRead = notification.IsRead,
                    Type = notification.Type,
                    CreatedAt = notification.CreatedAt,
                    TimeAgo = "Vừa xong"
                };

                return new DataResponse<NotificationDto>
                {
                    Success = true,
                    Data = notificationDto,
                    Message = "Tạo thông báo thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return new DataResponse<NotificationDto>
                {
                    Success = false,
                    Message = "Lỗi khi tạo thông báo",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            try
            {
                _logger.LogInformation("Marking notification as read: {NotificationId}", notificationId);

                // Verify notification belongs to user
                var notification = await _notificationRepository.FindByIdAsync(notificationId);
                if (notification == null || notification.ReceiverId != userId)
                {
                    return new DataResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy thông báo hoặc bạn không có quyền"
                    };
                }

                var result = await _notificationRepository.MarkAsReadAsync(notificationId);

                return new DataResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Đã đánh dấu đã đọc" : "Không thể đánh dấu đã đọc"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
                return new DataResponse<bool>
                {
                    Success = false,
                    Message = "Lỗi khi đánh dấu đã đọc",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<bool>> MarkAllAsReadAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Marking all notifications as read for user: {UserId}", userId);

                var result = await _notificationRepository.MarkAllAsReadAsync(userId);

                return new DataResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Đã đánh dấu tất cả đã đọc" : "Không thể đánh dấu tất cả đã đọc"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user: {UserId}", userId);
                return new DataResponse<bool>
                {
                    Success = false,
                    Message = "Lỗi khi đánh dấu tất cả đã đọc",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<bool>> DeleteNotificationAsync(Guid notificationId, Guid userId)
        {
            try
            {
                _logger.LogInformation("Deleting notification: {NotificationId}", notificationId);

                // Verify notification belongs to user
                var notification = await _notificationRepository.FindByIdAsync(notificationId);
                if (notification == null || notification.ReceiverId != userId)
                {
                    return new DataResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy thông báo hoặc bạn không có quyền"
                    };
                }

                 _notificationRepository.Remove(notification);
                await _unitOfWork.SaveChangesAsync();

                return new DataResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Xóa thông báo thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification: {NotificationId}", notificationId);
                return new DataResponse<bool>
                {
                    Success = false,
                    Message = "Lỗi khi xóa thông báo",
                    ErrorDetails = ex.Message
                };
            }
        }

        // Helper method để tính thời gian "... trước"
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} tuần trước";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} tháng trước";
            
            return $"{(int)(timeSpan.TotalDays / 365)} năm trước";
        }
    }
}

