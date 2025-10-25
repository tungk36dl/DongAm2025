using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.PersonalityTraitRepo
{
    /// <summary>
    /// Interface cho PersonalityTrait Repository
    /// </summary>
    public interface IPersonalityTraitRepository : IGenericRepository<PersonalityTrait, Guid>
    {
        /// <summary>
        /// Get personality trait by user ID
        /// </summary>
        Task<PersonalityTrait?> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Get all by MBTI type
        /// </summary>
        Task<List<PersonalityTrait>> GetByMbtiTypeAsync(string mbtiType);
    }
}

