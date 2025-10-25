using WebFindLove.Models.Repositories.UserRepo;
using WebFindLove.Models.Repositories.RoleRepo;

namespace WebFindLove.Models.Repositories
{
    /// <summary>
    /// Extension methods để đăng ký tất cả repositories vào DI Container
    /// Pattern: Service → Repository → DbContext (qua UnitOfWork)
    /// </summary>
    public static class RepositoryRegistration
    {
        public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
        {
            // User Repository
            services.AddScoped<IUserRepository, UserRepository>();
            
            // Role Repository
            services.AddScoped<IRoleRepository, RoleRepository>();

            // Thêm repositories khác ở đây khi cần
            // services.AddScoped<IXxxRepository, XxxRepository>();

            return services;
        }
    }
}
