using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models;
using WebFindLove.Models.Services.UserPreferenceService;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class UserPreferencesController : BaseController
    {
        private readonly IUserPreferenceService _service;
        private readonly ILogger<UserPreferencesController> _logger;

        public UserPreferencesController(
            IUserPreferenceService service,
            ILogger<UserPreferencesController> logger)
        {
            _service = service;
            _logger = logger;
            Logger = logger;
            _logger.LogInformation("UserPreferencesController initialized");
        }

        // GET: UserPreferences - View current user's preferences
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("GET User Preferences - User: {Username}", CurrentUser?.UserName);

            var response = await _service.GetByUserIdAsync(UserId!.Value);

            // If no preferences exist, create new model
            var model = response.Success && response.Data != null
                ? response.Data
                : new UserPreference { UserId = UserId!.Value };

            return View(model);
        }

        // GET: UserPreferences/Edit - Edit current user's preferences
        public async Task<IActionResult> Edit()
        {
            _logger.LogInformation("GET Edit Preferences - User: {Username}", CurrentUser?.UserName);

            var response = await _service.GetByUserIdAsync(UserId!.Value);

            // If no preferences exist, create new model
            var model = response.Success && response.Data != null
                ? response.Data
                : new UserPreference { UserId = UserId!.Value };

            return View(model);
        }

        // POST: UserPreferences/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserPreference model)
        {
            _logger.LogInformation("POST Edit Preferences - User: {Username}", CurrentUser?.UserName);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for edit preferences");
                return View(model);
            }

            // Ensure user can only edit their own preferences
            model.UserId = UserId!.Value;

            var response = await _service.CreateOrUpdateAsync(model, UserId);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to save preferences: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
                return View(model);
            }

            _logger.LogInformation("Preferences saved successfully for user: {Username}", CurrentUser?.UserName);
            TempData["SuccessMessage"] = "Preferences saved successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}

