using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.PhotoRepo
{
    /// <summary>
    /// Repository implementation cho Photo entity
    /// </summary>
    public class PhotoRepository : GenericRepository<Photo, Guid>, IPhotoRepository
    {
        public PhotoRepository(AppDbContext context) : base(context) { }

        public async Task<List<Photo>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Photos
                .Where(p => p.UserId == userId && p.IsActive)
                .OrderByDescending(p => p.IsPrimary)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Photo?> GetPrimaryPhotoAsync(Guid userId)
        {
            return await _context.Photos
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsPrimary && p.IsActive);
        }

        public async Task<bool> HasPrimaryPhotoAsync(Guid userId)
        {
            return await _context.Photos
                .AnyAsync(p => p.UserId == userId && p.IsPrimary && p.IsActive);
        }
    }
}

