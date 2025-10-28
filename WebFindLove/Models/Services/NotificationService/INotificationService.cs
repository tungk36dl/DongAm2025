using WebFindLove.Models.Services.NotificationService.Dto;

namespace WebFindLove.Models.Services.NotificationService
{
    /// <summary>
    /// Interface cho Notification Service
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Lấy danh sách thông báo của user
        /// </summary>
        Task<DataResponse<List<NotificationDto>>> GetUserNotificationsAsync(Guid userId, int pageSize = 10, int pageNumber = 1);

        /// <summary>
        /// Đếm số thông báo chưa đọc
        /// </summary>
        Task<DataResponse<int>> GetUnreadCountAsync(Guid userId);

        /// <summary>
        /// Tạo thông báo mới
        /// </summary>
        Task<DataResponse<NotificationDto>> CreateNotificationAsync(NotificationCreateDto dto);

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        Task<DataResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);

        /// <summary>
        /// Đánh dấu tất cả thông báo đã đọc
        /// </summary>
        Task<DataResponse<bool>> MarkAllAsReadAsync(Guid userId);

        /// <summary>
        /// Xóa thông báo
        /// </summary>
        Task<DataResponse<bool>> DeleteNotificationAsync(Guid notificationId, Guid userId);
    }
}

