using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.UserPreferenceRepo
{
    /// <summary>
    /// Repository implementation cho UserPreference entity
    /// </summary>
    public class UserPreferenceRepository : GenericRepository<UserPreference, Guid>, IUserPreferenceRepository
    {
        public UserPreferenceRepository(AppDbContext context) : base(context) { }

        public async Task<UserPreference?> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserPreferences
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<bool> ExistsForUserAsync(Guid userId)
        {
            return await _context.UserPreferences.AnyAsync(p => p.UserId == userId);
        }
    }
}

