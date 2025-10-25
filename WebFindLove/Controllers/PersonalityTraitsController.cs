using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models;
using WebFindLove.Models.Services.PersonalityTraitService;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class PersonalityTraitsController : BaseController
    {
        private readonly IPersonalityTraitService _service;
        private readonly ILogger<PersonalityTraitsController> _logger;

        public PersonalityTraitsController(
            IPersonalityTraitService service,
            ILogger<PersonalityTraitsController> logger)
        {
            _service = service;
            _logger = logger;
            Logger = logger;
            _logger.LogInformation("PersonalityTraitsController initialized");
        }

        // GET: PersonalityTraits - View current user's personality trait
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("GET Personality Traits - User: {Username}", CurrentUser?.UserName);

            var response = await _service.GetByUserIdAsync(UserId!.Value);

            // If no trait exists, create new model
            var model = response.Success && response.Data != null
                ? response.Data
                : new PersonalityTrait { UserId = UserId!.Value };

            return View(model);
        }

        // GET: PersonalityTraits/Edit
        public async Task<IActionResult> Edit()
        {
            _logger.LogInformation("GET Edit Personality Traits - User: {Username}", CurrentUser?.UserName);

            var response = await _service.GetByUserIdAsync(UserId!.Value);

            // If no trait exists, create new model
            var model = response.Success && response.Data != null
                ? response.Data
                : new PersonalityTrait { UserId = UserId!.Value };

            return View(model);
        }

        // POST: PersonalityTraits/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PersonalityTrait model)
        {
            _logger.LogInformation("POST Edit Personality Traits - User: {Username}, MBTI: {MbtiType}",
                CurrentUser?.UserName, model.MbtiType);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for edit personality traits");
                return View(model);
            }

            // Ensure user can only edit their own traits
            model.UserId = UserId!.Value;

            var response = await _service.CreateOrUpdateAsync(model, UserId);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to save personality traits: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
                return View(model);
            }

            _logger.LogInformation("Personality traits saved successfully for user: {Username}", CurrentUser?.UserName);
            TempData["SuccessMessage"] = "Personality traits saved successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}

