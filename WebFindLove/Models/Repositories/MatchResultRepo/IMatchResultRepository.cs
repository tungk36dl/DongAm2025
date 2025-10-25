using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.MatchResultRepo
{
    /// <summary>
    /// Interface cho MatchResult Repository
    /// </summary>
    public interface IMatchResultRepository : IGenericRepository<MatchResult, Guid>
    {
        /// <summary>
        /// Get all matches for a user
        /// </summary>
        Task<List<MatchResult>> GetMatchesByUserIdAsync(Guid userId);

        /// <summary>
        /// Get top matches for a user by score
        /// </summary>
        Task<List<MatchResult>> GetTopMatchesAsync(Guid userId, int count = 10);

        /// <summary>
        /// Check if match exists between two users
        /// </summary>
        Task<MatchResult?> GetMatchBetweenUsersAsync(Guid userId1, Guid userId2);
    }
}

