using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models;
using WebFindLove.Models.Services;
using WebFindLove.Models.Services.RoleService;
using WebFindLove.Models.Services.RoleService.Dto;
using WebFindLove.Models.Services.RoleService.ViewModels;
using WebFindLove.Helper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class RolesController : BaseController
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IRoleService roleService, ILogger<RolesController> logger)
        {
            _roleService = roleService;
            _logger = logger;
            _logger.LogInformation("RolesController initialized");
        }

        public async Task<IActionResult> Index([FromQuery] RoleSearch? search)
        {
            _logger.LogInformation("GET Roles Index - Search: {@Search}", search);
            
            var resp = await _roleService.GetAllAsync(search);
            if (!resp.Success)
            {
                _logger.LogError("Failed to retrieve roles list - Message: {Message}, ErrorDetails: {ErrorDetails}", 
                    resp.Message, resp.ErrorDetails);
                ViewBag.ErrorMessage = resp.Message ?? resp.ErrorDetails;
                ViewBag.Search = search ?? new RoleSearch();
                return View(new List<Role>());
            }
            
            _logger.LogInformation("Successfully retrieved {RoleCount} roles", resp.Data?.Count ?? 0);
            ViewBag.Search = search ?? new RoleSearch();
            return View(resp.Data ?? new List<Role>());
        }

        public async Task<IActionResult> Details(Guid id)
        {
            _logger.LogInformation("GET Role Details - RoleId: {RoleId}", id);
            
            var resp = await _roleService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null)
            {
                _logger.LogWarning("Role not found - RoleId: {RoleId}", id);
                return NotFound();
            }
            
            _logger.LogDebug("Role details retrieved - RoleName: {RoleName}, UserCount: {UserCount}", 
                resp.Data.Name, resp.Data.Users?.Count ?? 0);
            return View(resp.Data);
        }

        public IActionResult Create()
        {
            _logger.LogInformation("GET Create Role page");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleCreateVM model)
        {
            _logger.LogInformation("POST Create Role - RoleName: {RoleName}, IsActive: {IsActive}", model.Name, model.IsActive);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Role creation validation failed - RoleName: {RoleName}, Errors: {Errors}", 
                    model.Name, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            var role = new Role
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = model.IsActive
            };

            var result = await _roleService.AddAsync(role);
            if (!result.Success)
            {
                _logger.LogError("Failed to create role - RoleName: {RoleName}, Message: {Message}, ErrorDetails: {ErrorDetails}", 
                    model.Name, result.Message, result.ErrorDetails);
                ModelState.AddDataResponse(new DataResponse<object> { Success = result.Success, Message = result.Message, ErrorDetails = result.ErrorDetails });
                return View(model);
            }

            _logger.LogInformation("Role created successfully - RoleName: {RoleName}, RoleId: {RoleId}", 
                role.Name, role.Id);
            TempData["SuccessMessage"] = "Role created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            _logger.LogInformation("GET Edit Role - RoleId: {RoleId}", id);
            
            var resp = await _roleService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null)
            {
                _logger.LogWarning("Role not found for edit - RoleId: {RoleId}", id);
                return NotFound();
            }

            _logger.LogDebug("Loaded role for edit - RoleName: {RoleName}", resp.Data.Name);
            var model = new RoleUpdateVM
            {
                Id = resp.Data.Id,
                Name = resp.Data.Name,
                Description = resp.Data.Description,
                IsActive = resp.Data.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, RoleUpdateVM model)
        {
            _logger.LogInformation("POST Edit Role - RoleId: {RoleId}, RoleName: {RoleName}", id, model.Name);

            if (id != model.Id)
            {
                _logger.LogWarning("Role ID mismatch in edit request - URL ID: {UrlId}, Model ID: {ModelId}", id, model.Id);
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Role edit validation failed - RoleId: {RoleId}, Errors: {Errors}", 
                    id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            var role = new Role
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                IsActive = model.IsActive
            };

            var resp = await _roleService.UpdateAsync(role);
            if (!resp.Success)
            {
                _logger.LogError("Failed to update role - RoleId: {RoleId}, RoleName: {RoleName}, Message: {Message}, ErrorDetails: {ErrorDetails}", 
                    id, model.Name, resp.Message, resp.ErrorDetails);
                ModelState.AddDataResponse(new DataResponse<object> { Success = resp.Success, Message = resp.Message, ErrorDetails = resp.ErrorDetails });
                return View(model);
            }

            _logger.LogInformation("Role updated successfully - RoleId: {RoleId}, RoleName: {RoleName}", id, model.Name);
            TempData["SuccessMessage"] = "Role updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("GET Delete Role confirmation - RoleId: {RoleId}", id);
            
            var resp = await _roleService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null)
            {
                _logger.LogWarning("Role not found for deletion - RoleId: {RoleId}", id);
                return NotFound();
            }
            
            _logger.LogDebug("Loaded role for deletion confirmation - RoleName: {RoleName}, UserCount: {UserCount}", 
                resp.Data.Name, resp.Data.Users?.Count ?? 0);
            return View(resp.Data);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            _logger.LogInformation("POST Delete Role confirmed - RoleId: {RoleId}", id);
            
            var resp = await _roleService.DeleteAsync(id);
            if (!resp.Success)
            {
                _logger.LogError("Failed to delete role - RoleId: {RoleId}, Message: {Message}, ErrorDetails: {ErrorDetails}", 
                    id, resp.Message, resp.ErrorDetails);
                TempData["ErrorMessage"] = resp.Message ?? resp.ErrorDetails;
            }
            else
            {
                _logger.LogInformation("Role deleted successfully - RoleId: {RoleId}", id);
                TempData["SuccessMessage"] = "Role deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckNameExists(string name, Guid? excludeId = null)
        {
            _logger.LogDebug("Checking if role name exists - Name: {RoleName}, ExcludeId: {ExcludeId}", name, excludeId);
            
            var resp = await _roleService.IsNameExistsAsync(name, excludeId);
            if (resp.Success)
            {
                _logger.LogDebug("Role name check result - Name: {RoleName}, Exists: {Exists}", name, resp.Data);
                return Json(new { exists = resp.Data });
            }
            
            _logger.LogWarning("Role name check failed - Name: {RoleName}, Message: {Message}", name, resp.Message);
            return Json(new { exists = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetRolesWithUserCount()
        {
            _logger.LogDebug("Getting roles with user count");
            
            var resp = await _roleService.GetAllWithUserCountAsync();
            if (resp.Success)
            {
                _logger.LogInformation("Successfully retrieved {RoleCount} roles with user counts", resp.Data?.Count ?? 0);
                return Json(new { success = true, data = resp.Data });
            }
            
            _logger.LogError("Failed to retrieve roles with user count - Message: {Message}", resp.Message);
            return Json(new { success = false, message = resp.Message });
        }
    }
}
