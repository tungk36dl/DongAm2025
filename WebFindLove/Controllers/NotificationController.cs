using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models.Services.NotificationService;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
            Logger = logger;
        }

        // GET: Notification/Index
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            _logger.LogInformation("GET Notification Index - User: {Username}, Page: {Page}", CurrentUser?.UserName, page);

            if (!UserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var response = await _notificationService.GetUserNotificationsAsync(UserId.Value, pageSize, page);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to get notifications: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }

            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            return View(response.Data ?? new List<Models.Services.NotificationService.Dto.NotificationDto>());
        }

        // GET: Notification/GetUnreadCount - API endpoint
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!UserId.HasValue)
            {
                return Json(new { success = false, count = 0 });
            }

            var response = await _notificationService.GetUnreadCountAsync(UserId.Value);

            return Json(new { success = response.Success, count = response.Data });
        }

        // GET: Notification/GetRecent - API endpoint để lấy thông báo gần đây cho dropdown
        [HttpGet]
        public async Task<IActionResult> GetRecent(int count = 5)
        {
            if (!UserId.HasValue)
            {
                return Json(new { success = false, notifications = new List<object>() });
            }

            var response = await _notificationService.GetUserNotificationsAsync(UserId.Value, count, 1);

            return Json(new { success = response.Success, notifications = response.Data });
        }

        // POST: Notification/MarkAsRead/{id}
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            _logger.LogInformation("POST MarkAsRead - NotificationId: {NotificationId}, User: {Username}", id, CurrentUser?.UserName);

            if (!UserId.HasValue)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var response = await _notificationService.MarkAsReadAsync(id, UserId.Value);

            return Json(new { success = response.Success, message = response.Message });
        }

        // POST: Notification/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            _logger.LogInformation("POST MarkAllAsRead - User: {Username}", CurrentUser?.UserName);

            if (!UserId.HasValue)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var response = await _notificationService.MarkAllAsReadAsync(UserId.Value);

            if (response.Success)
            {
                TempData["SuccessMessage"] = "Đã đánh dấu tất cả thông báo là đã đọc";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Notification/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("POST Delete Notification - NotificationId: {NotificationId}, User: {Username}", id, CurrentUser?.UserName);

            if (!UserId.HasValue)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var response = await _notificationService.DeleteNotificationAsync(id, UserId.Value);

            return Json(new { success = response.Success, message = response.Message });
        }
    }
}

