using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.PhotoRepo
{
    /// <summary>
    /// Interface cho Photo Repository
    /// </summary>
    public interface IPhotoRepository : IGenericRepository<Photo, Guid>
    {
        /// <summary>
        /// Get all photos by user ID
        /// </summary>
        Task<List<Photo>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Get primary photo of user
        /// </summary>
        Task<Photo?> GetPrimaryPhotoAsync(Guid userId);

        /// <summary>
        /// Check if user has primary photo
        /// </summary>
        Task<bool> HasPrimaryPhotoAsync(Guid userId);
    }
}

