using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models;
using WebFindLove.Models.Services;
using WebFindLove.Models.Services.RoleService;
using WebFindLove.Models.Services.UserService.ViewModels;
using WebFindLove.Helper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, IRoleService roleService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _roleService = roleService;
            _passwordHasher = new PasswordHasher<User>();
            _logger = logger;
            _logger.LogInformation("UsersController initialized");
        }
        public async Task<IActionResult> Index([FromQuery] Models.Services.UserService.Dto.UserSearch? search)
        {
            _logger.LogInformation("GET Users Index - Requested by: {CurrentUser}, Search: {@Search}", CurrentUser?.UserName, search);
            
            var resp = await _userService.GetAllAsync(search);
            if (!resp.Success)
            {
                _logger.LogError("Failed to retrieve users list - Message: {Message}, ErrorDetails: {ErrorDetails}", 
                    resp.Message, resp.ErrorDetails);
                ViewBag.ErrorMessage = resp.Message ?? resp.ErrorDetails;
                ViewBag.Search = search ?? new Models.Services.UserService.Dto.UserSearch();
                return View(new List<User>());
            }
            
            _logger.LogInformation("Successfully retrieved {UserCount} users", resp.Data?.Count ?? 0);
            ViewBag.Search = search ?? new Models.Services.UserService.Dto.UserSearch();
            return View(resp.Data ?? new List<User>());
        }

        public async Task<IActionResult> Details(Guid id)
        {
            _logger.LogInformation("GET User Details - UserId: {UserId}, Requested by: {CurrentUser}", id, CurrentUser?.UserName);
            
            var resp = await _userService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null)
            {
                _logger.LogWarning("User not found - UserId: {UserId}", id);
                return NotFound();
            }
            
            _logger.LogDebug("User details retrieved - Username: {Username}, Email: {Email}", resp.Data.UserName, resp.Data.Email);
            return View(resp.Data);
        }

        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("GET Create User page - Requested by: {CurrentUser}", CurrentUser?.UserName);
            await LoadRolesAsync();
            return View(new UserCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateVM model)
        {
            _logger.LogInformation("POST Create User - Username: {Username}, Email: {Email}, Role: {Role}, Requested by: {CurrentUser}", 
                model.UserName, model.Email, model.Role, CurrentUser?.UserName);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("User creation validation failed - Username: {Username}, Errors: {Errors}", 
                    model.UserName, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                await LoadRolesAsync();
                return View(model);
            }

            // Convert ViewModel to User entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,
                IsActive = model.IsActive,
                RoleName = model.Role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Generated new user ID: {UserId} for username: {Username}", user.Id, user.UserName);

            // Hash password
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                _logger.LogDebug("Password hashed successfully for user: {Username}", user.UserName);
            }

            var result = await _userService.AddAsync(user);
            if (!result.Success)
            {
                _logger.LogError("Failed to create user - Username: {Username}, Message: {Message}, ErrorDetails: {ErrorDetails}", 
                    model.UserName, result.Message, result.ErrorDetails);
                ModelState.AddDataResponse(new DataResponse<object> { Success = result.Success, Message = result.Message, ErrorDetails = result.ErrorDetails });
                await LoadRolesAsync();
                return View(model);
            }

            _logger.LogInformation("User created successfully - Username: {Username}, UserId: {UserId}, CreatedBy: {CurrentUser}", 
                user.UserName, user.Id, CurrentUser?.UserName);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            _logger.LogInformation("GET Edit User - UserId: {UserId}, Requested by: {CurrentUser}", id, CurrentUser?.UserName);
            
            var resp = await _userService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null)
            {
                _logger.LogWarning("User not found for edit - UserId: {UserId}", id);
                return NotFound();
            }
            
            _logger.LogDebug("Loaded user for edit - Username: {Username}, Email: {Email}", resp.Data.UserName, resp.Data.Email);
            await LoadRolesAsync();
            return View(resp.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, User user)
        {
            _logger.LogInformation("POST Edit User - UserId: {UserId}, Username: {Username}, Requested by: {CurrentUser}", 
                id, user.UserName, CurrentUser?.UserName);

            if (id != user.Id)
            {
                _logger.LogWarning("User ID mismatch in edit request - URL ID: {UrlId}, Model ID: {ModelId}", id, user.Id);
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("User edit validation failed - UserId: {UserId}, Errors: {Errors}", 
                    id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(user);
            }

            var resp = await _userService.UpdateAsync(user);
            if (!resp.Success)
            {
                _logger.LogError("Failed to update user - UserId: {UserId}, Username: {Username}, Message: {Message}, ErrorDetails: {ErrorDetails}", 
                    id, user.UserName, resp.Message, resp.ErrorDetails);
                ModelState.AddDataResponse(new DataResponse<object> { Success = resp.Success, Message = resp.Message, ErrorDetails = resp.ErrorDetails });
                return View(user);
            }

            _logger.LogInformation("User updated successfully - UserId: {UserId}, Username: {Username}, UpdatedBy: {CurrentUser}", 
                id, user.UserName, CurrentUser?.UserName);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("GET Delete User confirmation - UserId: {UserId}, Requested by: {CurrentUser}", id, CurrentUser?.UserName);
            
            var resp = await _userService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null)
            {
                _logger.LogWarning("User not found for deletion - UserId: {UserId}", id);
                return NotFound();
            }
            
            _logger.LogDebug("Loaded user for deletion confirmation - Username: {Username}", resp.Data.UserName);
            return View(resp.Data);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            _logger.LogInformation("POST Delete User confirmed - UserId: {UserId}, Requested by: {CurrentUser}", id, CurrentUser?.UserName);
            
            var resp = await _userService.DeleteAsync(id);
            if (!resp.Success)
            {
                _logger.LogError("Failed to delete user - UserId: {UserId}, Message: {Message}, ErrorDetails: {ErrorDetails}", 
                    id, resp.Message, resp.ErrorDetails);
                TempData["ErrorMessage"] = resp.Message ?? resp.ErrorDetails;
            }
            else
            {
                _logger.LogInformation("User deleted successfully - UserId: {UserId}, DeletedBy: {CurrentUser}", id, CurrentUser?.UserName);
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRolesAsync()
        {
            _logger.LogDebug("Loading roles for dropdown");
            var rolesResp = await _roleService.GetAllAsync();
            if (rolesResp.Success && rolesResp.Data != null)
            {
                var activeRoles = rolesResp.Data.Where(r => r.IsActive).ToList();
                _logger.LogDebug("Loaded {RoleCount} active roles", activeRoles.Count);
                ViewBag.Roles = new SelectList(activeRoles, "Name", "Name");
            }
            else
            {
                _logger.LogWarning("Failed to load roles - Message: {Message}", rolesResp.Message);
                ViewBag.Roles = new SelectList(new List<Role>(), "Name", "Name");
            }
        }
    }
}
