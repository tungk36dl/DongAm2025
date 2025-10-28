using WebFindLove.Models.Services;

namespace WebFindLove.Models.Services.PasswordResetService
{
    public interface IPasswordResetService
    {
        Task<DataResponse<string>> GenerateResetTokenAsync(string email);
        Task<DataResponse<bool>> ValidateResetTokenAsync(string token);
        Task<DataResponse<bool>> ResetPasswordAsync(string token, string newPassword);
        Task CleanupExpiredTokensAsync();
    }
}

