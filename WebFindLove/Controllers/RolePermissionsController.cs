using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models.Services.RolePermissionService;
using WebFindLove.Models.Services.RolePermissionService.ViewModels;

namespace WebFindLove.Controllers
{
    public class RolePermissionsController : BaseController
    {
        private readonly IRolePermissionService _rolePermissionService;

        public RolePermissionsController(
            IRolePermissionService rolePermissionService,
            ILogger<RolePermissionsController> logger)
        {
            _rolePermissionService = rolePermissionService;
            Logger = logger;
        }

        // GET: RolePermissions/Manage/{roleId}
        [HttpGet]
        public async Task<IActionResult> Manage(Guid roleId)
        {
            Logger.LogInformation("Managing permissions for role {RoleId}", roleId);

            var response = await _rolePermissionService.GetManagePermissionsViewModelAsync(roleId);
            
            if (!response.Success)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction("Index", "Roles");
            }

            return View(response.Data);
        }

        // POST: RolePermissions/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] Guid roleId, [FromForm] List<Guid> permissionIds)
        {
            Logger.LogInformation(
                "Updating permissions for role {RoleId}. Count: {Count}", 
                roleId, 
                permissionIds?.Count ?? 0);

            var request = new UpdateRolePermissionsRequest
            {
                RoleId = roleId,
                PermissionIds = permissionIds ?? new List<Guid>()
            };

            var response = await _rolePermissionService.UpdateRolePermissionsAsync(request);

            if (response.Success)
            {
                TempData["Success"] = response.Message;
            }
            else
            {
                TempData["Error"] = response.Message;
            }

            return RedirectToAction("Manage", new { roleId });
        }

        // API endpoint for AJAX updates
        [HttpPost]
        public async Task<IActionResult> UpdateApi([FromBody] UpdateRolePermissionsRequest request)
        {
            Logger.LogInformation(
                "API: Updating permissions for role {RoleId}. Count: {Count}", 
                request.RoleId, 
                request.PermissionIds?.Count ?? 0);

            var response = await _rolePermissionService.UpdateRolePermissionsAsync(request);

            return Json(new
            {
                success = response.Success,
                message = response.Message,
                errorDetails = response.ErrorDetails
            });
        }
    }
}

