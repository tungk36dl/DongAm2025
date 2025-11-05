using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.MatchResultRepo
{
    /// <summary>
    /// Repository implementation cho MatchResult entity
    /// </summary>
    public class MatchResultRepository : GenericRepository<MatchResult, Guid>, IMatchResultRepository
    {
        public MatchResultRepository(AppDbContext context) : base(context) { }

        public async Task<List<MatchResult>> GetMatchesByUserIdAsync(Guid userId)
        {
            return await _context.MatchResults
                .Include(m => m.User)
                .Include(m => m.MatchedUser)
                .Where(m => (m.UserId == userId))
                .OrderByDescending(m => m.MatchScore)
                .ToListAsync();
        }

        public async Task<List<MatchResult>> GetTopMatchesAsync(Guid userId, int count = 10)
        {
            return await _context.MatchResults
                .Include(m => m.User)
                .Include(m => m.MatchedUser)
                .Where(m => m.UserId == userId && m.IsActive)
                .OrderByDescending(m => m.MatchScore)
                .Take(count)
                .ToListAsync();
        }

        public async Task<MatchResult?> GetMatchBetweenUsersAsync(Guid userId1, Guid userId2)
        {
            return await _context.MatchResults
                .Include(m => m.User)
                .Include(m => m.MatchedUser)
                .FirstOrDefaultAsync(m =>
                    ((m.UserId == userId1 && m.MatchedUserId == userId2) ||
                     (m.UserId == userId2 && m.MatchedUserId == userId1)) &&
                    m.IsActive);
        }
    }
}

