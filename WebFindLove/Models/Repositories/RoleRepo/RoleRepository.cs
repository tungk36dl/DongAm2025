using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.RoleRepo
{
    /// <summary>
    /// Repository implementation cho Role entity
    /// Kế thừa từ GenericRepository và implement IRoleRepository
    /// </summary>
    public class RoleRepository : GenericRepository<Role, Guid>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context) 
        { 
        }

        /// <summary>
        /// Get role by name
        /// </summary>
        public async Task<Role?> GetByNameAsync(string name)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == name);
        }

        /// <summary>
        /// Get role with users included for relationship checking
        /// </summary>
        public async Task<Role?> GetWithUsersAsync(Guid id)
        {
            return await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}

