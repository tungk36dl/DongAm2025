using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.Entities;

namespace WebFindLove.Models.Repositories.RolePermissionRepo
{
    public class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RolePermissionRepository> _logger;

        public RolePermissionRepository(AppDbContext context, ILogger<RolePermissionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Permission>> GetPermissionsByRoleIdAsync(Guid roleId)
        {
            try
            {
                return await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Include(rp => rp.Permission)
                    .Select(rp => rp.Permission)
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Action)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for role {RoleId}", roleId);
                return new List<Permission>();
            }
        }

        public async Task<Dictionary<string, List<Permission>>> GetAllPermissionsGroupedByModuleAsync()
        {
            try
            {
                var permissions = await _context.Permissions
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Action)
                    .ToListAsync();

                return permissions
                    .GroupBy(p => p.Module)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions grouped by module");
                return new Dictionary<string, List<Permission>>();
            }
        }

        public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
        {
            try
            {
                var exists = await _context.RolePermissions
                    .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (exists)
                {
                    _logger.LogWarning("Permission {PermissionId} already assigned to role {RoleId}", permissionId, roleId);
                    return true;
                }

                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                };

                _context.RolePermissions.Add(rolePermission);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Assigned permission {PermissionId} to role {RoleId}", permissionId, roleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);
                return false;
            }
        }

        public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
        {
            try
            {
                var rolePermission = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (rolePermission == null)
                {
                    _logger.LogWarning("Permission {PermissionId} not found for role {RoleId}", permissionId, roleId);
                    return false;
                }

                _context.RolePermissions.Remove(rolePermission);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Removed permission {PermissionId} from role {RoleId}", permissionId, roleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}", permissionId, roleId);
                return false;
            }
        }

        public async Task<bool> AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds)
        {
            try
            {
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                var newPermissions = permissionIds
                    .Except(existingPermissions)
                    .Select(pid => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = pid
                    })
                    .ToList();

                if (newPermissions.Any())
                {
                    _context.RolePermissions.AddRange(newPermissions);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Assigned {Count} permissions to role {RoleId}", newPermissions.Count, roleId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permissions to role {RoleId}", roleId);
                return false;
            }
        }

        public async Task<bool> RemoveAllPermissionsFromRoleAsync(Guid roleId)
        {
            try
            {
                var rolePermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(rolePermissions);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Removed all permissions from role {RoleId}", roleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing all permissions from role {RoleId}", roleId);
                return false;
            }
        }

        public async Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName)
        {
            try
            {
                return await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .AnyAsync(rp => rp.Permission.Name == permissionName && rp.Permission.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {PermissionName} for role {RoleId}", permissionName, roleId);
                return false;
            }
        }

        public async Task<List<string>> GetPermissionNamesByRoleIdAsync(Guid roleId)
        {
            try
            {
                return await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.Permission.IsActive)
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission names for role {RoleId}", roleId);
                return new List<string>();
            }
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            try
            {
                return await _context.Permissions
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Action)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all permissions");
                return new List<Permission>();
            }
        }

        public async Task<bool> SyncRolePermissionsAsync(Guid roleId, List<Guid> permissionIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove all existing permissions
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(existingPermissions);

                // Add new permissions
                var newPermissions = permissionIds
                    .Select(pid => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = pid
                    })
                    .ToList();

                _context.RolePermissions.AddRange(newPermissions);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Synced {Count} permissions for role {RoleId}", permissionIds.Count, roleId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error syncing permissions for role {RoleId}", roleId);
                return false;
            }
        }
    }
}

