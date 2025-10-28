using WebFindLove.Models.Services.UserService;
using WebFindLove.Models.Services.RoleService;
using WebFindLove.Models.Services.RolePermissionService;
using WebFindLove.Models.Services.UserPreferenceService;
using WebFindLove.Models.Services.MatchResultService;
using WebFindLove.Models.Services.MessageService;
using WebFindLove.Models.Services.ConversationService;
using WebFindLove.Models.Services.FileUploadService;
using WebFindLove.Models.Services.EmbeddingService;
using WebFindLove.Models.Services.MatchingService;
using WebFindLove.Models.Services.NotificationService;

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
            services.AddScoped<IRolePermissionService, RolePermissionService.RolePermissionService>();

            // Profile & Preference services
            services.AddScoped<IUserPreferenceService, UserPreferenceService.UserPreferenceService>();

            // Matching & Communication services
            services.AddScoped<IMatchResultService, MatchResultService.MatchResultService>();
            services.AddScoped<IMatchingService, MatchingService.MatchingService>();
            services.AddScoped<IMessageService, MessageService.MessageService>();
            services.AddScoped<IConversationService, ConversationService.ConversationService>();
            services.AddScoped<INotificationService, NotificationService.NotificationService>();

            // Utility services
            services.AddScoped<IFileUploadService, FileUploadService.FileUploadService>();
            services.AddScoped<IEmbeddingService, EmbeddingService.EmbeddingService>();

            return services;
        }
    }
}
