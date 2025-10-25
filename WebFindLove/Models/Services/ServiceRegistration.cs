using WebFindLove.Models.Services.UserService;
using WebFindLove.Models.Services.RoleService;

namespace WebFindLove.Models.Services
{
    /// <summary>
    /// Extension methods để đăng ký tất cả services vào DI Container
    /// Pattern: Controller → Service → Repository → UnitOfWork → DbContext
    /// Tuân theo Clean Architecture / Layered Architecture
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // User Service
            services.AddScoped<IUserService, UserService.UserService>();
            
            // Role Service  
            services.AddScoped<IRoleService, RoleService.RoleService>();

            // Thêm services khác ở đây khi cần
            // services.AddScoped<IXxxService, XxxService>();

            return services;
        }
    }
}
