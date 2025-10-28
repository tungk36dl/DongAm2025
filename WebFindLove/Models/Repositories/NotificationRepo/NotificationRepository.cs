using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.Entities;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.NotificationRepo
{
    /// <summary>
    /// Repository implementation cho Notification entity
    /// </summary>
    public class NotificationRepository : GenericRepository<Notification, Guid>, INotificationRepository
    {
        public NotificationRepository(AppDbContext context) : base(context) { }

        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int pageSize = 10, int pageNumber = 1)
        {
            return await _context.Notifications
                .Where(n => n.ReceiverId == userId)
                .Include(n => n.Sender)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOldNotificationsAsync(DateTime olderThan)
        {
            var oldNotifications = await _context.Notifications
                .Where(n => n.CreatedAt < olderThan)
                .ToListAsync();

            _context.Notifications.RemoveRange(oldNotifications);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

