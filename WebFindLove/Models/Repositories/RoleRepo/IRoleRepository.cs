using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.RoleRepo
{
    /// <summary>
    /// Interface cho Role Repository - kế thừa từ IGenericRepository
    /// Có thể mở rộng thêm các methods đặc thù cho Role
    /// </summary>
    public interface IRoleRepository : IGenericRepository<Role, Guid>
    {
        /// <summary>
        /// Get role by name
        /// </summary>
        Task<Role?> GetByNameAsync(string name);

        /// <summary>
        /// Get role with users included
        /// </summary>
        Task<Role?> GetWithUsersAsync(Guid id);
    }
}

