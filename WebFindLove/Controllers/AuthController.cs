using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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
            // Skip for Admin role (handled by PermissionAuthorizeAttribute)
            var roleName = user.Role?.Name ?? user.RoleName ?? "User";
            if (user.RoleId.HasValue && !string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
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
            else if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Admin user {Username}, skipping permission claims (handled by attribute)", user.UserName);
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

        // ============================
        // Google OAuth2 Authentication
        // ============================

        /// <summary>
        /// Initiates Google OAuth2 login
        /// </summary>
        //public async Task GoogleLogin()
        //{
        //    _logger.LogInformation("Initiating Google OAuth login");
        //    await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        //    {
        //        RedirectUri = "/Auth/GoogleCallback"
        //    });
        //}
        public IActionResult GoogleLogin()
        {
            _logger.LogInformation("Initiating Google OAuth login");
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleCallback", "Auth") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }



        /// <summary>
        /// Handles Google OAuth2 callback
        /// </summary>
        public async Task<IActionResult> GoogleCallback()
        {
            _logger.LogInformation("Google OAuth callback received");
            
            try
            {
                // var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Google authentication failed");
                    TempData["ErrorMessage"] = "Đăng nhập bằng Google thất bại. Vui lòng thử lại.";
                    return RedirectToAction("Login");
                }

                var claims = result.Principal.Claims.ToList();
                var googleId = claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var email = claims.FirstOrDefault(c =>
                    c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
                var name = claims.FirstOrDefault(c => c.Type == "name")?.Value;
                var picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

                _logger.LogInformation("Google user authenticated - Email: {Email}, Name: {Name}", email, name);

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Google authentication failed - Email is null");
                    TempData["ErrorMessage"] = "Không thể lấy thông tin email từ Google. Vui lòng thử lại.";
                    return RedirectToAction("Login");
                }

                // Check if user exists in database
                var usersResp = await _userService.GetAllAsync();
                var users = usersResp.Data ?? new List<User>();
                var existingUser = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (existingUser != null)
                {
                    _logger.LogInformation("Existing user found for Google login - Email: {Email}", email);
                    
                    // Check if account is active
                    if (!existingUser.IsActive)
                    {
                        _logger.LogWarning("Google login failed - Account is disabled");
                        TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ admin.";
                        return RedirectToAction("Login");
                    }

                    // Sign in with existing user
                    var userClaims = await CreateUserClaimsAsync(existingUser);
                    var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    var userRole = existingUser.Role?.Name ?? existingUser.RoleName ?? "User";
                    _logger.LogInformation("User logged in successfully via Google - Email: {Email}, Role: {Role}", email, userRole);

                    // Redirect based on role
                    if (userRole == "Admin")
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (userRole == "NhanVien")
                    {
                        return RedirectToAction("Index", "NhanVien");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    _logger.LogInformation("New Google user - Creating account - Email: {Email}", email);
                    
                    // Create new user from Google account
                    var newUser = new User
                    {
                        Id = Guid.NewGuid(),
                        UserName = email.Split('@')[0] + "_" + Guid.NewGuid().ToString().Substring(0, 3), // Generate unique username
                        Email = email,
                        FullName = name ?? "User",
                        IsActive = true,
                        RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), // User role
                        RoleName = "User",
                        Avatar = picture, // Save Google profile picture
                        PasswordHash = null, // No password for OAuth users
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var addResult = await _userService.AddAsync(newUser);
                    
                    if (addResult.Success && addResult.Data != null)
                    {
                        _logger.LogInformation("New user created successfully via Google - Email: {Email}", email);
                        
                        // Sign in the new user
                        var userClaims = await CreateUserClaimsAsync(addResult.Data);
                        var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                            new ClaimsPrincipal(claimsIdentity), authProperties);

                        _logger.LogInformation("New user logged in successfully via Google - Email: {Email}", email);
                        TempData["SuccessMessage"] = "Đăng ký và đăng nhập thành công bằng Google!";
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        _logger.LogError("Failed to create new user via Google - Email: {Email}, Message: {Message}", 
                            email, addResult.Message);
                        TempData["ErrorMessage"] = "Không thể tạo tài khoản. Vui lòng thử lại hoặc liên hệ admin.";
                        return RedirectToAction("Login");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during Google OAuth callback");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng nhập bằng Google. Vui lòng thử lại.";
                return RedirectToAction("Login");
            }
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

            // find user by username or email (optimized query)
            _logger.LogDebug("Querying database for user: {UsernameOrEmail}", usernameOrEmail);
            var userResp = await _userService.FindByUsernameOrEmailAsync(usernameOrEmail);
            var user = userResp.Success ? userResp.Data : null;

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

            // Check if user is OAuth-only user (no password)
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("Login failed: User {Username} is OAuth-only, cannot login with password", user.UserName);
                if (isAjax) return Json(new { success = false, message = "Tài khoản này đăng nhập bằng Google. Vui lòng dùng nút 'Đăng nhập bằng Google'." });
                ModelState.AddModelError(string.Empty, "Tài khoản này đăng nhập bằng Google. Vui lòng dùng nút 'Đăng nhập bằng Google'.");
                return View();
            }

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
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
                return RedirectToAction("VerifyToken");
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View();
        }

        [HttpGet]
        public IActionResult VerifyToken()
        {
            _logger.LogInformation("GET VerifyToken page accessed");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyToken(string token)
        {
            _logger.LogInformation("POST VerifyToken attempt with token");

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("VerifyToken failed: Token is empty");
                ModelState.AddModelError(string.Empty, "Vui lòng nhập mã xác nhận");
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã xác nhận" });
                }
                return View();
            }

            if (token.Length != 6)
            {
                _logger.LogWarning("VerifyToken failed: Invalid token length");
                ModelState.AddModelError(string.Empty, "Mã xác nhận phải có đúng 6 chữ số");
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Mã xác nhận phải có đúng 6 chữ số" });
                }
                return View();
            }

            // Validate token
            var validateResult = await _passwordResetService.ValidateResetTokenAsync(token);
            
            var isAjax = Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            if (!validateResult.Success)
            {
                _logger.LogWarning("VerifyToken failed: {Message}", validateResult.Message);
                ModelState.AddModelError(string.Empty, validateResult.Message ?? "Mã xác nhận không hợp lệ");
                
                if (isAjax)
                {
                    return Json(new { success = false, message = validateResult.Message ?? "Mã xác nhận không hợp lệ" });
                }
                return View();
            }

            // Token hợp lệ, lưu vào TempData để dùng ở bước tiếp theo
            TempData["ResetToken"] = token;
            TempData["SuccessMessage"] = "Mã xác nhận hợp lệ. Vui lòng nhập mật khẩu mới.";
            
            _logger.LogInformation("Token verified successfully");
            
            if (isAjax)
            {
                return Json(new { 
                    success = true, 
                    message = "Mã xác nhận hợp lệ",
                    redirectUrl = Url.Action("ResetPassword", "Auth")
                });
            }

            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            _logger.LogInformation("GET ResetPassword page accessed");
            
            // Kiểm tra xem có token đã được verify chưa
            if (TempData["ResetToken"] == null)
            {
                _logger.LogWarning("ResetPassword accessed without verified token, redirecting to VerifyToken");
                TempData["ErrorMessage"] = "Vui lòng xác thực mã trước khi đặt lại mật khẩu.";
                return RedirectToAction("VerifyToken");
            }

            // Giữ token trong TempData cho lần submit
            TempData.Keep("ResetToken");
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
        {
            _logger.LogInformation("POST ResetPassword attempt");

            // Lấy token từ TempData
            var token = TempData["ResetToken"] as string;
            
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("ResetPassword failed: No verified token found");
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Phiên làm việc hết hạn. Vui lòng thử lại.", redirectUrl = Url.Action("VerifyToken", "Auth") });
                }
                
                TempData["ErrorMessage"] = "Phiên làm việc hết hạn. Vui lòng xác thực mã lại.";
                return RedirectToAction("VerifyToken");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                _logger.LogWarning("ResetPassword failed: Missing password fields");
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ mật khẩu");
                
                // Giữ token cho lần submit tiếp theo
                TempData["ResetToken"] = token;
                TempData.Keep("ResetToken");
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ mật khẩu" });
                }
                return View();
            }

            if (newPassword != confirmPassword)
            {
                _logger.LogWarning("ResetPassword failed: Passwords don't match");
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp");
                
                // Giữ token cho lần submit tiếp theo
                TempData["ResetToken"] = token;
                TempData.Keep("ResetToken");
                
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
                
                // Giữ token cho lần submit tiếp theo
                TempData["ResetToken"] = token;
                TempData.Keep("ResetToken");
                
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });
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

            // Nếu thất bại, giữ token để thử lại
            TempData["ResetToken"] = token;
            TempData.Keep("ResetToken");
            
            ModelState.AddModelError(string.Empty, result.Message);
            return View();
        }
    }
}
