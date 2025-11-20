using WebFindLove.Models;
using WebFindLove.Models.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace WebFindLove.HelperServices
{
    public class DataSeedService : IDataSeedService
    {
        private readonly AppDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        public DataSeedService(AppDbContext context, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _passwordHasher = new PasswordHasher<User>();
            _configuration = configuration;
        }

        public async Task SeedDefaultAdminUserAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Đọc thông tin admin từ Configuration (từ .env hoặc appsettings.json)
                var adminUserName = _configuration["DefaultAdmin:UserName"];
                var adminEmail = _configuration["DefaultAdmin:Email"];
                var adminPassword = _configuration["DefaultAdmin:Password"];
                var adminFullName = _configuration["DefaultAdmin:FullName"] ?? "System Administrator";
                var adminRoleName = _configuration["DefaultAdmin:RoleName"] ?? "Admin";

                // Kiểm tra các giá trị bắt buộc
                if (string.IsNullOrWhiteSpace(adminUserName))
                {
                    Log.Warning("DefaultAdmin:UserName không được cấu hình trong .env hoặc appsettings.json, bỏ qua seed admin user");
                    return;
                }

                if (string.IsNullOrWhiteSpace(adminEmail))
                {
                    Log.Warning("DefaultAdmin:Email không được cấu hình trong .env hoặc appsettings.json, bỏ qua seed admin user");
                    return;
                }

                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    Log.Warning("DefaultAdmin:Password không được cấu hình trong .env hoặc appsettings.json, bỏ qua seed admin user");
                    return;
                }

                // Use AnyAsync instead of FirstOrDefaultAsync for existence check (much faster)
                var adminExists = await _context.Users
                    .AsNoTracking() // Don't track entity since we only check existence
                    .AnyAsync(u => u.UserName == adminUserName, cancellationToken);

                if (adminExists)
                {
                    Log.Debug("Admin user '{UserName}' already exists, skipping seed", adminUserName);
                    return;
                }

                // Get admin role (only if needed)
                var adminRole = await _context.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Name == adminRoleName, cancellationToken);

                if (adminRole == null)
                {
                    Log.Warning("Admin role '{RoleName}' not found, cannot seed admin user", adminRoleName);
                    return;
                }

                // Create admin user
                var adminUser = new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    UserName = adminUserName,
                    Email = adminEmail,
                    FullName = adminFullName,
                    IsActive = true,
                    RoleId = adminRole.Id,
                    RoleName = adminRoleName,
                    CreatedAt = DateTime.UtcNow
                };

                // Hash password
                adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, adminPassword);

                // Add to database
                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync(cancellationToken);
                
                stopwatch.Stop();
                Log.Information("Admin user seeded successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Admin user seeding was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, "Error seeding admin user after {ElapsedMs}ms: {Message}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
