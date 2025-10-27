using WebFindLove.Models.Entities;
using WebFindLove.Models.Services.RolePermissionService.ViewModels;

namespace WebFindLove.Models.Services.RolePermissionService
{
    public interface IRolePermissionService
    {
        Task<DataResponse<ManageRolePermissionsVM>> GetManagePermissionsViewModelAsync(Guid roleId);
        Task<DataResponse<bool>> UpdateRolePermissionsAsync(UpdateRolePermissionsRequest request);
        Task<DataResponse<List<string>>> GetUserPermissionsAsync(Guid userId);
        Task<DataResponse<bool>> CheckUserPermissionAsync(Guid userId, string permissionName);
    }
}

