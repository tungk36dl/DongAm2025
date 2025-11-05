using System.Diagnostics;
using WebFindLove.Models;
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models.Services;
using WebFindLove.Models.Services.UserService.Dto;

namespace WebFindLove.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;

        public HomeController(ILogger<HomeController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
            _logger.LogInformation("HomeController initialized");
        }

        public async Task<IActionResult> Index(string searchQuery)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var userName = User.Identity?.Name;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            _logger.LogInformation("GET Home Index - IsAuthenticated: {IsAuthenticated}, User: {Username}, Role: {Role}, SearchQuery: {SearchQuery}", 
                isAuthenticated, userName, userRole, searchQuery);
            
            ViewBag.SearchQuery = searchQuery;
            
            // If user is authenticated and there's a search query, search for users by FullName
            if (isAuthenticated && !string.IsNullOrWhiteSpace(searchQuery))
            {
                // Sử dụng hàm SearchByFullNameAsync để tìm kiếm chính xác theo tên đầy đủ
                var response = await _userService.SearchByFullNameAsync(searchQuery, pageSize: 20);
                
                if (response.Success && response.Data != null)
                {
                    // Filter out current user from results
                    var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (Guid.TryParse(currentUserId, out var userId))
                    {
                        ViewBag.SearchResults = response.Data.Where(u => u.Id != userId).ToList();
                    }
                    else
                    {
                        ViewBag.SearchResults = response.Data;
                    }

                    //_logger.LogInformation("SearchByFullName returned {Count} users for query: {Query}",
                        //ViewBag.SearchResults.Count, searchQuery);
                }
                else
                {
                    ViewBag.SearchResults = new List<User>();
                    if (!string.IsNullOrEmpty(response?.Message))
                    {
                        _logger.LogWarning("User search by full name failed: {Message}", response.Message);
                    }
                }
            }
            else
            {
                ViewBag.SearchResults = new List<User>();
            }
            
            return View();
        }

        public IActionResult Privacy()
        {
            _logger.LogInformation("GET Privacy page - User: {Username}", User.Identity?.Name);
            return View();
        }

        // Test Notification Toast
        public IActionResult TestNotifications(string type = "success")
        {
            switch (type.ToLower())
            {
                case "success":
                    TempData["SuccessMessage"] = "Đây là thông báo thành công! Mọi thứ đã hoạt động như mong đợi.";
                    break;
                case "error":
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi! Vui lòng thử lại sau.";
                    break;
                case "info":
                    TempData["InfoMessage"] = "Đây là thông tin quan trọng mà bạn cần biết.";
                    break;
                case "warning":
                    TempData["WarningMessage"] = "Cảnh báo! Hãy cẩn thận với hành động này.";
                    break;
                case "multiple":
                    TempData["SuccessMessage"] = "Thao tác thành công!";
                    TempData["InfoMessage"] = "Hệ thống sẽ tự động lưu sau 5 giây.";
                    break;
                default:
                    TempData["SuccessMessage"] = "Test notification!";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Error page displayed - RequestId: {RequestId}", requestId);
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
