namespace WebFindLove.Models.Services.RolePermissionService.ViewModels
{
    public class ManageRolePermissionsVM
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public List<ModulePermissionsVM> Modules { get; set; } = new();
    }

    public class ModulePermissionsVM
    {
        public string ModuleName { get; set; } = string.Empty;
        public List<PermissionItemVM> Permissions { get; set; } = new();
        public bool AllSelected { get; set; }
    }

    public class PermissionItemVM
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class UpdateRolePermissionsRequest
    {
        public Guid RoleId { get; set; }
        public List<Guid> PermissionIds { get; set; } = new();
    }
}

