using WebFindLove.Models.Repositories.UserRepo;
using WebFindLove.Models.Repositories.RoleRepo;
using WebFindLove.Models.Repositories.RolePermissionRepo;
using WebFindLove.Models.Repositories.UserPreferenceRepo;
using WebFindLove.Models.Repositories.MatchResultRepo;
using WebFindLove.Models.Repositories.MessageRepo;
using WebFindLove.Models.Repositories.ConversationRepo;
using WebFindLove.Models.Repositories.ConversationParticipantRepo;
using WebFindLove.Models.Repositories.NotificationRepo;
using WebFindLove.Models.Repositories.PasswordResetTokenRepo;

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
            // Core repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();

            // Profile & Preference repositories
            services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();

            // Matching & Communication repositories
            services.AddScoped<IMatchResultRepository, MatchResultRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IConversationParticipantRepository, ConversationParticipantRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();

            // Authentication & Security repositories
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

            return services;
        }
    }
}
