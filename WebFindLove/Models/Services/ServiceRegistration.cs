using WebFindLove.Models.Services.UserService;
using WebFindLove.Models.Services.RoleService;
using WebFindLove.Models.Services.UserPreferenceService;
using WebFindLove.Models.Services.PersonalityTraitService;
using WebFindLove.Models.Services.MatchResultService;
using WebFindLove.Models.Services.PhotoService;
using WebFindLove.Models.Services.MessageService;

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
            // Core services
            services.AddScoped<IUserService, UserService.UserService>();
            services.AddScoped<IRoleService, RoleService.RoleService>();

            // Profile & Preference services
            services.AddScoped<IUserPreferenceService, UserPreferenceService.UserPreferenceService>();
            services.AddScoped<IPersonalityTraitService, PersonalityTraitService.PersonalityTraitService>();

            // Matching & Communication services
            services.AddScoped<IMatchResultService, MatchResultService.MatchResultService>();
            services.AddScoped<IPhotoService, PhotoService.PhotoService>();
            services.AddScoped<IMessageService, MessageService.MessageService>();

            return services;
        }
    }
}
