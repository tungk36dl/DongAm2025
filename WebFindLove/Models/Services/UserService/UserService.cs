using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebFindLove.Models.Repositories.UserRepo;
using WebFindLove.Models.Services.UserService.Dto;
using WebFindLove.Models.Services.UserService.ViewModels;
using WebFindLove.Models.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using WebFindLove.Models.Services.FileUploadService;
using WebFindLove.Models.Services.FileUploadService.Dto;
using WebFindLove.Models.Services.EmbeddingService;
using WebFindLove.Helper.HelperServices;

namespace WebFindLove.Models.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        //private readonly IGenericRepository<User, Guid> _userRepository;
        private readonly IUserRepository _userRepository;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IFileUploadService _fileUploadService;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<UserService> _logger;
        private readonly IUrlHelperService _urlHelperService;


        public UserService(
            IUnitOfWork unitOfWork, 
            IUserRepository userRepository, 
            IFileUploadService fileUploadService, 
            IEmbeddingService embeddingService,
            IUrlHelperService urlHelperService,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
            _passwordHasher = new PasswordHasher<User>();
            _fileUploadService = fileUploadService;
            _embeddingService = embeddingService;
            _urlHelperService = urlHelperService;
            _logger = logger;
        }

        public async Task<DataResponse<List<User>>> GetAllAsync(UserSearch? search = null)
        {
            try
            {
                IQueryable<User> query = _userRepository.FindAll(null, r => r.Role);
                
                if (search != null)
                {
                    // Support both Keyword (from SearchBase) and Query
                    var searchTerm = !string.IsNullOrWhiteSpace(search.Query) 
                        ? search.Query 
                        : (!string.IsNullOrWhiteSpace(search.Keyword) ? search.Keyword : null);
                    
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        var qstr = searchTerm.Trim();
                        query = query.Where(u => (u.UserName != null && u.UserName.Contains(qstr))
                                                || (u.Email != null && u.Email.Contains(qstr))
                                                || (u.FullName != null && u.FullName.Contains(qstr)));
                    }

                    // Filter by Role
                    if (!string.IsNullOrWhiteSpace(search.Role))
                    {
                        query = query.Where(u => u.RoleName == search.Role);
                    }

                    if (search.IsActive.HasValue)
                        query = query.Where(u => u.IsActive == search.IsActive.Value);

                    // paging
                    var skip = (Math.Max(1, search.Page) - 1) * Math.Max(1, search.PageSize);
                    query = query.Skip(skip).Take(Math.Max(1, search.PageSize));
                }

                var data = await query.ToListAsync();

                // Không normalize avatar ở đây - để Controller xử lý khi trả về view
                // Avatar trong DB đã có format: uploads/avatars/filename.jpg

                return new DataResponse<List<User>> { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                return new DataResponse<List<User>> { Success = false, Message = "Failed to get users.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<int>> GetCountAsync(UserSearch? search = null)
        {
            try
            {
                IQueryable<User> query = _userRepository.FindAll(null, r => r.Role);
                
                if (search != null)
                {
                    // Support both Keyword (from SearchBase) and Query
                    var searchTerm = !string.IsNullOrWhiteSpace(search.Query) 
                        ? search.Query 
                        : (!string.IsNullOrWhiteSpace(search.Keyword) ? search.Keyword : null);
                    
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        var qstr = searchTerm.Trim();
                        query = query.Where(u => (u.UserName != null && u.UserName.Contains(qstr))
                                                || (u.Email != null && u.Email.Contains(qstr))
                                                || (u.FullName != null && u.FullName.Contains(qstr)));
                    }

                    // Filter by Role
                    if (!string.IsNullOrWhiteSpace(search.Role))
                    {
                        query = query.Where(u => u.RoleName == search.Role);
                    }

                    if (search.IsActive.HasValue)
                        query = query.Where(u => u.IsActive == search.IsActive.Value);
                }

                var count = await query.CountAsync();
                return new DataResponse<int> { Success = true, Data = count };
            }
            catch (Exception ex)
            {
                return new DataResponse<int> { Success = false, Message = "Failed to get user count.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<User?>> GetByIdAsync(Guid id)
        {
            try
            {
                var u = await _userRepository.FindByIdAsync(id, r => r.Role);
                // Không normalize avatar ở đây - để Controller xử lý khi trả về view
                // Avatar trong DB đã có format: uploads/avatars/filename.jpg
                return new DataResponse<User?> { Success = true, Data = u };
            }
            catch (Exception ex)
            {
                return new DataResponse<User?> { Success = false, Message = $"Failed to get user by id: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<User?>> FindByUsernameOrEmailAsync(string usernameOrEmail)
        {
            try
            {
                var user = await _userRepository.FindByUsernameOrEmailAsync(usernameOrEmail);
                // Không normalize avatar ở đây - để Controller xử lý khi trả về view
                // Avatar trong DB đã có format: uploads/avatars/filename.jpg
                return new DataResponse<User?> { Success = true, Data = user };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding user by username/email: {UsernameOrEmail}", usernameOrEmail);
                return new DataResponse<User?> { Success = false, Message = $"Failed to find user by username/email: {usernameOrEmail}", ErrorDetails = ex.Message };
            }
        }

        /// <summary>
        /// Tìm kiếm người dùng chính xác theo FullName (case-insensitive exact match)
        /// </summary>
        /// <param name="fullName">Tên đầy đủ cần tìm kiếm</param>
        /// <param name="pageSize">Số lượng kết quả tối đa (mặc định 20)</param>
        /// <returns>Danh sách người dùng có FullName khớp chính xác</returns>
        public async Task<DataResponse<List<User>>> SearchByFullNameAsync(string fullName, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    return new DataResponse<List<User>> 
                    { 
                        Success = false, 
                        Message = "Tên đầy đủ không được để trống.",
                        Data = new List<User>()
                    };
                }

                var searchTerm = fullName.Trim();
                IQueryable<User> query = _userRepository.FindAll(null, r => r.Role);

                // Tìm kiếm chính xác theo FullName (case-insensitive)
                // Sử dụng EF.Functions để hỗ trợ case-insensitive comparison
                query = query.Where(u => u.FullName != null && 
                                         u.FullName.ToLower() == searchTerm.ToLower());

                // Chỉ lấy user đang active
                query = query.Where(u => u.IsActive == true);

                // Giới hạn số lượng kết quả
                if (pageSize > 0)
                {
                    query = query.Take(pageSize);
                }

                var data = await query.ToListAsync();

                // Không normalize avatar ở đây - để Controller xử lý khi trả về view
                // Avatar trong DB đã có format: uploads/avatars/filename.jpg

                _logger.LogInformation("SearchByFullName found {Count} users for FullName: {FullName}", 
                    data.Count, searchTerm);

                return new DataResponse<List<User>> { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users by full name: {FullName}", fullName);
                return new DataResponse<List<User>> 
                { 
                    Success = false, 
                    Message = "Đã xảy ra lỗi khi tìm kiếm người dùng theo tên đầy đủ.", 
                    ErrorDetails = ex.Message,
                    Data = new List<User>()
                };
            }
        }

        public async Task<DataResponse<UserDto>> GetInfoAsync(Guid id)
        {
            try
            {
                var u = await _userRepository.FindByIdAsync(id, r => r.Role);
                var userDto = new UserDto()
                {
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Gender = u.Gender,
                    Hometown = u.Hometown,
                    // Avatar trong DB đã có format: uploads/avatars/filename.jpg
                    // Controller sẽ dùng GetUrl() để convert thành full URL khi trả về view
                    Avatar = u.Avatar,
                    DateOfBirth = u.DateOfBirth,
                    Bio = u.Bio,
                    Occupation = u.Occupation,
                    Location = u.Location,
                    Height = u.Height

                };
                return new DataResponse<UserDto> { Success = true, Data = userDto };
            }
            catch (Exception ex)
            {
                return new DataResponse<UserDto> { Success = false, Message = $"Failed to get user by id: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<User>> AddAsync(User user)
        {
            if (user == null)
            {
                return new DataResponse<User> { Success = false, Message = "User is required." };
            }

            try
            {
                // Validate data annotations on User
                var ctx = new ValidationContext(user);
                Validator.ValidateObject(user, ctx, validateAllProperties: true);

                // Check uniqueness of UserName and Email
                var fieldErrors = new Dictionary<string, List<string>>();
                if (!string.IsNullOrWhiteSpace(user.UserName))
                {
                    var existsUserName = await _userRepository.AnyAsync(u => u.UserName == user.UserName);
                    if (existsUserName)
                    {
                        fieldErrors.TryAdd(nameof(user.UserName), new List<string>());
                        fieldErrors[nameof(user.UserName)].Add("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var existsEmail = await _userRepository.AnyAsync(u => u.Email == user.Email);
                    if (existsEmail)
                    {
                        fieldErrors.TryAdd(nameof(user.Email), new List<string>());
                        fieldErrors[nameof(user.Email)].Add("Email đã được sử dụng. Vui lòng sử dụng email khác.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<User>
                    {
                        Success = false,
                        Message = "Có lỗi xảy ra khi đăng ký. Vui lòng kiểm tra lại thông tin.",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                // Normalize avatar path để đảm bảo có format uploads/avatars/filename.jpg khi lưu vào DB
                user.Avatar = NormalizeAvatarPathForDb(user.Avatar);

                _userRepository.Add(user);
                await _unitOfWork.SaveChangesAsync();
                
                return new DataResponse<User> { Success = true, Data = user };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<User> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                return new DataResponse<User> { Success = false, Message = "Failed to add user.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<User>> UpdateAsync(User user)
        {
            if (user == null)
            {
                return new DataResponse<User> { Success = false, Message = "User is required." };
            }

            try
            {
                var ctx = new ValidationContext(user);
                Validator.ValidateObject(user, ctx, validateAllProperties: true);

                var fieldErrors = new Dictionary<string, List<string>>();
                // uniqueness excluding current user
                if (!string.IsNullOrWhiteSpace(user.UserName))
                {
                    var existsUserName = await _userRepository.AnyAsync(u => u.Id != user.Id && u.UserName == user.UserName);
                    if (existsUserName)
                    {
                        fieldErrors.TryAdd(nameof(user.UserName), new List<string>());
                        fieldErrors[nameof(user.UserName)].Add("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var existsEmail = await _userRepository.AnyAsync(u => u.Id != user.Id && u.Email == user.Email);
                    if (existsEmail)
                    {
                        fieldErrors.TryAdd(nameof(user.Email), new List<string>());
                        fieldErrors[nameof(user.Email)].Add("Email đã được sử dụng. Vui lòng sử dụng email khác.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<User>
                    {
                        Success = false,
                        Message = "Có lỗi xảy ra khi cập nhật. Vui lòng kiểm tra lại thông tin.",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                // Normalize avatar path để đảm bảo có format uploads/avatars/filename.jpg khi lưu vào DB
                user.Avatar = NormalizeAvatarPathForDb(user.Avatar);
                
                user.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                
                return new DataResponse<User> { Success = true, Data = user };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<User> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                return new DataResponse<User> { Success = false, Message = "Failed to update user.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                var user = await _userRepository.FindByIdAsync(id);
                if (user == null)
                {
                    return new DataResponse<object> { Success = false, Message = "User not found." };
                }

                _userRepository.Remove(user);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<object> { Success = true, Data = null };
            }
            catch (Exception ex)
            {
                return new DataResponse<object> { Success = false, Message = $"Failed to delete user: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<User>> UpdateAccountAsync(EditAccountVM model)
        {
            if (model == null)
            {
                return new DataResponse<User> { Success = false, Message = "Model is required." };
            }

            try
            {
                var user = await _userRepository.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return new DataResponse<User> { Success = false, Message = "User not found." };
                }

                var fieldErrors = new Dictionary<string, List<string>>();

                // Check username uniqueness
                if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName != user.UserName)
                {
                    var existsUserName = await _userRepository.AnyAsync(u => u.Id != model.Id && u.UserName == model.UserName);
                    if (existsUserName)
                    {
                        fieldErrors.TryAdd(nameof(model.UserName), new List<string>());
                        fieldErrors[nameof(model.UserName)].Add("Username already exists.");
                    }
                }

                // Check email uniqueness
                if (!string.IsNullOrWhiteSpace(model.Email) && model.Email != user.Email)
                {
                    var existsEmail = await _userRepository.AnyAsync(u => u.Id != model.Id && u.Email == model.Email);
                    if (existsEmail)
                    {
                        fieldErrors.TryAdd(nameof(model.Email), new List<string>());
                        fieldErrors[nameof(model.Email)].Add("Email already exists.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<User>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                // Update account information
                user.UserName = model.UserName;
                user.Email = model.Email;

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
                }

                // Normalize avatar path để đảm bảo có format uploads/avatars/filename.jpg khi lưu vào DB
                user.Avatar = NormalizeAvatarPathForDb(user.Avatar);
                
                user.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return new DataResponse<User> { Success = true, Data = user };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<User> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                return new DataResponse<User> { Success = false, Message = "Failed to update account.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<User>> UpdateProfileAsync(EditProfileVM model)
        {
            if (model == null)
            {
                return new DataResponse<User> { Success = false, Message = "Model is required." };
            }

            try
            {
                var user = await _userRepository.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return new DataResponse<User> { Success = false, Message = "User not found." };
                }

                // Check if user has free profile updates left
                if (user.FreeProfileUpdatesLeft == null || user.FreeProfileUpdatesLeft <= 0)
                {
                    _logger.LogWarning("User {UserId} has no free profile updates left", model.Id);
                    return new DataResponse<User> 
                    { 
                        Success = false, 
                        Message = "Bạn đã hết số lần cập nhật profile miễn phí. Vui lòng liên hệ admin để được hỗ trợ." 
                    };
                }

                // Update profile information
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Gender = model.Gender;
                user.DateOfBirth = model.DateOfBirth;
                user.Height = model.Height;
                user.Location = model.Location;
                user.Hometown = model.Hometown;
                user.Bio = model.Bio;
                user.Interests = model.Interests;
                user.PersonalityType = model.PersonalityType;
                user.PersonalityText = model.PersonalityText;

                // Handle avatar upload using FileUploadService
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    _logger.LogInformation("Uploading avatar for user {UserId}", model.Id);

                    // Configure upload options
                    var uploadOptions = new FileUploadOptions
                    {
                        SubDirectory = "avatars",
                        AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
                        MaxFileSize = 5 * 1024 * 1024, // 5MB
                        GenerateUniqueFileName = true
                    };

                    // Upload new avatar
                    var uploadResult = await _fileUploadService.UploadFileAsync(model.AvatarFile, uploadOptions);
                    
                    if (!uploadResult.Success)
                    {
                        _logger.LogWarning("Avatar upload failed for user {UserId}: {Error}", model.Id, uploadResult.ErrorMessage);
                        return new DataResponse<User> 
                        { 
                            Success = false, 
                            Message = uploadResult.ErrorMessage ?? "Failed to upload avatar." 
                        };
                    }

                    // Delete old avatar if exists
                    if (!string.IsNullOrEmpty(user.Avatar))
                    {
                        // Extract filename from path if it contains path separators
                        var oldAvatarPath = user.Avatar.Contains('/') 
                            ? user.Avatar.Replace("uploads/", "").Replace("\\", "/") 
                            : Path.Combine("avatars", user.Avatar).Replace("\\", "/");
                        await _fileUploadService.DeleteFileAsync(oldAvatarPath);
                        _logger.LogInformation("Deleted old avatar for user {UserId}: {OldAvatar}", model.Id, user.Avatar);
                    }

                    // Update user avatar with full path: uploads/avatars/filename.jpg
                    user.Avatar = $"uploads/{uploadResult.FilePath}";
                    _logger.LogInformation("Avatar uploaded successfully for user {UserId}: {Avatar}", model.Id, user.Avatar);
                }

                // Decrement free profile updates count
                user.FreeProfileUpdatesLeft = (user.FreeProfileUpdatesLeft ?? 0) - 1;
                _logger.LogInformation("Decremented FreeProfileUpdatesLeft for user {UserId}. Remaining: {Count}", model.Id, user.FreeProfileUpdatesLeft);

                user.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // Generate and save profile embedding
                _logger.LogInformation("Generating profile embedding for user {UserId}", model.Id);
                var embeddingResult = await _embeddingService.SaveProfileEmbeddingAsync(user);
                if (!embeddingResult.Success)
                {
                    _logger.LogWarning("Failed to generate profile embedding for user {UserId}: {Message}", 
                        model.Id, embeddingResult.Message);
                    return new DataResponse<User> { Success = true, Data = user, Message= "Failed to generate profile embedding" };

                    // Continue even if embedding fails - không block việc cập nhật profile
                }
                else
                {
                    _logger.LogInformation("Profile embedding generated successfully for user {UserId}", model.Id);
                }

                _logger.LogInformation("Profile updated successfully for user {UserId}", model.Id);
                
                // Normalize avatar path before returning
                //user.Avatar = _urlHelperService.GetFullUrl(user.Avatar);
                
                return new DataResponse<User> { Success = true, Data = user };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", model.Id);
                return new DataResponse<User> { Success = false, Message = "Failed to update profile.", ErrorDetails = ex.Message };
            }
        }

        /// <summary>
        /// Normalize avatar path để đảm bảo có format phù hợp khi lưu vào DB
        /// - Nếu là URL từ bên ngoài (Google, Facebook, etc.): giữ nguyên full URL
        /// - Nếu là path local: normalize thành uploads/avatars/filename.jpg
        /// </summary>
        /// <param name="avatar">Avatar path có thể là: full URL (Google/Facebook), uploads/avatars/..., avatars/..., hoặc chỉ filename</param>
        /// <returns>
        /// - Full URL nếu là URL từ bên ngoài (Google, Facebook, etc.)
        /// - Relative path uploads/avatars/filename.jpg nếu là file local
        /// </returns>
        private string? NormalizeAvatarPathForDb(string? avatar)
        {
            if (string.IsNullOrEmpty(avatar))
            {
                return avatar;
            }

            // Nếu đã có uploads/avatars/ thì giữ nguyên (bỏ dấu / ở đầu nếu có)
            if (avatar.StartsWith("uploads/avatars/"))
            {
                return avatar;
            }
            if (avatar.StartsWith("/uploads/avatars/"))
            {
                return avatar.TrimStart('/');
            }

            // Nếu chỉ có avatars/ thì thêm uploads/
            if (avatar.StartsWith("avatars/"))
            {
                return $"uploads/{avatar}";
            }
            if (avatar.StartsWith("/avatars/"))
            {
                return $"uploads/{avatar.TrimStart('/')}";
            }

            // Nếu đã là full URL từ bên ngoài (Google, Facebook, etc.) - giữ nguyên URL
            if (avatar.StartsWith("http://") || avatar.StartsWith("https://"))
            {
                try
                {
                    var uri = new Uri(avatar);
                    var host = uri.Host.ToLower();
                    
                    // Kiểm tra xem URL có phải từ server của chúng ta không
                    // Nếu là từ server của chúng ta (localhost, domain của app), extract relative path
                    var path = uri.AbsolutePath.TrimStart('/');
                    if (path.StartsWith("uploads/avatars/"))
                    {
                        // URL từ server của chúng ta, extract relative path
                        return path;
                    }
                    
                    // URL từ bên ngoài (Google, Facebook, etc.) - giữ nguyên URL
                    // Vì ảnh không nằm trong server của chúng ta, cần lưu full URL để hiển thị sau này
                    return avatar;
                }
                catch
                {
                    // Nếu không parse được URI, giữ nguyên URL (có thể là URL hợp lệ nhưng parse lỗi)
                    return avatar;
                }
            }

            // Nếu chỉ là filename (không có / và không phải URL) thì thêm uploads/avatars/
            if (!avatar.Contains("/"))
            {
                return $"uploads/avatars/{avatar}";
            }

            // Trường hợp khác, giữ nguyên (có thể là path khác)
            return avatar;
        }
    }
}
