using WebFindLove.Models.Entities;

namespace WebFindLove.Models.Repositories.RolePermissionRepo
{
    public interface IRolePermissionRepository
    {
        // Get permissions by role
        Task<List<Permission>> GetPermissionsByRoleIdAsync(Guid roleId);
        
        // Get all permissions grouped by module
        Task<Dictionary<string, List<Permission>>> GetAllPermissionsGroupedByModuleAsync();
        
        // Assign permission to role
        Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
        
        // Remove permission from role
        Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
        
        // Assign multiple permissions to role
        Task<bool> AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds);
        
        // Remove all permissions from role
        Task<bool> RemoveAllPermissionsFromRoleAsync(Guid roleId);
        
        // Check if role has permission
        Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName);
        
        // Get permission names by role
        Task<List<string>> GetPermissionNamesByRoleIdAsync(Guid roleId);
        
        // Get all permissions
        Task<List<Permission>> GetAllPermissionsAsync();
        
        // Sync permissions for role (remove old, add new)
        Task<bool> SyncRolePermissionsAsync(Guid roleId, List<Guid> permissionIds);
    }
}

