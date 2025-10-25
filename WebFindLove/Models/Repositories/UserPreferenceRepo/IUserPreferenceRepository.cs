using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.UserPreferenceRepo
{
    /// <summary>
    /// Interface cho UserPreference Repository
    /// </summary>
    public interface IUserPreferenceRepository : IGenericRepository<UserPreference, Guid>
    {
        /// <summary>
        /// Get preference by user ID
        /// </summary>
        Task<UserPreference?> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Check if user has preference
        /// </summary>
        Task<bool> ExistsForUserAsync(Guid userId);
    }
}

