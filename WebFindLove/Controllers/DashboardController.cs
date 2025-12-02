using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models;
using WebFindLove.Models.Repositories.UserRepo;
using WebFindLove.Models.Repositories.MessageRepo;
using WebFindLove.Models.Repositories.ConversationRepo;
using WebFindLove.Models.Services;
using WebFindLove.Helper.HelperServices;

namespace WebFindLove.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : BaseController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IOnlineUserTrackingService _onlineUserTrackingService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IUserRepository userRepository,
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            IOnlineUserTrackingService onlineUserTrackingService,
            ILogger<DashboardController> logger)
        {
            _userRepository = userRepository;
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _onlineUserTrackingService = onlineUserTrackingService;
            _logger = logger;
            Logger = logger;
        }

        public async Task<IActionResult> Index(string period = "today")
        {
            try
            {
                _logger.LogInformation("GET Dashboard Index - Period: {Period}, Requested by: {CurrentUser}", 
                    period, CurrentUser?.UserName);

                var now = DateTime.UtcNow;
                DateTime startDate;
                DateTime endDate = now;

                // Xác định khoảng thời gian
                switch (period.ToLower())
                {
                    case "today":
                        startDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                        break;
                    case "yesterday":
                        var yesterday = now.AddDays(-1);
                        startDate = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0, DateTimeKind.Utc);
                        endDate = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 23, 59, 59, DateTimeKind.Utc);
                        break;
                    case "week":
                        startDate = now.AddDays(-7);
                        break;
                    case "month":
                        startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        break;
                    case "year":
                        startDate = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        break;
                    default:
                        startDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                        break;
                }

                // Thống kê Users
                var totalUsers = await _userRepository.CountAsync();
                var activeUsers = await _userRepository.CountAsync(u => u.IsActive == true);
                var newUsersInPeriod = await _userRepository.CountAsync(u => 
                    u.CreatedAt >= startDate && u.CreatedAt <= endDate);
                
                // Thống kê theo ngày trong tháng (cho biểu đồ)
                var newUsersByDay = new List<object>();
                if (period == "month")
                {
                    var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                    for (int day = 1; day <= daysInMonth; day++)
                    {
                        var dayStart = new DateTime(now.Year, now.Month, day, 0, 0, 0, DateTimeKind.Utc);
                        var dayEnd = dayStart.AddDays(1).AddSeconds(-1);
                        var count = await _userRepository.CountAsync(u => 
                            u.CreatedAt >= dayStart && u.CreatedAt <= dayEnd);
                        newUsersByDay.Add(new { day = day, count = count });
                    }
                }

                // Thống kê Messages
                var totalMessages = await _messageRepository.CountAsync();
                var messagesInPeriod = await _messageRepository.CountAsync(m => 
                    m.SentAt >= startDate && m.SentAt <= endDate);

                // Thống kê Conversations
                var totalConversations = await _conversationRepository.CountAsync();
                var conversationsInPeriod = await _conversationRepository.CountAsync(c => 
                    c.CreatedAt >= startDate && c.CreatedAt <= endDate);

                // Thống kê Users Online
                var onlineUserCount = _onlineUserTrackingService.GetOnlineUserCount();

                // Thống kê theo giờ trong ngày (cho biểu đồ)
                var messagesByHour = new List<object>();
                if (period == "today")
                {
                    for (int hour = 0; hour < 24; hour++)
                    {
                        var hourStart = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, DateTimeKind.Utc);
                        var hourEnd = hourStart.AddHours(1).AddSeconds(-1);
                        var count = await _messageRepository.CountAsync(m => 
                            m.SentAt >= hourStart && m.SentAt <= hourEnd);
                        messagesByHour.Add(new { hour = hour, count = count });
                    }
                }

                // Thống kê theo tuần trong tháng
                var newUsersByWeek = new List<object>();
                if (period == "month")
                {
                    var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                    var weekNumber = 1;
                    var currentWeekStart = firstDayOfMonth;
                    
                    while (currentWeekStart <= lastDayOfMonth)
                    {
                        var weekEnd = currentWeekStart.AddDays(6);
                        if (weekEnd > lastDayOfMonth) weekEnd = lastDayOfMonth;
                        
                        var count = await _userRepository.CountAsync(u => 
                            u.CreatedAt >= currentWeekStart && u.CreatedAt <= weekEnd.AddDays(1).AddSeconds(-1));
                        newUsersByWeek.Add(new { week = weekNumber, count = count });
                        
                        currentWeekStart = weekEnd.AddDays(1);
                        weekNumber++;
                    }
                }

                ViewBag.Period = period;
                ViewBag.TotalUsers = totalUsers;
                ViewBag.ActiveUsers = activeUsers;
                ViewBag.NewUsersInPeriod = newUsersInPeriod;
                ViewBag.TotalMessages = totalMessages;
                ViewBag.MessagesInPeriod = messagesInPeriod;
                ViewBag.TotalConversations = totalConversations;
                ViewBag.ConversationsInPeriod = conversationsInPeriod;
                ViewBag.OnlineUserCount = onlineUserCount;
                ViewBag.NewUsersByDay = newUsersByDay;
                ViewBag.MessagesByHour = messagesByHour;
                ViewBag.NewUsersByWeek = newUsersByWeek;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;

                _logger.LogInformation("Dashboard data loaded successfully - TotalUsers: {TotalUsers}, OnlineUsers: {OnlineUsers}", 
                    totalUsers, onlineUserCount);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu dashboard.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult GetOnlineUsersCount()
        {
            try
            {
                var count = _onlineUserTrackingService.GetOnlineUserCount();
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online users count");
                return Json(new { success = false, count = 0 });
            }
        }
    }
}

