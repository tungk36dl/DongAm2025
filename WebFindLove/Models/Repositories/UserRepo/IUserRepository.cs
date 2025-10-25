using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.UserRepo
{
    public interface IUserRepository : IGenericRepository<User, Guid>
    {
        Task<User?> GetByEmailAsync(string email);
    }
}
