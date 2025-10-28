using WebFindLove.Models.Entities;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.NotificationRepo
{
    /// <summary>
    /// Interface cho Notification Repository
    /// </summary>
    public interface INotificationRepository : IGenericRepository<Notification, Guid>
    {
        /// <summary>
        /// Lấy danh sách thông báo của user
        /// </summary>
        Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int pageSize = 10, int pageNumber = 1);

        /// <summary>
        /// Đếm số thông báo chưa đọc của user
        /// </summary>
        Task<int> GetUnreadCountAsync(Guid userId);

        /// <summary>
        /// Đánh dấu một thông báo đã đọc
        /// </summary>
        Task<bool> MarkAsReadAsync(Guid notificationId);

        /// <summary>
        /// Đánh dấu tất cả thông báo của user đã đọc
        /// </summary>
        Task<bool> MarkAllAsReadAsync(Guid userId);

        /// <summary>
        /// Xóa thông báo cũ (tùy chọn)
        /// </summary>
        Task<bool> DeleteOldNotificationsAsync(DateTime olderThan);
    }
}

