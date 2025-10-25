using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models;
using WebFindLove.Models.Services.PhotoService;
using WebFindLove.Models.Services.PhotoService.Dto;
using WebFindLove.Models.Services.PhotoService.ViewModels;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class PhotosController : BaseController
    {
        private readonly IPhotoService _photoService;
        private readonly ILogger<PhotosController> _logger;

        public PhotosController(IPhotoService photoService, ILogger<PhotosController> logger)
        {
            _photoService = photoService;
            _logger = logger;
            Logger = logger;
            _logger.LogInformation("PhotosController initialized");
        }

        // GET: Photos
        public async Task<IActionResult> Index([FromQuery] PhotoSearch? search)
        {
            _logger.LogInformation("GET Photos Index - User: {Username}, Search: {@Search}", CurrentUser?.UserName, search);

            // If not admin, only show current user's photos
            if (UserRole != "Admin")
            {
                if (search == null) search = new PhotoSearch();
                search.UserId = UserId;
            }

            var response = await _photoService.GetAllAsync(search);
            
            if (!response.Success)
            {
                _logger.LogWarning("Failed to get photos: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }

            ViewData["Search"] = search;
            return View(response.Data ?? new List<PhotoDto>());
        }

        // GET: Photos/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            _logger.LogInformation("GET Photo Details - PhotoId: {PhotoId}, User: {Username}", id, CurrentUser?.UserName);

            var response = await _photoService.GetByIdAsync(id);

            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("Photo not found: {PhotoId}", id);
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            // Check authorization (Admin or photo owner)
            if (UserRole != "Admin" && response.Data.UserId != UserId)
            {
                _logger.LogWarning("Unauthorized access to photo: {PhotoId} by user: {Username}", id, CurrentUser?.UserName);
                TempData["ErrorMessage"] = "You don't have permission to view this photo.";
                return RedirectToAction(nameof(Index));
            }

            return View(response.Data);
        }

        // GET: Photos/Create
        public IActionResult Create()
        {
            _logger.LogInformation("GET Create Photo - User: {Username}", CurrentUser?.UserName);

            var model = new PhotoCreateVM
            {
                UserId = UserId!.Value // Current user
            };

            return View(model);
        }

        // POST: Photos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhotoCreateVM model)
        {
            _logger.LogInformation("POST Create Photo - User: {Username}, IsPrimary: {IsPrimary}", CurrentUser?.UserName, model.IsPrimary);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for create photo");
                return View(model);
            }

            // Ensure user can only create photos for themselves (unless Admin)
            if (UserRole != "Admin")
            {
                model.UserId = UserId!.Value;
            }

            var response = await _photoService.CreateAsync(model, UserId);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to create photo: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
                return View(model);
            }

            _logger.LogInformation("Photo created successfully: {PhotoId}", response.Data?.Id);
            TempData["SuccessMessage"] = "Photo created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Photos/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            _logger.LogInformation("GET Edit Photo - PhotoId: {PhotoId}, User: {Username}", id, CurrentUser?.UserName);

            var response = await _photoService.GetByIdAsync(id);

            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("Photo not found: {PhotoId}", id);
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            // Check authorization
            if (UserRole != "Admin" && response.Data.UserId != UserId)
            {
                _logger.LogWarning("Unauthorized edit attempt on photo: {PhotoId} by user: {Username}", id, CurrentUser?.UserName);
                TempData["ErrorMessage"] = "You don't have permission to edit this photo.";
                return RedirectToAction(nameof(Index));
            }

            var model = new PhotoUpdateVM
            {
                Id = response.Data.Id,
                PhotoUrl = response.Data.PhotoUrl,
                IsPrimary = response.Data.IsPrimary,
                IsActive = response.Data.IsActive,
                Description = response.Data.Description
            };

            return View(model);
        }

        // POST: Photos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PhotoUpdateVM model)
        {
            _logger.LogInformation("POST Edit Photo - PhotoId: {PhotoId}, User: {Username}", id, CurrentUser?.UserName);

            if (id != model.Id)
            {
                _logger.LogWarning("ID mismatch in edit photo: URL={UrlId}, Model={ModelId}", id, model.Id);
                TempData["ErrorMessage"] = "Invalid photo ID.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for edit photo");
                return View(model);
            }

            // Verify ownership
            var existingResponse = await _photoService.GetByIdAsync(id);
            if (!existingResponse.Success || existingResponse.Data == null)
            {
                TempData["ErrorMessage"] = "Photo not found.";
                return RedirectToAction(nameof(Index));
            }

            if (UserRole != "Admin" && existingResponse.Data.UserId != UserId)
            {
                _logger.LogWarning("Unauthorized edit attempt on photo: {PhotoId} by user: {Username}", id, CurrentUser?.UserName);
                TempData["ErrorMessage"] = "You don't have permission to edit this photo.";
                return RedirectToAction(nameof(Index));
            }

            var response = await _photoService.UpdateAsync(model, UserId);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to update photo: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
                return View(model);
            }

            _logger.LogInformation("Photo updated successfully: {PhotoId}", id);
            TempData["SuccessMessage"] = "Photo updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Photos/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("GET Delete Photo confirmation - PhotoId: {PhotoId}, User: {Username}", id, CurrentUser?.UserName);

            var response = await _photoService.GetByIdAsync(id);

            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("Photo not found: {PhotoId}", id);
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            // Check authorization
            if (UserRole != "Admin" && response.Data.UserId != UserId)
            {
                _logger.LogWarning("Unauthorized delete attempt on photo: {PhotoId} by user: {Username}", id, CurrentUser?.UserName);
                TempData["ErrorMessage"] = "You don't have permission to delete this photo.";
                return RedirectToAction(nameof(Index));
            }

            return View(response.Data);
        }

        // POST: Photos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            _logger.LogInformation("POST Delete Photo confirmed - PhotoId: {PhotoId}, User: {Username}", id, CurrentUser?.UserName);

            // Verify ownership
            var existingResponse = await _photoService.GetByIdAsync(id);
            if (!existingResponse.Success || existingResponse.Data == null)
            {
                TempData["ErrorMessage"] = "Photo not found.";
                return RedirectToAction(nameof(Index));
            }

            if (UserRole != "Admin" && existingResponse.Data.UserId != UserId)
            {
                _logger.LogWarning("Unauthorized delete attempt on photo: {PhotoId} by user: {Username}", id, CurrentUser?.UserName);
                TempData["ErrorMessage"] = "You don't have permission to delete this photo.";
                return RedirectToAction(nameof(Index));
            }

            var response = await _photoService.DeleteAsync(id);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to delete photo: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }
            else
            {
                _logger.LogInformation("Photo deleted successfully: {PhotoId}", id);
                TempData["SuccessMessage"] = "Photo deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Photos/SetPrimary/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimary(Guid id)
        {
            _logger.LogInformation("POST SetPrimary Photo - PhotoId: {PhotoId}, User: {Username}", id, CurrentUser?.UserName);

            var response = await _photoService.SetPrimaryPhotoAsync(id, UserId!.Value);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to set primary photo: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }
            else
            {
                _logger.LogInformation("Primary photo set successfully: {PhotoId}", id);
                TempData["SuccessMessage"] = "Primary photo set successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

