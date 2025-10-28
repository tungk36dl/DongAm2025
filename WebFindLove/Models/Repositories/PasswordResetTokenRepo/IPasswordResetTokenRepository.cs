using WebFindLove.Models.Entities;

namespace WebFindLove.Models.Repositories.PasswordResetTokenRepo
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetToken?> GetByTokenAsync(string token);
        Task<PasswordResetToken?> GetByEmailAsync(string email);
        Task<List<PasswordResetToken>> GetValidTokensByEmailAsync(string email);
        Task AddAsync(PasswordResetToken token);
        Task UpdateAsync(PasswordResetToken token);
        Task DeleteExpiredTokensAsync();
        Task InvalidateTokensByEmailAsync(string email);
    }
}

