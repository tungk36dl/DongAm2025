using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.UserRepo
{
    public class UserRepository : GenericRepository<User, Guid>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
