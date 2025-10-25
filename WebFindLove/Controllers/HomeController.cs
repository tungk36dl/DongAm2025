using System.Diagnostics;
using WebFindLove.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebFindLove.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _logger.LogInformation("HomeController initialized");
        }

        public IActionResult Index()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var userName = User.Identity?.Name;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            _logger.LogInformation("GET Home Index - IsAuthenticated: {IsAuthenticated}, User: {Username}, Role: {Role}", 
                isAuthenticated, userName, userRole);
            
            return View();
        }

        public IActionResult Privacy()
        {
            _logger.LogInformation("GET Privacy page - User: {Username}", User.Identity?.Name);
            return View();
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
