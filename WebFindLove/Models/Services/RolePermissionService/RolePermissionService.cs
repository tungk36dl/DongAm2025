using System;
using WebFindLove.Models.Entities;
using WebFindLove.Models.Repositories.RolePermissionRepo;
using WebFindLove.Models.Repositories.RoleRepo;
using WebFindLove.Models.Repositories.UserRepo;
using WebFindLove.Models.Services.RolePermissionService.ViewModels;

namespace WebFindLove.Models.Services.RolePermissionService
{
    public class RolePermissionService : IRolePermissionService
    {
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<RolePermissionService> _logger;

        public RolePermissionService(
            IRolePermissionRepository rolePermissionRepository,
            IRoleRepository roleRepository,
            IUserRepository userRepository,
            ILogger<RolePermissionService> logger)
        {
            _rolePermissionRepository = rolePermissionRepository;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<DataResponse<ManageRolePermissionsVM>> GetManagePermissionsViewModelAsync(Guid roleId)
        {
            try
            {
                // Get role
                var role = await _roleRepository.FindByIdAsync(roleId);

                if (role == null)
                {
                    return new DataResponse<ManageRolePermissionsVM> 
                    { 
                        Success = false, 
                        Message = "Không tìm thấy vai trò" 
                    };
                }

                // Get all permissions grouped by module
                var allPermissionsGrouped = await _rolePermissionRepository.GetAllPermissionsGroupedByModuleAsync();

                // Get assigned permissions for this role
                var assignedPermissions = await _rolePermissionRepository.GetPermissionsByRoleIdAsync(roleId);
                var assignedPermissionIds = assignedPermissions.Select(p => p.Id).ToHashSet();

                // Build view model
                var viewModel = new ManageRolePermissionsVM
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    RoleDescription = role.Description,
                    Modules = allPermissionsGrouped.Select(kvp => new ModulePermissionsVM
                    {
                        ModuleName = kvp.Key,
                        Permissions = kvp.Value.Select(p => new PermissionItemVM
                        {
                            Id = p.Id,
                            Action = p.Action,
                            Name = p.Name,
                            Description = p.Description,
                            IsAssigned = assignedPermissionIds.Contains(p.Id)
                        }).ToList()
                    }).ToList()
                };

                // Check if all permissions in each module are selected
                foreach (var module in viewModel.Modules)
                {
                    module.AllSelected = module.Permissions.All(p => p.IsAssigned);
                }

                return new DataResponse<ManageRolePermissionsVM> 
                { 
                    Success = true, 
                    Data = viewModel 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting manage permissions view model for role {RoleId}", roleId);
                return new DataResponse<ManageRolePermissionsVM> 
                { 
                    Success = false, 
                    Message = "Có lỗi xảy ra khi tải dữ liệu phân quyền", 
                    ErrorDetails = ex.Message 
                };
            }
        }

        public async Task<DataResponse<bool>> UpdateRolePermissionsAsync(UpdateRolePermissionsRequest request)
        {
            try
            {
                // Validate role exists
                var role = await _roleRepository.FindByIdAsync(request.RoleId);

                if (role == null)
                {
                    return new DataResponse<bool> 
                    { 
                        Success = false, 
                        Message = "Không tìm thấy vai trò" 
                    };
                }

                // Validate all permission IDs exist
                var allPermissions = await _rolePermissionRepository.GetAllPermissionsAsync();
                var validPermissionIds = allPermissions.Select(p => p.Id).ToHashSet();
                
                var invalidPermissionIds = request.PermissionIds
                    .Where(id => !validPermissionIds.Contains(id))
                    .ToList();

                if (invalidPermissionIds.Any())
                {
                    _logger.LogWarning("Invalid permission IDs: {Ids}", string.Join(", ", invalidPermissionIds));
                    return new DataResponse<bool> 
                    { 
                        Success = false, 
                        Message = "Có quyền không hợp lệ trong danh sách" 
                    };
                }

                // Sync permissions
                var success = await _rolePermissionRepository.SyncRolePermissionsAsync(
                    request.RoleId, 
                    request.PermissionIds);

                if (success)
                {
                    _logger.LogInformation(
                        "Updated permissions for role {RoleId}. Total: {Count}", 
                        request.RoleId, 
                        request.PermissionIds.Count);
                    
                    return new DataResponse<bool> 
                    { 
                        Success = true, 
                        Data = true,
                        Message = $"Đã cập nhật {request.PermissionIds.Count} quyền cho vai trò {role.Name}" 
                    };
                }

                return new DataResponse<bool> 
                { 
                    Success = false, 
                    Message = "Không thể cập nhật quyền" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for role {RoleId}", request.RoleId);
                return new DataResponse<bool> 
                { 
                    Success = false, 
                    Message = "Có lỗi xảy ra khi cập nhật quyền", 
                    ErrorDetails = ex.Message 
                };
            }
        }

        public async Task<DataResponse<List<string>>> GetUserPermissionsAsync(Guid userId)
        {
            try
            {
                // Get user with role
                var user = await _userRepository.FindByIdAsync(userId, u => u.Role);

                if (user == null || user.RoleId == null)
                {
                    return new DataResponse<List<string>> 
                    { 
                        Success = true, 
                        Data = new List<string>() 
                    };
                }

                // Admin role has all permissions, no need to query
                var roleName = user.Role?.Name ?? user.RoleName;
                if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("User {UserId} is Admin, skipping permission query", userId);
                    return new DataResponse<List<string>> 
                    { 
                        Success = true, 
                        Data = new List<string>() // Admin permissions are handled by PermissionAuthorizeAttribute
                    };
                }

                // Get permissions for user's role
                var permissions = await _rolePermissionRepository.GetPermissionNamesByRoleIdAsync(user.RoleId.Value);

                return new DataResponse<List<string>> 
                { 
                    Success = true, 
                    Data = permissions 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                return new DataResponse<List<string>> 
                { 
                    Success = false, 
                    Message = "Có lỗi xảy ra khi lấy quyền người dùng", 
                    ErrorDetails = ex.Message 
                };
            }
        }

        public async Task<DataResponse<bool>> CheckUserPermissionAsync(Guid userId, string permissionName)
        {
            try
            {
                // Get user with role
                var user = await _userRepository.FindByIdAsync(userId, u => u.Role);

                if (user == null || user.RoleId == null)
                {
                    return new DataResponse<bool> 
                    { 
                        Success = true, 
                        Data = false 
                    };
                }

                // Check if role has permission
                var hasPermission = await _rolePermissionRepository.RoleHasPermissionAsync(
                    user.RoleId.Value, 
                    permissionName);

                return new DataResponse<bool> 
                { 
                    Success = true, 
                    Data = hasPermission 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permissionName, userId);
                return new DataResponse<bool> 
                { 
                    Success = false, 
                    Message = "Có lỗi xảy ra khi kiểm tra quyền", 
                    ErrorDetails = ex.Message 
                };
            }
        }
    }
}

