using Microsoft.AspNetCore.Identity;
using WebFindLove.Models.Repositories.PasswordResetTokenRepo;
using WebFindLove.Models.Repositories.UserRepo;
using WebFindLove.Models.Entities;
using WebFindLove.Helper.HelperServices;

namespace WebFindLove.Models.Services.PasswordResetService
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IPasswordResetTokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PasswordResetService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;

        public PasswordResetService(
            IPasswordResetTokenRepository tokenRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            IWebHostEnvironment environment,
            ILogger<PasswordResetService> logger)
        {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _environment = environment;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<DataResponse<string>> GenerateResetTokenAsync(string email)
        {
            try
            {
                // Kiểm tra email có tồn tại trong hệ thống không
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Reset password attempt for non-existent email: {Email}", email);
                    // Không nên tiết lộ email có tồn tại hay không vì lý do bảo mật
                    return new DataResponse<string>
                    {
                        Success = true,
                        Message = "Nếu email tồn tại trong hệ thống, mã xác nhận đã được gửi.",
                        Data = null
                    };
                }

                // Vô hiệu hóa tất cả token cũ của email này
                await _tokenRepository.InvalidateTokensByEmailAsync(email);

                // Tạo token mới (6 chữ số ngẫu nhiên)
                var random = new Random();
                var tokenCode = random.Next(100000, 999999).ToString();

                // Lưu token vào database
                var resetToken = new PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    Token = tokenCode,
                    ExpiredAt = DateTime.UtcNow.AddMinutes(15), // Token có hiệu lực 15 phút
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _tokenRepository.AddAsync(resetToken);

                // Gửi email
                var emailBody = await GenerateResetEmailBodyAsync(user.FullName ?? email, tokenCode);
                await _emailService.SendEmailAsync(email, "Mã xác nhận đổi mật khẩu - WebFindLove", emailBody, isHtml: true);

                _logger.LogInformation("Password reset token generated and sent to: {Email}", email);

                return new DataResponse<string>
                {
                    Success = true,
                    Message = "Mã xác nhận đã được gửi đến email của bạn.",
                    Data = tokenCode // Chỉ để test, production nên trả null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating reset token for email: {Email}", email);
                return new DataResponse<string>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo mã xác nhận. Vui lòng thử lại.",
                    Data = null
                };
            }
        }

        public async Task<DataResponse<bool>> ValidateResetTokenAsync(string token)
        {
            try
            {
                var resetToken = await _tokenRepository.GetByTokenAsync(token);
                
                if (resetToken == null)
                {
                    return new DataResponse<bool>
                    {
                        Success = false,
                        Message = "Mã xác nhận không hợp lệ hoặc đã hết hạn.",
                        Data = false
                    };
                }

                return new DataResponse<bool>
                {
                    Success = true,
                    Message = "Mã xác nhận hợp lệ.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset token");
                return new DataResponse<bool>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xác thực mã.",
                    Data = false
                };
            }
        }

        public async Task<DataResponse<bool>> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                // Lấy token
                var resetToken = await _tokenRepository.GetByTokenAsync(token);
                if (resetToken == null)
                {
                    return new DataResponse<bool>
                    {
                        Success = false,
                        Message = "Mã xác nhận không hợp lệ hoặc đã hết hạn.",
                        Data = false
                    };
                }

                // Lấy user
                var user = await _userRepository.GetByEmailAsync(resetToken.Email);
                if (user == null)
                {
                    return new DataResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy người dùng.",
                        Data = false
                    };
                }

                // Lấy user với tracking để cập nhật
                var userFromDb = await _userRepository.FindSingleAsync(
                    u => u.Id == user.Id, 
                    asTracking: true);
                
                if (userFromDb == null)
                {
                    return new DataResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy người dùng.",
                        Data = false
                    };
                }

                // Hash password mới
                userFromDb.PasswordHash = _passwordHasher.HashPassword(userFromDb, newPassword);

                // Đánh dấu token đã sử dụng
                resetToken.IsUsed = true;
                await _tokenRepository.UpdateAsync(resetToken);

                _logger.LogInformation("Password reset successful for email: {Email}", resetToken.Email);

                return new DataResponse<bool>
                {
                    Success = true,
                    Message = "Mật khẩu đã được đặt lại thành công.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password with token");
                return new DataResponse<bool>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi đặt lại mật khẩu.",
                    Data = false
                };
            }
        }

        public async Task CleanupExpiredTokensAsync()
        {
            try
            {
                await _tokenRepository.DeleteExpiredTokensAsync();
                _logger.LogInformation("Expired password reset tokens cleaned up");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
            }
        }

        private async Task<string> GenerateResetEmailBodyAsync(string fullName, string token)
        {
            // Đọc template từ file
            var templatePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, 
                "Core", "Email", "Templete", "PasswordResetTemplate.html");

            if (!File.Exists(templatePath))
            {
                // Fallback nếu không tìm thấy template
                return $@"
                    <html>
                    <body>
                        <h2>Xin chào {fullName},</h2>
                        <p>Mã xác nhận đổi mật khẩu của bạn là: <strong>{token}</strong></p>
                        <p>Mã này có hiệu lực trong 15 phút.</p>
                        <p>Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.</p>
                        <br/>
                        <p>Trân trọng,<br/>WebFindLove Team</p>
                    </body>
                    </html>
                ";
            }

            var template = await File.ReadAllTextAsync(templatePath);
            
            // Thay thế placeholders
            template = template.Replace("{{FullName}}", fullName)
                              .Replace("{{Token}}", token)
                              .Replace("{{Year}}", DateTime.Now.Year.ToString());

            return template;
        }
    }
}

