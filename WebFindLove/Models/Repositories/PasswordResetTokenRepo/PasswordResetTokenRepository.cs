using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.Entities;

namespace WebFindLove.Models.Repositories.PasswordResetTokenRepo
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AppDbContext _context;

        public PasswordResetTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetToken?> GetByTokenAsync(string token)
        {
            return await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiredAt > DateTime.UtcNow);
        }

        public async Task<PasswordResetToken?> GetByEmailAsync(string email)
        {
            return await _context.PasswordResetTokens
                .Where(t => t.Email == email && !t.IsUsed && t.ExpiredAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PasswordResetToken>> GetValidTokensByEmailAsync(string email)
        {
            return await _context.PasswordResetTokens
                .Where(t => t.Email == email && !t.IsUsed && t.ExpiredAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task AddAsync(PasswordResetToken token)
        {
            await _context.PasswordResetTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PasswordResetToken token)
        {
            _context.PasswordResetTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteExpiredTokensAsync()
        {
            var expiredTokens = await _context.PasswordResetTokens
                .Where(t => t.ExpiredAt <= DateTime.UtcNow || t.IsUsed)
                .ToListAsync();

            _context.PasswordResetTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }

        public async Task InvalidateTokensByEmailAsync(string email)
        {
            var tokens = await _context.PasswordResetTokens
                .Where(t => t.Email == email && !t.IsUsed)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsUsed = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}

