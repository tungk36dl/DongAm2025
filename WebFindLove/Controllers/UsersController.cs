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

        [Authorize(Roles = "Admin")]
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
            if(id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Id rỗng!";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                var userId = UserId;
                var isAdmin = !string.IsNullOrEmpty(UserRole) ? UserRole.Equals("Admin") : false;
                if(!isAdmin && (id != Guid.Empty && id != userId))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền!";
                    return RedirectToAction("Index", "Home");
                }
                _logger.LogInformation("GET User Details - UserId: {UserId}, Requested by: {CurrentUser}", id, CurrentUser?.UserName);

                var resp = await _userService.GetByIdAsync(id);
                if (!resp.Success || resp.Data == null)
                {
                    _logger.LogWarning("User not found - UserId: {UserId}", id);
                    return NotFound();
                }

                _logger.LogDebug("User details retrieved - Username: {Username}, Email: {Email}", resp.Data.UserName, resp.Data.Email);
                return View(resp.Data);
            }catch(Exception ex)
            {
                return BadRequest();
            }
            
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("GET Create User page - Requested by: {CurrentUser}", CurrentUser?.UserName);
            await LoadRolesAsync();
            return View(new UserCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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

            var isAdmin = !string.IsNullOrEmpty(UserRole) ? UserRole.Equals("Admin") : false;
            if (!isAdmin && id != user.Id)
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

        // ============================
        // Edit Account (Username & Password)
        // ============================
        public async Task<IActionResult> EditAccount()
        {
            if (CurrentUser == null)
            {
                _logger.LogWarning("Unauthorized access attempt to EditAccount");
                return RedirectToAction("Login", "Auth");
            }

            _logger.LogInformation("GET EditAccount - UserId: {UserId}, Requested by: {CurrentUser}", CurrentUser.Id, CurrentUser.UserName);

            var resp = await _userService.GetByIdAsync(CurrentUser.Id);
            if (!resp.Success || resp.Data == null)
            {
                _logger.LogWarning("User not found for edit account - UserId: {UserId}", CurrentUser.Id);
                return NotFound();
            }

            var model = new EditAccountVM
            {
                Id = resp.Data.Id,
                UserName = resp.Data.UserName,
                Email = resp.Data.Email
            };

            _logger.LogDebug("Loaded user for edit account - Username: {Username}, Email: {Email}", resp.Data.UserName, resp.Data.Email);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccount(EditAccountVM model)
        {
            if (CurrentUser == null || model.Id != CurrentUser.Id)
            {
                _logger.LogWarning("Unauthorized edit account attempt - UserId: {UserId}", model.Id);
                return Forbid();
            }

            _logger.LogInformation("POST EditAccount - UserId: {UserId}, Username: {Username}, Requested by: {CurrentUser}",
                model.Id, model.UserName, CurrentUser.UserName);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit account validation failed - UserId: {UserId}, Errors: {Errors}",
                    model.Id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            var result = await _userService.UpdateAccountAsync(model);
            if (!result.Success)
            {
                _logger.LogError("Failed to update account - UserId: {UserId}, Username: {Username}, Message: {Message}, ErrorDetails: {ErrorDetails}",
                    model.Id, model.UserName, result.Message, result.ErrorDetails);
                ModelState.AddDataResponse(new DataResponse<object> { Success = result.Success, Message = result.Message, ErrorDetails = result.ErrorDetails });
                return View(model);
            }

            _logger.LogInformation("Account updated successfully - UserId: {UserId}, Username: {Username}", model.Id, model.UserName);
            TempData["SuccessMessage"] = "Account information updated successfully.";
            
            // Update session if username changed
            if (result.Data != null && result.Data.UserName != CurrentUser.UserName)
            {
                HttpContext.Session.SetObjectAsJson("CurrentUser", result.Data);
            }

            return RedirectToAction(nameof(EditAccount));
        }

        // ============================
        // Edit Profile (Personal Information)
        // ============================
        public async Task<IActionResult> EditProfile()
        {
            if (CurrentUser == null)
            {
                _logger.LogWarning("Unauthorized access attempt to EditProfile");
                return RedirectToAction("Login", "Auth");
            }

            _logger.LogInformation("GET EditProfile - UserId: {UserId}, Requested by: {CurrentUser}", CurrentUser.Id, CurrentUser.UserName);

            var resp = await _userService.GetByIdAsync(CurrentUser.Id);
            if (!resp.Success || resp.Data == null)
            {
                _logger.LogWarning("User not found for edit profile - UserId: {UserId}", CurrentUser.Id);
                return NotFound();
            }

            var model = new EditProfileVM
            {
                Id = resp.Data.Id,
                FullName = resp.Data.FullName,
                PhoneNumber = resp.Data.PhoneNumber,
                Gender = resp.Data.Gender,
                DateOfBirth = resp.Data.DateOfBirth,
                Height = resp.Data.Height,
                Location = resp.Data.Location,
                Hometown = resp.Data.Hometown,
                Bio = resp.Data.Bio,
                Interests = resp.Data.Interests,
                PersonalityType = resp.Data.PersonalityType,
                PersonalityText = resp.Data.PersonalityText,
                Avatar = resp.Data.Avatar
            };

            _logger.LogDebug("Loaded user for edit profile - FullName: {FullName}", resp.Data.FullName);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileVM model)
        {
            try
            {
                if (CurrentUser == null || model.Id != CurrentUser.Id)
                {
                    _logger.LogWarning("Unauthorized edit profile attempt - UserId: {UserId}", model.Id);
                    return Forbid();
                }

                _logger.LogInformation("POST EditProfile - UserId: {UserId}, FullName: {FullName}, Requested by: {CurrentUser}",
                    model.Id, model.FullName, CurrentUser.UserName);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit profile validation failed - UserId: {UserId}, Errors: {Errors}",
                        model.Id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return View(model);
                }

                var result = await _userService.UpdateProfileAsync(model);
                if (!result.Success)
                {
                    _logger.LogError("Failed to update profile - UserId: {UserId}, Message: {Message}, ErrorDetails: {ErrorDetails}",
                        model.Id, result.Message, result.ErrorDetails);
                    ModelState.AddDataResponse(new DataResponse<object> { Success = result.Success, Message = result.Message, ErrorDetails = result.ErrorDetails });
                    return View(model);
                }

                _logger.LogInformation("Profile updated successfully - UserId: {UserId}", model.Id);
                TempData["SuccessMessage"] = "Profile information updated successfully.";

                // Update session with new user data
                if (result.Data != null)
                {
                    //HttpContext.Session.SetObjectAsJson("CurrentUser", result.Data);
                }

                return RedirectToAction(nameof(EditProfile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating profile - UserId: {UserId}", model.Id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }
    }
}
