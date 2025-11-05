using WebFindLove.Models;
using WebFindLove.Models.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace WebFindLove.HelperServices
{
    public class DataSeedService : IDataSeedService
    {
        private readonly AppDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasher<User> _passwordHasher;

        public DataSeedService(AppDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task SeedDefaultAdminUserAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Use AnyAsync instead of FirstOrDefaultAsync for existence check (much faster)
                var adminExists = await _context.Users
                    .AsNoTracking() // Don't track entity since we only check existence
                    .AnyAsync(u => u.UserName == "admin", cancellationToken);

                if (adminExists)
                {
                    Log.Debug("Admin user already exists, skipping seed");
                    return;
                }

                // Get admin role (only if needed)
                var adminRole = await _context.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);

                if (adminRole == null)
                {
                    Log.Warning("Admin role not found, cannot seed admin user");
                    return;
                }

                // Create admin user
                var adminUser = new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    UserName = "admin",
                    Email = "admin@bacha.com",
                    FullName = "System Administrator",
                    IsActive = true,
                    RoleId = adminRole.Id,
                    RoleName = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                // Hash password
                adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, "123");

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
