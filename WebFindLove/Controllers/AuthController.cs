using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebFindLove.Models;
using WebFindLove.Models.Services;
using WebFindLove.Models.Services.RolePermissionService;
using WebFindLove.Models.Services.PasswordResetService;

namespace WebFindLove.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly IRolePermissionService _rolePermissionService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService, 
            IRolePermissionService rolePermissionService,
            IPasswordResetService passwordResetService,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _rolePermissionService = rolePermissionService;
            _passwordResetService = passwordResetService;
            _passwordHasher = new PasswordHasher<User>();
            _logger = logger;
            _logger.LogInformation("AuthController initialized");
        }

        /// <summary>
        /// Helper method to create claims with permissions for authentication
        /// </summary>
        private async Task<List<Claim>> CreateUserClaimsAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? user.RoleName ?? "User")
            };

            // Get user permissions and add to claims
            if (user.RoleId.HasValue)
            {
                var permissionsResponse = await _rolePermissionService.GetUserPermissionsAsync(user.Id);
                if (permissionsResponse.Success && permissionsResponse.Data != null)
                {
                    foreach (var permission in permissionsResponse.Data)
                    {
                        claims.Add(new Claim("Permission", permission));
                    }
                    _logger.LogDebug("Added {Count} permissions to claims for user {Username}", 
                        permissionsResponse.Data.Count, user.UserName);
                }
            }

            return claims;
        }

        public IActionResult Register()
        {
            _logger.LogInformation("GET Register page accessed");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model, string password)
        {
            _logger.LogInformation("POST Register attempt for username: {Username}, email: {Email}", model.UserName, model.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Register validation failed for username: {Username}. Errors: {Errors}", 
                    model.UserName, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Register failed: Password is empty for username: {Username}", model.UserName);
                ModelState.AddModelError("Password", "Password is required");
                return View(model);
            }

            model.Id = Guid.NewGuid();
            _logger.LogDebug("Generated new user ID: {UserId} for username: {Username}", model.Id, model.UserName);
            
            model.PasswordHash = _passwordHasher.HashPassword(model, password);
            _logger.LogDebug("Password hashed successfully for username: {Username}", model.UserName);

            model.RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            model.RoleName = "User";
            var op = await _userService.AddAsync(model);

            var isAjax = Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            _logger.LogDebug("Register request - isAjax: {IsAjax}, Username: {Username}", isAjax, model.UserName);
            if (isAjax)
            {
                if (op.Success)
                {
                    _logger.LogInformation("User registered successfully via AJAX - Username: {Username}, Email: {Email}, UserId: {UserId}", 
                        model.UserName, model.Email, model.Id);

                    // Create authentication claims with permissions
                    var claims = await CreateUserClaimsAsync(model);

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), authProperties);
                    
                    _logger.LogInformation("User {Username} signed in successfully after registration", model.UserName);
                    return Json(new { success = true, message = "Registration successful" });
                }

                _logger.LogError("User registration failed via AJAX - Username: {Username}, Message: {Message}, ErrorDetails: {ErrorDetails}", 
                    model.UserName, op.Message, op.ErrorDetails);

                // try parse field errors from ErrorDetails
                object? fieldErrors = null;
                if (!string.IsNullOrEmpty(op.ErrorDetails))
                {
                    try
                    {
                        fieldErrors = System.Text.Json.JsonSerializer.Deserialize<object>(op.ErrorDetails);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize error details for username: {Username}", model.UserName);
                    }
                }

                return Json(new { success = false, message = op.Message, fieldErrors });
            }

            if (op.Success)
            {
                _logger.LogInformation("User registered successfully - Username: {Username}, Email: {Email}, UserId: {UserId}", 
                    model.UserName, model.Email, model.Id);

                // Create authentication claims with permissions
                var claims = await CreateUserClaimsAsync(model);

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);
                
                _logger.LogInformation("User {Username} signed in successfully after registration", model.UserName);
                return RedirectToAction("Index", "Home");
            }

            _logger.LogError("User registration failed - Username: {Username}, Message: {Message}, ErrorDetails: {ErrorDetails}", 
                model.UserName, op.Message, op.ErrorDetails);

            if (!string.IsNullOrWhiteSpace(op.Message)) ModelState.AddModelError(string.Empty, op.Message);

            if (!string.IsNullOrEmpty(op.ErrorDetails))
            {
                try
                {
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(op.ErrorDetails!);
                    if (dict != null)
                    {
                        _logger.LogDebug("Parsed error details: {ErrorCount} fields with errors", dict.Count);
                        foreach (var kv in dict)
                        {
                            foreach (var err in kv.Value)
                            {
                                ModelState.AddModelError(kv.Key, err);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse error details, using raw message");
                    ModelState.AddModelError(string.Empty, op.ErrorDetails);
                }
            }

            return View(model);
        }

        public IActionResult Login()
        {
            _logger.LogInformation("GET Login page accessed");
            return View();
        }

        [HttpPost]
        // [ValidateAntiForgeryToken] // Temporarily disabled for debugging
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            _logger.LogInformation("POST Login attempt for username/email: {UsernameOrEmail}", usernameOrEmail);
            
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Login attempt with empty username or password");
                ModelState.AddModelError(string.Empty, "Username and password required");
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Username and password required" });
                }
                return View();
            }

            // find user by username or email
            _logger.LogDebug("Fetching users from database to find matching user");
            var usersResp = await _userService.GetAllAsync();
            var users = usersResp.Data ?? new List<User>();
            _logger.LogDebug("Found {UserCount} users in database", users.Count);
            
            var user = users.Find(u => string.Equals(u.UserName, usernameOrEmail, StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.Email, usernameOrEmail, StringComparison.OrdinalIgnoreCase));

            var isAjax = Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            _logger.LogDebug("Login request - isAjax: {IsAjax}, UsernameOrEmail: {UsernameOrEmail}", isAjax, usernameOrEmail);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for username/email: {UsernameOrEmail}", usernameOrEmail);
                if (isAjax) return Json(new { success = false, message = "Invalid credentials" });
                ModelState.AddModelError(string.Empty, "Invalid credentials");
                return View();
            }
            
            _logger.LogDebug("User found - Username: {Username}, Email: {Email}, IsActive: {IsActive}, Role: {Role}", 
                user.UserName, user.Email, user.IsActive, user.RoleName);

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, password);
            if (verify == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Login failed: Invalid password for user: {Username}", user.UserName);
                if (isAjax) return Json(new { success = false, message = "Invalid credentials" });
                ModelState.AddModelError(string.Empty, "Invalid credentials");
                return View();
            }

            _logger.LogDebug("Password verified successfully for user: {Username}", user.UserName);

            try
            {
                // Create authentication claims with permissions
                var claims = await CreateUserClaimsAsync(user);

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                var userRole = user.Role?.Name ?? user.RoleName ?? "User";
                _logger.LogInformation("User logged in successfully - Username: {Username}, Role: {Role}, UserId: {UserId}", 
                    user.UserName, userRole, user.Id);

                if (isAjax) 
                {
                    return Json(new { success = true, message = "Login successful" });
                }

                if (userRole == "Admin")
                {
                    _logger.LogDebug("Redirecting admin user to Admin dashboard");
                    return RedirectToAction("Index", "Admin");
                }
                else if (userRole == "NhanVien")
                {
                    _logger.LogDebug("Redirecting NhanVien user to NhanVien dashboard");
                    return RedirectToAction("Index", "NhanVien");
                }
                else
                {
                    _logger.LogDebug("Redirecting regular user to Home");
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during login process for user: {UsernameOrEmail}", usernameOrEmail);
                if (isAjax) return Json(new { success = false, message = "An error occurred while logging in. Please try again." });
                ModelState.AddModelError(string.Empty, "An error occurred while logging in. Please try again.");
                return View();
            }
        }

        [HttpGet]
        public IActionResult ValidateSession()
        {
            _logger.LogDebug("Validating session for user");
            // Check if user is authenticated via session
            if (User.Identity?.IsAuthenticated == true)
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                _logger.LogInformation("Session validation successful for user: {Username}", userName);
                return Json(new { 
                    success = true, 
                    message = "Session is valid",
                    user = new {
                        id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                        name = userName,
                        email = User.FindFirst(ClaimTypes.Email)?.Value,
                        role = User.FindFirst(ClaimTypes.Role)?.Value
                    }
                });
            }
            _logger.LogWarning("Session validation failed - User not authenticated");
            return Json(new { success = false, message = "Session is invalid" });
        }

        public async Task<IActionResult> Logout()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            _logger.LogInformation("User logout - Username: {Username}", userName);
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            _logger.LogInformation("User {Username} logged out successfully", userName);
            return RedirectToAction("Index", "Home");
        }

        // ============================
        // Forgot Password Module
        // ============================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            _logger.LogInformation("GET ForgotPassword page accessed");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            _logger.LogInformation("POST ForgotPassword attempt for email: {Email}", email);

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("ForgotPassword failed: Email is empty");
                ModelState.AddModelError("Email", "Vui lòng nhập địa chỉ email");
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Vui lòng nhập địa chỉ email" });
                }
                return View();
            }

            var result = await _passwordResetService.GenerateResetTokenAsync(email);
            
            _logger.LogInformation("ForgotPassword result for email {Email}: Success={Success}, Message={Message}", 
                email, result.Success, result.Message);

            var isAjax = Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            if (isAjax)
            {
                return Json(new { 
                    success = result.Success, 
                    message = result.Message,
                    redirectUrl = result.Success ? Url.Action("ResetPassword", "Auth") : null
                });
            }

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("ResetPassword");
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            _logger.LogInformation("GET ResetPassword page accessed");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            _logger.LogInformation("POST ResetPassword attempt with token");

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                _logger.LogWarning("ResetPassword failed: Missing required fields");
                ModelState.AddModelError(string.Empty, "Vui lòng điền đầy đủ thông tin");
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin" });
                }
                return View();
            }

            if (newPassword != confirmPassword)
            {
                _logger.LogWarning("ResetPassword failed: Passwords don't match");
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp");
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });
                }
                return View();
            }

            if (newPassword.Length < 6)
            {
                _logger.LogWarning("ResetPassword failed: Password too short");
                ModelState.AddModelError(string.Empty, "Mật khẩu phải có ít nhất 6 ký tự");
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });
                }
                return View();
            }

            // Validate token first
            var validateResult = await _passwordResetService.ValidateResetTokenAsync(token);
            if (!validateResult.Success)
            {
                _logger.LogWarning("ResetPassword failed: Invalid token");
                ModelState.AddModelError(string.Empty, validateResult.Message);
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = validateResult.Message });
                }
                return View();
            }

            // Reset password
            var result = await _passwordResetService.ResetPasswordAsync(token, newPassword);
            
            _logger.LogInformation("ResetPassword result: Success={Success}, Message={Message}", 
                result.Success, result.Message);

            var isAjax = Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            if (isAjax)
            {
                return Json(new { 
                    success = result.Success, 
                    message = result.Message,
                    redirectUrl = result.Success ? Url.Action("Login", "Auth") : null
                });
            }

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message + " Vui lòng đăng nhập với mật khẩu mới.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View();
        }
    }
}
