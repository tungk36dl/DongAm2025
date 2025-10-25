using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.PersonalityTraitRepo
{
    /// <summary>
    /// Repository implementation cho PersonalityTrait entity
    /// </summary>
    public class PersonalityTraitRepository : GenericRepository<PersonalityTrait, Guid>, IPersonalityTraitRepository
    {
        public PersonalityTraitRepository(AppDbContext context) : base(context) { }

        public async Task<PersonalityTrait?> GetByUserIdAsync(Guid userId)
        {
            return await _context.PersonalityTraits
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<List<PersonalityTrait>> GetByMbtiTypeAsync(string mbtiType)
        {
            return await _context.PersonalityTraits
                .Include(p => p.User)
                .Where(p => p.MbtiType == mbtiType)
                .ToListAsync();
        }
    }
}

