using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WebFindLove.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
            _logger.LogInformation("AdminController initialized");
        }

        public IActionResult Index()
        {
            var userName = User.Identity?.Name;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            _logger.LogInformation("GET Admin Dashboard - AdminUser: {Username}, UserId: {UserId}", userName, userId);
            
            // Kiểm tra quyền Admin
            if (User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Unauthenticated user attempted to access Admin dashboard");
                return RedirectToAction("Login", "Auth");
            }

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                _logger.LogWarning("Non-admin user attempted to access Admin dashboard - User: {Username}, Role: {Role}", 
                    userName, userRole);
                return RedirectToAction("Index", "Home");
            }

            _logger.LogDebug("Admin dashboard accessed successfully by: {Username}", userName);
            return View();
        }
    }
}
