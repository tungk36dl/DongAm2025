using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task<User?> FindByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => 
                    (u.UserName != null && u.UserName.ToLower() == usernameOrEmail.ToLower()) ||
                    (u.Email != null && u.Email.ToLower() == usernameOrEmail.ToLower()));
        }
    }
}
